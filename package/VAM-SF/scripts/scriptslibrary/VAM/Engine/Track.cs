using System.Collections.Generic;
using StorybrewCommon.Storyboarding;

namespace StorybrewScripts.Vam
{
    // 2D point in storyboard space.
    public struct Vec2
    {
        public double X, Y;
        public Vec2(double x, double y) { X = x; Y = y; }
        public static Vec2 Lerp(Vec2 a, Vec2 b, double t)
            => new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }

    // One keyframe. Ease is the curve used to reach this keyframe from the previous one.
    public sealed class Key<T>
    {
        public double Time;
        public T Value;
        public OsbEasing Ease;
        public Key(double time, T value, OsbEasing ease) { Time = time; Value = value; Ease = ease; }
    }

    // Sorted keyframe list. The emitter walks consecutive keys into commands.
    public sealed class Track<T>
    {
        public readonly List<Key<T>> Keys = new List<Key<T>>();

        public int Count { get { return Keys.Count; } }
        public Key<T> this[int i] { get { return Keys[i]; } }
        public Key<T> First { get { return Keys[0]; } }
        public Key<T> Last { get { return Keys[Keys.Count - 1]; } }

        public void Add(double time, T value, OsbEasing ease = OsbEasing.None)
        {
            var k = new Key<T>(time, value, ease);
            int i = Keys.Count;
            while (i > 0 && Keys[i - 1].Time > time) i--;
            Keys.Insert(i, k);
        }
    }
}
