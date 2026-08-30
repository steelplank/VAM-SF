using System;
using System.Collections.Generic;
using System.Linq;
using SColor = System.Drawing.Color;

namespace StorybrewScripts.Vam
{
    // Builds the default (v1) plan for one object. Runs as the first pipeline step, so an
    // empty modifier chain reproduces the v1 render exactly. Config is set by VAM_Generator.
    public sealed class VamPlanBuilder
    {
        // geometry + timelines
        public VamGeometry Geometry;
        public VamArProfile ArProfile;
        public VamHdProfile HdProfile;
        public VamProfile Profile;          // may be null
        public bool OverrideWithMapAr;
        public double MapApproachRate;
        public double BaseScale;

        // scroll velocity (core feature; may be null / empty = no reshape)
        public VamScrollVelocity ScrollVelocity;
        public bool EnableScrollVelocity;

        // hidden
        public bool EnableHidden;
        public bool HiddenUseGameValues;
        public double HiddenFadeStart, HiddenFadeEnd;

        // fade in (fake mania Fade In; profile 'fi' column only)
        public bool EnableFadeIn;

        // catch / miss
        public bool EnableCatchMiss;
        public double CatchTriggerWindow;
        public double MissFadeDuration;

        // appearance
        public bool RotateObjects;
        public bool UseComboColors;
        public string BananaColor;
        public bool UseSkinSprites;
        public string SpriteFolder;
        public bool DrawOverlays;
        public bool HyperDashGlow;
        public string HyperDashColor;

        const double OsuHyperGlowScale = 1.2, OsuHyperGlowAlpha = 0.7;
        const double OsuBananaEndScale = 0.6;
        const double OsuHiddenOffsetMul = 0.6, OsuHiddenDurationMul = 0.16;
        const double OsuHdFadeStartFrac = 0.40, OsuHdFadeWidthFrac = OsuHiddenDurationMul;
        const double ExitY = 540.0;

        // Fade In geometry (fraction of the fall). The reveal COMPLETES at FiEndFrac; the object is
        // invisible above it and fades in over a band of FiWidthFrac. fi=5 (normal) reveals by 0.25 -
        // comfortably above HD's 0.40 fade-out start, so fi=5 + hd=5 leaves a visible reading window.
        // Higher fi reveals lower (later): FiEndFrac += (fi-5)*FiPerLevel. Mirrors mania's ~21% default
        // top cover, tuned to sit above the default Hidden band.
        const double FiEndFracAt5 = 0.25, FiWidthFrac = OsuHiddenDurationMul, FiPerLevel = 0.02;
        const double FiMaxEndFrac = 0.90; // never hide the catch itself; always fully revealed before landing

        readonly Dictionary<VamObject, double> trigStart = new Dictionary<VamObject, double>();
        readonly Dictionary<VamObject, double> trigEnd = new Dictionary<VamObject, double>();

        // Per-object HitSound windows, clamped to half the gap to the nearest detectable neighbour.
        public void ComputeTriggerWindows(List<VamObject> objects)
        {
            trigStart.Clear(); trigEnd.Clear();
            if (!EnableCatchMiss) return;
            var det = objects.Where(o => IsDetected(o.Type)).ToList();
            for (int k = 0; k < det.Count; k++)
            {
                double t = det[k].Time;
                double prevGap = k > 0 ? t - det[k - 1].Time : double.PositiveInfinity;
                double nextGap = k < det.Count - 1 ? det[k + 1].Time - t : double.PositiveInfinity;
                double ws = t - Math.Min(CatchTriggerWindow, prevGap / 2.0);
                double we = t + Math.Min(CatchTriggerWindow, nextGap / 2.0);
                if (we <= ws) { ws = t - 0.5; we = t + 0.5; }
                trigStart[det[k]] = ws; trigEnd[det[k]] = we;
            }
        }

        public VamPlan Build(VamObject obj)
        {
            var plan = new VamPlan(obj);

            double ar = OverrideWithMapAr ? MapApproachRate
                      : (Profile != null && Profile.HasAr ? Profile.ArAt(obj.Time) : ArProfile.ArAt(obj.Time));
            double preempt = VamGeometry.Preempt(ar);
            double spawnTime = obj.Time - preempt, catchTime = obj.Time;
            double x = Geometry.MapX(obj.X);

            plan.SpawnTime = spawnTime;
            plan.CatchTime = catchTime;
            plan.Preempt = preempt;
            if (obj.Type == VamObjectType.Banana)
            {
                // Bananas scale with circle size like everything else (stable applies the same
                // SpriteDisplaySize), then spin from a random startScale down to 0.6. Final size
                // = fruit * 0.6; start size = fruit * startScale (0.6..1.2 from the rng).
                double start = double.IsNaN(obj.ScaleStart) ? OsuBananaEndScale : obj.ScaleStart;
                plan.Scale = BaseScale * OsuBananaEndScale;
                plan.ScaleStart = BaseScale * start;
            }
            else
            {
                plan.Scale = BaseScale * ScaleFactor(obj.Type);
            }
            plan.Position.Add(spawnTime, new Vec2(x, Geometry.SpawnY));
            plan.Position.Add(catchTime, new Vec2(x, Geometry.CatchY));

            plan.BodyPath = SpritePathFor(obj);
            plan.OverlayPath = DrawOverlays ? OverlayPathFor(obj) : null;
            plan.BodyColor = ColorFor(obj);

            if (HyperDashGlow && obj.HyperDash && obj.Type != VamObjectType.Banana)
            {
                plan.DrawGlow = true;
                plan.GlowColor = ParseHex(HyperDashColor);
                plan.GlowScale = OsuHyperGlowScale;
                plan.GlowAlpha = OsuHyperGlowAlpha;
            }

            bool triggerDriven = EnableCatchMiss && IsDetected(obj.Type);
            plan.TriggerDriven = triggerDriven;
            double fallVel = (Geometry.CatchY - Geometry.SpawnY) / preempt;
            plan.ExitY = ExitY;
            plan.MissExitDur = fallVel > 0 ? (ExitY - Geometry.CatchY) / fallVel : MissFadeDuration;
            plan.MissFadeDur = MissFadeDuration;
            if (triggerDriven && trigStart.ContainsKey(obj)) { plan.TrigWinStart = trigStart[obj]; plan.TrigWinEnd = trigEnd[obj]; }

            if (RotateObjects) SetRotation(plan, obj, preempt);
            if (EnableHidden) SetHidden(plan, obj, spawnTime, catchTime, preempt);
            if (EnableFadeIn) SetFadeIn(plan, obj, spawnTime, catchTime, preempt);

            // Scroll velocity is the last core transform: it reshapes the finished fall so every
            // object moves at the timeline's current multiplier, still landing on its beat. Runs on
            // both the direct and modifier paths (it's part of the base plan, not the mod pipeline),
            // so it is independent of EnableModifiers. No-op unless SV keyframes actually overlap.
            if (EnableScrollVelocity && ScrollVelocity != null && ScrollVelocity.HasKeyframes)
                ScrollVelocity.Reshape(plan);

            return plan;
        }

        // osu per-object rotation via StatelessRNG seeded by the object's time.
        void SetRotation(VamPlan p, VamObject obj, double preempt)
        {
            int seed = obj.Time;
            if (obj.Type == VamObjectType.Banana)
            {
                p.RotStart = Deg2Rad(180.0 * (VamRng.NextSingle(seed, 1) * 2.0 - 1.0));
                p.RotEnd = Deg2Rad(180.0 * (VamRng.NextSingle(seed, 2) * 2.0 - 1.0));
            }
            else if (obj.Type == VamObjectType.Droplet || obj.Type == VamObjectType.TinyDroplet)
            {
                double start = VamRng.NextSingle(seed, 1) * 20.0;
                double sweep = 720.0 * preempt / (preempt + 2000.0);
                p.RotStart = Deg2Rad(start);
                p.RotEnd = Deg2Rad(start + sweep);
            }
            else
            {
                double tilt = (VamRng.NextSingle(seed, 1) - 0.5) * 40.0;
                p.RotStart = p.RotEnd = Deg2Rad(tilt);
                p.MissRotEnd = Deg2Rad(tilt * 2.0);
            }
        }

        // Two HD sources: profile 'hd' 0..10 scale (fades fully out), or HdKeyframes intensity.
        void SetHidden(VamPlan p, VamObject obj, double spawnTime, double catchTime, double preempt)
        {
            if (Profile != null && Profile.HasHd)
            {
                double scale = Profile.HdScaleAt(catchTime);
                if (scale > 0.0)
                {
                    double sf, ef; ScaleToFractions(scale, out sf, out ef);
                    p.HdFade = true;
                    p.HdStart = spawnTime + preempt * sf;
                    p.HdEnd = spawnTime + preempt * ef;
                    p.HdRemain = 0.0;
                }
                return;
            }
            double intensity = HdProfile.IntensityAt(catchTime);
            if (intensity <= 0.0) return;
            p.HdFade = true;
            if (HiddenUseGameValues)
            {
                p.HdStart = catchTime - preempt * OsuHiddenOffsetMul;
                p.HdEnd = p.HdStart + preempt * OsuHiddenDurationMul;
            }
            else
            {
                double fs = Clamp01(HiddenFadeStart), fe = Clamp01(HiddenFadeEnd);
                if (fe <= fs) fe = Math.Min(1.0, fs + 0.05);
                p.HdStart = spawnTime + preempt * fs;
                p.HdEnd = spawnTime + preempt * fe;
            }
            p.HdRemain = 1.0 - intensity;
        }

        static void ScaleToFractions(double scale, out double startFrac, out double endFrac)
        {
            double s = scale < 0 ? 0 : (scale > 10 ? 10 : scale);
            startFrac = OsuHdFadeStartFrac + (5.0 - s) * (OsuHdFadeStartFrac / 5.0);
            if (startFrac < 0.0) startFrac = 0.0;
            endFrac = startFrac + OsuHdFadeWidthFrac;
            if (endFrac > 1.0) endFrac = 1.0;
        }

        // Fade In (profile 'fi' column). The object is invisible at spawn and fades in over
        // [FiStart, FiEnd], where FiEnd is a fraction of the fall set by the fi scale. Composes with
        // Hidden in the emitter (the two factors multiply), so the reveal and the HD fade-out carve a
        // visible band between them.
        void SetFadeIn(VamPlan p, VamObject obj, double spawnTime, double catchTime, double preempt)
        {
            if (Profile == null || !Profile.HasFi) return;
            double scale = Profile.FiScaleAt(catchTime);
            if (scale <= 0.0) return;

            double s = scale < 0 ? 0 : (scale > 10 ? 10 : scale);
            double endFrac = FiEndFracAt5 + (s - 5.0) * FiPerLevel;
            if (endFrac > FiMaxEndFrac) endFrac = FiMaxEndFrac;
            double startFrac = endFrac - FiWidthFrac;
            if (startFrac < 0.0) startFrac = 0.0;

            p.FadeIn = true;
            p.FiStart = spawnTime + preempt * startFrac;
            p.FiEnd = spawnTime + preempt * endFrac;
        }

        // Only fruits raise storyboard HitSound triggers.
        static bool IsDetected(VamObjectType t) { return t == VamObjectType.Fruit; }

        // osu! object size vs a fruit: big droplet 0.8 (LegacyDropletPiece), tiny droplet 0.4
        // (that 0.8 times DrawableTinyDroplet's ScaleFactor of 0.5). Fruit and banana stay 1.0.
        static double ScaleFactor(VamObjectType t)
        {
            switch (t)
            {
                case VamObjectType.Droplet: return 0.8;
                case VamObjectType.TinyDroplet: return 0.4;
                default: return 1.0;
            }
        }

        static readonly string[] FruitElements = { "fruit-pear", "fruit-grapes", "fruit-orange", "fruit-apple" };

        static string ElementFor(VamObject obj)
        {
            switch (obj.Type)
            {
                case VamObjectType.Banana: return "fruit-bananas";
                case VamObjectType.Droplet:
                case VamObjectType.TinyDroplet: return "fruit-drop";
                default: return FruitElements[((obj.IndexInBeatmap % 4) + 4) % 4];
            }
        }

        // Every object type has an untinted overlay (fruit, drop, bananas). osu draws the tinted
        // body then the overlay on top; some skins leave the body a 1x1 pixel and put the whole
        // sprite in the overlay, so it must render for all types or those objects vanish.
        static string OverlayElementFor(VamObject obj) => ElementFor(obj) + "-overlay";

        string PathFor(string element)
        {
            if (string.IsNullOrEmpty(element)) return null;
            if (UseSkinSprites) return element + ".png";
            var folder = SpriteFolder ?? "";
            if (folder.Length > 0 && !folder.EndsWith("/")) folder += "/";
            return folder + element + ".png";
        }

        string SpritePathFor(VamObject obj) { return PathFor(ElementFor(obj)); }
        string OverlayPathFor(VamObject obj) { return PathFor(OverlayElementFor(obj)); }

        SColor? ColorFor(VamObject obj)
        {
            if (obj.Type == VamObjectType.Banana) return ParseHex(BananaColor);
            if (UseComboColors) return SColor.FromArgb(obj.R, obj.G, obj.B);
            return null;
        }

        static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
        static double Deg2Rad(double d) { return d * Math.PI / 180.0; }

        static SColor ParseHex(string hex)
        {
            var fallback = SColor.FromArgb(255, 60, 60);
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            hex = hex.Trim().TrimStart('#');
            if (hex.Length != 6) return fallback;
            try
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return SColor.FromArgb(r, g, b);
            }
            catch { return fallback; }
        }
    }
}
