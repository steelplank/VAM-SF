using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CtbLoader.Common;

namespace CtbLoader.Targets.Osu
{
    // A class for converting sliders into their effective paths with very heavy focus on compatibility with osu!
    // Significant portions of the code are taken from https://github.com/ppy/osu-framework/blob/master/osu.Framework/Utils/PathApproximator.cs
    // Might refactor at some point to fix all the meaningless names and magic numbers in the copied code
    public static class OsuSliderPathGenerator
    {
        private const float _OSU_BEZIER_TOLERANCE = 0.25f;
        private const float _OSU_PI = 3.14159274f;

        public static List<Vector2> GetPath(OsuSlider slider)
        {
            var result = new List<Vector2>();

            var controlPoints = new List<Vector2>() { new(slider.X, slider.Y) };
            controlPoints.AddRange(slider.CurvePoints.Select(curvePoint => new Vector2(curvePoint.X, curvePoint.Y)));

            var currentSegment = new List<Vector2>();
            
            for (int i = 1; i < controlPoints.Count; i++)
            {
                var controlPoint = controlPoints[i];
                var previousControlPoint = controlPoints[i - 1];

                currentSegment.Add(previousControlPoint);

                if (controlPoint == previousControlPoint)
                {
                    result.AddRange(GetSegmentPath(currentSegment, slider.CurveType));
                    currentSegment.Clear();
                }
            }

            currentSegment.Add(controlPoints[^1]);
            result.AddRange(GetSegmentPath(currentSegment, slider.CurveType));

            // FIX (AR-modification framework): match osu!'s SliderPath by dropping consecutive
            // duplicate points (the segment concatenation above can repeat a shared anchor).
            // Keeps the length interpolation clean and avoids a Remove-by-value hazard in the trim.
            var deduped = new List<Vector2>();
            foreach (var point in result)
                if (deduped.Count == 0 || deduped[^1] != point)
                    deduped.Add(point);

            return deduped;
        }

        private static List<Vector2> GetSegmentPath(List<Vector2> controlPoints, OsuCurveType curveType)
        {
            if (controlPoints.Count == 1)
                return controlPoints;

            // Catmull curve types are not not achievable in the osu!stable editor, supporting them is not a priority
            var result = curveType switch
            {
                OsuCurveType.Linear => controlPoints,
                OsuCurveType.Bezier => GetBezierPath(controlPoints),
                OsuCurveType.Perfect => GetCircularArcPath(controlPoints),
                OsuCurveType.Catmull => throw new NotSupportedException(),
                _ => throw new NotSupportedException()
            };

            return result;
        }

        private static List<Vector2> GetCircularArcPath(List<Vector2> controlPoints)
        {
            var result = new List<Vector2>();

            // FIX (AR-modification framework): osu! only treats a "P" segment as a circular arc
            // when it has EXACTLY 3 control points; any other count (e.g. a 2-point perfect slider
            // with a single anchor) falls back to Bezier. The original `> 3` check let 2-point
            // segments through and then crashed on controlPoints[2] (IndexOutOfRange).
            if (controlPoints.Count != 3)
                return GetBezierPath(controlPoints);

            var a = controlPoints[0];
            var b = controlPoints[1];
            var c = controlPoints[2];

            if (AreCollinear(a, b, c))
                return controlPoints;

            CircleThroughPoints(a, b, c, out var center, out var radius, out var tInitial, out var tFinal);

            var curveLength = Math.Abs((tFinal - tInitial) * radius);
            int segments = (int)(curveLength * 0.125f);

            result.Add(a);

            for (int i = 1; i < segments; i++)
            {
                double progress = (double)i / segments;
                double t = tFinal * progress + tInitial * (1 - progress);

                result.Add(new Vector2((float)(Math.Cos(t) * radius), (float)(Math.Sin(t) * radius)) + center);
            }

            result.Add(c);

            return result;
        }

        private static bool AreCollinear(Vector2 a, Vector2 b, Vector2 c)
            => (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y) == 0f;

        private static void CircleThroughPoints(Vector2 a, Vector2 b, Vector2 c,
            out Vector2 center, out float radius, out double tInitial, out double tFinal)
        {
            var d = 2 * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
            var aMagSq = a.LengthSquared();
            var bMagSq = b.LengthSquared();
            var cMagSq = c.LengthSquared();

            center = new Vector2(
                (aMagSq * (b.Y - c.Y) + bMagSq * (c.Y - a.Y) + cMagSq * (a.Y - b.Y)) / d,
                (aMagSq * (c.X - b.X) + bMagSq * (a.X - c.X) + cMagSq * (b.X - a.X)) / d);

            radius = Vector2.Distance(center, a);

            tInitial = CircleTAt(a, center);
            var tMid = CircleTAt(b, center);
            tFinal = CircleTAt(c, center);

            while (tMid < tInitial)
                tMid += 2 * _OSU_PI;

            while (tFinal < tInitial)
                tFinal += 2 * _OSU_PI;

            if (tMid > tFinal)
                tFinal -= 2 * _OSU_PI;
        }

        private static double CircleTAt(Vector2 pt, Vector2 centre)
            => Math.Atan2(pt.Y - centre.Y, pt.X - centre.X);

        private static List<Vector2> GetBezierPath(List<Vector2> controlPoints)
        {
            var result = new List<Vector2>();
            var groups = SplitControlPointGroups(controlPoints);

            foreach (var group in groups)
            {
                result.AddRange(GetBezierPathSection(group));
            }

            result.Add(controlPoints[^1]);

            return result;
        }

        private static List<Vector2> GetBezierPathSection(List<Vector2> controlPoints)
        {
            var output = new List<Vector2>();

            if (controlPoints.Count < 2)
                return output;

            var toFlatten = new Stack<Vector2[]>();
            var freeBuffers = new Stack<Vector2[]>();

            var subdivisionBuffer1 = new Vector2[controlPoints.Count];
            var subdivisionBuffer2 = new Vector2[controlPoints.Count * 2 - 1];

            toFlatten.Push(controlPoints.ToArray());

            var leftChild = subdivisionBuffer2;

            while (toFlatten.Count > 0)
            {
                var parent = toFlatten.Pop();

                if (BezierIsFlatEnough(parent))
                {
                    BezierApproximate(parent, output, subdivisionBuffer1, subdivisionBuffer2);
                    freeBuffers.Push(parent);
                    continue;
                }

                var rightChild = freeBuffers.Count > 0 ? freeBuffers.Pop() : new Vector2[controlPoints.Count];

                BezierSubdivide(parent, leftChild, rightChild, subdivisionBuffer1);

                for (int i = 0; i < controlPoints.Count; ++i)
                    parent[i] = leftChild[i];

                toFlatten.Push(rightChild);
                toFlatten.Push(parent);
            }

            // Skip the last control point in order to avoid overlaps in GetBezierPath()
            //output.Add(controlPoints[^1]);

            return output;
        }

        private static bool BezierIsFlatEnough(Vector2[] controlPoints)
        {
            for (int i = 1; i < controlPoints.Length - 1; i++)
                if ((controlPoints[i - 1] - 2 * controlPoints[i] + controlPoints[i + 1]).LengthSquared() > _OSU_BEZIER_TOLERANCE)
                    return false;

            return true;
        }

        private static void BezierSubdivide(Vector2[] controlPoints, Vector2[] left, Vector2[] right, Vector2[] subdivisionBuffer1)
        {
            var midpoints = subdivisionBuffer1;

            for (int i = 0; i < controlPoints.Length; ++i)
                midpoints[i] = controlPoints[i];

            for (int i = 0; i < controlPoints.Length; i++)
            {
                left[i] = midpoints[0];
                right[controlPoints.Length - i - 1] = midpoints[controlPoints.Length - i - 1];

                for (int j = 0; j < controlPoints.Length - i - 1; j++)
                    midpoints[j] = (midpoints[j] + midpoints[j + 1]) / 2;
            }
        }

        private static void BezierApproximate(Vector2[] controlPoints, List<Vector2> output, Vector2[] subdivisionBuffer1, Vector2[] subdivisionBuffer2)
        {
            var left = subdivisionBuffer2;
            var right = subdivisionBuffer1;

            BezierSubdivide(controlPoints, left, right, subdivisionBuffer1);

            for (int i = 0; i < controlPoints.Length - 1; ++i)
                left[controlPoints.Length + i] = right[i + 1];

            output.Add(controlPoints[0]);

            for (int i = 1; i < controlPoints.Length - 1; ++i)
            {
                var index = 2 * i;
                var p = 0.25f * (left[index - 1] + 2 * left[index] + left[index + 1]);

                output.Add(p);
            }
        }

        private static List<List<Vector2>> SplitControlPointGroups(List<Vector2> controlPoints)
        {
            var result = new List<List<Vector2>>();
            var currentGroup = new List<Vector2>();

            // FIX (AR-modification framework): the original loop started at i=1 and never added
            // controlPoints[0], so every Bezier segment lost its FIRST control point. That made
            // multi-anchor sliders' paths too short, and the length-trim then over-extrapolated
            // the end (fruits landing far off, sometimes offscreen). Seed the group with the
            // first point, and when a red anchor (doubled point) splits a group, start the next
            // group at that shared anchor.
            if (controlPoints.Count > 0)
                currentGroup.Add(controlPoints[0]);

            for (int i = 1; i < controlPoints.Count; i++)
            {
                var controlPoint = controlPoints[i];
                var previousControlPoint = controlPoints[i - 1];

                if (controlPoint.X == previousControlPoint.X && controlPoint.Y == previousControlPoint.Y)
                {
                    if (currentGroup.Count == 0)
                    {
                        currentGroup.Add(controlPoint);
                        continue;
                    }

                    result.Add(currentGroup);
                    currentGroup = [];
                    currentGroup.Add(controlPoint);
                }
                else
                {
                    currentGroup.Add(controlPoint);
                }
            }

            if (currentGroup.Count > 0)
                result.Add(currentGroup);

            return result;
        }
    }
}
