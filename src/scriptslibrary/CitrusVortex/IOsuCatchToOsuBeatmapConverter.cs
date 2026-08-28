using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrusVortex.Targets.Osu.Conversion
{
    public interface IOsuCatchToOsuBeatmapConverter
    {
        OsuBeatmap Convert(OsuCatchBeatmap catchBeatmap);
    }
}
