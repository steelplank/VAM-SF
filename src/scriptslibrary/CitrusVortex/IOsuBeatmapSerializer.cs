using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrusVortex.Targets.Osu.Serialization
{
    public interface IOsuBeatmapSerializer
    {
        List<string> Serialize(OsuBeatmap beatmap);
    }
}
