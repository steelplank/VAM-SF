using System.Collections.Generic;

namespace CitrusVortex.Targets.Osu
{
    public class OsuCatchBananaShower : OsuCatchHitObject
    {
        public int EndTime { get; set; }
        public List<OsuCatchBanana> Bananas { get; set; } = [];
    }
}
