// A C# implementation of the PRNG found at http://burtleburtle.net/bob/rand/smallprng.html by Bob Jenkins
// This is released into the public domain as was the original C version

using System;
using System.Runtime.CompilerServices;

namespace JSFDN
{
    public class JSFRng : Random
    {
        private uint a;
        private uint b;
        private uint c;
        private uint d;

        public JSFRng(int seed)
        {
            a = 0xf1ea5eed;
            b = (uint)seed;
            c = (uint)seed;
            d = (uint)seed;

            for (int i = 0; i < 20; i++)
            {
                _ = Next();
            }
        }

        public JSFRng(uint a, uint b, uint c, uint d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public JSFRng(string stateString)
        {
            string[] vals = stateString.Split(",");
            a = Convert.ToUInt32(vals[0]);
            b = Convert.ToUInt32(vals[1]);
            c = Convert.ToUInt32(vals[2]);
            d = Convert.ToUInt32(vals[3]);
        }

        public JSFRng DeepCopy()
        {
            JSFRng newJSFRng = new JSFRng(this.a, this.b, this.c, this.d);
            return (newJSFRng);
        }

        public (uint, uint, uint, uint) ExportState()
        {
            return ((a, b, c, d));
        }

        public string ExportStateString()
        {
            return ($"{a},{b},{c},{d}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rotate(uint x, int k)
        {
            return ((uint)((uint)(x) << (k)) | ((uint)(x) >> (32 - (k))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextU()
        {
            uint e = a - Rotate(b, 27);
            a = b ^ Rotate(c, 17);
            b = c + d;
            c = d + e;
            d = e + a;

            return (d);
        }

        public uint NextU(uint s)
        {
            uint x = NextU();
            ulong m = (ulong)s * (ulong)x;
            ulong l = (uint)m; // Mod 2^32

            if (l < ((ulong)s))
            {
                ulong t = (UInt32.MaxValue - ((ulong)s)) % ((ulong)s);
                while (l < t)
                {
                    x = NextU();
                    m = (ulong)s * (ulong)x;
                    l = (uint)m;
                }
            }

            return ((uint)(m >> 32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Next()
        {
            uint e = a - Rotate(b, 27);
            a = b ^ Rotate(c, 17);
            b = c + d;
            c = d + e;
            d = e + a;

            uint t = d >> 31;
            return ((int)((d + t) ^ t));
        }

        //See algorithm in https://arxiv.org/pdf/1805.10941.pdf
        public override int Next(int s)
        {
            uint x = NextU();
            ulong m = (ulong)s * (ulong)x;
            ulong l = (uint)m; // Mod 2^32

            if (l < ((ulong)s))
            {
                ulong t = (UInt32.MaxValue - ((ulong)s)) % ((ulong)s);
                while (l < t)
                {
                    x = NextU();
                    m = (ulong)s * (ulong)x;
                    l = (uint)m;
                }
            }

            uint r = (uint)(m >> 32);
            uint j = r >> 31;
            return ((int)((r + j) ^ j));
        }

        public override void NextBytes(Span<byte> buffer)
        {
            int i = 0;
            uint t = 0;
            for (; i <= (buffer.Length - 4); i += 4)
            {
                t = NextU();
                buffer[i] = (byte)t;
                buffer[i + 1] = (byte)(t >> 8);
                buffer[i + 2] = (byte)(t >> 16);
                buffer[i + 3] = (byte)(t >> 24);
            }

            if (buffer.Length % 4 != 0)
            {
                t = NextU();
                for (; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)(t >> ((i % 4) * 8));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double Sample()
        {
            return ((double)NextU() * (1.0 / (double)UInt32.MaxValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double NextDouble()
        {
            return (Sample());
        }

    }
}
