using System;

namespace StorybrewScripts.Vam
{
    // Maps osu!catch playfield coordinates + AR into storyboard space.
    //
    // osu!catch facts (from the osu! source):
    //   * CatchPlayfield.WIDTH = 512 -> fruit x in [0,512], centre 256. Same width as
    //     osu!standard, so x maps like standard: storyboard x = osuX + 64 (centre 320).
    //   * A fruit falls at CONSTANT velocity from the top of the play area to the catch line,
    //     arriving at its Time. TimeRange = DifficultyRange(AR,1800,1200,450) == standard preempt.
    //
    // IMPORTANT: osu renders the catch playfield SCALED to fill the screen, so the on-screen
    // fall distance is NOT the raw 384 osu units — it depends on resolution. So the top (SpawnY)
    // and the catch line (CatchY) are BOTH exposed and calibrated by eye against the real fruits.
    // Fall speed = (CatchY - SpawnY) / preempt, so aligning both endpoints to the real fruit
    // makes the speed match automatically.
    public struct VamGeometry
    {
        // Storyboard x of osu-x = 256 (playfield centre). Default 320 = screen centre.
        public double CenterX;

        // Storyboard width the osu-x range [0..512] is stretched across (512 = osu-accurate).
        public double PlayfieldWidth;

        // Storyboard y where a fruit first appears (top of the fall).
        public double SpawnY;

        // Storyboard y of the catch line (where the fruit meets the catcher plate).
        public double CatchY;

        public double MapX(double osuX)
        {
            return CenterX + (osuX - 256.0) * (PlayfieldWidth / 512.0);
        }

        // osu! approach preempt in milliseconds for a given AR (== catch TimeRange).
        public static double Preempt(double approachRate)
        {
            if (approachRate > 5.0)
                return 1200.0 - 750.0 * (approachRate - 5.0) / 5.0;
            return 1200.0 + 600.0 * (5.0 - approachRate) / 5.0;
        }

        // osu! circle-size -> catcher/fruit scale (LegacyRulesetExtensions.CalculateScaleFromCircleSize * 2).
        public static double CatcherScaleFromCircleSize(double circleSize)
        {
            return 1.0 - 0.7 * (circleSize - 5.0) / 5.0;
        }

        // osu! circle-size -> OBJECT scale, i.e. LegacyRulesetExtensions.CalculateScaleFromCircleSize(cs)
        // = (1 - 0.7*(cs-5)/5) / 2. This is the scale osu! applies to a catch object's drawable.
        //
        // A catch object's drawable content box is OBJECT_RADIUS*2 = 128 local units, scaled by this
        // value. So a 128x128-px sprite drawn on the storyboard at Scale = ObjectScaleFromCircleSize(cs)
        // reproduces osu!'s on-playfield object size 1:1 (PlayfieldWidth 512 maps 1:1 to storyboard units).
        // Tiny droplets additionally carry a 0.5 ScaleFactor (applied by the effect).
        public static double ObjectScaleFromCircleSize(double circleSize)
        {
            return CatcherScaleFromCircleSize(circleSize) / 2.0;
        }
    }
}
