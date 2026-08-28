using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtbLoader.Targets.Osu.Conversion
{
    public interface IOsuToOsuCatchBeatmapConverter
    {
        OsuCatchBeatmap Convert(OsuBeatmap beatmap);
    }
}
