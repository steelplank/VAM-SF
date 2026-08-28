using CtbLoader.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtbLoader.Targets.Osu.Serialization
{
    // Documentation: https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29
    public class OsuV14BeatmapDeserializer : IOsuBeatmapDeserializer
    {
        private const string _VALID_HEADER = "osu file format v14";

        private const string _GENERAL_SECTION = "[General]";
        private const string _EDITOR_SECTION = "[Editor]";
        private const string _METADATA_SECTION = "[Metadata]";
        private const string _DIFFICULTY_SECTION = "[Difficulty]";
        private const string _EVENTS_SECTION = "[Events]";
        private const string _TIMING_POINTS_SECTION = "[TimingPoints]";
        private const string _COLOURS_SECTION = "[Colours]";
        private const string _HIT_OBJECTS_SECTION = "[HitObjects]";

        private readonly string[] _sectionStrings =
        [
            _GENERAL_SECTION,
            _EDITOR_SECTION,
            _METADATA_SECTION,
            _DIFFICULTY_SECTION,
            _EVENTS_SECTION,
            _TIMING_POINTS_SECTION,
            _COLOURS_SECTION,
            _HIT_OBJECTS_SECTION
        ];

        public OsuBeatmap Deserialize(List<string> serializedLines)
        {
            var beatmap = new OsuBeatmap() { FileFormatVersion = 14 };

            var preprocessedLines = PreprocessSerializedLines(serializedLines);

            if (preprocessedLines.Count == 0)
                return beatmap;

            if (preprocessedLines[0] != _VALID_HEADER)
                throw new FormatException();

            var currentSection = _GENERAL_SECTION;

            for (int i = 1; i < preprocessedLines.Count; i++)
            {
                var line = preprocessedLines[i];

                if (_sectionStrings.Contains(line))
                {
                    currentSection = line;
                    continue;
                }

                switch (currentSection) 
                {
                    case _GENERAL_SECTION:
                        ParseGeneralSectionLine(beatmap, line);
                        break;

                    case _EDITOR_SECTION:
                        ParseEditorSectionLine(beatmap, line);
                        break;

                    case _METADATA_SECTION:
                        ParseMetadataSectionLine(beatmap, line);
                        break;

                    case _DIFFICULTY_SECTION:
                        ParseDifficultySectionLine(beatmap, line);
                        break;

                    case _EVENTS_SECTION:
                        ParseEventsSectionLine(beatmap, line);
                        break;

                    case _TIMING_POINTS_SECTION:
                        ParseTimingPointsSectionLine(beatmap, line);
                        break;

                    case _COLOURS_SECTION:
                        ParseColoursSectionLine(beatmap, line);
                        break;

                    case _HIT_OBJECTS_SECTION:
                        ParseHitObjectsSectionLine(beatmap, line);
                        break;

                    default:
                    break; // FIX: tolerate unknown/unsupported keys
                }
            }

            return beatmap;
        }

        private void ParseGeneralSectionLine(OsuBeatmap beatmap, string line)
        {
            // FIX (AR-modification framework): tolerate lines without a "key: value" pair (blank
            // values, stray lines) instead of throwing on split[1] — hit on real maps (deltaMAX).
            var split = line.Split(": ");
            if (split.Length < 2) return;
            var key = split[0];
            var value = split[1];

            // AI generated
            switch (key)
            {
                case nameof(beatmap.AudioFilename):
                    beatmap.AudioFilename = value;
                    break;

                case nameof(beatmap.AudioLeadIn):
                    beatmap.AudioLeadIn = int.Parse(value);
                    break;

                case nameof(beatmap.AudioHash):
                    beatmap.AudioHash = value;
                    break;

                case nameof(beatmap.PreviewTime):
                    beatmap.PreviewTime = int.Parse(value);
                    break;

                case nameof(beatmap.Countdown):
                    beatmap.Countdown = Enum.Parse<OsuCountdown>(value, true);
                    break;

                case nameof(beatmap.SampleSet):
                    beatmap.SampleSet = value;
                    break;

                case nameof(beatmap.StackLeniency):
                    beatmap.StackLeniency = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.Mode):
                    beatmap.Mode = Enum.Parse<OsuMode>(value, true);
                    break;

                case nameof(beatmap.LetterboxInBreaks):
                    beatmap.LetterboxInBreaks = ParseOsuBool(value);
                    break;

                case nameof(beatmap.StoryFireInFront):
                    beatmap.StoryFireInFront = ParseOsuBool(value);
                    break;

                case nameof(beatmap.UseSkinSprites):
                    beatmap.UseSkinSprites = ParseOsuBool(value);
                    break;

                case nameof(beatmap.AlwaysShowPlayfield):
                    beatmap.AlwaysShowPlayfield = ParseOsuBool(value);
                    break;

                case nameof(beatmap.OverlayPosition):
                    beatmap.OverlayPosition = Enum.Parse<OsuOverlayPosition>(value, true);
                    break;

                case nameof(beatmap.SkinPreference):
                    beatmap.SkinPreference = value;
                    break;

                case nameof(beatmap.EpilepsyWarning):
                    beatmap.EpilepsyWarning = ParseOsuBool(value);
                    break;

                case nameof(beatmap.CountdownOffset):
                    beatmap.CountdownOffset = int.Parse(value);
                    break;

                case nameof(beatmap.SpecialStyle):
                    beatmap.SpecialStyle = ParseOsuBool(value);
                    break;

                case nameof(beatmap.WidescreenStoryboard):
                    beatmap.WidescreenStoryboard = ParseOsuBool(value);
                    break;

                case nameof(beatmap.SamplesMatchPlaybackRate):
                    beatmap.SamplesMatchPlaybackRate = ParseOsuBool(value);
                    break;

                default:
                    break; // FIX: tolerate unknown/unsupported keys
            }
        }

        private void ParseEditorSectionLine(OsuBeatmap beatmap, string line)
        {
            var split = line.Split(": ");
            if (split.Length < 2) return;
            var key = split[0];
            var value = split[1];

            // AI generated
            switch (key)
            {
                case nameof(beatmap.Bookmarks):
                    beatmap.Bookmarks = value.Split(',').Select(int.Parse).ToList();
                    break;

                case nameof(beatmap.DistanceSpacing):
                    beatmap.DistanceSpacing = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.BeatDivisor):
                    beatmap.BeatDivisor = int.Parse(value);
                    break;

                case nameof(beatmap.GridSize):
                    beatmap.GridSize = int.Parse(value);
                    break;

                case nameof(beatmap.TimelineZoom):
                    beatmap.TimelineZoom = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                default:
                    break; // FIX: tolerate unknown/unsupported keys
            }
        }

        private void ParseMetadataSectionLine(OsuBeatmap beatmap, string line)
        {
            // FIX (AR-modification framework): split on the FIRST ':' only — titles/sources/tags
            // legitimately contain colons, and this also tolerates a missing value.
            var idx = line.IndexOf(':');
            if (idx < 0) return;
            var key = line.Substring(0, idx);
            var value = line.Substring(idx + 1);

            // AI generated
            switch (key)
            {
                case nameof(beatmap.Title):
                    beatmap.Title = value;
                    break;

                case nameof(beatmap.TitleUnicode):
                    beatmap.TitleUnicode = value;
                    break;

                case nameof(beatmap.Artist):
                    beatmap.Artist = value;
                    break;

                case nameof(beatmap.ArtistUnicode):
                    beatmap.ArtistUnicode = value;
                    break;

                case nameof(beatmap.Creator):
                    beatmap.Creator = value;
                    break;

                case nameof(beatmap.Version):
                    beatmap.Version = value;
                    break;

                case nameof(beatmap.Source):
                    beatmap.Source = value;
                    break;

                case nameof(beatmap.Tags):
                    beatmap.Tags = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                    break;

                case nameof(beatmap.BeatmapID):
                    beatmap.BeatmapID = int.Parse(value);
                    break;

                case nameof(beatmap.BeatmapSetID):
                    beatmap.BeatmapSetID = int.Parse(value);
                    break;

                default:
                    break; // FIX: tolerate unknown/unsupported keys
            }
        }

        private void ParseDifficultySectionLine(OsuBeatmap beatmap, string line)
        {
            var split = line.Split(':');
            if (split.Length < 2) return;
            var key = split[0];
            var value = split[1];

            switch (key)
            {
                case nameof(beatmap.HPDrainRate):
                    beatmap.HPDrainRate = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.CircleSize):
                    beatmap.CircleSize = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.OverallDifficulty):
                    beatmap.OverallDifficulty = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.ApproachRate):
                    beatmap.ApproachRate = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.SliderMultiplier):
                    beatmap.SliderMultiplier = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case nameof(beatmap.SliderTickRate):
                    beatmap.SliderTickRate = double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                default:
                    break; // FIX: tolerate unknown/unsupported keys
            }
        }

        private void ParseEventsSectionLine(OsuBeatmap beatmap, string line)
        {
            var split = line.Split(',');
            var eventType = split[0];

            // TODO: Support storyboard events
            switch (eventType)
            {
                case "0":
                case "Background":
                    ParseBackgroundEventLine(beatmap, split);
                    break;
                case "1":
                case "Video":
                    ParseVideoEventLine(beatmap, split);
                    break;
                case "2":
                case "Break":
                    ParseBreakEventLine(beatmap, split);
                    break;
            }
        }

        // FIX (AR-modification framework): reads a field with a fallback so optional/missing
        // fields don't throw. In the .osu format the X/Y offsets on background & video events are
        // optional (e.g. `0,0,"bg.jpg"` has no offsets), which crashed the fixed-index parsers on
        // real maps (deltaMAX's video line).
        private static int SafeInt(string[] arr, int i, int fallback)
            => i < arr.Length && int.TryParse(arr[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        private void ParseBackgroundEventLine(OsuBeatmap beatmap, string[] splitLine)
        {
            if (splitLine.Length < 3) return;
            var background = new OsuBackground()
            {
                StartTime = SafeInt(splitLine, 1, 0),
                Filename = splitLine[2],
                XOffset = SafeInt(splitLine, 3, 0),
                YOffset = SafeInt(splitLine, 4, 0)
            };

            beatmap.Events.Add(background);
        }

        private void ParseVideoEventLine(OsuBeatmap beatmap, string[] splitLine)
        {
            if (splitLine.Length < 3) return;
            var video = new OsuVideo()
            {
                StartTime = SafeInt(splitLine, 1, 0),
                Filename = splitLine[2],
                XOffset = SafeInt(splitLine, 3, 0),
                YOffset = SafeInt(splitLine, 4, 0)
            };

            beatmap.Events.Add(video);
        }

        private void ParseBreakEventLine(OsuBeatmap beatmap, string[] splitLine)
        {
            if (splitLine.Length < 3) return;
            var @break = new OsuBreak()
            {
                StartTime = SafeInt(splitLine, 1, 0),
                EndTime = SafeInt(splitLine, 2, 0)
            };

            beatmap.Events.Add(@break);
        }

        private void ParseTimingPointsSectionLine(OsuBeatmap beatmap, string line)
        {
            // FIX (AR-modification framework): only "time,beatLength" are required in the .osu
            // spec; meter/sampleSet/sampleIndex/volume/uninherited/effects are all optional and
            // are omitted by older or hand-edited maps (very common on variable-BPM maps with many
            // timing points). The original code hard-indexed split[7]/[6]/... and threw
            // "Index was outside the bounds of the array" on any short line. Read every field
            // defensively with osu!'s default fallbacks, and infer uninherited from the beatLength
            // sign when the flag is absent (positive = uninherited red line, negative = inherited).
            var split = line.Split(',');
            if (split.Length < 2) return; // not a usable timing point

            double GetDouble(int i, double fallback) =>
                i < split.Length && double.TryParse(split[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                    ? v : fallback;
            int GetInt(int i, int fallback) =>
                i < split.Length && int.TryParse(split[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                    ? v : fallback;

            double beatLength = GetDouble(1, 0);
            bool uninherited = split.Length > 6 ? GetInt(6, 1) == 1 : beatLength >= 0;
            int effects = GetInt(7, 0);

            var timingPoint = new OsuTimingPoint()
            {
                Time = (int)Math.Round(GetDouble(0, 0)),  // some maps use fractional timing-point times
                BeatLength = beatLength,
                Meter = GetInt(2, 4),
                SampleSet = (OsuSampleSet)GetInt(3, 0),
                SampleIndex = GetInt(4, 0),
                Volume = GetInt(5, 100),
                Uninherited = uninherited,
                IsKiaiTime = (effects & 1) == 1,
                OmitFirstBarLine = (effects & 8) != 0
            };

            beatmap.TimingPoints.Add(timingPoint);
        }

        private void ParseColoursSectionLine(OsuBeatmap beatmap, string line)
        {
            var split = line.Split(" : ");
            if (split.Length < 2) return;
            var key = split[0];
            var value = split[1];

            var colorValues = value.Split(',');
            if (colorValues.Length < 3) return; // malformed colour line — skip rather than crash
            byte cv(int i) => byte.TryParse(colorValues[i].Trim(), out var b) ? b : (byte)0;
            var color = new Color()
            {
                Red = cv(0),
                Green = cv(1),
                Blue = cv(2)
            };

            // I'm assuming that osu! looks at the order within the .osu file rather than actual combo numbers
            // It's a possible bug if that assumption is wrong, but it's very minor and unlikely
            if (key[..5] == "Combo")
            {
                beatmap.ComboColors.Add(color);
                return;
            }

            switch (key)
            {
                case nameof(beatmap.SliderTrackOverride):
                    beatmap.SliderTrackOverride = color;
                    break;

                case nameof(beatmap.SliderBorder):
                    beatmap.SliderBorder = color;
                    break;
            }
        }

        private void ParseHitObjectsSectionLine(OsuBeatmap beatmap, string line)
        {
            // New combo information or mania holds are not relevant for our purposes
            const int circleTypeFlag = 1 << 0;
            const int sliderTypeFlag = 1 << 1;
            const int spinnerTypeFlag = 1 << 3;

            var split = line.Split(',');
            var type = int.Parse(split[3]);

            if ((type & circleTypeFlag) != 0)
                ParseCircleHitObjectLine(beatmap, split);
            else if ((type & sliderTypeFlag) != 0)
                ParseSliderHitObjectLine(beatmap, split);
            else if ((type & spinnerTypeFlag) != 0)
                ParseSpinnerHitObjectLine(beatmap, split);
        }

        private void ParseCircleHitObjectLine(OsuBeatmap beatmap, string[] splitLine)
        {
            var hitSample = ParseHitSample(splitLine[5]);

            var circle = new OsuCircle()
            {
                X = int.Parse(splitLine[0]),
                Y = int.Parse(splitLine[1]),
                Time = int.Parse(splitLine[2]),
                HitSound = (OsuHitSound)int.Parse(splitLine[4]),
                HitSample = hitSample
            };

            SetComboFlags(circle, splitLine);
            beatmap.HitObjects.Add(circle);
        }

        private void ParseSliderHitObjectLine(OsuBeatmap beatmap, string[] splitLine)
        {
            var curveInfoString = splitLine[5];
            var splitCurveInfo = curveInfoString.Split('|');
            var curveType = splitCurveInfo[0];
            var splitCurvePoints = splitCurveInfo[1..];
            var curvePoints = splitCurvePoints.Select(str =>
            {
                var split = str.Split(':');

                var curvePoint = new Point()
                {
                    X = int.Parse(split[0]),
                    Y = int.Parse(split[1])
                };

                return curvePoint;
            }).ToList();

            var slider = new OsuSlider()
            {
                X = int.Parse(splitLine[0]),
                Y = int.Parse(splitLine[1]),
                Time = int.Parse(splitLine[2]),
                HitSound = (OsuHitSound)int.Parse(splitLine[4]),
                CurveType = (OsuCurveType)char.Parse(curveType),
                CurvePoints = curvePoints,
                Slides = int.Parse(splitLine[6]),
                Length = double.Parse(splitLine[7])
            };

            if (splitLine.Length > 8)
            {
                var edgeSoundsString = splitLine[8];
                var splitEdgeSounds = edgeSoundsString.Split('|');
                var edgeSounds = splitEdgeSounds.Select(edgeSound => (OsuHitSound)int.Parse(edgeSound)).ToList();

                var edgeSetsString = splitLine[9];
                var splitEdgeSets = edgeSetsString.Split('|');
                var edgeSets = splitEdgeSets.Select(str =>
                {
                    var split = str.Split(':');

                    var edgeSet = new OsuEdgeSet()
                    {
                        NormalSet = (OsuSampleSet)int.Parse(split[0]),
                        AdditionSet = (OsuSampleSet)int.Parse(split[1])
                    };

                    return edgeSet;
                }).ToList();

                var hitSample = ParseHitSample(splitLine[10]);

                slider.EdgeSets = edgeSets;
                slider.EdgeSounds = edgeSounds;
                slider.HitSample = hitSample;
            }

            SetComboFlags(slider, splitLine);
            beatmap.HitObjects.Add(slider);
        }

        private void ParseSpinnerHitObjectLine(OsuBeatmap beatmap, string[] splitLine)
        {
            var hitSample = ParseHitSample(splitLine[6]);

            var circle = new OsuSpinner()
            {
                X = int.Parse(splitLine[0]),
                Y = int.Parse(splitLine[1]),
                Time = int.Parse(splitLine[2]),
                HitSound = (OsuHitSound)int.Parse(splitLine[4]),
                EndTime = int.Parse(splitLine[5]),
                HitSample = hitSample
            };

            SetComboFlags(circle, splitLine);
            beatmap.HitObjects.Add(circle);
        }

        private static OsuHitSample ParseHitSample(string hitSampleString)
        {
            var splitHitSample = hitSampleString.Split(':');

            var hitSample = new OsuHitSample()
            {
                NormalSet = (OsuSampleSet)int.Parse(splitHitSample[0]),
                AdditionSet = (OsuSampleSet)int.Parse(splitHitSample[1]),
                Index = int.Parse(splitHitSample[2]),
                Volume = int.Parse(splitHitSample[3]),
                Filename = splitHitSample[4]
            };

            return hitSample;
        }

        // FIX (AR-modification framework): .osu stores booleans as 0/1, not true/false.
        private static bool ParseOsuBool(string value)
        {
            value = value.Trim();
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // FIX (AR-modification framework): the original deserializer discarded the new-combo
        // flag ("not relevant for our purposes"). osu!catch colours fruits by combo, so we
        // recover the new-combo bit (0x4) and the combo-colour skip count (bits 4-6) here.
        private static void SetComboFlags(OsuHitObject hitObject, string[] splitLine)
        {
            var type = int.Parse(splitLine[3]);
            hitObject.IsNewCombo = (type & 4) != 0;
            hitObject.ComboColorSkipAmount = (type >> 4) & 7;
        }

        private List<string> PreprocessSerializedLines(List<string> serializedLines)
        {
            var filteredLines =
                serializedLines
                .Where(str => !str.StartsWith("//") && !string.IsNullOrWhiteSpace(str))
                .Select(str => str.Trim())
                .ToList();

            return filteredLines;
        }
    }
}
