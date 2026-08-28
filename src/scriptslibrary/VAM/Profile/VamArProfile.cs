using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewScripts.Vam
{
    // Easing curves used when interpolating AR between keyframes.
    // These are independent of osb sprite easing (fruits always fall linearly).
    // NOTE: values are APPENDED (never reordered) so existing saved effect configs — which store
    // the Easing dropdown as an integer index — keep pointing at the same curve.
    public enum VamEasing
    {
        Linear,
        SineInOut,
        QuadInOut,
        CubicInOut,
        QuadIn,
        QuadOut,
        SineIn,
        SineOut,
        CubicIn,
        CubicOut
    }

    // A single "at this time, AR is this" point.
    public struct ArKeyframe
    {
        public double Time;
        public double Ar;
    }

    // Dynamic approach-rate profile.
    //
    // Configured from a simple text string so it can live in a storybrew [Configurable]
    // field. Format: a comma/semicolon/newline separated list of "time:ar" pairs, e.g.
    //     0:8, 45000:10, 90000:9.5
    // Times are in milliseconds. Between keyframes the AR is interpolated (optionally eased);
    // before the first / after the last keyframe the nearest value is held.
    //
    // If the string is empty, the profile is a single constant AR (constantAr).
    public class VamArProfile
    {
        private readonly List<ArKeyframe> _keys = new List<ArKeyframe>();
        private readonly double _constantAr;
        private readonly bool _easingEnabled;
        private readonly VamEasing _easing;

        public VamArProfile(string keyframeText, double constantAr, bool easingEnabled, VamEasing easing)
        {
            _constantAr = constantAr;
            _easingEnabled = easingEnabled;
            _easing = easing;

            if (!string.IsNullOrWhiteSpace(keyframeText))
                Parse(keyframeText);

            _keys.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        public bool HasKeyframes { get { return _keys.Count > 0; } }

        private void Parse(string text)
        {
            var separators = new char[] { ',', ';', '\n', '\r' };
            var parts = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in parts)
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;

                var colon = part.IndexOf(':');
                if (colon < 0) continue;

                var timeStr = part.Substring(0, colon).Trim();
                var arStr = part.Substring(colon + 1).Trim();

                double time, ar;
                if (double.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out time) &&
                    double.TryParse(arStr, NumberStyles.Any, CultureInfo.InvariantCulture, out ar))
                {
                    _keys.Add(new ArKeyframe { Time = time, Ar = ar });
                }
            }
        }

        // The AR value active at the given time.
        public double ArAt(double time)
        {
            if (_keys.Count == 0) return _constantAr;
            if (_keys.Count == 1) return _keys[0].Ar;

            if (time <= _keys[0].Time) return _keys[0].Ar;
            if (time >= _keys[_keys.Count - 1].Time) return _keys[_keys.Count - 1].Ar;

            // Find the segment [k0, k1] containing time.
            for (int i = 0; i < _keys.Count - 1; i++)
            {
                var k0 = _keys[i];
                var k1 = _keys[i + 1];
                if (time >= k0.Time && time <= k1.Time)
                {
                    if (k1.Time <= k0.Time) return k0.Ar;
                    double t = (time - k0.Time) / (k1.Time - k0.Time);
                    if (_easingEnabled) t = Ease(_easing, t);
                    return k0.Ar + (k1.Ar - k0.Ar) * t;
                }
            }
            return _constantAr;
        }

        public static double Ease(VamEasing easing, double t)
        {
            if (t < 0) t = 0; else if (t > 1) t = 1;
            switch (easing)
            {
                case VamEasing.SineInOut:
                    return -(Math.Cos(Math.PI * t) - 1.0) / 2.0;
                case VamEasing.QuadInOut:
                    return t < 0.5 ? 2.0 * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0;
                case VamEasing.CubicInOut:
                    return t < 0.5 ? 4.0 * t * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 3.0) / 2.0;
                case VamEasing.QuadIn:
                    return t * t;
                case VamEasing.QuadOut:
                    return 1.0 - (1.0 - t) * (1.0 - t);
                case VamEasing.SineIn:
                    return 1.0 - Math.Cos(t * Math.PI / 2.0);
                case VamEasing.SineOut:
                    return Math.Sin(t * Math.PI / 2.0);
                case VamEasing.CubicIn:
                    return t * t * t;
                case VamEasing.CubicOut:
                    return 1.0 - Math.Pow(1.0 - t, 3.0);
                case VamEasing.Linear:
                default:
                    return t;
            }
        }
    }
}
