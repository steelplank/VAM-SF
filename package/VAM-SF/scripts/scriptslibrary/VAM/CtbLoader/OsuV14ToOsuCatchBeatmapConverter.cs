using CtbLoader.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CtbLoader.Targets.Osu.Conversion
{
    // A lot of the code here is copied without any optimization
    // When optimizing or refactoring, ensure that any rounding errors are consistent with osu!
    public class OsuV14ToOsuCatchBeatmapConverter : IOsuToOsuCatchBeatmapConverter
    {
        private const int _OSU_PLAYFIELD_WIDTH = 512;
        private const int _MAX_TINY_DROPLET_OFFSET = 20;

        public OsuCatchBeatmap Convert(OsuBeatmap beatmap)
        {
            var catchBeatmap = new OsuCatchBeatmap();

            ConvertSharedData(catchBeatmap, beatmap);

            var rng = new OsuRandomNumberGenerator();

            foreach (var hitObject in beatmap.HitObjects)
            {
                switch (hitObject)
                {
                    case OsuCircle circle:
                        ConvertAndAddCircle(catchBeatmap, circle);
                        break;
                    
                    case OsuSlider slider:
                        ConvertAndAddSlider(catchBeatmap, beatmap, slider, rng);
                        break;

                    case OsuSpinner spinner:
                        ConvertAndAddSpinner(catchBeatmap, spinner, rng);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return catchBeatmap;
        }

        // Mostly AI generated
        private void ConvertSharedData(OsuCatchBeatmap catchBeatmap, OsuBeatmap beatmap)
        {
            catchBeatmap.FileFormatVersion = beatmap.FileFormatVersion;

            // [General]
            catchBeatmap.AudioFilename = beatmap.AudioFilename;
            catchBeatmap.AudioLeadIn = beatmap.AudioLeadIn;
            catchBeatmap.AudioHash = beatmap.AudioHash;
            catchBeatmap.PreviewTime = beatmap.PreviewTime;
            catchBeatmap.Countdown = beatmap.Countdown;
            catchBeatmap.SampleSet = beatmap.SampleSet;
            catchBeatmap.StackLeniency = beatmap.StackLeniency;
            catchBeatmap.Mode = beatmap.Mode;
            catchBeatmap.LetterboxInBreaks = beatmap.LetterboxInBreaks;
            catchBeatmap.StoryFireInFront = beatmap.StoryFireInFront;
            catchBeatmap.UseSkinSprites = beatmap.UseSkinSprites;
            catchBeatmap.AlwaysShowPlayfield = beatmap.AlwaysShowPlayfield;
            catchBeatmap.OverlayPosition = beatmap.OverlayPosition;
            catchBeatmap.SkinPreference = beatmap.SkinPreference;
            catchBeatmap.EpilepsyWarning = beatmap.EpilepsyWarning;
            catchBeatmap.CountdownOffset = beatmap.CountdownOffset;
            catchBeatmap.SpecialStyle = beatmap.SpecialStyle;
            catchBeatmap.WidescreenStoryboard = beatmap.WidescreenStoryboard;
            catchBeatmap.SamplesMatchPlaybackRate = beatmap.SamplesMatchPlaybackRate;

            // [Editor]
            catchBeatmap.Bookmarks = new List<int>(beatmap.Bookmarks);
            catchBeatmap.DistanceSpacing = beatmap.DistanceSpacing;
            catchBeatmap.BeatDivisor = beatmap.BeatDivisor;
            catchBeatmap.GridSize = beatmap.GridSize;
            catchBeatmap.TimelineZoom = beatmap.TimelineZoom;

            // [Metadata]
            catchBeatmap.Title = beatmap.Title;
            catchBeatmap.TitleUnicode = beatmap.TitleUnicode;
            catchBeatmap.Artist = beatmap.Artist;
            catchBeatmap.ArtistUnicode = beatmap.ArtistUnicode;
            catchBeatmap.Creator = beatmap.Creator;
            catchBeatmap.Version = beatmap.Version;
            catchBeatmap.Source = beatmap.Source;
            catchBeatmap.Tags = new List<string>(beatmap.Tags);
            catchBeatmap.BeatmapID = beatmap.BeatmapID;
            catchBeatmap.BeatmapSetID = beatmap.BeatmapSetID;

            // [Difficulty]
            catchBeatmap.HPDrainRate = beatmap.HPDrainRate;
            catchBeatmap.CircleSize = beatmap.CircleSize;
            catchBeatmap.OverallDifficulty = beatmap.OverallDifficulty;
            catchBeatmap.ApproachRate = beatmap.ApproachRate;
            catchBeatmap.SliderMultiplier = beatmap.SliderMultiplier;
            catchBeatmap.SliderTickRate = beatmap.SliderTickRate;

            // [Events]
            catchBeatmap.Events = new List<OsuEvent>(beatmap.Events);

            // [TimingPoints]
            catchBeatmap.TimingPoints = beatmap.TimingPoints;

            // [Colours]
            catchBeatmap.ComboColors = new List<Color>(beatmap.ComboColors);
            catchBeatmap.SliderTrackOverride = beatmap.SliderTrackOverride;
            catchBeatmap.SliderBorder = beatmap.SliderBorder;
        }

        private void ConvertAndAddCircle(OsuCatchBeatmap catchBeatmap, OsuCircle circle)
        {
            var fruit = new OsuCatchFruit()
            {
                X = circle.X,
                Time = circle.Time
            };

            catchBeatmap.HitObjects.Add(fruit);
        }

        private void ConvertAndAddSlider(OsuCatchBeatmap catchBeatmap, OsuBeatmap beatmap, OsuSlider slider, OsuRandomNumberGenerator rng)
        {
            var juiceStream = new OsuCatchJuiceStream()
            {
                X = slider.X,
                Time = slider.Time
            };

            var firstNote = new OsuCatchFruit()
            {
                X = juiceStream.X,
                Time = juiceStream.Time
            };

            juiceStream.Components.Add(firstNote);

            var sliderPath = OsuSliderPathGenerator.GetPath(slider);

            var fruitAndDropletTimes = GetFruitAndDropletTimes(beatmap, slider, sliderPath, out var sliderEndTime, out var partialLengths);
            var fruitsAndDropletsPerSlide = fruitAndDropletTimes.Count / slider.Slides;
            var sliderRepeatTimes = new List<int>();

            for (int i = 1; i < fruitAndDropletTimes.Count; i++)
            {
                if (i % fruitsAndDropletsPerSlide == 0)
                    sliderRepeatTimes.Add(fruitAndDropletTimes[i - 1]);
            }

            var repeatFruitPosition = sliderPath[^1];

            var lastTime = slider.Time;

            // IMPORTANT:
            // Actually, osu! stores objects' positions as floats despite not allowing the user to place them on non-integer coordinates
            // For now, I'm casting these floats as ints. If this causes any inconsistencies, I will take a deeper look into osu!'s behavior
            for (int i = 0; i < fruitAndDropletTimes.Count; i++)
            {
                var time = fruitAndDropletTimes[i];
                var timeDifference = (float)(time - lastTime);

                if (timeDifference > 80)
                {
                    while (timeDifference > 100)
                        timeDifference /= 2;

                    for (float tinyDropletTime = lastTime + timeDifference; tinyDropletTime < time; tinyDropletTime += timeDifference)
                    {
                        Vector2 positionAtTime = GetPositionAtTime(slider, sliderPath, sliderEndTime, partialLengths, (int)tinyDropletTime);

                        var tinyDroplet = new OsuCatchTinyDroplet()
                        {
                            X = (int)positionAtTime.X,
                            XOffset = rng.Next(-_MAX_TINY_DROPLET_OFFSET, _MAX_TINY_DROPLET_OFFSET),
                            Time = (int)tinyDropletTime
                        };

                        juiceStream.Components.Add(tinyDroplet);
                    }
                }

                lastTime = time;

                if (i == fruitAndDropletTimes.Count - 1)
                    continue;

                var repeatLocation = sliderRepeatTimes.BinarySearch(time);

                if (repeatLocation >= 0)
                {
                    var fruit = new OsuCatchFruit()
                    {
                        X = repeatLocation % 2 == 1 ? slider.X : (int)repeatFruitPosition.X,
                        Time = time
                    };

                    juiceStream.Components.Add(fruit);
                }
                else
                {
                    var positionAtTime = GetPositionAtTime(slider, sliderPath, sliderEndTime, partialLengths, time);

                    var droplet = new OsuCatchDroplet()
                    {
                        X = (int)positionAtTime.X,
                        Time = time
                    };

                    // Burn a random number to match osu!'s behavior
                    _ = rng.NextUInt();

                    juiceStream.Components.Add(droplet);
                }
            }

            // FIX (AR-modification framework): a slider with an EVEN number of slides ends back
            // at its START, not at the path end. The original always used the path end
            // (repeatFruitPosition), so reverse sliders put their final fruit on the wrong side.
            var lastFruit = new OsuCatchFruit()
            {
                X = (slider.Slides % 2 == 1) ? (int)repeatFruitPosition.X : slider.X,
                Time = sliderEndTime
            };

            juiceStream.Components.Add(lastFruit);

            catchBeatmap.HitObjects.Add(juiceStream);
        }

        private Vector2 GetPositionAtTime(OsuSlider slider, List<Vector2> sliderPath, int sliderEndTime, List<double> partialLengths, int time)
        {
            if (time < slider.Time || time > sliderEndTime)
                return new Vector2(slider.X, slider.Y);

            var slideProgress = (time - slider.Time) / ((float)(sliderEndTime - slider.Time) / slider.Slides);

            if (slideProgress % 2 > 1)
                slideProgress = 1 - (slideProgress % 1);
            else
                slideProgress %= 1;

            var lengthAtProgress = (float)(slider.Length * slideProgress);

            return GetPositionAtLength(slider, sliderPath, partialLengths, lengthAtProgress);
        }

        private Vector2 GetPositionAtLength(OsuSlider slider, List<Vector2> sliderPath, List<double> partialLengths, float length)
        {
            if (sliderPath.Count == 0 || partialLengths.Count == 0)
                return new Vector2(slider.X, slider.Y);

            if (length == 0)
                return sliderPath[0];

            if (length >= partialLengths[^1])
                return sliderPath[^1];

            var index = partialLengths.BinarySearch(length);

            if (index < 0)
                index = Math.Min(~index, partialLengths.Count - 1);

            var lengthNext = partialLengths[index];
            var lengthPrevious = index != 0 ? partialLengths[index - 1] : 0;

            var result = sliderPath[index];

            if (lengthNext != lengthPrevious)
                result += (sliderPath[index + 1] - sliderPath[index]) * (float)((length - lengthPrevious) / (lengthNext - lengthPrevious));

            return result;
        }

        // I'm using LengthSquared() with Math.Sqrt instead of Length() and then casting to float in order to stay as close as possible to osu!'s implementation
        // The difference between using Math.Sqrt and MathF.Sqrt (like Vector2.Length() does) is extremely small,
        // but in rare cases it could be the cause of rounding inconsistencies
        private float GetOsuCompatibleDistance(Vector2 v1, Vector2 v2)
        {
            var differenceVector = v1 - v2;
            var lengthSquared = differenceVector.X * differenceVector.X + differenceVector.Y * differenceVector.Y;

            return (float)Math.Sqrt((double)lengthSquared);
        }

        // This function should never be this convoluted under normal circumstances
        // However, all steps are necessary to replicate the floating point error/rounding mess in osu!
        // The out parameters are not too related to the function itself, but they're still (for some reason) necessary outside of it
        // I'll leave those parameters be for now, this section will require a refactor later on in the project either way
        private List<int> GetFruitAndDropletTimes(OsuBeatmap beatmap, OsuSlider slider, List<Vector2> sliderPath, out int sliderEndTime, out List<double> partialLengths)
        {
            const double minSegmentLength = 0.0001d;

            var result = new List<int>();

            // TODO: Create a more performant implementation of timing point searching if needed
            var currentRedLine =
                beatmap.TimingPoints
                .Where(timingPoint => timingPoint.Uninherited && timingPoint.Time <= slider.Time)
                .MaxBy(timingPoint => timingPoint.Time)
                // FIX (AR-modification framework): a slider before the first red line found none ->
                // MaxBy returns null -> currentRedLine.Time/BeatLength crashed. osu! applies the
                // first red line to earlier objects, so fall back to it; if the map has no red line
                // at all, use a safe default beatLength instead of crashing.
                ?? beatmap.TimingPoints.Where(tp => tp.Uninherited).MinBy(tp => tp.Time)
                ?? new OsuTimingPoint { Time = 0, BeatLength = 500, Uninherited = true };

            var redLineTime = currentRedLine.Time;
            var currentGreenLine =
                beatmap.TimingPoints
                .Where(timingPoint => !timingPoint.Uninherited && timingPoint.Time <= slider.Time && timingPoint.Time >= redLineTime)
                .MaxBy(timingPoint => timingPoint.Time);

            var bpmMultiplier = 1d;
            var sliderScoringPointDistance = 100 * beatmap.SliderMultiplier / beatmap.SliderTickRate;

            if (currentGreenLine is not null)
            {
                bpmMultiplier = Math.Clamp((float)-currentGreenLine.BeatLength, 10f, 1000f) / 100d;
            }

            var sliderVelocity = sliderScoringPointDistance * beatmap.SliderTickRate * 1000f / (currentRedLine.BeatLength * bpmMultiplier);
            var tickDistance = sliderScoringPointDistance / bpmMultiplier;

            if (tickDistance > slider.Length)
                tickDistance = slider.Length;

            var totalUntrimmedLength = 0d;

            for (int i = 1; i < sliderPath.Count; i++)
            {
                var point = sliderPath[i];
                var previousPoint = sliderPath[i - 1];

                var length = GetOsuCompatibleDistance(point, previousPoint);
                totalUntrimmedLength += length;
            }

            var excess = totalUntrimmedLength - slider.Length;

            while (sliderPath.Count > 1)
            {
                var point = sliderPath[^1];
                var previousPoint = sliderPath[^2];

                var lastLineLength = GetOsuCompatibleDistance(point, previousPoint);

                if (lastLineLength > excess + minSegmentLength)
                {
                    if (point != previousPoint)
                    {
                        var differenceVector = point - previousPoint;
                        var inverseLength = 1f / GetOsuCompatibleDistance(point, previousPoint);
                        var normalizedVector = new Vector2(differenceVector.X * inverseLength, differenceVector.Y * inverseLength);

                        sliderPath[^1] = previousPoint + Vector2.Multiply(lastLineLength - (float)excess, normalizedVector);
                    }

                    break;
                }

                sliderPath.Remove(point);
                excess -= lastLineLength;
            }

            var totalLength = 0d;
            partialLengths = new List<double>();

            for (int i = 1; i < sliderPath.Count; i++)
            {
                var point = sliderPath[i];
                var previousPoint = sliderPath[i - 1];

                var length = GetOsuCompatibleDistance(point, previousPoint);
                totalLength += length;

                partialLengths.Add(totalLength);
            }

            var scoringLengthTotal = 0d;
            var currentTime = (double)slider.Time;
            // FIX (AR-modification framework): scoringDistance must PERSIST across slides. osu!
            // carries the leftover tick distance over each repeat so ticks on the way back land on
            // the SAME points as on the way out (mirrored). Resetting it to 0 per slide made reverse
            // droplets march continuously and sit on the wrong positions. (SliderOsu lines 980-989.)
            var scoringDistance = 0d;

            for (int i = 0; i < slider.Slides; i++)
            {
                var isReverse = (i & 1) == 1;
                var start = isReverse ? sliderPath.Count - 2 : 0;
                var end = isReverse ? -1 : sliderPath.Count - 1;
                var direction = isReverse ? -1 : 1;

                var distanceToEnd = totalLength;
                var minTickDistanceFromEnd = 0.01 * sliderVelocity;
                var skipTick = false;

                for (int j = start; j != end; j += direction)
                {
                    var distance = (float)(partialLengths[j] - (j == 0 ? 0 : partialLengths[j - 1]));

                    var duration = 1000f * distance / sliderVelocity;

                    scoringDistance += distance;
                    currentTime += duration;

                    while (scoringDistance >= tickDistance && !skipTick)
                    {
                        scoringLengthTotal += tickDistance;
                        scoringDistance -= tickDistance;
                        distanceToEnd -= tickDistance;

                        skipTick = distanceToEnd <= minTickDistanceFromEnd;

                        if (skipTick)
                            break;

                        result.Add((int)(slider.Time + (float)scoringLengthTotal / sliderVelocity * 1000));
                    }
                }

                scoringLengthTotal += scoringDistance;

                result.Add((int)(slider.Time + (float)scoringLengthTotal / sliderVelocity * 1000));

                // Carry the remainder to the next slide so reverse ticks mirror the forward ones.
                if (skipTick)
                    scoringDistance = 0;
                else
                {
                    scoringLengthTotal -= tickDistance - scoringDistance;
                    scoringDistance = tickDistance - scoringDistance;
                }
            }

            sliderEndTime = (int)currentTime;

            // I suspect this line is the reason why 1/2 slider generation is messed up
            if (result.Count > 0)
                result[^1] = Math.Max(slider.Time + (sliderEndTime - slider.Time) / 2, result[^1] - 36);

            return result;
        }

        private void ConvertAndAddSpinner(OsuCatchBeatmap catchBeatmap, OsuSpinner spinner, OsuRandomNumberGenerator rng)
        {
            var bananaShower = new OsuCatchBananaShower()
            {
                Time = spinner.Time,
                EndTime = spinner.EndTime
            };

            var timeDifference = spinner.EndTime - spinner.Time;

            if (timeDifference <= 0)
                return;

            while (timeDifference > 100)
                timeDifference /= 2;

            for (float time = spinner.Time; time <= spinner.EndTime; time += timeDifference)
            {
                var banana = new OsuCatchBanana()
                {
                    Time = (int)time,
                    X = rng.Next(_OSU_PLAYFIELD_WIDTH)
                };

                // osu! draws FOUR randoms per banana from this shared pool: X (above), then
                // type, scale, colour. Capturing scale as NextDouble consumes the same single
                // NextUInt the burn did, so X positions stay 1:1 with osu!. Type and colour are
                // still burned (VAM picks its own banana sprite/colour) to keep the stream synced.
                _ = rng.NextUInt();                            // banana type
                banana.ScaleStart = rng.NextDouble() * 1.6 + 0.6; // banana start scale 0.6..2.2 (spins to 0.6)
                _ = rng.NextUInt();                            // banana colour

                bananaShower.Bananas.Add(banana);
            }

            // FIX (AR-modification framework): the original code built the banana shower and
            // advanced the RNG for every banana, but never added the shower to the beatmap, so
            // banana showers were silently dropped. The RNG stream is already correct above, so
            // adding the shower here keeps banana positions 1:1 with osu!.
            catchBeatmap.HitObjects.Add(bananaShower);
        }
    }
}
