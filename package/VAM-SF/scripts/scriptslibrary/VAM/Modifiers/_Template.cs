namespace StorybrewScripts.Vam
{
    // Copy this file, rename the class, and fill in AppliesTo + Apply. It is auto-discovered
    // once EnableModifiers is on. This template does nothing (AppliesTo returns false).
    public sealed class _Template : VamModifier
    {
        public override bool AppliesTo(VamObject obj, VamContext ctx) { return false; }

        public override void Apply(VamPlan plan, VamObject obj, VamContext ctx)
        {
            // edit plan.Position (ShiftSpawn / SplitFall / add keyframes), plan.Scale, etc.
        }
    }
}
