namespace CitrusVortex.Targets.Osu
{
    public class OsuTimingPoint
    {
        public int Time { get; set; }
        public double BeatLength { get; set; }
        public int Meter { get; set; }
        public OsuSampleSet SampleSet { get; set; }
        public int SampleIndex { get; set; }
        public int Volume { get; set; }
        public bool Uninherited { get; set; }
        public bool IsKiaiTime { get; set; }
        public bool OmitFirstBarLine { get; set; }
    }
}
