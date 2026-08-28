using System.Collections.Generic;
using CitrusVortex.Common;

namespace CitrusVortex.Targets.Osu
{
    public class OsuSlider : OsuHitObject
    {
        public OsuCurveType CurveType { get; set; }
        public List<Point> CurvePoints { get; set; } = [];
        public int Slides { get; set; }
        public double Length { get; set; }
        public List<OsuHitSound> EdgeSounds { get; set; } = [];
        public List<OsuEdgeSet> EdgeSets { get; set; } = [];
    }
}
