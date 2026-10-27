using System;

namespace EatMeLibrary
{
    /// <summary>
    /// A BFG shot warps the board round it as it flies, until it bursts: a swirling pinch, as if space were pulled into
    /// it, drawn by the client's BoardDistort shader for up to MAX_SOURCES shots on the board. The pinch pulses every
    /// PULSE_MS. GetOffset mirrors the shader's sums.
    /// </summary>
    public static class BfgWarp
    {
        /// <summary>
        /// The most BFG shots the shader warps round at once (its array size).
        /// </summary>
        public const int MAX_SOURCES = 4;

        /// <summary>
        /// How far from a shot's centre the warp reaches, in cells.
        /// </summary>
        public const float RADIUS_CELLS = 2.5f;

        /// <summary>
        /// How far round the board is twisted at the shot's centre, in radians, easing to none at the edge.
        /// </summary>
        public const float SWIRL_RADIANS = 1.5f;

        /// <summary>
        /// How hard the board is pulled in at the shot's centre at the height of the pulse: 0.4 shows what is 40%
        /// further out.
        /// </summary>
        public const float PINCH = 0.4f;

        /// <summary>
        /// The share of PINCH the pulse takes away at its lowest.
        /// </summary>
        public const float PULSE_SHARE = 0.35f;

        /// <summary>
        /// How long the pinch takes to pulse once.
        /// </summary>
        public const int PULSE_MS = 400;

        /// <summary>
        /// True for the weapon effects that warp the board: the BFG shot.
        /// </summary>
        public static bool IsSource(eWeaponEffect kind)
        {
            return kind == eWeaponEffect.BfgShot;
        }

        /// <summary>
        /// The pinch after a time: PINCH at the height of the pulse, PINCH less PULSE_SHARE of it at the lowest.
        /// </summary>
        public static float GetPinch(TimeSpan time)
        {
            var wave = 0.5f + 0.5f * MathF.Sin((float)(time.TotalMilliseconds / PULSE_MS) * MathF.PI * 2f);
            return PINCH * (1f - PULSE_SHARE * (1f - wave));
        }

        /// <summary>
        /// How far the warp moves the pixel dx, dy pixels from a shot's centre, as the shader does: the pixel shows what
        /// lies swirled round by SWIRL_RADIANS and pinch further out, both easing in with the square of the distance to
        /// radiusPixels, and nothing changes at or past it.
        /// </summary>
        public static (float X, float Y) GetOffset(float dx, float dy, float radiusPixels, float swirl, float pinch)
        {
            if (radiusPixels <= 0f)
                return (0f, 0f);

            var falloff = Math.Clamp(1f - MathF.Sqrt(dx * dx + dy * dy) / radiusPixels, 0f, 1f);
            var weight = falloff * falloff;
            var angle = swirl * weight;
            var scale = 1f + pinch * weight;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);

            var sampleX = (dx * cos - dy * sin) * scale;
            var sampleY = (dx * sin + dy * cos) * scale;

            // each pixel shows what was offset away from it
            return (dx - sampleX, dy - sampleY);
        }
    }
}
