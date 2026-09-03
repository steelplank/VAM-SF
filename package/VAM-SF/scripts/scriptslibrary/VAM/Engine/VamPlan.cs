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
        public double HdStart, HdEnd;   // fade-OUT window (ms): peak -> peak*HdRemain
        public double HdRemain = 1.0;   // fraction of peak left at the catch
        public double HdStartFrac, HdEndFrac;   // fall-height fractions; the ms window above is derived from these on the FINAL (post-SV) path so the fade tracks screen height, not clock time

        // fake mania Fade In: the object is invisible at spawn and fades IN over a band near the
        // top of the fall (0 -> peak over [FiStart, FiEnd]), fully visible below it. Composes with
        // HD: the emitter multiplies the fade-in and HD fade-out factors, so the two carve a visible
        // reading window out of the middle of the fall.
        public bool FadeIn;
        public double FiStart, FiEnd;   // fade-in window (ms): 0 -> peak
        public double FiStartFrac, FiEndFrac;   // fall-height fractions (see HD note); derived to ms on the final path

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

        // Time the fall passes a given storyboard Y, read off the FINAL (post-SV) path so fades track
        // screen height, not clock time. Fall is monotonic; flat SV-freeze segments are skipped.
        public double TimeAtY(double y)
        {
            var keys = Position.Keys;
            if (keys.Count == 0) return SpawnTime;
            if (keys.Count == 1) return keys[0].Time;
            if (y <= keys[0].Value.Y) return keys[0].Time;
            if (y >= keys[keys.Count - 1].Value.Y) return keys[keys.Count - 1].Time;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                double y0 = keys[i].Value.Y, y1 = keys[i + 1].Value.Y;
                if (y1 > y0 && y >= y0 && y <= y1)
                    return keys[i].Time + (keys[i + 1].Time - keys[i].Time) * ((y - y0) / (y1 - y0));
            }
            return keys[keys.Count - 1].Time;
        }

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
