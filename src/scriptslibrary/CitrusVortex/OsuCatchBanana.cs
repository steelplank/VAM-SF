namespace CitrusVortex.Targets.Osu
{
    public class OsuCatchBanana : OsuCatchHitObject
    {
        // Start scale from the shared rng (stable: NextDouble*0.6+0.6). Spins down to 0.6.
        public double ScaleStart;
    }
}
