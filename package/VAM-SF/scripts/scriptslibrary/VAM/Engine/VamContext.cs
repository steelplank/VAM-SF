namespace StorybrewScripts.Vam
{
    // Read-only map/effect state handed to every modifier. Never mutated.
    public sealed class VamContext
    {
        public VamBeatmap Beatmap;
        public VamGeometry Geometry;
        public double CircleSize;
        public double MapApproachRate;
        public VamProfile Profile;      // AR/HD timeline; may be null

        // Parsed [mod:*] blocks from VAM-profile.txt. A mod reads its own block via Mod("name");
        // no engine change is ever needed to add a mod.
        public System.Collections.Generic.Dictionary<string, VamModConfig> Mods;
        public VamModConfig Mod(string name)
        {
            VamModConfig c;
            if (Mods != null && Mods.TryGetValue(name, out c)) return c;
            return new VamModConfig(name);   // empty, Present = false
        }

        // ms preempt for an AR (osu TimeRange).
        public double PreemptFor(double ar) { return VamGeometry.Preempt(ar); }
    }

    // osu StatelessRNG, reproduced exactly. RandomSingle(series) = NextSingle((int)StartTime, series).
    public static class VamRng
    {
        static ulong Mix(ulong x)
        {
            unchecked
            {
                x ^= x >> 33; x *= 0xff51afd7ed558ccd;
                x ^= x >> 33; x *= 0xc4ceb9fe1a85ec53;
                x ^= x >> 33; return x;
            }
        }
        static ulong NextULong(int seed, int series)
        {
            unchecked { return Mix((((ulong)(uint)series << 32) | (uint)seed) ^ 0x12345678); }
        }
        public static float NextSingle(int seed, int series)
            => (float)(NextULong(seed, series) & ((1 << 24) - 1)) / (1 << 24);
    }
}
