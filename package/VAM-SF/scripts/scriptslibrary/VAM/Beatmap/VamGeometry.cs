using System;

namespace StorybrewScripts.Vam
{
    // Maps osu!catch playfield coordinates + AR into storyboard space. Fruits fall at CONSTANT
    // velocity to the catch line at their Time; preempt = DifficultyRange(AR,1800,1200,450). osu
    // renders the playfield SCALED to the screen, so fall distance isn't the raw 384 units - SpawnY
    // and CatchY are both exposed and calibrated by eye, and fall speed follows from the two.
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

        // osu! circle-size -> OBJECT scale = CalculateScaleFromCircleSize(cs) = (1 - 0.7*(cs-5)/5) / 2.
        // A catch object's drawable box is OBJECT_RADIUS*2 = 128 local units, so a 128px sprite at this
        // Scale reproduces osu!'s object size 1:1 (PlayfieldWidth 512 = storyboard units). Tiny droplets
        // carry an extra 0.5 ScaleFactor (applied by the effect).
        public static double ObjectScaleFromCircleSize(double circleSize)
        {
            return CatcherScaleFromCircleSize(circleSize) / 2.0;
        }
    }
}
