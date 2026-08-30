using System.Collections.Generic;
using StorybrewCommon.Storyboarding;

namespace StorybrewScripts.Vam
{
    // Only component that writes storyboard commands. Turns a finished plan into up to three
    // sprites (glow, body, overlay), each following the shared path with role-specific tweaks.
    public sealed class VamEmitter
    {
        public void Emit(StoryboardLayer layer, VamPlan p)
        {
            if (p.DrawGlow)
            {
                var g = layer.CreateSprite(p.BodyPath, OsbOrigin.Centre);
                EmitMove(g, p.Position);
                EmitScale(g, p, p.GlowScale);
                g.Color(p.SpawnTime, p.GlowColor);
                g.Additive(p.SpawnTime);
                EmitRot(g, p);
                EmitOpacity(g, p, p.GlowAlpha);
                EmitEnding(g, p, p.GlowAlpha);
            }

            var body = layer.CreateSprite(p.BodyPath, OsbOrigin.Centre);
            EmitMove(body, p.Position);
            EmitScale(body, p, 1.0);
            if (p.BodyColor.HasValue) body.Color(p.SpawnTime, p.BodyColor.Value);
            EmitRot(body, p);
            EmitOpacity(body, p, 1.0);
            EmitEnding(body, p, 1.0);

            if (!string.IsNullOrEmpty(p.OverlayPath))
            {
                var ov = layer.CreateSprite(p.OverlayPath, OsbOrigin.Centre);
                EmitMove(ov, p.Position);
                EmitScale(ov, p, 1.0);
                EmitRot(ov, p);
                EmitOpacity(ov, p, 1.0);
                EmitEnding(ov, p, 1.0);
            }
        }

        // Static size, or an animated spawn->catch size when ScaleStart is set (banana spin).
        static void EmitScale(OsbSprite s, VamPlan p, double factor)
        {
            if (double.IsNaN(p.ScaleStart) || p.ScaleStart == p.Scale)
                s.Scale(p.SpawnTime, p.Scale * factor);
            else
                s.Scale(OsbEasing.None, p.SpawnTime, p.CatchTime, p.ScaleStart * factor, p.Scale * factor);
        }

        static void EmitMove(OsbSprite s, Track<Vec2> pos)
        {
            for (int i = 0; i < pos.Count - 1; i++)
            {
                var a = pos[i]; var b = pos[i + 1];
                s.Move(b.Ease, a.Time, b.Time, a.Value.X, a.Value.Y, b.Value.X, b.Value.Y);
            }
        }

        static void EmitRot(OsbSprite s, VamPlan p)
        {
            if (p.RotStart == p.RotEnd) { if (p.RotStart != 0) s.Rotate(p.SpawnTime, p.RotStart); }
            else s.Rotate(OsbEasing.None, p.SpawnTime, p.CatchTime, p.RotStart, p.RotEnd);
        }

        // Opacity across [spawn, catch] = the Fade In factor (0 -> 1 near the top) times the HD
        // fade-out factor (1 -> HdRemain lower down), emitted as linear pieces between the two
        // windows' breakpoints. Culled (0) before spawn. With neither active this is the original
        // instant 0 -> peak at spawn.
        static void EmitOpacity(OsbSprite s, VamPlan p, double peak)
        {
            if (!p.FadeIn && !p.HdFade) { s.Fade(p.SpawnTime, p.SpawnTime, 0, peak); return; }

            var times = new List<double> { p.SpawnTime };
            if (p.FadeIn) { AddTime(times, p, p.FiStart); AddTime(times, p, p.FiEnd); }
            if (p.HdFade) { AddTime(times, p, p.HdStart); AddTime(times, p, p.HdEnd); }
            times.Sort();
            var uniq = new List<double>();
            foreach (var t in times)
                if (uniq.Count == 0 || t > uniq[uniq.Count - 1] + 1e-6) uniq.Add(t);

            // Appear: instant to the composed alpha at spawn (0 while Fade In still hides it).
            double a0 = peak * FadeInFactor(p, uniq[0]) * HdFactor(p, uniq[0]);
            s.Fade(p.SpawnTime, p.SpawnTime, 0, a0);

            for (int i = 0; i < uniq.Count - 1; i++)
            {
                double t0 = uniq[i], t1 = uniq[i + 1];
                double aS = peak * FadeInFactor(p, t0) * HdFactor(p, t0);
                double aE = peak * FadeInFactor(p, t1) * HdFactor(p, t1);

                // Where both windows overlap the product of two ramps is quadratic; split at the
                // midpoint so the straight segments track it. Elsewhere one factor is flat -> exact.
                bool fiMoves = p.FadeIn && t1 > p.FiStart && t0 < p.FiEnd;
                bool hdMoves = p.HdFade && t1 > p.HdStart && t0 < p.HdEnd;
                if (fiMoves && hdMoves && t1 > t0)
                {
                    double tm = 0.5 * (t0 + t1);
                    double aM = peak * FadeInFactor(p, tm) * HdFactor(p, tm);
                    s.Fade(t0, tm, aS, aM);
                    s.Fade(tm, t1, aM, aE);
                }
                else if (aS != aE)
                {
                    s.Fade(t0, t1, aS, aE);
                }
            }
        }

        static void AddTime(List<double> times, VamPlan p, double t)
        {
            if (t < p.SpawnTime) t = p.SpawnTime;
            if (t > p.CatchTime) t = p.CatchTime;
            times.Add(t);
        }

        // 0 before FiStart, ramps 0 -> 1 over [FiStart, FiEnd], 1 after. 1 when Fade In is off.
        static double FadeInFactor(VamPlan p, double t)
        {
            if (!p.FadeIn) return 1.0;
            if (t <= p.FiStart) return 0.0;
            if (t >= p.FiEnd) return 1.0;
            return (t - p.FiStart) / (p.FiEnd - p.FiStart);
        }

        // 1 before HdStart, ramps 1 -> HdRemain over [HdStart, HdEnd], HdRemain after. 1 when HD is off.
        static double HdFactor(VamPlan p, double t)
        {
            if (!p.HdFade) return 1.0;
            if (t <= p.HdStart) return 1.0;
            if (t >= p.HdEnd) return p.HdRemain;
            double u = (t - p.HdStart) / (p.HdEnd - p.HdStart);
            return 1.0 + (p.HdRemain - 1.0) * u;
        }

        static void EmitEnding(OsbSprite s, VamPlan p, double peak)
        {
            if (!p.TriggerDriven) { s.Fade(p.CatchTime, 0); return; }

            var cp = p.CatchPoint;
            s.Move(OsbEasing.None, p.CatchTime, p.CatchTime + p.MissExitDur, cp.X, cp.Y, cp.X, p.ExitY);
            double endOpacity = peak * p.HdRemain;
            if (endOpacity > 0.0) s.Fade(p.CatchTime, p.CatchTime + p.MissFadeDur, endOpacity, 0);
            if (p.MissRotEnd != p.RotEnd) s.Rotate(OsbEasing.Out, p.CatchTime, p.CatchTime + p.MissFadeDur, p.RotEnd, p.MissRotEnd);

            s.StartTriggerGroup("HitSound", p.TrigWinStart, p.TrigWinEnd);
            s.Scale(0.0, 0.0);
            s.EndGroup();
        }
    }
}
