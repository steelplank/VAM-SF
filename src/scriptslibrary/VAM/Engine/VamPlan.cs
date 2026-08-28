using SColor = System.Drawing.Color;

namespace StorybrewScripts.Vam
{
    // One object's full visual life. Built by VamPlanBuilder, rewritten by modifiers,
    // consumed by VamEmitter. Times in ms, coords in storyboard space.
    // Opacity/rotation/ending are stored as resolved values (not tracks yet); Position is
    // the editable path. Modifiers mostly touch Position via the helpers.
    public sealed class VamPlan
    {
        public readonly VamObject Source;
        public VamPlan(VamObject source) { Source = source; }

        // approach window (rotation, fade and HD are keyed to these)
        public double SpawnTime;
        public double CatchTime;
        public double Preempt;

        // path: default [(SpawnTime, spawnX,SpawnY), (CatchTime, catchX,CatchY)]
        public readonly Track<Vec2> Position = new Track<Vec2>();

        // size (per-type factor already applied). Scale is the value at CatchTime; when
        // ScaleStart is set (not NaN) the size animates ScaleStart -> Scale over spawn..catch
        // (osu's banana spin). NaN = static size held at Scale.
        public double Scale;
        public double ScaleStart = double.NaN;

        // rotation in radians; MissRotEnd is where a missed fruit rotates to
        public double RotStart, RotEnd, MissRotEnd;

        // opacity, peak normalized to 1.0 (emitter scales by each sprite's peak)
        public bool HdFade;
        public double HdStart, HdEnd;   // fade window (ms)
        public double HdRemain = 1.0;   // fraction of peak left at the catch

        // ending
        public bool TriggerDriven;      // miss default + HitSound catch trigger
        public double TrigWinStart, TrigWinEnd;
        public double MissExitDur, MissFadeDur, ExitY;

        // sprites
        public string BodyPath;
        public string OverlayPath;      // null = no overlay
        public SColor? BodyColor;       // null = untinted
        public bool DrawGlow;
        public SColor GlowColor;
        public double GlowScale = 1.2, GlowAlpha = 0.7;

        // false = author accepts breaking 1:1 (skips the catch-anchor warning)
        public bool KeepsCatchAnchor = true;

        // catch point = last path keyframe (miss continues straight down from here)
        public Vec2 CatchPoint { get { return Position.Last.Value; } }

        // Move the spawn keyframe; catch stays put, so a straight fall becomes a diagonal.
        public void ShiftSpawn(double dx, double dy)
        {
            var k = Position.First;
            k.Value = new Vec2(k.Value.X + dx, k.Value.Y + dy);
        }

        // Insert a midpoint at 'atFraction' of the path and re-time it so the two segments
        // run at speeds 1 : secondSpeedFactor, still arriving at CatchTime.
        public void SplitFall(double atFraction, double secondSpeedFactor)
        {
            if (Position.Count < 2 || atFraction <= 0 || atFraction >= 1) return;
            if (secondSpeedFactor <= 0) secondSpeedFactor = 1;
            var a = Position.First;
            var b = Position.Last;
            var mid = Vec2.Lerp(a.Value, b.Value, atFraction);
            double d1 = atFraction, d2 = 1.0 - atFraction;
            double total = b.Time - a.Time;
            double v = (d1 + d2 / secondSpeedFactor) / total;   // base units per ms
            double midTime = a.Time + d1 / v;
            Position.Add(midTime, mid, b.Ease);
        }
    }
}
