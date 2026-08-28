using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewScripts.Vam
{
    // Core scroll-velocity timeline for osu!catch - a first-class VAM:SF feature, alongside AR and
    // HD (not a mod). A single velocity timeline scales how fast EVERY object falls at each moment:
    // spike it and they all rush, drop it and they crawl or freeze in place. Each object still
    // reaches the catcher at its exact hit time and x, because its on-screen height is the velocity
    // integrated backwards from its own catch. The AR profile sets the base fall speed; SV
    // multiplies that base speed over time.
    //
    // Configured in VAM-profile.txt in its own [sv] section, separate from the AR/HD keyframes:
    //
    //   [sv]
    //   60000:6      # from 60.0s, fall 6x speed (SV is 1 before the first keyframe)
    //   60500:0      # from 60.5s, freeze in place
    //   60900:0.15   # from 60.9s, slow creep
    //   61200:1      # from 61.2s, back to normal
    //
    //   value = velocity MULTIPLIER on the normal (AR) fall speed. 1 = normal, >1 faster, <1 slower,
    //   0 = frozen. Never negative. Stepped: a value holds until the next keyframe; it is 1 before
    //   the first keyframe and holds the last value after the last (end with :1 to return to normal).
    //   HD is not adjusted while SV is active.
    public sealed class VamScrollVelocity
    {
        readonly List<double> _t = new List<double>();  // node times (asc)
        readonly List<double> _v = new List<double>();  // multiplier from that node until the next
        readonly List<double> _S = new List<double>();  // cumulative integral of SV at each node (S(_t[0]) = 0)
        double _firstT, _lastT;

        public bool HasKeyframes { get { return _t.Count > 0; } }

        // Build from the full VAM-profile.txt text. Reads ONLY the [sv] section; AR/HD lines and any
        // [mod:*] blocks are ignored. Null / missing section => HasKeyframes == false (a pure no-op).
        public VamScrollVelocity(string profileText)
        {
            var nodes = ParseSvSection(profileText);
            nodes.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var n in nodes)
            {
                // ignore a duplicate-time node (keep the first); stepped SV can't hold two values at once
                if (_t.Count > 0 && n.Key == _t[_t.Count - 1]) continue;
                _t.Add(n.Key);
                _v.Add(n.Value < 0 ? 0 : n.Value);
            }
            if (_t.Count == 0) return;
            _S.Add(0.0);
            for (int i = 1; i < _t.Count; i++)
                _S.Add(_S[i - 1] + _v[i - 1] * (_t[i] - _t[i - 1]));
            _firstT = _t[0]; _lastT = _t[_t.Count - 1];
        }

        // Reshape a built plan's fall so the object moves at vk*SV(t) throughout, still landing at its
        // exact catch time and x. No-op when the fall never overlaps the timeline. CatchTime and the
        // catch keyframe (x, CatchY) are left untouched; only the spawn side and the path are rewritten.
        public void Reshape(VamPlan plan)
        {
            if (_t.Count == 0 || plan.Position.Count < 2) return;

            var spawnKey = plan.Position.First;
            var catchKey = plan.Position.Last;
            double spawnBase = spawnKey.Time;
            double catchTime = catchKey.Time;
            double basePreempt = catchTime - spawnBase;
            if (basePreempt <= 0) return;

            // only reshape when the fall actually overlaps the SV timeline
            if (catchTime <= _firstT || spawnBase >= _lastT) return;

            double x = catchKey.Value.X;
            double spawnY = spawnKey.Value.Y;
            double catchY = catchKey.Value.Y;
            double dist = catchY - spawnY;
            if (dist == 0) return;
            double vk = dist / basePreempt;      // base fall speed, units/ms

            double sCatch = SAt(catchTime);
            double target = sCatch - basePreempt; // S value at the new spawn (remaining = full dist)
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

            plan.SpawnTime = newSpawn;   // fade-in follows the new appearance time
            plan.Preempt = catchTime - newSpawn;
        }

        // Cumulative integral of SV: SV = 1 before the first node, stepped between, last value held
        // after the last. Anchored so S(_t[0]) = 0.
        double SAt(double time)
        {
            if (time <= _firstT) return time - _firstT;                        // SV = 1 before first
            if (time >= _lastT) return _S[_S.Count - 1] + _v[_v.Count - 1] * (time - _lastT);
            for (int i = _t.Count - 2; i >= 0; i--)
                if (time >= _t[i]) return _S[i] + _v[i] * (time - _t[i]);
            return 0.0;
        }

        // Earliest time t with S(t) = target (S is non-decreasing; flat during a freeze).
        double InvertS(double target)
        {
            if (target <= 0.0) return _firstT + target;                        // pre-first, SV = 1
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

        // Pull the 'time:value' keyframes out of the [sv] section only. Comments (# or //) and blanks
        // are skipped; any other [section] header ends the block. Case-insensitive on the header.
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
                var vtok = right.Split(':')[0].Trim();   // tolerate trailing flags after a 2nd colon

                double t, val;
                if (double.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out t) &&
                    double.TryParse(vtok, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    list.Add(new KeyValuePair<double, double>(t, val));
            }
            return list;
        }
    }
}
