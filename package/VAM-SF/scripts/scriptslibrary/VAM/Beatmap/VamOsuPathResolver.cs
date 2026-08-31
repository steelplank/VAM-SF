using System.IO;

namespace StorybrewScripts.Vam
{
    // storybrew doesn't expose the .osu path, so we locate the difficulty file in the mapset folder
    // ourselves. Matches by BeatmapID, then difficulty Version, then falls back to the only .osu.
    public static class VamOsuPathResolver
    {
        public static string Resolve(string mapsetPath, string version, long beatmapId)
        {
            if (string.IsNullOrEmpty(mapsetPath) || !Directory.Exists(mapsetPath))
                return null;

            var files = Directory.GetFiles(mapsetPath, "*.osu");
            if (files.Length == 0) return null;
            if (files.Length == 1) return files[0];

            if (beatmapId > 0)
            {
                var byId = FindByField(files, "BeatmapID:", v =>
                    long.TryParse(v, out var id) && id == beatmapId);
                if (byId != null) return byId;
            }

            if (!string.IsNullOrEmpty(version))
            {
                var byVersion = FindByField(files, "Version:", v => v == version);
                if (byVersion != null) return byVersion;
            }

            return files[0];
        }

        private static string FindByField(string[] files, string field, System.Func<string, bool> match)
        {
            foreach (var file in files)
            {
                foreach (var raw in File.ReadLines(file))
                {
                    var line = raw.Trim();
                    if (line.StartsWith(field))
                    {
                        if (match(line.Substring(field.Length).Trim()))
                            return file;
                        break; // field present but no match -> try next file
                    }
                    // stop scanning once we're clearly past the metadata block
                    if (line.StartsWith("[TimingPoints]") || line.StartsWith("[HitObjects]"))
                        break;
                }
            }
            return null;
        }
    }
}
