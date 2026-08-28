using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrusVortex.Common;

namespace CitrusVortex.Targets.Osu.Conversion
{
    public class OsuCatchToOsuV14BeatmapConverter : IOsuCatchToOsuBeatmapConverter
    {
        private const int _DEFAULT_SPINNER_X_POSITION = 256;
        private const int _DEFAULT_Y_POSITION = 192;

        // 600 BPM is chosen as a reference point here since sliders travel at 1 px/ms at that tempo. Any other choice is valid
        private const double _600BPM_BEAT_LENGTH = 100d;
        private const double _OSU_600BPM_SLIDER_VELOCITY = 1d;

        private const double _OSU_MIN_SLIDER_VELOCITY_MULTIPLIER = 0.1d;
        private const double _OSU_MAX_SLIDER_VELOCITY_MULTIPLIER = 10d;
        private const double _OSU_MAX_BASE_SLIDER_MULTIPLIER = 3.6d;

        public OsuBeatmap Convert(OsuCatchBeatmap catchBeatmap)
        {
            var beatmap = new OsuBeatmap();

            beatmap.SliderMultiplier = _OSU_MAX_BASE_SLIDER_MULTIPLIER;

            ConvertSharedData(beatmap, catchBeatmap);
            CreateTimingPoints(beatmap, catchBeatmap);

            var rng = new OsuRandomNumberGenerator();

            foreach (var hitObject in catchBeatmap.HitObjects)
            {
                switch (hitObject)
                {
                    case OsuCatchFruit fruit:
                        ConvertAndAddFruit(beatmap, fruit);
                        break;

                    case OsuCatchJuiceStream juiceStream:
                        ConvertAndAddJuiceStream(beatmap, catchBeatmap, juiceStream);
                        break;

                    case OsuCatchBananaShower bananaShower:
                        ConvertAndAddBananaShower(beatmap, bananaShower);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return beatmap;
        }

        // Mostly AI generated
        private void ConvertSharedData(OsuBeatmap beatmap, OsuCatchBeatmap catchBeatmap)
        {
            beatmap.FileFormatVersion = catchBeatmap.FileFormatVersion;

            // [General]
            beatmap.AudioFilename = catchBeatmap.AudioFilename;
            beatmap.AudioLeadIn = catchBeatmap.AudioLeadIn;
            //beatmap.AudioHash = catchBeatmap.AudioHash;
            beatmap.PreviewTime = catchBeatmap.PreviewTime;
            beatmap.Countdown = catchBeatmap.Countdown;
            beatmap.SampleSet = catchBeatmap.SampleSet;
            beatmap.StackLeniency = catchBeatmap.StackLeniency;
            beatmap.Mode = catchBeatmap.Mode;
            beatmap.LetterboxInBreaks = catchBeatmap.LetterboxInBreaks;
            //beatmap.StoryFireInFront = catchBeatmap.StoryFireInFront;
            beatmap.UseSkinSprites = catchBeatmap.UseSkinSprites;
            //beatmap.AlwaysShowPlayfield = catchBeatmap.AlwaysShowPlayfield;
            beatmap.OverlayPosition = catchBeatmap.OverlayPosition;
            beatmap.SkinPreference = catchBeatmap.SkinPreference;
            beatmap.EpilepsyWarning = catchBeatmap.EpilepsyWarning;
            beatmap.CountdownOffset = catchBeatmap.CountdownOffset;
            beatmap.SpecialStyle = catchBeatmap.SpecialStyle;
            beatmap.WidescreenStoryboard = catchBeatmap.WidescreenStoryboard;
            beatmap.SamplesMatchPlaybackRate = catchBeatmap.SamplesMatchPlaybackRate;

            // [Editor]
            beatmap.Bookmarks = new List<int>(catchBeatmap.Bookmarks);
            beatmap.DistanceSpacing = catchBeatmap.DistanceSpacing;
            beatmap.BeatDivisor = catchBeatmap.BeatDivisor;
            beatmap.GridSize = catchBeatmap.GridSize;
            beatmap.TimelineZoom = catchBeatmap.TimelineZoom;

            // [Metadata]
            beatmap.Title = catchBeatmap.Title;
            beatmap.TitleUnicode = catchBeatmap.TitleUnicode;
            beatmap.Artist = catchBeatmap.Artist;
            beatmap.ArtistUnicode = catchBeatmap.ArtistUnicode;
            beatmap.Creator = catchBeatmap.Creator;
            beatmap.Version = catchBeatmap.Version;
            beatmap.Source = catchBeatmap.Source;
            beatmap.Tags = new List<string>(catchBeatmap.Tags);
            beatmap.BeatmapID = catchBeatmap.BeatmapID;
            beatmap.BeatmapSetID = catchBeatmap.BeatmapSetID;

            // [Difficulty]
            beatmap.HPDrainRate = catchBeatmap.HPDrainRate;
            beatmap.CircleSize = catchBeatmap.CircleSize;
            beatmap.OverallDifficulty = catchBeatmap.OverallDifficulty;
            beatmap.ApproachRate = catchBeatmap.ApproachRate;
            //beatmap.SliderMultiplier = catchBeatmap.SliderMultiplier;
            beatmap.SliderTickRate = catchBeatmap.SliderTickRate;

            // [Events]
            beatmap.Events = new List<OsuEvent>(catchBeatmap.Events);

            // [Colours]
            beatmap.ComboColors = new List<Color>(catchBeatmap.ComboColors);
            beatmap.SliderTrackOverride = catchBeatmap.SliderTrackOverride;
            beatmap.SliderBorder = catchBeatmap.SliderBorder;
        }

        private double GetBaseSliderVelocity(OsuTimingPoint timingPoint, double sliderMultiplier)
        {
            return _OSU_600BPM_SLIDER_VELOCITY * (_600BPM_BEAT_LENGTH / timingPoint.BeatLength) * sliderMultiplier;
        }

        private void CreateTimingPoints(OsuBeatmap beatmap, OsuCatchBeatmap catchBeatmap)
        {
            var juiceStreams = 
                catchBeatmap.HitObjects
                .OfType<OsuCatchJuiceStream>()
                .ToList();

            var uninheritedTimingPoints =
                catchBeatmap.TimingPoints
                .Where(timingPoint => timingPoint.Uninherited)
                .ToList();

            var inheritedTimingPoints =
                catchBeatmap.TimingPoints
                .Where(timingPoint => !timingPoint.Uninherited)
                .ToList();

            var juiceStreamsByTimingPoint = new List<(OsuTimingPoint TimingPoint, List<OsuCatchJuiceStream> JuiceStreams, List<OsuTimingPoint> InheritedTimingPoints)>();

            if (uninheritedTimingPoints.Count == 1)
            {
                juiceStreamsByTimingPoint.Add((uninheritedTimingPoints[0], juiceStreams, inheritedTimingPoints));
            }
            else
            {
                for (int i = 1; i < catchBeatmap.TimingPoints.Count; i++)
                {
                    var timingPoint = catchBeatmap.TimingPoints[i - 1];
                    var nextTimingPoint = catchBeatmap.TimingPoints[i];

                    var filteredJuiceStreams =
                        juiceStreams
                        .Where(juiceStream => juiceStream.Time >= timingPoint.Time && juiceStream.Time < nextTimingPoint.Time)
                        .ToList();

                    var filteredInheritedTimingPoints =
                        inheritedTimingPoints
                        .Where(inheritedTimingPoint => inheritedTimingPoint.Time >= timingPoint.Time && inheritedTimingPoint.Time < nextTimingPoint.Time)
                        .ToList();

                    juiceStreamsByTimingPoint.Add((timingPoint, filteredJuiceStreams, filteredInheritedTimingPoints));
                }
            }

            for (int timingPointIndex = 0; timingPointIndex < juiceStreamsByTimingPoint.Count; timingPointIndex++)
            {
                var (timingPoint, juiceStreamGroup, inheritedTimingPointGroup) = juiceStreamsByTimingPoint[timingPointIndex];

                if (juiceStreamGroup.Count == 0)
                    continue;

                var baseSliderVelocity = GetBaseSliderVelocity(timingPoint, catchBeatmap.SliderMultiplier);
                var maxRequiredVelocityMultiplier = _OSU_MIN_SLIDER_VELOCITY_MULTIPLIER;

                foreach (var juiceStream in juiceStreamGroup)
                {
                    for (int i = 1; i < juiceStream.Components.Count; i++)
                    {
                        var component = juiceStream.Components[i - 1];
                        var nextComponent = juiceStream.Components[i];

                        var requiredVelocity = (double)Math.Abs(nextComponent.X - component.X) / (nextComponent.Time - component.Time);
                        var requiredVelocityMultiplier = requiredVelocity / baseSliderVelocity;

                        if (requiredVelocityMultiplier > maxRequiredVelocityMultiplier)
                            maxRequiredVelocityMultiplier = requiredVelocityMultiplier;
                    }
                }

                if (maxRequiredVelocityMultiplier > _OSU_MAX_SLIDER_VELOCITY_MULTIPLIER)
                    throw new NotSupportedException();

                // We know there's at least one element in the list thanks to the check at the beginning, we don't have to use FirstOrDefault
                var firstJuiceStream = juiceStreamGroup.First();
                var firstInheritedTimingPoint = inheritedTimingPointGroup.FirstOrDefault();

                beatmap.TimingPoints.Add(timingPoint);

                if (firstInheritedTimingPoint is null || firstInheritedTimingPoint.Time > firstJuiceStream.Time)
                {
                    var newInheritedTimingPoint = new OsuTimingPoint()
                    {
                        Time = timingPoint.Time,
                        BeatLength = -100 / maxRequiredVelocityMultiplier,
                        Meter = timingPoint.Meter,
                        SampleSet = timingPoint.SampleSet,
                        SampleIndex = timingPoint.SampleIndex,
                        Volume = timingPoint.Volume,
                        Uninherited = false,
                        IsKiaiTime = timingPoint.IsKiaiTime,
                        OmitFirstBarLine = timingPoint.OmitFirstBarLine
                    };

                    beatmap.TimingPoints.Add(newInheritedTimingPoint);
                }
            }
        }

        private void ConvertAndAddFruit(OsuBeatmap beatmap, OsuCatchFruit fruit)
        {
            var circle = new OsuCircle()
            {
                X = fruit.X,
                Y = _DEFAULT_Y_POSITION,
                Time = fruit.Time
            };

            beatmap.HitObjects.Add(circle);
        }

        private void ConvertAndAddJuiceStream(OsuBeatmap beatmap, OsuCatchBeatmap catchBeatmap, OsuCatchJuiceStream juiceStream)
        {
            // Every juice stream can be represented with a linear slider
            var slider = new OsuSlider()
            {
                X = juiceStream.X,
                Y = _DEFAULT_Y_POSITION,
                Time = juiceStream.Time,
                Slides = juiceStream.Components.Count(component => component is OsuCatchFruit) - 1,
                CurvePoints = [],
                CurveType = OsuCurveType.Linear,
                EdgeSets = [],
                EdgeSounds = []
            };

            var currentRedLine =
                beatmap.TimingPoints
                .Where(timingPoint => timingPoint.Uninherited && timingPoint.Time <= slider.Time)
                .MaxBy(timingPoint => timingPoint.Time);

            var currentGreenLine =
                beatmap.TimingPoints
                .Where(timingPoint => !timingPoint.Uninherited && timingPoint.Time <= slider.Time && timingPoint.Time >= currentRedLine.Time)
                .MaxBy(timingPoint => timingPoint.Time);

            var sliderVelocity = GetBaseSliderVelocity(currentRedLine, beatmap.SliderMultiplier);

            if (currentGreenLine is not null)
                sliderVelocity *= -100 / currentGreenLine.BeatLength;

            // Temporarily add the slider head to curve points to have slider.CurvePoints[^1] always return
            slider.CurvePoints.Add(new Point() { X = juiceStream.X, Y = _DEFAULT_Y_POSITION });

            var totalLength = 0d;
            var excessLength = 0d;

            // Even with excessLength as a correction term, rounding might misplace certain components by tiny amounts. Needs testing
            for (int i = 1; i < juiceStream.Components.Count; i++)
            {
                var previousComponent = juiceStream.Components[i - 1];
                var component = juiceStream.Components[i];

                var deltaX = component.X - previousComponent.X;
                var length = (component.Time - previousComponent.Time) * sliderVelocity + excessLength;
                var unroundedDeltaY = Math.Sqrt(length * length - deltaX * deltaX);
                var deltaY = Math.Round(unroundedDeltaY);

                totalLength += length;
                excessLength = length - Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                var lastCurvePoint = slider.CurvePoints[^1];
                var point = new Point() { X = lastCurvePoint.X + deltaX, Y = lastCurvePoint.Y + (int)deltaY };

                slider.CurvePoints.Add(point);
            }

            slider.CurvePoints.RemoveAt(0);

            slider.Length = totalLength;

            beatmap.HitObjects.Add(slider);
        }

        private void ConvertAndAddBananaShower(OsuBeatmap beatmap, OsuCatchBananaShower bananaShower)
        {
            var spinner = new OsuSpinner()
            {
                X = _DEFAULT_SPINNER_X_POSITION,
                Y = _DEFAULT_Y_POSITION,
                Time = bananaShower.Time,
                EndTime = bananaShower.EndTime
            };

            beatmap.HitObjects.Add(spinner);
        }
    }
}
