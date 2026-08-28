using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewScripts.Vam
{
    // A single "at this time, Hidden intensity is this" point (intensity 0..1).
    public struct HdKeyframe
    {
        public double Time;
        public double Intensity;
    }

    // Dynamic fake-Hidden profile — the HD analogue of VamArProfile.
    //
    // Configured from a text string so it lives in a storybrew [Configurable] field. Format:
    // comma/semicolon/newline separated "time:intensity" pairs, intensity 0..1, e.g.
    //     60000:0, 65000:1, 90000:1, 95000:0
    // meaning: HD ramps IN from 0 to full over 60s..65s, stays full to 90s, then ramps OUT to
    // 0 by 95s. Between keyframes the intensity is interpolated (optionally eased); before the
    // first / after the last keyframe the nearest value is held. Intensity 0 = no HD (object
    // stays fully visible), 1 = full osu!catch HD (object gone before the catch).
    //
    // If the string is empty, the profile is a single constant intensity (constantIntensity).
    public class VamHdProfile
    {
        private readonly List<HdKeyframe> _keys = new List<HdKeyframe>();
        private readonly double _constantIntensity;
        private readonly bool _easingEnabled;
        private readonly VamEasing _easing;

        public VamHdProfile(string keyframeText, double constantIntensity, bool easingEnabled, VamEasing easing)
        {
            _constantIntensity = Clamp01(constantIntensity);
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
                var valStr = part.Substring(colon + 1).Trim();

                double time, val;
                if (double.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out time) &&
                    double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                {
                    _keys.Add(new HdKeyframe { Time = time, Intensity = Clamp01(val) });
                }
            }
        }

        // The HD intensity (0..1) active at the given time.
        public double IntensityAt(double time)
        {
            if (_keys.Count == 0) return _constantIntensity;
            if (_keys.Count == 1) return _keys[0].Intensity;

            if (time <= _keys[0].Time) return _keys[0].Intensity;
            if (time >= _keys[_keys.Count - 1].Time) return _keys[_keys.Count - 1].Intensity;

            for (int i = 0; i < _keys.Count - 1; i++)
            {
                var k0 = _keys[i];
                var k1 = _keys[i + 1];
                if (time >= k0.Time && time <= k1.Time)
                {
                    if (k1.Time <= k0.Time) return k0.Intensity;
                    double t = (time - k0.Time) / (k1.Time - k0.Time);
                    if (_easingEnabled) t = VamArProfile.Ease(_easing, t);
                    return k0.Intensity + (k1.Intensity - k0.Intensity) * t;
                }
            }
            return _constantIntensity;
        }

        private static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
    }
}
