using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StorybrewScripts.Vam
{
    // Runs the default builder, then the modifier chain, per object. Modifiers are auto-discovered
    // by reflection (any IVamModifier with a parameterless ctor). Failures are isolated and logged.
    public sealed class VamPipeline
    {
        public readonly List<IVamModifier> Modifiers = new List<IVamModifier>();
        readonly Action<string> log;
        int warned;

        public VamPipeline(Action<string> log) { this.log = log; }

        public void DiscoverModifiers()
        {
            Type[] types;
            try { types = Assembly.GetExecutingAssembly().GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            catch (Exception e) { Log("modifier discovery failed: " + e.Message); return; }

            foreach (var t in types)
            {
                if (t == null || t.IsAbstract || t.IsInterface) continue;
                if (!typeof(IVamModifier).IsAssignableFrom(t)) continue;
                if (t.GetConstructor(Type.EmptyTypes) == null) continue;
                try { Modifiers.Add((IVamModifier)Activator.CreateInstance(t)); }
                catch (Exception e) { Log("could not construct " + t.Name + ": " + e.Message); }
            }
            Modifiers.Sort((a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName);
            });
        }

        public VamPlan Run(VamPlanBuilder builder, VamObject obj, VamContext ctx)
        {
            var plan = builder.Build(obj);
            foreach (var m in Modifiers)
            {
                bool applies;
                try { applies = m.AppliesTo(obj, ctx); }
                catch (Exception e) { Warn(m.Name + ".AppliesTo threw: " + e.Message); continue; }
                if (!applies) continue;
                try { m.Apply(plan, obj, ctx); }
                catch (Exception e) { Warn(m.Name + ".Apply threw: " + e.Message); }
            }
            CheckCatchAnchor(plan, ctx);
            return plan;
        }

        void CheckCatchAnchor(VamPlan p, VamContext ctx)
        {
            if (!p.KeepsCatchAnchor) return;
            var last = p.Position.Last;
            if (Math.Abs(last.Time - p.CatchTime) > 0.5 || Math.Abs(last.Value.Y - ctx.Geometry.CatchY) > 0.5)
                Warn("a modifier moved the catch keyframe but left KeepsCatchAnchor = true (gameplay may desync)");
        }

        void Warn(string m) { if (warned++ < 8) Log(m); }
        void Log(string m) { if (log != null) log("VAM modifiers: " + m); }
    }
}
