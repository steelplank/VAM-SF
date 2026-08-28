using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrusVortex.Targets.Osu.Serialization
{
    public interface IOsuBeatmapDeserializer
    {
        OsuBeatmap Deserialize(List<string> serializedLines);
    }
}
