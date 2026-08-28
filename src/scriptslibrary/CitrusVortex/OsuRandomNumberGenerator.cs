using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrusVortex.Targets.Osu
{
    // For compatibility purposes
    // https://github.com/ppy/osu/blob/master/osu.Game/Utils/LegacyRandom.cs
    public class OsuRandomNumberGenerator
    {
        private const int _OSU_SEED = 1337;

        private const double _INT_TO_REAL = 1.0 / (int.MaxValue + 1.0);
        private const uint _INT_MASK = 0x7FFFFFFF;
        private const uint _INITIAL_Y = 842502087;
        private const uint _INITIAL_Z = 3579807591;
        private const uint _INITIAL_W = 273326509;

        private uint _y = _INITIAL_Y;
        private uint _z = _INITIAL_Z;
        private uint _w = _INITIAL_W;
        private uint _x = _OSU_SEED;

        public uint NextUInt()
        {
            uint t = _x ^ _x << 11;
            _x = _y;
            _y = _z;
            _z = _w;
            return _w = _w ^ _w >> 19 ^ t ^ t >> 8;
        }

        public double NextDouble() => _INT_TO_REAL * Next();

        public int Next() => (int)(_INT_MASK & NextUInt());

        public int Next(int max) => (int)(NextDouble() * max);

        public int Next(int min, int max) => (int)(min + NextDouble() * (max - min));

        public int Next(double min, double max) => (int)(min + NextDouble() * (max - min));

        private uint _bitBuffer;
        private int _bitIndex = 32;

        public bool NextBool()
        {
            if (_bitIndex == 32)
            {
                _bitBuffer = NextUInt();
                _bitIndex = 1;

                return (_bitBuffer & 1) == 1;
            }

            _bitIndex++;

            return ((_bitBuffer >>= 1) & 1) == 1;
        }

        // Do not use to skip NextBool
        public void SkipUInt(int count)
        {
            for (int i = 0; i < count; i++)
                _ = NextUInt();
        }
    }
}
