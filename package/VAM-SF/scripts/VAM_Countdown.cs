using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewScripts.Vam;
using System;

namespace StorybrewScripts
{
    /// <summary>
    /// Redraws osu!'s pre-map countdown (Ready?, 3, 2, 1, GO!) in the storyboard, because the
    /// cover hides osu!'s own countdown behind it.
    ///
    /// Timing + animation mirror osu! stable's Player.InitializeCountdown():
    ///   - reads [General] Countdown (0 None / 1 Normal / 2 Half / 3 Double) + CountdownOffset;
    ///   - beat length is the timing point at the first object, DOUBLED when it is <= 333ms
    ///     (BPM >= 180, so a fast map doesn't machine-gun the counts), then Half = x2, Double = /2;
    ///   - GO! lands on the beat BEFORE the first object; 3/2/1 precede it a beat apart;
    ///     Ready? runs from GO-6 to GO-3 beats; CountdownOffset shifts the whole thing earlier;
    ///   - each count pops in with a 1.4 -> 1 scale over the last 0.2 beat and holds (osu!'s
    ///     "new layout", i.e. the modern default skin).
    ///
    /// Assets: uses the BUNDLED SD PNGs by default, rendered at Scale (1.0 = osu! size). This is
    /// the reliable choice for a released map. UseSkinSprites can draw the player's skin countdown
    /// instead, but note the storyboard limitation: osu! does NOT substitute a default when the
    /// player's skin is missing an element, so a skin without a countdown shows NOTHING (there is no
    /// fallback to the bundled art). Leave it off unless you know the target skin has a countdown.
    ///
    /// Put this effect's layer ABOVE the cover and the objects.
    /// </summary>
    public class VAM_Countdown : StoryboardObjectGenerator
    {
        public enum CountdownEnable { Auto, ForceOn, ForceOff }
        public enum CountdownSpeed { Normal, Half, Double }

        [Group("Enable")]
        [Description("Auto = show the countdown only when the .osu [General] Countdown is not 0, using its speed. ForceOn = always show it (uses SpeedWhenForced). ForceOff = never show it.")]
        [Configurable] public CountdownEnable Mode = CountdownEnable.Auto;
        [Description("Speed used ONLY when Mode = ForceOn (in Auto the speed comes from the .osu). Normal = one count per beat, Half = one per two beats, Double = two counts per beat.")]
        [Configurable] public CountdownSpeed SpeedWhenForced = CountdownSpeed.Normal;

        [Group("Sprites")]
        [Description("Draw the PLAYER'S skin countdown instead of the bundled art. WARNING: osu! storyboards do NOT fall back to a default when a skin lacks an element - if the player's skin has no countdown, NOTHING shows. The bundled art (leave this OFF) is the only reliable choice for a released map. Requires 'UseSkinSprites: 1' in the .osu (the installer sets it).")]
        [Configurable] public bool UseSkinSprites = false;
        [Description("Folder holding the bundled countdown PNGs (ready.png, count3.png, count2.png, count1.png, go.png). Ignored when UseSkinSprites is on.")]
        [Configurable] public string SpriteFolder = "sb/vam/";
        [Description("Show the 'Ready?' element (GO-6 to GO-3 beats). Turn off if your asset set has no ready.png.")]
        [Configurable] public bool ShowReady = true;
        [Description("Show the 'GO!' element on the beat before the first object.")]
        [Configurable] public bool ShowGo = true;

        [Group("Placement")]
        [Description("Screen x of the countdown centre (320 = widescreen centre).")]
        [Configurable] public double CenterX = 320;
        [Description("Screen y of the countdown centre (240 = middle of the 0..480 screen).")]
        [Configurable] public double CenterY = 240;
        [Description("Scale for the countdown sprites. The bundled art is SD (1x), so 1.0 renders it at osu!'s size; skin countdowns are also 1x, so 1.0 fits them too. osu!'s per-element pop (1.4 -> 1) multiplies this.")]
        [Configurable] public double Scale = 1.0;

        [Group("Timing")]
        [Description("Extra beats of lead added on top of the .osu CountdownOffset. Positive = the whole countdown starts (and ends) earlier.")]
        [Configurable] public int ExtraOffsetBeats = 0;
        [Description("Beat length override in ms per beat (60000/BPM). <= 0 = use the map's timing at the first object. The <=333ms fast-BPM doubling is applied AFTER this.")]
        [Configurable] public double BeatLengthOverride = 0;

        [Group("Layer")]
        [Description("Put this ABOVE the cover and the objects so the countdown shows. Set its OSB Layer to Overlay.")]
        [Configurable] public string LayerName = "VAM Countdown";

        public override void Generate()
        {
            var osuPath = VamOsuPathResolver.Resolve(MapsetPath, Beatmap.Name, Beatmap.Id);
            if (string.IsNullOrEmpty(osuPath)) { Log("VAM_Countdown: no .osu found in " + MapsetPath); return; }

            VamBeatmap map;
            try { map = VamLoader.Load(osuPath, computeHyperDash: false); }
            catch (Exception e) { Log("VAM_Countdown: load/convert failed: " + e.Message); return; }

            if (map.Objects.Count == 0) { Log("VAM_Countdown: no objects to lead into."); return; }

            if (Mode == CountdownEnable.ForceOff) { Log("VAM_Countdown: ForceOff, nothing drawn."); return; }
            if (Mode == CountdownEnable.Auto && map.CountdownMode == 0)
            { Log("VAM_Countdown: .osu Countdown is None (set Mode = ForceOn to override)."); return; }

            // Beat length: map timing at the first object, DOUBLED if <=333ms (osu! fast-BPM rule),
            // then the speed multiplier from the mode. (osu!: Half x2, Double /2, Normal x1.)
            double bl = BeatLengthOverride > 0 ? BeatLengthOverride : map.BeatLengthAtStart;
            if (bl <= 0) bl = 500.0;
            if (bl <= 333.0) bl *= 2.0;                          // osu!: "if the bpm is too fast, double it"
            double speedMul = Mode == CountdownEnable.ForceOn ? SpeedMul(SpeedWhenForced) : SpeedMul(map.CountdownMode);
            double interval = bl * speedMul;
            if (interval <= 0) return;

            // GO! lands one beat before the first object; CountdownOffset (+ ExtraOffsetBeats) shifts
            // the whole sequence earlier. (osu! aligns GO! to the beat grid; assuming the first object
            // sits on a beat - true for essentially every map - anchoring on it gives the same result.)
            double t0 = map.Objects[0].Time;
            double go = t0 - (1 + map.CountdownOffset + ExtraOffsetBeats) * interval;

            // Enough lead-in for the sequence? osu! only shows it when GO-4 beats is still after 0.
            if (Mode == CountdownEnable.Auto && go - 4 * interval <= 0)
            { Log("VAM_Countdown: not enough lead-in before the first object; skipped (osu! does the same)."); return; }

            var layer = GetLayer(LayerName);

            if (ShowReady) EmitReady(layer, go, interval);
            EmitCount(layer, "count3", go - 3 * interval, interval);
            EmitCount(layer, "count2", go - 2 * interval, interval);
            EmitCount(layer, "count1", go - 1 * interval, interval);
            if (ShowGo) EmitGo(layer, go, interval);

            Log($"VAM_Countdown: {(Mode == CountdownEnable.ForceOn ? "forced" : "auto")} " +
                $"(mode {map.CountdownMode}, x{speedMul}, beat {bl:0.#}ms, {(UseSkinSprites ? "skin" : "bundled")} art), " +
                $"GO! at {go:0}ms, first object {t0:0}ms.");
        }

        // Ready?: fades in over GO-6..GO-5 beats, holds, then scales 1 -> 1.2 while fading out over
        // GO-4..GO-3 beats (osu! new layout).
        private void EmitReady(StoryboardLayer layer, double go, double bl)
        {
            var path = PathFor("ready");
            if (string.IsNullOrEmpty(path)) return;
            var s = layer.CreateSprite(path, OsbOrigin.Centre);
            s.Move(go - 6 * bl, CenterX, CenterY);
            s.Scale(go - 6 * bl, Scale);
            s.Fade(go - 6 * bl, go - 5 * bl, 0, 1);
            s.Scale(OsbEasing.None, go - 4 * bl, go - 3 * bl, Scale, 1.2 * Scale);
            s.Fade(go - 4 * bl, go - 3 * bl, 1, 0);
        }

        // 3 / 2 / 1: pops in with a 1.4 -> 1 scale over the last 0.2 beat, holds ~0.8 beat, fades out
        // over the final 0.2 beat (ending exactly as the next count appears). 'at' is when it lands.
        private void EmitCount(StoryboardLayer layer, string element, double at, double bl)
        {
            var path = PathFor(element);
            if (string.IsNullOrEmpty(path)) return;
            var s = layer.CreateSprite(path, OsbOrigin.Centre);
            s.Move(at - 0.2 * bl, CenterX, CenterY);
            s.Fade(at - 0.2 * bl, at, 0, 1);
            s.Scale(OsbEasing.None, at - 0.2 * bl, at, 1.4 * Scale, Scale);
            s.Fade(at + 0.8 * bl, at + bl, 1, 0);
        }

        // GO!: scales 1.4 -> 1 over GO-0.6..GO+0.2 beats, fades in over the last 0.2 beat, holds, then
        // fades out over GO+0.3..GO+1 beats (osu! new layout).
        private void EmitGo(StoryboardLayer layer, double go, double bl)
        {
            var path = PathFor("go");
            if (string.IsNullOrEmpty(path)) return;
            var s = layer.CreateSprite(path, OsbOrigin.Centre);
            s.Move(go - 0.6 * bl, CenterX, CenterY);
            s.Fade(go - 0.6 * bl, 0);                          // cull until the fade-in (scale starts earlier)
            s.Scale(OsbEasing.None, go - 0.6 * bl, go + 0.2 * bl, 1.4 * Scale, Scale);
            s.Fade(go - 0.2 * bl, go, 0, 1);
            s.Fade(go + 0.3 * bl, go + bl, 1, 0);
        }

        // element -> sprite path. UseSkinSprites -> "element.png" (osu! draws the player's skin
        // element, or NOTHING if the skin lacks it - storyboards have no default fallback). Otherwise
        // the bundled PNG at SpriteFolder + name.
        private string PathFor(string element)
        {
            if (string.IsNullOrEmpty(element)) return null;
            if (UseSkinSprites) return element + ".png";
            var folder = SpriteFolder ?? "";
            if (folder.Length > 0 && !folder.EndsWith("/")) folder += "/";
            return folder + element + ".png";
        }

        // osu! Countdown speed -> interval multiplier: Normal = 1 beat, Half = 2 beats (slower),
        // Double = 0.5 beat (faster). Accepts the enum or the raw .osu value (1/2/3).
        private static double SpeedMul(CountdownSpeed s)
        {
            switch (s) { case CountdownSpeed.Half: return 2.0; case CountdownSpeed.Double: return 0.5; default: return 1.0; }
        }
        private static double SpeedMul(int countdownMode)
        {
            switch (countdownMode) { case 2: return 2.0; case 3: return 0.5; default: return 1.0; }
        }
    }
}
