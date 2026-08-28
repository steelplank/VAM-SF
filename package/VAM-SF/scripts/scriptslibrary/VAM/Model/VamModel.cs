using System.Collections.Generic;

namespace StorybrewScripts.Vam
{
    // The kind of catch object, mirroring osu!catch's renderable elements.
    public enum VamObjectType
    {
        Fruit,        // a full fruit (from a circle, slider head/tail/repeat)
        Droplet,      // a large droplet along a slider path
        TinyDroplet,  // a small droplet along a slider path
        Banana        // a banana from a banana shower (spinner)
    }

    // A single, flattened, renderable catch object.
    // This is the framework's own model: it decouples every effect from the
    // CtbLoader tree (juice streams / banana showers are already expanded here).
    public class VamObject
    {
        public VamObjectType Type;

        // osu! playfield x (0..512). For tiny droplets this already includes the x-offset.
        public double X;

        // The exact time (ms) the object reaches the catcher. This never changes with
        // fake AR: only the *approach* is faked, the landing stays 1:1 with the map.
        public int Time;

        // Combo colour (fruits/droplets). Bananas ignore this (rainbow).
        public byte R = 255, G = 255, B = 255;

        // True when the object is (or precedes) a hyperdash, per osu!'s algorithm.
        public bool HyperDash;

        // Index of the object inside the flattened, time-sorted list (filled by the loader).
        public int Index;

        // Index of the SOURCE hit object in the beatmap (nested objects share their parent's).
        // osu! picks the fruit visual (pear/grape/orange/apple) from IndexInBeatmap % 4.
        public int IndexInBeatmap;

        // Per-banana random start scale (osu spins bananas from this down to 0.6). NaN = unset.
        public double ScaleStart = double.NaN;
    }

    // The full flattened beatmap plus difficulty data the effects need.
    public class VamBeatmap
    {
        public List<VamObject> Objects = new List<VamObject>();

        public double CircleSize;
        public double ApproachRate;   // the map's REAL AR (used for calibration / defaults)

        // osu! [General] countdown settings, surfaced for the CatchCountdown effect (the playfield
        // cover hides osu!'s own countdown, so we redraw it in the storyboard).
        //   CountdownMode: 0 = None, 1 = Normal, 2 = Half (slower), 3 = Double (faster).
        //   CountdownOffset: beats the countdown is shifted earlier by (from the .osu).
        //   BeatLengthAtStart: ms per beat of the uninherited timing point in force at the first object.
        public int CountdownMode;
        public int CountdownOffset;
        public double BeatLengthAtStart = 500.0; // sane default (120 BPM) if the map has no timing

        // Convenience: catchable objects only (everything except... nothing is excluded here,
        // all of fruit/droplet/tiny/banana are catchable, but bananas don't take part in hyperdash).
    }
}
