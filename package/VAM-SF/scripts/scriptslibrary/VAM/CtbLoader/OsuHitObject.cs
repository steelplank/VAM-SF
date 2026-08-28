namespace CtbLoader.Targets.Osu
{
    public abstract class OsuHitObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Time { get; set; }
        public bool IsNewCombo { get; set; }
        public int ComboColorSkipAmount { get; set; }
        public OsuHitSound HitSound { get; set; }
        public OsuHitSample HitSample { get; set; }
    }
}
