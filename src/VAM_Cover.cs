using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewScripts.Vam;
using System;

namespace StorybrewScripts
{
    /// Darkens the whole catch playfield with a cover, split into TWO regions on TWO layers so the catcher stays visible while everything else is hidden
    public class VAM_Cover : StoryboardObjectGenerator
    {
        [Group("Timing (0,0 = whole map)")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;

        [Group("Lead-in")]
        [Description("Automatically bring the cover up BEFORE the first object can appear, so maps with little/no audio lead-in are never seen uncovered.")]
        [Configurable] public bool AutoLeadIn = true;
        [Description("Safety preempt (ms) subtracted from the first object's time to place the lead-in.")]
        [Configurable] public double LeadInPreempt = 1800;

        [Group("Cover")]
        [Description("Cover image. Empty or 'black' = a pure-black tile. 'background' = the map's background. Any other value = that image path.")]
        [Configurable] public string SpritePath = "sb/vam/black.png";
        [Description("Bundled pure-black tile used when SpritePath is empty or 'black'.")]
        [Configurable] public string BlackTilePath = "sb/vam/black.png";
        [Description("Cover opacity. 1.0 fully hides what's behind it.")]
        [Configurable] public double Opacity = 1.0;
        [Description("Match to the catch line.")]
        [Configurable] public double CoverBottomY = 412;
        [Description("Bottom edge the BOTTOM cover extends to.")]
        [Configurable] public double ScreenBottomY = 480;
        [Description("Storyboard width to span (854 covers widescreen; 640 for 4:3).")]
        [Configurable] public double CoverWidth = 854;

        [Group("Layers")]
        [Description("TOP region layer.")]
        [Configurable] public string LayerName = "Overlay";
        [Description("BOTTOM region layer.")]
        [Configurable] public string BottomLayerName = "Background";

        public override void Generate()
        {
            // Empty or "black" -> pure-black tile. "background" -> the map background. Anything else -> that image.
            string path;
            if (string.IsNullOrEmpty(SpritePath) || SpritePath.Trim().ToLowerInvariant() == "black")
                path = BlackTilePath;
            else if (SpritePath.Trim().ToLowerInvariant() == "background")
                path = Beatmap.BackgroundPath;
            else
                path = SpritePath;

            if (string.IsNullOrEmpty(path))
            {
                Log("VAM_Cover: no cover sprite path available.");
                return;
            }

            int start = StartTime;
            int end = EndTime > 0 ? EndTime : (int)AudioDuration;

            // Smart lead-in:
            // Shift the cover start to BEFORE the earliest object can spawn. For any object, spawn = time - preempt >= firstObjectTime - maxPreempt, so a cover that is fully opaque by (firstObjectTime - LeadInPreempt) is up before anything is visible.
            if (AutoLeadIn)
            {
                var osuPath = VamOsuPathResolver.Resolve(MapsetPath, Beatmap.Name, Beatmap.Id);
                int firstTime;
                if (TryFirstObjectTime(osuPath, out firstTime))
                {
                    int leadStart = (int)Math.Floor(firstTime - LeadInPreempt);
                    if (leadStart < start) { start = leadStart; Log($"VAM_Cover: lead-in -> cover starts at {start}ms (first object {firstTime}ms)."); }
                }
                else Log("VAM_Cover: lead-in skipped (could not read the first object time).");
            }

            double imgW = 1920, imgH = 1080;
            try
            {
                var bitmap = GetMapsetBitmap(path);
                imgW = bitmap.Width;
                imgH = bitmap.Height;
            }
            catch (Exception e)
            {
                Log("VAM_Cover: could not read cover size, assuming 1920x1080. " + e.Message);
            }
            if (CoverBottomY > 0)
                EmitCover(GetLayer(LayerName), path, imgW, imgH, 0, CoverBottomY, start, end);
            double bottomHeight = ScreenBottomY - CoverBottomY;
            if (!string.IsNullOrEmpty(BottomLayerName) && bottomHeight > 0)
                EmitCover(GetLayer(BottomLayerName), path, imgW, imgH, CoverBottomY, ScreenBottomY, start, end);
        }

        private void EmitCover(StoryboardLayer layer, string path, double imgW, double imgH,
                               double topY, double bottomY, int start, int end)
        {
            double height = bottomY - topY;
            int fade = 300;
            int appear = start - fade;   // may be negative (osu! allows pre-0 storyboard times)

            var cover = layer.CreateSprite(path, OsbOrigin.TopCentre);
            cover.ScaleVec(appear, CoverWidth / imgW, height / imgH);
            cover.Move(appear, 320, topY);
            cover.Fade(appear, start, 0, Opacity);
            cover.Fade(end, end + fade, Opacity, 0);
        }

        // Cheap first-object time from the .osu [HitObjects] (first line's 3rd field), without the full map conversion.
        // Assuming that objects are ALWAYS stored in time order, so the first line is the earliest. Could be false in some cases but fuck it, we ball.
        private static bool TryFirstObjectTime(string osuPath, out int time)
        {
            time = 0;
            if (string.IsNullOrEmpty(osuPath)) return false;
            try
            {
                bool inObjects = false;
                foreach (var raw in System.IO.File.ReadAllLines(osuPath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line[0] == '[') { inObjects = string.Equals(line, "[HitObjects]", StringComparison.OrdinalIgnoreCase); continue; }
                    if (!inObjects) continue;
                    var parts = line.Split(',');
                    if (parts.Length >= 3 && int.TryParse(parts[2].Trim(), out time)) return true;
                }
            }
            catch { }
            return false;
        }
    }
}
