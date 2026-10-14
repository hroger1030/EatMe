using System;

namespace EatMeLibrary
{
    /// <summary>
    /// A color packed into one uint in MonoGame's Color layout (0xAABBGGRR)
    /// Built so a server can work with colors compatible with monogame and 
    /// not require all the bloat.
    /// </summary>
    public readonly struct FastColor : IEquatable<FastColor>
    {
        private const float ONE_OVER_SIXTY = 1f / 60f;

        /// <summary>
        /// Fully transparent (alpha 0).
        /// </summary>
        public static readonly FastColor TRANSPARENT = new FastColor(0);

        public uint PackedValue { get; }

        public byte A => (byte)(PackedValue >> 24);
        public byte B => (byte)(PackedValue >> 16);
        public byte G => (byte)(PackedValue >> 8);
        public byte R => (byte)PackedValue;

        public FastColor(uint packedValue)
        {
            PackedValue = packedValue;
        }

        /// <summary>
        /// An opaque color.
        /// </summary>
        public static FastColor FromRgb(byte r, byte g, byte b)
        {
            return FromArgb(byte.MaxValue, r, g, b);
        }

        public static FastColor FromArgb(byte a, byte r, byte g, byte b)
        {
            return new FastColor((uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r);
        }

        /// <summary>
        /// An opaque color from hue (degrees, wrapped into 0 to 360), saturation and value (each clamped to 0 to 1).
        /// </summary>
        public static FastColor FromHsv(float hue, float saturation, float value)
        {
            hue %= 360f;

            if (hue < 0f)
                hue += 360f;

            saturation = Math.Clamp(saturation, 0f, 1f);
            value = Math.Clamp(value, 0f, 1f);

            var sector = hue * ONE_OVER_SIXTY;
            var sectorIndex = (int)MathF.Floor(sector) % 6;
            var fraction = sector - MathF.Floor(sector);

            var p = value * (1f - saturation);
            var q = value * (1f - (fraction * saturation));
            var t = value * (1f - ((1f - fraction) * saturation));

            var (r, g, b) = sectorIndex switch
            {
                0 => (value, t, p),
                1 => (q, value, p),
                2 => (p, value, t),
                3 => (p, q, value),
                4 => (t, p, value),
                _ => (value, p, q),
            };

            return FromArgb(byte.MaxValue,(byte)MathF.Round(r * byte.MaxValue), (byte)MathF.Round(g * byte.MaxValue), (byte)MathF.Round(b * byte.MaxValue));
        }

        public bool Equals(FastColor other)
        {
            return PackedValue == other.PackedValue;
        }

        public override bool Equals(object obj)
        {
            return obj is FastColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return PackedValue.GetHashCode();
        }

        public static bool operator ==(FastColor left, FastColor right)
        {
            return left.PackedValue == right.PackedValue;
        }

        public static bool operator !=(FastColor left, FastColor right)
        {
            return left.PackedValue != right.PackedValue;
        }
    }
}
