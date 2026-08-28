using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewScripts.Vam
{
    // One keyframe from a [mod:*] block: 'time : value [: anything]'. Raw keeps the rest of the
    // line so a mod can read its own trailing flags (easing, etc.) if it wants them.
    public sealed class VamModKey
    {
        public double Time;
        public double Value;
        public string Raw;
    }

    // Config for a single [mod:name] block in VAM-profile.txt. Lines whose first token is a number
    // are keyframes (time:value); every other line is a setting (key: value). A mod reads its own
    // block with ctx.Mod("name") - the engine never has to change to add a mod.
    public sealed class VamModConfig
    {
        public readonly string Name;
        public bool Present;                                     // was the [mod:name] block found
        public readonly List<VamModKey> Keyframes = new List<VamModKey>();
        private readonly Dictionary<string, string> _settings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public VamModConfig(string name) { Name = name; }

        public bool HasKeyframes { get { return Keyframes.Count > 0; } }

        public string Get(string key, string def = null)
        {
            string v; return _settings.TryGetValue(key, out v) ? v : def;
        }
        public double GetDouble(string key, double def)
        {
            var v = Get(key); double d;
            return v != null && double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out d) ? d : def;
        }
        public bool GetBool(string key, bool def)
        {
            var v = Get(key);
            if (v == null) return def;
            v = v.Trim().ToLowerInvariant();
            return v == "1" || v == "true" || v == "yes" || v == "on";
        }

        internal void AddSetting(string k, string v) { _settings[k.Trim()] = v.Trim(); }

        // Parse every [mod:*] block out of the profile text. Top-level (unsectioned) AR/HD lines
        // and any non-mod sections are ignored here. Comments (# or //) and blanks are skipped.
        public static Dictionary<string, VamModConfig> ParseAll(string text)
        {
            var map = new Dictionary<string, VamModConfig>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return map;

            VamModConfig cur = null;
            foreach (var raw0 in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var raw = raw0;
                int h = raw.IndexOf('#'); if (h >= 0) raw = raw.Substring(0, h);
                int s = raw.IndexOf("//", StringComparison.Ordinal); if (s >= 0) raw = raw.Substring(0, s);
                var line = raw.Trim();
                if (line.Length == 0) continue;

                if (line[0] == '[')
                {
                    cur = null;
                    var inside = line.Trim('[', ']').Trim();
                    if (inside.StartsWith("mod:", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = inside.Substring(4).Trim();
                        if (name.Length > 0 && !map.TryGetValue(name, out cur))
                        {
                            cur = new VamModConfig(name) { Present = true };
                            map[name] = cur;
                        }
                    }
                    continue;
                }
                if (cur == null) continue;

                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                var left = line.Substring(0, colon).Trim();
                var right = line.Substring(colon + 1).Trim();

                double t;
                if (double.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out t))
                {
                    var vtok = right.Split(':')[0].Trim();
                    double val;
                    if (double.TryParse(vtok, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                        cur.Keyframes.Add(new VamModKey { Time = t, Value = val, Raw = right });
                }
                else
                {
                    cur.AddSetting(left, right);
                }
            }
            return map;
        }
    }
}
