using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrusVortex.Targets.Osu.Serialization
{
    // Documentation: https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29
    public class OsuV14BeatmapSerializer : IOsuBeatmapSerializer
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

        public List<string> Serialize(OsuBeatmap beatmap)
        {
            // TODO: Estimate capacity once it's time for optimization
            var serializedLines = new List<string>();

            if (beatmap.FileFormatVersion != 14)
                throw new FormatException();

            serializedLines.Add(_VALID_HEADER);
            serializedLines.Add(Environment.NewLine);
            SerializeGeneralSection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeEditorSection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeMetadataSection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeDifficultySection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeEventsSection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeTimingPointsSection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeColoursSection(beatmap, serializedLines);
            serializedLines.Add(Environment.NewLine);
            SerializeHitObjectsSection(beatmap, serializedLines);

            return serializedLines;
        }

        private void SerializeGeneralSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_GENERAL_SECTION);

            const string separator = ": ";

            serializedLines.Add(nameof(beatmap.AudioFilename) + separator + beatmap.AudioFilename);
            serializedLines.Add(nameof(beatmap.AudioLeadIn) + separator + beatmap.AudioLeadIn);
            serializedLines.Add(nameof(beatmap.AudioHash) + separator + beatmap.AudioHash);
            serializedLines.Add(nameof(beatmap.PreviewTime) + separator + beatmap.PreviewTime);
            serializedLines.Add(nameof(beatmap.Countdown) + separator + beatmap.Countdown);
            serializedLines.Add(nameof(beatmap.SampleSet) + separator + beatmap.SampleSet);
            serializedLines.Add(nameof(beatmap.StackLeniency) + separator + beatmap.StackLeniency);
            serializedLines.Add(nameof(beatmap.Mode) + separator + beatmap.Mode);
            serializedLines.Add(nameof(beatmap.LetterboxInBreaks) + separator + beatmap.LetterboxInBreaks);
            serializedLines.Add(nameof(beatmap.StoryFireInFront) + separator + beatmap.StoryFireInFront);
            serializedLines.Add(nameof(beatmap.UseSkinSprites) + separator + beatmap.UseSkinSprites);
            serializedLines.Add(nameof(beatmap.AlwaysShowPlayfield) + separator + beatmap.AlwaysShowPlayfield);
            serializedLines.Add(nameof(beatmap.OverlayPosition) + separator + beatmap.OverlayPosition);
            serializedLines.Add(nameof(beatmap.SkinPreference) + separator + beatmap.SkinPreference);
            serializedLines.Add(nameof(beatmap.EpilepsyWarning) + separator + beatmap.EpilepsyWarning);
            serializedLines.Add(nameof(beatmap.CountdownOffset) + separator + beatmap.CountdownOffset);
            serializedLines.Add(nameof(beatmap.SpecialStyle) + separator + beatmap.SpecialStyle);
            serializedLines.Add(nameof(beatmap.WidescreenStoryboard) + separator + beatmap.WidescreenStoryboard);
            serializedLines.Add(nameof(beatmap.SamplesMatchPlaybackRate) + separator + beatmap.SamplesMatchPlaybackRate);
        }

        private void SerializeEditorSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_EDITOR_SECTION);

            const string separator = ": ";
            const string bookmarksSeparator = ",";

            serializedLines.Add(nameof(beatmap.Bookmarks) + separator + string.Join(bookmarksSeparator, beatmap.Bookmarks));
            serializedLines.Add(nameof(beatmap.DistanceSpacing) + separator + beatmap.DistanceSpacing);
            serializedLines.Add(nameof(beatmap.BeatDivisor) + separator + beatmap.BeatDivisor);
            serializedLines.Add(nameof(beatmap.GridSize) + separator + beatmap.GridSize);
            serializedLines.Add(nameof(beatmap.TimelineZoom) + separator + beatmap.TimelineZoom);
        }

        private void SerializeMetadataSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_METADATA_SECTION);

            const string separator = ":";
            const string tagsSeparator = " ";

            serializedLines.Add(nameof(beatmap.Title) + separator + beatmap.Title);
            serializedLines.Add(nameof(beatmap.TitleUnicode) + separator + beatmap.TitleUnicode);
            serializedLines.Add(nameof(beatmap.Artist) + separator + beatmap.Artist);
            serializedLines.Add(nameof(beatmap.ArtistUnicode) + separator + beatmap.ArtistUnicode);
            serializedLines.Add(nameof(beatmap.Creator) + separator + beatmap.Creator);
            serializedLines.Add(nameof(beatmap.Version) + separator + beatmap.Version);
            serializedLines.Add(nameof(beatmap.Source) + separator + beatmap.Source);
            serializedLines.Add(nameof(beatmap.Tags) + separator + string.Join(tagsSeparator, beatmap.Tags));
            serializedLines.Add(nameof(beatmap.BeatmapID) + separator + beatmap.BeatmapID);
            serializedLines.Add(nameof(beatmap.BeatmapSetID) + separator + beatmap.BeatmapSetID);
        }

        private void SerializeDifficultySection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_DIFFICULTY_SECTION);

            const string separator = ":";

            serializedLines.Add(nameof(beatmap.HPDrainRate) + separator + beatmap.HPDrainRate);
            serializedLines.Add(nameof(beatmap.CircleSize) + separator + beatmap.CircleSize);
            serializedLines.Add(nameof(beatmap.OverallDifficulty) + separator + beatmap.OverallDifficulty);
            serializedLines.Add(nameof(beatmap.ApproachRate) + separator + beatmap.ApproachRate);
            serializedLines.Add(nameof(beatmap.SliderMultiplier) + separator + beatmap.SliderMultiplier);
            serializedLines.Add(nameof(beatmap.SliderTickRate) + separator + beatmap.SliderTickRate);
        }

        private void SerializeEventsSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_EVENTS_SECTION);

            const string backgroundEventType = "0";
            const string videoEventType = "1";
            const string breakEventType = "2";

            const string separator = ",";

            foreach (var @event in beatmap.Events)
            {
                var serializedLine = @event switch
                {
                    OsuBackground background =>
                        string.Join(separator, backgroundEventType, background.StartTime, background.Filename, background.XOffset, background.YOffset),

                    OsuVideo video =>
                        string.Join(separator, videoEventType, video.StartTime, video.Filename, video.XOffset, video.YOffset),

                    OsuBreak @break =>
                        string.Join(separator, @breakEventType, @break.StartTime, @break.EndTime),

                    _ => throw new NotSupportedException()
                };

                serializedLines.Add(serializedLine);
            }
        }

        private void SerializeTimingPointsSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_TIMING_POINTS_SECTION);

            const string separator = ",";

            foreach (var timingPoint in beatmap.TimingPoints)
            {
                var effects = (timingPoint.IsKiaiTime ? 1 : 0) | ((timingPoint.OmitFirstBarLine ? 1 : 0) << 3);

                var serializedLine =
                    string.Join(separator,
                        timingPoint.Time,
                        timingPoint.BeatLength,
                        timingPoint.Meter,
                        timingPoint.SampleSet,
                        timingPoint.SampleIndex,
                        timingPoint.Volume,
                        timingPoint.Uninherited,
                        effects
                    );

                serializedLines.Add(serializedLine);
            }
        }

        private void SerializeColoursSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_COLOURS_SECTION);

            const string separator = " : ";
            const string comboColorSeparator = ",";
            const string comboColorKeyFormat = "Combo{0}";

            for (int i = 0; i < beatmap.ComboColors.Count; i++)
            {
                var comboColor = beatmap.ComboColors[i];
                var serializedComboColor =
                    string.Join(comboColorSeparator, comboColor.Red, comboColor.Green, comboColor.Blue);

                serializedLines.Add(string.Format(comboColorKeyFormat, i + 1) + separator + serializedComboColor);
            }

            // SliderTrackOverride and SliderTrackBorder are not relevant for our purposes, can be skipped for now
        }

        private void SerializeHitObjectsSection(OsuBeatmap beatmap, List<string> serializedLines)
        {
            serializedLines.Add(_HIT_OBJECTS_SECTION);

            foreach (var hitObject in beatmap.HitObjects)
            {
                var serializedLine = hitObject switch
                {
                    OsuCircle circle =>
                        GetSerializedCircle(circle),

                    OsuSlider slider =>
                        GetSerializedSlider(slider),

                    OsuSpinner spinner =>
                        GetSerializedSpinner(spinner),

                    _ => throw new NotSupportedException()
                };

                serializedLines.Add(serializedLine);
            }
        }

        private string GetSerializedHitSample(OsuHitSample hitSample)
        {
            if (hitSample == null)
                return "0:0:0:0:";

            return $"{hitSample.NormalSet}:{hitSample.AdditionSet}:{hitSample.Index}:{hitSample.Volume}:{hitSample.Filename}";
        }

        private string GetSerializedCircle(OsuCircle circle)
        {
            const int circleTypeBits = 1;

            const string separator = ",";

            var newComboBits = (circle.IsNewCombo ? 1 : 0) << 2;
            var comboColorSkipBits = circle.ComboColorSkipAmount << 4;
            var type = circleTypeBits | newComboBits | comboColorSkipBits;

            return string.Join(separator,
                circle.X,
                circle.Y,
                circle.Time,
                type,
                circle.HitSound,
                GetSerializedHitSample(circle.HitSample)
            );
        }

        private string GetSerializedSlider(OsuSlider slider)
        {
            const int sliderTypeBits = 2;

            const string separator = ",";
            const string curvePointSeparator = "|";
            const string edgeSoundSeparator = "|";
            const string edgeSetSeparator = "|";

            var newComboBits = (slider.IsNewCombo ? 1 : 0) << 2;
            var comboColorSkipBits = slider.ComboColorSkipAmount << 4;
            var type = sliderTypeBits | newComboBits | comboColorSkipBits;

            // I should handle the case where the slider has no curve points in the future, leaving for now
            var curvePoints = string.Join(curvePointSeparator, slider.CurvePoints.Select(p => $"{p.X}:{p.Y}"));
            var curveInfo = slider.CurveType + curvePointSeparator + curvePoints;

            var edgeSounds = slider.EdgeSounds.Count > 0
                ? string.Join(edgeSoundSeparator, slider.EdgeSounds)
                : "0";

            var edgeSets = slider.EdgeSets.Count > 0
                ? string.Join(edgeSetSeparator, slider.EdgeSets.Select(edgeSet => $"{edgeSet.NormalSet}:{edgeSet.AdditionSet}"))
                : "0:0";

            return string.Join(separator,
                slider.X,
                slider.Y,
                slider.Time,
                type,
                slider.HitSound,
                curveInfo,
                slider.Slides,
                slider.Length,
                edgeSounds,
                edgeSets,
                GetSerializedHitSample(slider.HitSample)
            );
        }

        private string GetSerializedSpinner(OsuSpinner spinner)
        {
            const int spinnerTypeBits = 8;

            const string separator = ",";

            return string.Join(separator,
                spinner.X,
                spinner.Y,
                spinner.Time,
                spinnerTypeBits,
                spinner.HitSound,
                spinner.EndTime,
                GetSerializedHitSample(spinner.HitSample)
            );
        }
    }
}
