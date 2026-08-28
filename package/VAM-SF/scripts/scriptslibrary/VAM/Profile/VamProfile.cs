using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewScripts.Vam
{
    // One parsed line of a catch profile: an AR keyframe and/or an HD keyframe at a time,
    // with per-keyframe easing and instant-step scope.
    public class VamProfileKey
    {
        public double Time;

        public bool HasAr;
        public double Ar;

        public bool HasHd;
        public double Hd;        // 0..10 fade scale (0 off, 5 osu default, 10 hardest); fixed osu width, slides the line

        public VamEasing Easing;  // curve used for the transition INTO this keyframe
        public bool StepIn;      // arriving transition is instant (scope "after")
        public bool StepOut;     // leaving transition is instant  (scope "before")
    }

    // A combined AR + fake-Hidden profile parsed from a text file (one keyframe per line).
    //
    //   time:ar[:flag[:flag...]]
    //
    // Flags (optional, any order, case-insensitive):
    //   - an easing name: linear/none, sine, sinein, sineout, quad, quadin, quadout,
    //                     cubic, cubicin, cubicout  (curve used to ease INTO this keyframe)
    //   - a scope:  both (default) | before | after
    //         both   -> eased on both sides
    //         after  -> the transition INTO this keyframe is INSTANT (hold the previous value,
    //                   then snap here). This is how you make a SUDDEN change at this time.
    //         before -> the transition OUT of this keyframe is INSTANT (this value holds until
    //                   the next keyframe, then snaps there).
    //   - hd fade: bare "hd" (or hd=true) = osu!'s default Hidden; hd=false = off; hd=0..10 dials
    //       WHERE the fade happens (0 = off, 5 = osu default, 10 = hardest). The fade keeps osu!'s
    //       exact width at every setting; the number just slides the line up/down the screen.
    //
    // The AR value is required per line, except an HD-only line may leave it blank (time::hd=1)
    // or use '-' / '_' to mean "no AR keyframe here, only HD".
    //
    // Lines starting with '#' or '//' are comments; blank lines are ignored; anything after a
    // '#' on a line is stripped as an inline comment.
    public class VamProfile
    {
        private readonly List<VamProfileKey> _arKeys = new List<VamProfileKey>();
        private readonly List<VamProfileKey> _hdKeys = new List<VamProfileKey>();
        private readonly double _constantAr;

        // Bare "hd" / hd=true == old boolean Hidden ON == osu!'s default Hidden on the 0..10 scale.
        private const double OsuDefaultHdScale = 5.0;

        private readonly List<string> _errors = new List<string>();
        public IReadOnlyList<string> Errors { get { return _errors; } }

        public bool HasAr { get { return _arKeys.Count > 0; } }
        public bool HasHd { get { return _hdKeys.Count > 0; } }

        public VamProfile(string text, double constantAr, bool easingEnabled, VamEasing defaultEasing)
        {
            _constantAr = constantAr;
            var fallbackEasing = easingEnabled ? defaultEasing : VamEasing.Linear;

            if (!string.IsNullOrWhiteSpace(text))
                Parse(text, fallbackEasing);

            _arKeys.Sort((a, b) => a.Time.CompareTo(b.Time));
            _hdKeys.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        private void Parse(string text, VamEasing fallbackEasing)
        {
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool inSection = false;
            for (int lineNo = 0; lineNo < lines.Length; lineNo++)
            {
                var raw = lines[lineNo];

                // strip inline comments
                int hash = raw.IndexOf('#');
                if (hash >= 0) raw = raw.Substring(0, hash);
                int slash = raw.IndexOf("//", StringComparison.Ordinal);
                if (slash >= 0) raw = raw.Substring(0, slash);

                var line = raw.Trim();
                if (line.Length == 0) continue;

                // AR/HD keyframes are top-level only. A [section] header (e.g. [mod:sv]) begins a
                // block that belongs to a modifier; skip it and everything under it so a mod's
                // 'time:value' lines are never mistaken for AR keyframes.
                if (line[0] == '[') { inSection = true; continue; }
                if (inSection) continue;

                var parts = line.Split(':');
                if (parts.Length < 2)
                {
                    _errors.Add($"line {lineNo + 1}: expected 'time:ar', got '{line}'");
                    continue;
                }

                if (!double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double time))
                {
                    _errors.Add($"line {lineNo + 1}: bad time '{parts[0].Trim()}'");
                    continue;
                }

                var key = new VamProfileKey { Time = time, Easing = fallbackEasing };

                // AR value (may be blank / '-' / '_' for an HD-only keyframe)
                var arStr = parts[1].Trim();
                if (arStr.Length > 0 && arStr != "-" && arStr != "_")
                {
                    if (double.TryParse(arStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ar))
                    {
                        key.HasAr = true;
                        key.Ar = ar;
                    }
                    else
                    {
                        _errors.Add($"line {lineNo + 1}: bad AR '{arStr}'");
                        continue;
                    }
                }

                // flags
                for (int i = 2; i < parts.Length; i++)
                {
                    var tok = parts[i].Trim();
                    if (tok.Length == 0) continue;
                    var low = tok.ToLowerInvariant();

                    if (low.StartsWith("hd"))
                    {
                        // bare "hd" / hd=true / hd=on  -> osu!'s default Hidden (scale 5)
                        // hd=false / hd=off            -> off (scale 0)
                        // hd=<0..10>                   -> a fade scale (0 visible .. 10 invisible)
                        double hd = OsuDefaultHdScale;   // bare "hd" == old hd=1 == normal Hidden
                        int eq = low.IndexOf('=');
                        if (eq >= 0)
                        {
                            var v = low.Substring(eq + 1).Trim();
                            if (v == "true" || v == "on" || v == "yes") hd = OsuDefaultHdScale;
                            else if (v == "false" || v == "off" || v == "no") hd = 0.0;
                            else if (!double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out hd))
                            {
                                _errors.Add($"line {lineNo + 1}: bad hd value '{v}'");
                                continue;
                            }
                        }
                        key.HasHd = true;
                        key.Hd = hd < 0 ? 0 : (hd > 10 ? 10 : hd);
                        continue;
                    }

                    switch (low)
                    {
                        case "both": key.StepIn = false; key.StepOut = false; break;
                        case "after": case "step": case "snap": case "instant":
                            key.StepIn = true; break;                 // instant INTO this keyframe
                        case "before":
                            key.StepOut = true; break;                // instant OUT of this keyframe
                        default:
                            if (TryParseEasing(low, out var e)) key.Easing = e;
                            else _errors.Add($"line {lineNo + 1}: unknown flag '{tok}'");
                            break;
                    }
                }

                if (key.HasAr) _arKeys.Add(key);
                if (key.HasHd) _hdKeys.Add(key);
                if (!key.HasAr && !key.HasHd)
                    _errors.Add($"line {lineNo + 1}: line sets neither AR nor hd");
            }
        }

        private static bool TryParseEasing(string name, out VamEasing easing)
        {
            switch (name)
            {
                case "linear": case "none": easing = VamEasing.Linear; return true;
                case "sine": case "sineinout": case "inoutsine": easing = VamEasing.SineInOut; return true;
                case "sinein": case "insine": easing = VamEasing.SineIn; return true;
                case "sineout": case "outsine": easing = VamEasing.SineOut; return true;
                case "quad": case "quadinout": easing = VamEasing.QuadInOut; return true;
                case "quadin": easing = VamEasing.QuadIn; return true;
                case "quadout": easing = VamEasing.QuadOut; return true;
                case "cubic": case "cubicinout": easing = VamEasing.CubicInOut; return true;
                case "cubicin": easing = VamEasing.CubicIn; return true;
                case "cubicout": easing = VamEasing.CubicOut; return true;
                default: easing = VamEasing.Linear; return false;
            }
        }

        // AR value at a time (falls back to the constant AR if there are no AR keyframes).
        public double ArAt(double time)
        {
            return Sample(_arKeys, time, k => k.Ar, _constantAr, holdBeforeFirst: true);
        }

        // Fake-Hidden fade SCALE (0..10) at a time. 0 (off) before the first hd keyframe.
        // Interpolated between hd keyframes just like AR (slides the fade line smoothly).
        public double HdScaleAt(double time)
        {
            return Sample(_hdKeys, time, k => k.Hd, 0.0, holdBeforeFirst: false);
        }

        // Piecewise sampler shared by AR and HD. Each segment eases with the destination
        // keyframe's curve, UNLESS the segment is stepped (start.StepOut or end.StepIn), in which
        // case the start value is held and snaps to the end value at the end time.
        // holdBeforeFirst: before the first keyframe, hold its value (AR) vs. return the fallback (HD off).
        private static double Sample(List<VamProfileKey> keys, double time,
                                     Func<VamProfileKey, double> val, double fallback, bool holdBeforeFirst)
        {
            if (keys.Count == 0) return fallback;
            if (time <= keys[0].Time) return holdBeforeFirst ? val(keys[0]) : (time < keys[0].Time ? fallback : val(keys[0]));
            if (time >= keys[keys.Count - 1].Time) return val(keys[keys.Count - 1]);

            for (int i = 0; i < keys.Count - 1; i++)
            {
                var a = keys[i];
                var b = keys[i + 1];
                if (time >= a.Time && time <= b.Time)
                {
                    if (b.Time <= a.Time) return val(b);
                    bool stepped = a.StepOut || b.StepIn;
                    if (stepped) return time >= b.Time ? val(b) : val(a);
                    double p = (time - a.Time) / (b.Time - a.Time);
                    double e = VamArProfile.Ease(b.Easing, p);
                    return val(a) + (val(b) - val(a)) * e;
                }
            }
            return fallback;
        }
    }
}
