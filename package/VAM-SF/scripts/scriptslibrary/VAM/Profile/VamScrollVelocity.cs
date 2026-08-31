using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewScripts.Vam
{
    // Scroll-velocity timeline: a stepped multiplier scales how fast every object falls over time,
    // while each still lands on its exact beat and x (the fall height is SV integrated backwards from
    // the catch). Read from the [sv] section of VAM-profile.txt; 1x before the first keyframe, last
    // value held after it. Syntax lives in VAM-profile.txt.
    public sealed class VamScrollVelocity
    {
        readonly List<double> _t = new List<double>();  // keyframe times, ascending
        readonly List<double> _v = new List<double>();  // multiplier held from each time to the next
        readonly List<double> _S = new List<double>();  // SV integrated up to each time, S(_t[0]) = 0
        double _firstT, _lastT;

        public bool HasKeyframes { get { return _t.Count > 0; } }

        // Missing/empty [sv] section => HasKeyframes false (a no-op).
        public VamScrollVelocity(string profileText)
        {
            var nodes = ParseSvSection(profileText);
            nodes.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var n in nodes)
            {
                if (_t.Count > 0 && n.Key == _t[_t.Count - 1]) continue;  // stepped SV holds one value per time
                _t.Add(n.Key);
                _v.Add(n.Value < 0 ? 0 : n.Value);
            }
            if (_t.Count == 0) return;
            _S.Add(0.0);
            for (int i = 1; i < _t.Count; i++)
                _S.Add(_S[i - 1] + _v[i - 1] * (_t[i] - _t[i - 1]));
            _firstT = _t[0]; _lastT = _t[_t.Count - 1];
        }

        // Rewrite a plan's fall to move at the SV-scaled speed, still landing at its catch time and x.
        // No-op unless the fall overlaps the timeline; only the spawn side and the path change.
        public void Reshape(VamPlan plan)
        {
            if (_t.Count == 0 || plan.Position.Count < 2) return;

            var spawnKey = plan.Position.First;
            var catchKey = plan.Position.Last;
            double spawnBase = spawnKey.Time;
            double catchTime = catchKey.Time;
            double basePreempt = catchTime - spawnBase;
            if (basePreempt <= 0) return;

            if (catchTime <= _firstT || spawnBase >= _lastT) return;

            double x = catchKey.Value.X;
            double spawnY = spawnKey.Value.Y;
            double catchY = catchKey.Value.Y;
            double dist = catchY - spawnY;
            if (dist == 0) return;
            double vk = dist / basePreempt;      // base fall speed, units/ms

            double sCatch = SAt(catchTime);
            double target = sCatch - basePreempt;
            double newSpawn = InvertS(target);
            if (newSpawn >= catchTime) return;

            var keys = plan.Position.Keys;
            keys.Clear();
            keys.Add(new Key<Vec2>(newSpawn, new Vec2(x, spawnY), StorybrewCommon.Storyboarding.OsbEasing.None));
            for (int i = 0; i < _t.Count; i++)
            {
                double t = _t[i];
                if (t <= newSpawn || t >= catchTime) continue;
                double y = catchY - vk * (sCatch - SAt(t));
                if (y < spawnY) y = spawnY; else if (y > catchY) y = catchY;
                keys.Add(new Key<Vec2>(t, new Vec2(x, y), StorybrewCommon.Storyboarding.OsbEasing.None));
            }
            keys.Add(new Key<Vec2>(catchTime, new Vec2(x, catchY), StorybrewCommon.Storyboarding.OsbEasing.None));

            plan.SpawnTime = newSpawn;
            plan.Preempt = catchTime - newSpawn;
        }

        // SV integrated to 'time' (SV = 1 before the first node, stepped between, last value held after).
        double SAt(double time)
        {
            if (time <= _firstT) return time - _firstT;
            if (time >= _lastT) return _S[_S.Count - 1] + _v[_v.Count - 1] * (time - _lastT);
            for (int i = _t.Count - 2; i >= 0; i--)
                if (time >= _t[i]) return _S[i] + _v[i] * (time - _t[i]);
            return 0.0;
        }

        // Earliest time with S(t) = target (S is flat during a freeze, so pick the start of it).
        double InvertS(double target)
        {
            if (target <= 0.0) return _firstT + target;
            double sLast = _S[_S.Count - 1];
            if (target >= sLast)
            {
                double vLast = _v[_v.Count - 1];
                return vLast <= 0 ? _lastT : _lastT + (target - sLast) / vLast;
            }
            for (int i = 0; i < _t.Count - 1; i++)
            {
                if (target >= _S[i] && target <= _S[i + 1])
                    return _v[i] <= 0 ? _t[i] : _t[i] + (target - _S[i]) / _v[i];
            }
            return _firstT;
        }

        // Stepped multiplier at 'time'.
        public double MultiplierAt(double time)
        {
            if (_t.Count == 0) return 1.0;
            if (time < _t[0]) return 1.0;
            for (int i = _t.Count - 1; i >= 0; i--)
                if (time >= _t[i]) return _v[i];
            return 1.0;
        }

        // Largest |multiplier - 1| within +/- window of 'time'. The SV tint uses a window, not a point
        // sample, so an object sitting exactly on a boundary (e.g. a slider end on the return to 1x)
        // still counts as "on SV" despite rounding. 0 when SV stays at 1x across the window.
        public double MaxDeviation(double time, double window)
        {
            if (_t.Count == 0) return 0.0;
            if (window < 0) window = 0;
            double lo = time - window, hi = time + window;
            double dev = 0.0;
            for (int i = 0; i < _t.Count; i++)
            {
                double segStart = _t[i];
                double segEnd = (i + 1 < _t.Count) ? _t[i + 1] : double.PositiveInfinity;
                if (segStart <= hi && segEnd >= lo)
                    dev = Math.Max(dev, Math.Abs(_v[i] - 1.0));
            }
            return dev;
        }

        // 'time:value' pairs from the [sv] section only; # and // comments and blanks skipped, any
        // other [section] header ends it.
        static List<KeyValuePair<double, double>> ParseSvSection(string text)
        {
            var list = new List<KeyValuePair<double, double>>();
            if (string.IsNullOrEmpty(text)) return list;

            bool inSv = false;
            foreach (var raw0 in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var raw = raw0;
                int h = raw.IndexOf('#'); if (h >= 0) raw = raw.Substring(0, h);
                int s = raw.IndexOf("//", StringComparison.Ordinal); if (s >= 0) raw = raw.Substring(0, s);
                var line = raw.Trim();
                if (line.Length == 0) continue;

                if (line[0] == '[')
                {
                    var inside = line.Trim('[', ']').Trim();
                    inSv = string.Equals(inside, "sv", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inSv) continue;

                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                var left = line.Substring(0, colon).Trim();
                var right = line.Substring(colon + 1).Trim();
                var vtok = right.Split(':')[0].Trim();   // ignore trailing flags

                double t, val;
                if (double.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out t) &&
                    double.TryParse(vtok, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    list.Add(new KeyValuePair<double, double>(t, val));
            }
            return list;
        }
    }
}
