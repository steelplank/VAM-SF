namespace StorybrewScripts.Vam
{
    // Proof-of-concept modifier. In a time window, start each object offset to the side so it
    // slides in diagonally, still landing on the plate at CatchTime. Off by default; set
    // Enabled = true (edit here) to test. Alternates side by index for a criss-cross.
    public sealed class SpawnOffset : VamModifier
    {
        public bool Enabled = false;
        public double StartMs = 0, EndMs = 0;   // 0,0 = whole map
        public double OffsetX = 200;

        public override bool AppliesTo(VamObject obj, VamContext ctx)
        {
            return Enabled && TimeWindow(obj, StartMs, EndMs);
        }

        public override void Apply(VamPlan plan, VamObject obj, VamContext ctx)
        {
            double dir = (obj.IndexInBeatmap % 2 == 0) ? -1.0 : 1.0;
            plan.ShiftSpawn(dir * OffsetX, 0);
        }
    }
}
