using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StorybrewScripts.Vam
{
    // Preprocessor for the `hold` directive inside VAM-profile.txt's [sv] section: a mania-style
    // "hold, then snap" scroll pattern authored in a single line instead of a long hand-written loop.
    //
    //     hold <start> <beat-fraction> <slow> <fast> -> <end>
    //
    // Over [start, end] it lays a beat-fraction grid (the same osu! editor grid the loop expander
    // uses, re-anchoring on every red line so it follows BPM changes). Every object that lands in the
    // window "creeps" at <slow> and "snaps" at <fast> on the ONE division before it reaches the
    // catcher; every other division holds <slow>. So a fruit-every-beat map on 1/16 becomes
    // 15x slow + 1x fast, a 2/1 gap becomes 31x slow + 1x fast, and a map whose rhythm keeps changing
    // (DeltaMAX) auto-fits each gap - reproducing the hand loop tick-for-tick.
    //
    // The rule is per grid division: the division whose interval a landing falls into snaps at <fast>,
    // so EVERY object lands during a fast division and nothing ever crawls into the catcher at <slow>.
    // It expands to plain `time:value` SV lines before VamScrollVelocity parses, so the scroll physics
    // (backward integral, land-on-beat, spawn timing) are exactly those of a hand-typed [sv] block.
    // A `<end>:1` keyframe is appended so a held <slow> value doesn't leak past the window.
    public static class VamSvHoldExpander
    {
        // Hard safety cap so a huge span with a tiny fraction can't emit an unbounded file.
        private const int MaxKeyframesPerHold = 200000;

        public static string Expand(string text, VamBeatmap map)
        {
            List<string> ignored;
            return Expand(text, map, out ignored);
        }

        public static string Expand(string text, VamBeatmap map, out List<string> errors)
        {
            errors = new List<string>();
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var sb = new StringBuilder();
            bool first = true;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!IsHoldHeader(lines[i]))
                {
                    AppendLine(sb, ref first, lines[i]);
                    continue;
                }

                double start, end, frac, slow, fast;
                string err;
                if (!TryParseHold(lines[i], out start, out end, out frac, out slow, out fast, out err))
                {
                    errors.Add($"hold (line {i + 1}): {err}; line ignored");
                    continue;
                }

                bool capped;
                var ticks = VamLoopExpander.BuildBeatGrid(map, start, end, frac, MaxKeyframesPerHold, out capped);
                if (capped)
                    errors.Add($"hold (line {i + 1}): exceeded {MaxKeyframesPerHold} keyframes; truncated");
                if (ticks.Count == 0)
                {
                    errors.Add($"hold (line {i + 1}): no beat divisions between start and end; line ignored");
                    continue;
                }

                // Object landing times inside the window, ascending. Every catchable landing counts, so
                // no object can crawl into the platter during a slow division.
                var objs = CollectObjectTimes(map, start, end);

                // Each grid division k governs [ticks[k], ticks[k+1]); the division whose interval a
                // landing falls into (right-inclusive) snaps at <fast>. A forward object pointer keeps
                // the whole pass O(divisions + objects). A division sitting exactly on <end> is dropped:
                // it governs nothing inside the window and is replaced by the return-to-1x below.
                long endMs = (long)Math.Floor(end + 0.5);
                int oi = 0;
                for (int k = 0; k < ticks.Count; k++)
                {
                    long lo = ticks[k];
                    if (lo >= endMs) continue;                               // final tick == end -> handled by the :1 below
                    double hi = (k + 1 < ticks.Count) ? ticks[k + 1] : end;
                    while (oi < objs.Count && objs[oi] <= lo) oi++;          // drop landings on/behind this division's left edge
                    bool isFast = oi < objs.Count && objs[oi] <= hi;         // a landing inside (lo, hi] -> snap here
                    double v = isFast ? fast : slow;
                    AppendLine(sb, ref first, lo.ToString(CultureInfo.InvariantCulture) + ":" +
                                              v.ToString(CultureInfo.InvariantCulture));
                }

                // Return to 1x at the window end (SV holds its last value forever otherwise).
                AppendLine(sb, ref first, endMs.ToString(CultureInfo.InvariantCulture) + ":1");
            }

            return sb.ToString();
        }

        // --- helpers ------------------------------------------------------------------------------

        private static void AppendLine(StringBuilder sb, ref bool first, string line)
        {
            if (!first) sb.Append('\n');
            first = false;
            sb.Append(line);
        }

        // A line whose first token (comments stripped) is the word 'hold'.
        private static bool IsHoldHeader(string raw)
        {
            var line = StripComment(raw).Trim();
            if (line.Length < 4) return false;
            if (!line.StartsWith("hold", StringComparison.OrdinalIgnoreCase)) return false;
            if (line.Length == 4) return true;                 // bare 'hold' -> header (errors on args)
            char c = line[4];
            return c == ' ' || c == '\t';
        }

        private static bool TryParseHold(string raw, out double start, out double end,
            out double frac, out double slow, out double fast, out string err)
        {
            start = end = frac = slow = fast = 0; err = null;

            var line = StripComment(raw).Trim();
            var toks = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            // toks[0] is 'hold'; the "->" separator is optional sugar.
            var args = new List<string>();
            for (int k = 1; k < toks.Length; k++)
                if (toks[k] != "->") args.Add(toks[k]);

            if (args.Count != 5)
            { err = "expected 'hold <start> <beat-fraction> <slow> <fast> -> <end>'"; return false; }
            if (!double.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out start))
            { err = $"bad start time '{args[0]}'"; return false; }
            if (!VamLoopExpander.TryParseFraction(args[1], out frac))
            { err = $"bad beat fraction '{args[1]}'"; return false; }
            if (frac <= 0) { err = $"beat fraction must be > 0 ('{args[1]}')"; return false; }
            if (!double.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out slow))
            { err = $"bad slow speed '{args[2]}'"; return false; }
            if (!double.TryParse(args[3], NumberStyles.Any, CultureInfo.InvariantCulture, out fast))
            { err = $"bad fast speed '{args[3]}'"; return false; }
            if (!double.TryParse(args[4], NumberStyles.Any, CultureInfo.InvariantCulture, out end))
            { err = $"bad end time '{args[4]}'"; return false; }
            if (slow < 0) slow = 0;
            if (fast < 0) fast = 0;
            if (end < start) { err = $"end ({args[4]}) is before start ({args[0]})"; return false; }
            return true;
        }

        // Landing times (ms) of every catchable object with start <= Time <= end, ascending. The
        // beatmap's Objects list is already time-sorted, so this stays in order.
        private static List<double> CollectObjectTimes(VamBeatmap map, double start, double end)
        {
            var list = new List<double>();
            if (map == null || map.Objects == null) return list;
            foreach (var o in map.Objects)
                if (o.Time >= start && o.Time <= end) list.Add(o.Time);
            list.Sort();
            return list;
        }

        private static string StripComment(string raw)
        {
            if (raw == null) return "";
            var s = raw;
            int h = s.IndexOf('#'); if (h >= 0) s = s.Substring(0, h);
            int sl = s.IndexOf("//", StringComparison.Ordinal); if (sl >= 0) s = s.Substring(0, sl);
            return s;
        }
    }
}
