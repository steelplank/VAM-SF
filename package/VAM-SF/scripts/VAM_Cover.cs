using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewScripts.Vam;
using System;

namespace StorybrewScripts
{
    // How an image cover (background / custom) is fitted to the screen before it is split.
    //   Fill    = cover: scale to fill, crop the overflow. A 4:3 / 16:9 / any matching-aspect
    //             image shows edge to edge with NO distortion; only a mismatched aspect is cropped.
    //   Fit     = contain: whole image visible, black bars fill the rest (still fully opaque).
    //   Stretch = the old behaviour: stretch each half to fill (distorts non-matching images).
    public enum CoverFit { Fill, Fit, Stretch }

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
        [Description("Cover opacity. 1.0 fully hides what's behind it. Keep at 1.0 for an image cover (use Background dim to darken instead).")]
        [Configurable] public double Opacity = 1.0;
        [Description("Match to the catch line.")]
        [Configurable] public double CoverBottomY = 412;
        [Description("Bottom edge the BOTTOM cover extends to.")]
        [Configurable] public double ScreenBottomY = 480;
        [Description("Storyboard width to span (854 covers widescreen; 640 for 4:3).")]
        [Configurable] public double CoverWidth = 854;

        [Group("Image cover (background / custom)")]
        [Description("How an image cover is fitted before it is split in two. Fill = no distortion for matching aspect, crop otherwise; Fit = letterbox; Stretch = old stretch-to-fill.")]
        [Configurable] public CoverFit BackgroundFit = CoverFit.Fill;
        [Description("Darken an image cover so the storyboard fruits stay readable. 0 = untouched, 1 = black. The cover stays fully opaque; this bakes the dim into the pixels.")]
        [Configurable] public double BackgroundDim = 0.75;

        [Group("Layers")]
        [Description("TOP region layer.")]
        [Configurable] public string LayerName = "Overlay";
        [Description("BOTTOM region layer.")]
        [Configurable] public string BottomLayerName = "Background";

        const double CoverCenterX = 320;

        public override void Generate()
        {
            // Empty or "black" -> pure-black tile (solid, safe to stretch). "background" -> the map
            // background. Anything else -> that image path. Real images go through the crop pipeline.
            string path; bool solidTile;
            if (string.IsNullOrEmpty(SpritePath) || SpritePath.Trim().ToLowerInvariant() == "black")
                { path = BlackTilePath; solidTile = true; }
            else if (SpritePath.Trim().ToLowerInvariant() == "background")
                { path = Beatmap.BackgroundPath; solidTile = false; }
            else
                { path = SpritePath; solidTile = false; }

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

            // A real image gets the crop pipeline (no stretching); the solid tile, or an explicit
            // Stretch choice, keeps the simple stretch. EmitBackgroundCover falls back to stretch
            // if the bitmap can't be read.
            if (!solidTile && BackgroundFit != CoverFit.Stretch && EmitBackgroundCover(path, start, end))
                return;

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

        // Build a dimmed, aspect-correct composite of the image at the cover's aspect, split it at the
        // catch line into a top and bottom PNG, and draw each 1:1 on its layer. osu! has no runtime
        // crop, so the crop is baked here at build time. Returns false (and the caller stretches) if
        // anything goes wrong. Both halves are opaque, so black bars in Fit still hide the fruits.
        private bool EmitBackgroundCover(string path, int start, int end)
        {
            System.Drawing.Bitmap src;
            try { src = GetMapsetBitmap(path); }   // storybrew-cached; do NOT dispose
            catch (Exception e) { Log("VAM_Cover: image cover read failed, falling back to stretch. " + e.Message); return false; }

            try
            {
                int W = src.Width, H = src.Height;
                if (W <= 0 || H <= 0 || CoverWidth <= 0 || ScreenBottomY <= 0) return false;

                // High-res composite at the cover's own aspect (so drawing it back is a pure uniform scale).
                int canvasW = 1920;
                int canvasH = Math.Max(2, (int)Math.Round(canvasW * ScreenBottomY / CoverWidth));
                int splitRow = (int)Math.Round(canvasH * (CoverBottomY / ScreenBottomY));
                splitRow = Math.Max(1, Math.Min(canvasH - 1, splitRow));

                // Destination rect of the image inside the canvas: cover (Fill) vs contain (Fit).
                double s = BackgroundFit == CoverFit.Fit
                    ? Math.Min((double)canvasW / W, (double)canvasH / H)
                    : Math.Max((double)canvasW / W, (double)canvasH / H);
                double dw = W * s, dh = H * s;
                double dx = (canvasW - dw) / 2.0, dy = (canvasH - dh) / 2.0;

                string outAbs = System.IO.Path.Combine(MapsetPath, "sb", "vam");
                System.IO.Directory.CreateDirectory(outAbs);
                const string topRel = "sb/vam/vam-cover-top.png";
                const string botRel = "sb/vam/vam-cover-bottom.png";

                using (var canvas = new System.Drawing.Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var g = System.Drawing.Graphics.FromImage(canvas))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.Clear(System.Drawing.Color.Black);
                        g.DrawImage(src,
                            new System.Drawing.RectangleF((float)dx, (float)dy, (float)dw, (float)dh),
                            new System.Drawing.RectangleF(0, 0, W, H),
                            System.Drawing.GraphicsUnit.Pixel);
                        int dim = (int)Math.Round(Math.Max(0.0, Math.Min(1.0, BackgroundDim)) * 255);
                        if (dim > 0)
                            using (var b = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(dim, 0, 0, 0)))
                                g.FillRectangle(b, 0, 0, canvasW, canvasH);
                    }
                    using (var top = canvas.Clone(new System.Drawing.Rectangle(0, 0, canvasW, splitRow), canvas.PixelFormat))
                        top.Save(System.IO.Path.Combine(MapsetPath, "sb", "vam", "vam-cover-top.png"), System.Drawing.Imaging.ImageFormat.Png);
                    if (canvasH - splitRow > 0)
                        using (var bot = canvas.Clone(new System.Drawing.Rectangle(0, splitRow, canvasW, canvasH - splitRow), canvas.PixelFormat))
                            bot.Save(System.IO.Path.Combine(MapsetPath, "sb", "vam", "vam-cover-bottom.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                if (CoverBottomY > 0)
                    EmitPiece(GetLayer(LayerName), topRel, canvasW, splitRow, CoverWidth, CoverBottomY, 0, start, end);
                double bottomHeight = ScreenBottomY - CoverBottomY;
                if (!string.IsNullOrEmpty(BottomLayerName) && bottomHeight > 0 && canvasH - splitRow > 0)
                    EmitPiece(GetLayer(BottomLayerName), botRel, canvasW, canvasH - splitRow, CoverWidth, bottomHeight, CoverBottomY, start, end);

                Log($"VAM_Cover: image cover generated ({BackgroundFit}, dim {BackgroundDim:0.##}).");
                return true;
            }
            catch (Exception e)
            {
                Log("VAM_Cover: image cover generation failed, falling back to stretch. " + e.Message);
                return false;
            }
        }

        // One generated crop piece, scaled to exactly fill its band (top-anchored, centred on 320).
        private void EmitPiece(StoryboardLayer layer, string relPath, int pieceW, int pieceH,
                               double destW, double destH, double topY, int start, int end)
        {
            int fade = 300;
            int appear = start - fade;
            var s = layer.CreateSprite(relPath, OsbOrigin.TopCentre);
            s.ScaleVec(appear, destW / pieceW, destH / pieceH);
            s.Move(appear, CoverCenterX, topY);
            s.Fade(appear, start, 0, Opacity);
            s.Fade(end, end + fade, Opacity, 0);
        }

        private void EmitCover(StoryboardLayer layer, string path, double imgW, double imgH,
                               double topY, double bottomY, int start, int end)
        {
            double height = bottomY - topY;
            int fade = 300;
            int appear = start - fade;   // may be negative (osu! allows pre-0 storyboard times)

            var cover = layer.CreateSprite(path, OsbOrigin.TopCentre);
            cover.ScaleVec(appear, CoverWidth / imgW, height / imgH);
            cover.Move(appear, CoverCenterX, topY);
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
