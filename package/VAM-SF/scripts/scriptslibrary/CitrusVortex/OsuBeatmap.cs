using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrusVortex.Common;

namespace CitrusVortex.Targets.Osu
{
    // Documentation: https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29
    public class OsuBeatmap
    {
        public int FileFormatVersion { get; set; }

        // [General] Section
        public string AudioFilename { get; set; }
        public int AudioLeadIn { get; set; }
        public string AudioHash { get; set; } // Deprecated
        public int PreviewTime { get; set; }
        public OsuCountdown Countdown { get; set; }
        public string SampleSet { get; set; }
        public double StackLeniency { get; set; }
        public OsuMode Mode { get; set; }
        public bool LetterboxInBreaks { get; set; }
        public bool StoryFireInFront { get; set; } // Deprecated
        public bool UseSkinSprites { get; set; }
        public bool AlwaysShowPlayfield { get; set; } // Deprecated
        public OsuOverlayPosition OverlayPosition { get; set; }
        public string SkinPreference { get; set; }
        public bool EpilepsyWarning { get; set; }
        public int CountdownOffset { get; set; }
        public bool SpecialStyle { get; set; }
        public bool WidescreenStoryboard { get; set; }
        public bool SamplesMatchPlaybackRate { get; set; }

        // [Editor] Section
        public List<int> Bookmarks { get; set; } = [];
        public double DistanceSpacing { get; set; }
        public int BeatDivisor { get; set; }
        public int GridSize { get; set; }
        public double TimelineZoom { get; set; }

        // [Metadata] Section
        public string Title { get; set; }
        public string TitleUnicode { get; set; }
        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Creator { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; } = [];
        public int BeatmapID { get; set; }
        public int BeatmapSetID { get; set; }

        // [Difficulty] Section
        public double HPDrainRate { get; set; }
        public double CircleSize { get; set; }
        public double OverallDifficulty { get; set; }
        public double ApproachRate { get; set; }
        public double SliderMultiplier { get; set; }
        public double SliderTickRate { get; set; }

        // [Events] Section
        public List<OsuEvent> Events { get; set; } = [];

        // [TimingPoints] Section
        public List<OsuTimingPoint> TimingPoints { get; set; } = [];

        // [Colours] Section
        public List<Color> ComboColors { get; set; } = [];
        public Color? SliderTrackOverride { get; set; }
        public Color? SliderBorder { get; set; }

        // [HitObjects] Section
        public List<OsuHitObject> HitObjects { get; set; } = [];
    }
}
