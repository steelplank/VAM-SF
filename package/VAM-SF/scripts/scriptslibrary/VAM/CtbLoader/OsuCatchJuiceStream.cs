using System.Collections.Generic;

namespace CtbLoader.Targets.Osu
{
    public class OsuCatchJuiceStream : OsuCatchHitObject
    {
        public List<OsuCatchHitObject> Components { get; set; } = [];
    }
}
