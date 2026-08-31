using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StorybrewScripts.Vam
{
    // Preprocessor for VAM-profile.txt: expands compact `loop <start> <beat-fraction> -> <end> ...
    // end` blocks into explicit time:value lines before any section parser runs, so it works for
    // [sv], top-level AR/HD, and [mod:*] alike. Value lines cycle in order and only WHOLE cycles are
    // emitted (a loop always ends on the last value). Times snap to the osu! beat grid and round
    // HALF UP, matching the editor tick-for-tick; the timing point at <start> anchors the whole block.
    public static class VamLoopExpander
    {
        // Hard safety cap so a huge span with a tiny fraction can't emit an unbounded file.
        private const int MaxKeyframesPerLoop = 200000;

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
                if (!IsLoopHeader(lines[i]))
                {
                    AppendLine(sb, ref first, lines[i]);
                    continue;
                }

                double start, end, stepMs, offset, beatLength;
                string err;
                if (!TryParseLoopHeader(lines[i], map, out start, out end, out stepMs, out offset, out beatLength, out err))
                {
                    errors.Add($"loop (line {i + 1}): {err}; block ignored");
                    i = SkipBody(lines, i);   // swallow through 'end' (or up to a section/EOF) so the body isn't parsed as keyframes
                    continue;
                }

                // Collect body value lines until 'end'. A section header, a nested loop, or EOF before
                // 'end' means the block is unterminated: report it and DON'T swallow that stopping line.
                var values = new List<string>();
                int j = i + 1;
                bool closed = false;
                for (; j < lines.Length; j++)
                {
                    var stripped = StripComment(lines[j]).Trim();
                    if (stripped.Length == 0) continue;                                      // blanks / comment-only lines
                    if (string.Equals(stripped, "end", StringComparison.OrdinalIgnoreCase)) { closed = true; break; }
                    if (stripped[0] == '[' || IsLoopHeader(lines[j])) break;                 // unterminated
                    values.Add(stripped);
                }

                if (!closed)
                {
                    errors.Add($"loop (line {i + 1}): no matching 'end'; block ignored");
                    i = j - 1;   // re-process the stopping line (section header / next loop / EOF) normally
                    continue;
                }
                if (values.Count == 0)
                {
                    errors.Add($"loop (line {i + 1}): no value lines between 'loop' and 'end'; block ignored");
                    i = j;
                    continue;
                }

                // Grid anchor: snap the requested start to the nearest real beat-fraction position, so
                // the emitted times sit exactly where the editor would place them.
                double k0 = Math.Floor((start - offset) / stepMs + 0.5);
                double baseT = offset + k0 * stepMs;

                // Collect every grid time in [start-snapped, end].
                var times = new List<long>();
                bool capped = false;
                for (int n = 0; ; n++)
                {
                    if (n > MaxKeyframesPerLoop) { capped = true; break; }
                    double exact = baseT + n * stepMs;
                    long t = (long)Math.Floor(exact + 0.5);   // osu!-style round half up
                    if (t > end) break;
                    if (t >= 0) times.Add(t);
                }
                if (capped)
                    errors.Add($"loop (line {i + 1}): exceeded {MaxKeyframesPerLoop} keyframes; truncated");

                // Emit only WHOLE cycles of the value list, so the loop always ends on the LAST value.
                // Any trailing partial cycle is dropped even when there is still room before <end>.
                int whole = (times.Count / values.Count) * values.Count;
                if (whole == 0)
                    errors.Add($"loop (line {i + 1}): span holds only {times.Count} keyframe(s), " +
                               $"fewer than one full cycle of {values.Count} value(s); nothing emitted");
                for (int k = 0; k < whole; k++)
                    AppendLine(sb, ref first, times[k].ToString(CultureInfo.InvariantCulture) + ":" + values[k % values.Count]);

                i = j;   // skip past 'end'
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

        // A line whose first token (comments stripped) is the word 'loop'.
        private static bool IsLoopHeader(string raw)
        {
            var line = StripComment(raw).Trim();
            if (line.Length < 4) return false;
            if (!line.StartsWith("loop", StringComparison.OrdinalIgnoreCase)) return false;
            if (line.Length == 4) return true;                 // bare 'loop' -> header (errors on args)
            char c = line[4];
            return c == ' ' || c == '\t';
        }

        // Skip from the loop header at 'i' through its 'end' (returns the index of 'end'). If no 'end'
        // is found before a section header / next loop / EOF, returns the last consumed line so the
        // outer loop re-processes the stopping line.
        private static int SkipBody(string[] lines, int i)
        {
            for (int j = i + 1; j < lines.Length; j++)
            {
                var stripped = StripComment(lines[j]).Trim();
                if (string.Equals(stripped, "end", StringComparison.OrdinalIgnoreCase)) return j;
                if (stripped.Length > 0 && (stripped[0] == '[' || IsLoopHeader(lines[j]))) return j - 1;
            }
            return lines.Length - 1;
        }

        private static bool TryParseLoopHeader(string raw, VamBeatmap map, out double start, out double end,
            out double stepMs, out double offset, out double beatLength, out string err)
        {
            start = end = stepMs = offset = beatLength = 0; err = null;

            var line = StripComment(raw).Trim();
            var toks = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            // toks[0] is 'loop'; the "->" separator is optional sugar.
            var args = new List<string>();
            for (int k = 1; k < toks.Length; k++)
                if (toks[k] != "->") args.Add(toks[k]);

            if (args.Count != 3) { err = "expected 'loop <start> <beat-fraction> -> <end>'"; return false; }
            if (!double.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out start))
            { err = $"bad start time '{args[0]}'"; return false; }
            double frac;
            if (!TryParseFraction(args[1], out frac)) { err = $"bad beat fraction '{args[1]}'"; return false; }
            if (frac <= 0) { err = $"beat fraction must be > 0 ('{args[1]}')"; return false; }
            if (!double.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out end))
            { err = $"bad end time '{args[2]}'"; return false; }
            if (end < start) { err = $"end ({args[2]}) is before start ({args[0]})"; return false; }

            var timing = map != null ? map.TimingAt(start) : new VamTiming(0, 500.0);
            offset = timing.Time;
            beatLength = timing.BeatLength > 0 ? timing.BeatLength : 500.0;
            stepMs = frac * beatLength;
            if (stepMs <= 0) { err = "computed step is not positive"; return false; }
            return true;
        }

        // "a/b" (any real a, b) or a bare decimal. Returns the fraction of a beat.
        private static bool TryParseFraction(string s, out double frac)
        {
            frac = 0;
            if (string.IsNullOrEmpty(s)) return false;
            int slash = s.IndexOf('/');
            if (slash >= 0)
            {
                var np = s.Substring(0, slash).Trim();
                var dp = s.Substring(slash + 1).Trim();
                double num, den;
                if (!double.TryParse(np, NumberStyles.Any, CultureInfo.InvariantCulture, out num)) return false;
                if (!double.TryParse(dp, NumberStyles.Any, CultureInfo.InvariantCulture, out den)) return false;
                if (den == 0) return false;
                frac = num / den;
                return true;
            }
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out frac);
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
