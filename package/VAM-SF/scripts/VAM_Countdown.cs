using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewScripts.Vam;
using System;
using System.Collections.Generic;

namespace StorybrewScripts
{
    /// <summary>
    /// Redraws osu!'s pre-map countdown (3, 2, 1, GO!) in the storyboard, because the
    /// CatchPlayfieldCover hides osu!'s own countdown behind the black cover.
    ///
    /// It reads the [General] Countdown setting from the same .osu the rest of the framework loads:
    ///   Countdown = 0 None / 1 Normal / 2 Half (slower) / 3 Double (faster), plus CountdownOffset.
    /// By default it mirrors the map (Auto): shows the countdown only when the map has one, at the
    /// map's speed. Set Mode = ForceOff to suppress it, or ForceOn to always show one (handy for
    /// testing, since neither osu!'s countdown nor storyboard timing previews reliably in the editor).
    ///
    /// The counts are beat-synced to the timing point in force at the first object. GO! lands on the
    /// beat BEFORE the first object (osu! plays "GO!" a beat before the first note), and 3/2/1 precede
    /// it one beat apart. There is no "Ready?" element — osu!lazer never implemented this countdown
    /// (ppy/osu #4628, closed "not planned") and stable is closed-source, so its exact timing/scale
    /// are unknown. Put this effect's layer ABOVE the cover and the objects.
    /// </summary>
    public class VAM_Countdown : StoryboardObjectGenerator
    {
        public enum CountdownEnable { Auto, ForceOn, ForceOff }
        public enum CountdownSpeed { Normal, Half, Double }

        [Group("Enable")]
        [Description("Auto = show the countdown only when the .osu [General] Countdown is not 0, using its speed. ForceOn = always show it (uses SpeedWhenForced). ForceOff = never show it (the manual off switch).")]
        [Configurable] public CountdownEnable Mode = CountdownEnable.Auto;
        [Description("Speed used ONLY when Mode = ForceOn (in Auto the speed comes from the .osu). Normal = one count per beat, Half = one per two beats, Double = two counts per beat.")]
        [Configurable] public CountdownSpeed SpeedWhenForced = CountdownSpeed.Normal;

        [Group("Sprites")]
        [Description("Use the player's SKIN countdown elements (count3/count2/count1/go) instead of the bundled PNGs. REQUIRES 'UseSkinSprites: 1' in the .osu [General] (the installer sets it). Off = use the bundled PNGs in SpriteFolder.")]
        [Configurable] public bool UseSkinSprites = false;
        [Description("Folder holding the bundled countdown PNGs (count3.png, count2.png, count1.png, go.png). Ignored when UseSkinSprites is on.")]
        [Configurable] public string SpriteFolder = "sb/vam/";
        [Description("Show the 'GO!' element on the beat before the first object.")]
        [Configurable] public bool ShowGo = true;

        [Group("Placement")]
        [Description("Screen x of the countdown centre (320 = widescreen centre).")]
        [Configurable] public double CenterX = 320;
        [Description("Screen y of the countdown centre (240 = middle of the 0..480 screen).")]
        [Configurable] public double CenterY = 240;
        [Description("Uniform scale for the countdown sprites. (osu! never open-sourced this countdown, so there is no official value to lock to — 0.5 matches the bundled art; adjust if you swap in skin sprites.)")]
        [Configurable] public double Scale = 0.5;
        [Description("Fade in/out time (ms) applied to each element as it appears / gives way to the next.")]
        [Configurable] public double FadeTime = 80;

        [Group("Timing")]
        [Description("Extra beats of lead added on top of the .osu CountdownOffset. Positive = the whole countdown starts (and ends) earlier. Use to nudge it if it feels off.")]
        [Configurable] public int ExtraOffsetBeats = 0;
        [Description("Beat length override in ms per beat (60000/BPM). <= 0 = use the map's timing at the first object.")]
        [Configurable] public double BeatLengthOverride = 0;

        [Group("Layer")]
        [Description("Put this ABOVE the cover and the objects so the countdown shows. Set its OSB Layer to Overlay.")]
        [Configurable] public string LayerName = "VAM Countdown";

        public override void Generate()
        {
            var osuPath = VamOsuPathResolver.Resolve(MapsetPath, Beatmap.Name, Beatmap.Id);
            if (string.IsNullOrEmpty(osuPath))
            {
                Log("VAM_Countdown: could not locate a .osu file in " + MapsetPath);
                return;
            }

            VamBeatmap map;
            try
            {
                map = VamLoader.Load(osuPath, computeHyperDash: false);
            }
            catch (Exception e)
            {
                Log("VAM_Countdown: failed to load/convert beatmap: " + e.Message);
                return;
            }

            if (map.Objects.Count == 0)
            {
                Log("VAM_Countdown: no objects to lead into.");
                return;
            }

            // Decide whether to draw, and at what speed.
            if (Mode == CountdownEnable.ForceOff)
            {
                Log("VAM_Countdown: Mode = ForceOff, nothing drawn.");
                return;
            }
            if (Mode == CountdownEnable.Auto && map.CountdownMode == 0)
            {
                Log("VAM_Countdown: .osu Countdown is None; nothing drawn (set Mode = ForceOn to override).");
                return;
            }

            double speedMul = Mode == CountdownEnable.ForceOn
                ? SpeedMul(SpeedWhenForced)
                : SpeedMulFromOsu(map.CountdownMode);

            double beat = BeatLengthOverride > 0 ? BeatLengthOverride : map.BeatLengthAtStart;
            if (beat <= 0) beat = 500.0;
            double interval = beat * speedMul;

            double t0 = map.Objects[0].Time;
            int offsetBeats = map.CountdownOffset + ExtraOffsetBeats;
            // The whole sequence is shifted earlier by offsetBeats.
            double anchor = t0 - offsetBeats * interval;

            // Build the sequence (each element shows until the next; last holds for one interval).
            // GO! lands ONE beat BEFORE the first object (osu! plays "GO!" on the beat before the
            // first note, not on it), and the 3/2/1 counts precede it a beat apart. There is no
            // "Ready?" element: osu!lazer never implemented this countdown (ppy/osu #4628, closed
            // "not planned") and stable is closed-source, so its exact "Ready" timing isn't known.
            var seq = new List<KeyValuePair<string, double>>();
            seq.Add(new KeyValuePair<string, double>("count3", anchor - 4 * interval));
            seq.Add(new KeyValuePair<string, double>("count2", anchor - 3 * interval));
            seq.Add(new KeyValuePair<string, double>("count1", anchor - 2 * interval));
            if (ShowGo) seq.Add(new KeyValuePair<string, double>("go", anchor - 1 * interval));

            var layer = GetLayer(LayerName);

            int shown = 0;
            for (int i = 0; i < seq.Count; i++)
            {
                double show = seq[i].Value;
                double until = (i + 1 < seq.Count) ? seq[i + 1].Value : show + interval;

                if (until <= 0) continue;      // entirely before the audio starts
                if (show < 0) show = 0;         // clamp the start into the audio
                if (until <= show) continue;

                ShowElement(layer, seq[i].Key, show, until);
                shown++;
            }

            Log($"VAM_Countdown: {(Mode == CountdownEnable.ForceOn ? "forced" : "auto")} " +
                $"(mode {map.CountdownMode}, speed x{speedMul}, beat {beat:0.#}ms), " +
                $"{shown} element(s) leading into t0={t0}.");
        }

        // Draws one countdown element as a static, centred sprite that fades in, holds, and fades out.
        private void ShowElement(StoryboardLayer layer, string element, double show, double until)
        {
            var path = PathFor(element);
            if (string.IsNullOrEmpty(path)) return;

            var s = layer.CreateSprite(path, OsbOrigin.Centre);
            // A degenerate move keeps the sprite at a constant position for its whole life.
            s.Move(OsbEasing.None, show, until, CenterX, CenterY, CenterX, CenterY);
            s.Scale(show, Scale);

            double fi = FadeTime;
            double maxFade = (until - show) / 2.0;
            if (fi > maxFade) fi = maxFade;
            if (fi < 0) fi = 0;

            s.Fade(show, show + fi, 0, 1);
            s.Fade(until - fi, until, 1, 0);
        }

        // Resolves a countdown element name to a sprite path. UseSkinSprites -> "element.png"
        // (resolved from the player's skin when UseSkinSprites:1 is set in the .osu). Otherwise the
        // bundled PNG at SpriteFolder + element + ".png".
        private string PathFor(string element)
        {
            if (string.IsNullOrEmpty(element)) return null;
            if (UseSkinSprites) return element + ".png";

            var folder = SpriteFolder ?? "";
            if (folder.Length > 0 && !folder.EndsWith("/")) folder += "/";
            return folder + element + ".png";
        }

        // osu! Countdown speed as an interval multiplier: Normal = 1 beat, Half = 2 beats
        // (slower), Double = 0.5 beat (faster).
        private static double SpeedMul(CountdownSpeed s)
        {
            switch (s)
            {
                case CountdownSpeed.Half: return 2.0;
                case CountdownSpeed.Double: return 0.5;
                default: return 1.0;
            }
        }

        // Maps the .osu Countdown value (1 Normal / 2 Half / 3 Double) to an interval multiplier.
        private static double SpeedMulFromOsu(int countdownMode)
        {
            switch (countdownMode)
            {
                case 2: return 2.0;   // Half
                case 3: return 0.5;   // Double
                default: return 1.0;  // Normal (and any unexpected value)
            }
        }
    }
}
