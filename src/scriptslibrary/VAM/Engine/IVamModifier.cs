namespace StorybrewScripts.Vam
{
    // The whole extension surface: one predicate, one transform.
    public interface IVamModifier
    {
        string Name { get; }
        int Order { get; }                                  // lower runs earlier
        bool AppliesTo(VamObject obj, VamContext ctx);
        void Apply(VamPlan plan, VamObject obj, VamContext ctx);
    }

    // Base class with sane defaults. Author overrides AppliesTo + Apply.
    public abstract class VamModifier : IVamModifier
    {
        public virtual string Name { get { return GetType().Name; } }
        public virtual int Order { get { return 100; } }

        public abstract bool AppliesTo(VamObject obj, VamContext ctx);
        public abstract void Apply(VamPlan plan, VamObject obj, VamContext ctx);

        // true when obj.Time is inside [start,end]; (0,0) means the whole map.
        protected static bool TimeWindow(VamObject obj, double startMs, double endMs)
        {
            if (startMs == 0 && endMs == 0) return true;
            return obj.Time >= startMs && obj.Time <= endMs;
        }

        protected static bool OnlyFruits(VamObject obj) { return obj.Type == VamObjectType.Fruit; }
        protected static bool EveryNth(VamObject obj, int n) { return n > 0 && (obj.Index % n) == 0; }
    }
}
