using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The heat round Holy Fire flames and Sun Flower fireballs: a wavy wobble of the board's pixels near each one,
    /// drawn by the client's BoardDistort shader for up to MAX_SOURCES on the board (GetOffset mirrors its sums).
    /// </summary>
    public static class HeatHaze
    {
        /// <summary>
        /// The most hot things the shader wobbles round at once (its array size).
        /// </summary>
        public const int MAX_SOURCES = 8;

        /// <summary>
        /// How far from a hot thing's centre the haze reaches, in cells.
        /// </summary>
        public const float RADIUS_CELLS = 2.5f;

        /// <summary>
        /// How far the haze moves a pixel right by a hot thing.
        /// </summary>
        public const float AMPLITUDE_PIXELS = 5f;

        /// <summary>
        /// How tightly the waves are packed, in radians per pixel.
        /// </summary>
        public const float WAVES_PER_PIXEL = 0.18f;

        /// <summary>
        /// How long the waves take to go once round.
        /// </summary>
        public const int CYCLE_MS = 700;

        /// <summary>
        /// True for the weapon effects that make a heat haze: Holy Fire's flames and Sun Flower's fireballs.
        /// </summary>
        public static bool IsSource(eWeaponEffect kind)
        {
            return kind == eWeaponEffect.HolyFlame || kind == eWeaponEffect.Fireball;
        }

        /// <summary>
        /// How far round the waves have gone after a time, in radians, 0 to 2 pi.
        /// </summary>
        public static float GetPhase(TimeSpan time)
        {
            var cycles = time.TotalMilliseconds / CYCLE_MS;
            return (float)((cycles - Math.Floor(cycles)) * 2.0 * Math.PI);
        }

        /// <summary>
        /// How far the haze moves the pixel dx, dy pixels from a hot thing's centre, as the shader does: a sine wave
        /// each way, fading to nothing at radiusPixels.
        /// </summary>
        public static (float X, float Y) GetOffset(float dx, float dy, float radiusPixels, float amplitude, float phase)
        {
            if (radiusPixels <= 0f)
                return (0f, 0f);

            var falloff = Math.Clamp(1f - MathF.Sqrt(dx * dx + dy * dy) / radiusPixels, 0f, 1f);
            return (MathF.Sin(dy * WAVES_PER_PIXEL + phase) * amplitude * falloff,
                MathF.Cos(dx * WAVES_PER_PIXEL + phase) * amplitude * falloff);
        }
    }
}
