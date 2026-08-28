namespace CitrusVortex.Targets.Osu
{
    public class OsuVideo : OsuEvent
    {
        public string Filename { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }
    }
}
