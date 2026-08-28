using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CitrusVortex.Targets.Osu;
using CitrusVortex.Targets.Osu.Conversion;
using CitrusVortex.Targets.Osu.Serialization;
using CvColor = CitrusVortex.Common.Color;

namespace StorybrewScripts.Vam
{
    // Loads a .osu file, runs it through CitrusVortex (osu! -> osu!catch), and flattens
    // the result into the framework's own List<VamObject>, complete with combo colours
    // and hyperdash flags. This is the single bridge between CitrusVortex and the effects.
    public static class VamLoader
    {
        // osu! stable default combo colours (used only when the map specifies none).
        private static readonly byte[][] DefaultComboColors =
        {
            new byte[] { 255, 192, 0 },
            new byte[] { 0, 202, 0 },
            new byte[] { 18, 124, 255 },
            new byte[] { 242, 24, 57 },
        };

        public static VamBeatmap Load(string osuPath, bool computeHyperDash = true)
        {
            // CitrusVortex parses ".osu" numbers with the current culture in several places.
            // Force invariant so decimal points parse correctly regardless of the OS locale
            // (e.g. Polish/German comma-decimal machines), then restore.
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                return LoadInternal(osuPath, computeHyperDash);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        private static VamBeatmap LoadInternal(string osuPath, bool computeHyperDash)
        {
            var lines = File.ReadAllLines(osuPath).ToList();

            var deserializer = new OsuV14BeatmapDeserializer();
            var converter = new OsuV14ToOsuCatchBeatmapConverter();

            var osuMap = deserializer.Deserialize(lines);
            var catchMap = converter.Convert(osuMap);

            var result = new VamBeatmap
            {
                CircleSize = catchMap.CircleSize,
                ApproachRate = catchMap.ApproachRate,
                CountdownMode = (int)osuMap.Countdown,   // OsuCountdown: None=0, Normal=1, Half=2, Double=3
                CountdownOffset = osuMap.CountdownOffset
            };

            // Build the combo-colour palette.
            var palette = BuildPalette(catchMap.ComboColors);

            // Combo colour per top-level object, derived from the ORIGINAL osu! hit objects
            // (the converter drops the new-combo flag, so we recompute it here). The converter
            // emits exactly one top-level catch object per source hit object, in order, so we
            // can walk both lists together.
            var comboColors = ComputeComboColors(osuMap.HitObjects, palette);

            for (int i = 0; i < catchMap.HitObjects.Count; i++)
            {
                var hit = catchMap.HitObjects[i];
                byte[] color = (i < comboColors.Count) ? comboColors[i] : new byte[] { 255, 255, 255 };
                Flatten(hit, color, i, result.Objects);
            }

            // Time-order everything and index it.
            result.Objects = result.Objects.OrderBy(o => o.Time).ToList();
            for (int i = 0; i < result.Objects.Count; i++)
                result.Objects[i].Index = i;

            // Beat length in force at the first object (for the countdown's beat spacing).
            double firstTime = result.Objects.Count > 0 ? result.Objects[0].Time : 0;
            result.BeatLengthAtStart = BeatLengthAt(osuMap.TimingPoints, firstTime);

            if (computeHyperDash)
                ComputeHyperDash(result);

            return result;
        }

        // ms per beat of the uninherited (red) timing point active at 'time' — the latest one at
        // or before it, falling back to the earliest uninherited point, then to 500ms (120 BPM).
        private static double BeatLengthAt(List<OsuTimingPoint> tps, double time)
        {
            double bl = 500.0;
            if (tps != null && tps.Count > 0)
            {
                double bestTime = double.NegativeInfinity;
                bool found = false;
                foreach (var tp in tps)
                    if (tp.Uninherited && tp.BeatLength > 0 && tp.Time <= time && tp.Time > bestTime)
                    { bl = tp.BeatLength; bestTime = tp.Time; found = true; }
                if (!found)
                    foreach (var tp in tps)
                        if (tp.Uninherited && tp.BeatLength > 0) { bl = tp.BeatLength; break; }
            }
            return bl > 0 ? bl : 500.0;
        }

        private static List<byte[]> BuildPalette(List<CvColor> comboColors)
        {
            var palette = new List<byte[]>();
            if (comboColors != null)
            {
                foreach (var c in comboColors)
                    palette.Add(new byte[] { c.Red, c.Green, c.Blue });
            }
            if (palette.Count == 0)
            {
                foreach (var c in DefaultComboColors)
                    palette.Add(c);
            }
            return palette;
        }

        private static List<byte[]> ComputeComboColors(List<OsuHitObject> hitObjects, List<byte[]> palette)
        {
            // Faithful port of osu!'s IHasComboInformation.UpdateComboInformation:
            //   int index = last?.ComboIndexWithOffsets ?? 0;
            //   if (NewCombo || last == null) index += ComboOffset + 1;
            // The colour is palette[index % count]. Crucially the FIRST object is treated as a
            // new combo and increments to 1, so the first combo uses palette[1] (Combo2), NOT
            // palette[0]. (The old code started at 0, shifting every object one colour off.)
            var colors = new List<byte[]>(hitObjects.Count);
            int index = 0;
            bool first = true;

            foreach (var ho in hitObjects)
            {
                if (ho.IsNewCombo || first)
                    index += ho.ComboColorSkipAmount + 1;
                first = false;

                int idx = palette.Count > 0 ? ((index % palette.Count) + palette.Count) % palette.Count : 0;
                colors.Add(palette.Count > 0 ? palette[idx] : new byte[] { 255, 255, 255 });
            }

            return colors;
        }

        private static void Flatten(OsuCatchHitObject hit, byte[] color, int indexInBeatmap, List<VamObject> output)
        {
            switch (hit)
            {
                case OsuCatchFruit fruit:
                    output.Add(Make(VamObjectType.Fruit, fruit.X, fruit.Time, color, indexInBeatmap));
                    break;

                case OsuCatchDroplet droplet:
                    output.Add(Make(VamObjectType.Droplet, droplet.X, droplet.Time, color, indexInBeatmap));
                    break;

                case OsuCatchTinyDroplet tiny:
                    output.Add(Make(VamObjectType.TinyDroplet, tiny.X + tiny.XOffset, tiny.Time, color, indexInBeatmap));
                    break;

                case OsuCatchBanana banana:
                    output.Add(Make(VamObjectType.Banana, banana.X, banana.Time, color, indexInBeatmap));
                    break;

                case OsuCatchJuiceStream juice:
                    foreach (var component in juice.Components)
                        Flatten(component, color, indexInBeatmap, output);
                    break;

                case OsuCatchBananaShower shower:
                    foreach (var banana in shower.Bananas)
                    {
                        var vo = Make(VamObjectType.Banana, banana.X, banana.Time, color, indexInBeatmap);
                        vo.ScaleStart = banana.ScaleStart;   // from the shared rng pool during conversion
                        output.Add(vo);
                    }
                    break;
            }
        }

        private static VamObject Make(VamObjectType type, double x, int time, byte[] color, int indexInBeatmap)
        {
            return new VamObject
            {
                Type = type,
                X = x,
                Time = time,
                R = color[0],
                G = color[1],
                B = color[2],
                IndexInBeatmap = indexInBeatmap
            };
        }

        // Faithful port of osu!'s CatchBeatmapProcessor.initialiseHyperDash.
        // Bananas do not take part in hyperdash. Marks the object BEFORE an
        // impossible jump as a hyperdash (that's the one osu! tints).
        private static void ComputeHyperDash(VamBeatmap map)
        {
            const double catcherSpeed = 1.0; // osu! Catcher.BASE_DASH_SPEED

            // osu! only counts fruits and BIG droplets for hyperdash — tiny droplets are
            // explicitly excluded (they never trigger or receive a hyperdash on stable).
            var palpable = map.Objects
                .Where(o => o.Type == VamObjectType.Fruit || o.Type == VamObjectType.Droplet)
                .ToList();
            if (palpable.Count < 2) return;

            double scale = VamGeometry.CatcherScaleFromCircleSize(map.CircleSize);
            double catchWidth = 106.75 * Math.Abs(scale) * 0.8; // BASE_SIZE * scale * ALLOWED_CATCH_RANGE
            // osu!: halfCatcherWidth = CalculateCatchWidth/2, then /= ALLOWED_CATCH_RANGE (the 0.8 cancels).
            double halfCatcherWidth = catchWidth / 2.0 / 0.8;

            int lastDirection = 0;
            double lastExcess = halfCatcherWidth;

            for (int i = 0; i < palpable.Count - 1; i++)
            {
                var current = palpable[i];
                var next = palpable[i + 1];

                int thisDirection = next.X > current.X ? 1 : -1;
                double timeToNext = next.Time - current.Time - 1000.0 / 60.0 / 4.0;
                double distanceToNext = Math.Abs(next.X - current.X) -
                                        (lastDirection == thisDirection ? lastExcess : halfCatcherWidth);
                double distanceToHyper = timeToNext * catcherSpeed - distanceToNext;

                if (distanceToHyper < 0)
                {
                    current.HyperDash = true;
                    lastExcess = halfCatcherWidth;
                }
                else
                {
                    lastExcess = Math.Max(0.0, Math.Min(distanceToHyper, halfCatcherWidth));
                }

                lastDirection = thisDirection;
            }
        }
    }
}
