using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewScripts.Vam;
using System;

namespace StorybrewScripts
{
    /// <summary>
    /// Fake osu!catch Flashlight: a dark vignette with a clear hole that follows the
    /// catcher. Since the real catcher position isn't known to the storyboard, the hole
    /// tracks where the catcher MUST be — the x of each incoming fruit/droplet — so the
    /// catcher stays lit and visible while the rest of the screen is dimmed.
    ///
    /// IMPORTANT (editor step): set this effect's layer OSB Layer to "Foreground" (above the
    /// object copy) so it darkens the fake fruits too.
    /// </summary>
    public class VAM_Flashlight : StoryboardObjectGenerator
    {
        [Group("Timing (0,0 = whole map)")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;

        [Group("Vignette")]
        [Configurable] public string SpritePath = "sb/vam/flashlight.png";
        [Description("Darkness of the covered area (0..1).")]
        [Configurable] public double Darkness = 0.92;
        [Description("Storyboard scale of the vignette sprite (bigger = larger clear hole).")]
        [Configurable] public double VignetteScale = 1.2;

        [Group("Playfield geometry (match CatchObjectCopy)")]
        [Configurable] public double CenterX = 320;
        [Configurable] public double PlayfieldWidth = 512;
        [Description("Storyboard y the hole sits at (the catch line).")]
        [Configurable] public double CatchY = 412;

        [Group("Tracking")]
        [Description("Also follow droplets (smoother) or only full fruits.")]
        [Configurable] public bool FollowDroplets = true;
        [Description("Ease the hole's movement between objects.")]
        [Configurable] public bool SmoothTracking = true;

        [Group("Layer")]
        [Configurable] public string LayerName = "VAM Flashlight";

        public override void Generate()
        {
            var osuPath = VamOsuPathResolver.Resolve(MapsetPath, Beatmap.Name, Beatmap.Id);
            if (string.IsNullOrEmpty(osuPath))
            {
                Log("VAM_Flashlight: could not locate a .osu file in " + MapsetPath);
                return;
            }

            VamBeatmap map;
            try { map = VamLoader.Load(osuPath, computeHyperDash: false); }
            catch (Exception e) { Log("VAM_Flashlight: load failed: " + e.Message); return; }

            var geometry = new VamGeometry { CenterX = CenterX, PlayfieldWidth = PlayfieldWidth, SpawnY = 0, CatchY = CatchY };

            int start = StartTime;
            int end = EndTime > 0 ? EndTime : (int)AudioDuration;

            var layer = GetLayer(LayerName);
            var vignette = layer.CreateSprite(SpritePath, OsbOrigin.Centre);
            vignette.Scale(start, VignetteScale);
            vignette.Color(start, System.Drawing.Color.Black);
            vignette.Fade(Math.Max(0, start - 300), start, 0, Darkness);
            vignette.Fade(end, end + 300, Darkness, 0);
            vignette.MoveY(start, CatchY);

            // Build the catcher path from the objects the catcher has to reach.
            var easing = SmoothTracking ? OsbEasing.InOutSine : OsbEasing.None;
            int last = int.MinValue;
            double lastX = geometry.MapX(256);
            bool first = true;

            foreach (var obj in map.Objects)
            {
                if (obj.Type == VamObjectType.Banana) continue;
                if (!FollowDroplets && obj.Type != VamObjectType.Fruit) continue;
                if (obj.Time < start || obj.Time > end) continue;

                double x = geometry.MapX(obj.X);

                if (first)
                {
                    vignette.MoveX(start, x);
                    lastX = x;
                    last = obj.Time;
                    first = false;
                    continue;
                }

                if (obj.Time > last)
                {
                    vignette.MoveX(easing, last, obj.Time, lastX, x);
                    lastX = x;
                    last = obj.Time;
                }
            }
        }
    }
}
