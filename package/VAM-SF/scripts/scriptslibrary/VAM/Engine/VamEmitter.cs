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
                EmitFadeIn(g, p, p.GlowAlpha);
                EmitEnding(g, p, p.GlowAlpha);
            }

            var body = layer.CreateSprite(p.BodyPath, OsbOrigin.Centre);
            EmitMove(body, p.Position);
            EmitScale(body, p, 1.0);
            if (p.BodyColor.HasValue) body.Color(p.SpawnTime, p.BodyColor.Value);
            EmitRot(body, p);
            EmitFadeIn(body, p, 1.0);
            EmitEnding(body, p, 1.0);

            if (!string.IsNullOrEmpty(p.OverlayPath))
            {
                var ov = layer.CreateSprite(p.OverlayPath, OsbOrigin.Centre);
                EmitMove(ov, p.Position);
                EmitScale(ov, p, 1.0);
                EmitRot(ov, p);
                EmitFadeIn(ov, p, 1.0);
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

        // Instant fade 0 -> peak at spawn (held at 0 before spawn so the sprite stays culled),
        // then the optional HD fade toward peak*HdRemain.
        static void EmitFadeIn(OsbSprite s, VamPlan p, double peak)
        {
            if (!p.HdFade) { s.Fade(p.SpawnTime, p.SpawnTime, 0, peak); return; }
            double target = peak * p.HdRemain;
            double fs = p.HdStart < p.SpawnTime ? p.SpawnTime : p.HdStart;
            double fe = p.HdEnd;
            if (fe <= fs) { s.Fade(p.SpawnTime, p.SpawnTime, 0, target); return; }
            s.Fade(p.SpawnTime, p.SpawnTime, 0, peak);
            s.Fade(fs, fe, peak, target);
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
