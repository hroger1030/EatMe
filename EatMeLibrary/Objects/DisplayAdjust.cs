using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Limits and colour maths for the client's brightness and colour bloom settings. The client's Brightness and
    /// Bloom shaders do the same sums; AdjustColor and GetBlurWeights mirror them so they can be tested. The bloom
    /// only glows what the game board draws into its glow target (GlowSources), blurred into a tight halo and a wide
    /// one and added back many times over (GetBloomTightGain, GetBloomWideGain), so the maze reads as neon.
    /// </summary>
    public static class DisplayAdjust
    {
        /// <summary>
        /// Brightness in percent; the default leaves the picture unchanged.
        /// </summary>
        public const float BRIGHTNESS_DEFAULT = 100f;

        public const float BRIGHTNESS_MIN = 50f;

        /// <summary>
        /// Brighter than this would only clip the bright colours to white.
        /// </summary>
        public const float BRIGHTNESS_MAX = 100f;

        /// <summary>
        /// Colour bloom in percent; 0 is off.
        /// </summary>
        public const float BLOOM_DEFAULT = 0f;

        public const float BLOOM_MIN = 0f;

        /// <summary>
        /// At 100% the blurred glow is added back at full strength.
        /// </summary>
        public const float BLOOM_MAX = 100f;

        /// <summary>
        /// Blur taps from the centre out, centre included, so 2 * BLOOM_BLUR_TAPS - 1 samples per pass.
        /// The Bloom shader's Weights array is this long.
        /// </summary>
        public const int BLOOM_BLUR_TAPS = 5;

        /// <summary>
        /// Spread of the Gaussian blur, in texels of the half size bloom target.
        /// </summary>
        public const float BLOOM_BLUR_SIGMA = 2f;

        /// <summary>
        /// How many times over the blurred glow is added back at 100%, so the halo is strong enough to read as neon light.
        /// </summary>
        public const float BLOOM_MAX_GAIN = 4f;

        /// <summary>
        /// The wide halo (blurred at quarter size) is added at this share of the gain, the tight one (half size) at the rest.
        /// </summary>
        public const float BLOOM_WIDE_SHARE = 0.6f;

        /// <summary>
        /// Share of the desktop's width and height the game's window takes in windowed mode, so it and its title bar fit.
        /// </summary>
        public const float WINDOWED_SCREEN_FRACTION = 0.8f;

        /// <summary>
        /// Clamps a brightness to BRIGHTNESS_MIN to BRIGHTNESS_MAX. Anything that isn't a number becomes BRIGHTNESS_DEFAULT.
        /// </summary>
        public static float ClampBrightness(float brightness)
        {
            if (!float.IsFinite(brightness))
                return BRIGHTNESS_DEFAULT;

            return Math.Clamp(brightness, BRIGHTNESS_MIN, BRIGHTNESS_MAX);
        }

        /// <summary>
        /// The shader's Brightness parameter for a brightness in percent: 1 for 100%, 0.5 for 50%.
        /// </summary>
        public static float GetMultiplier(float brightness)
        {
            return ClampBrightness(brightness) / 100f;
        }

        /// <summary>
        /// One colour (channels 0 to 1) at a brightness in percent, as the shader draws it. Black stays black.
        /// </summary>
        public static (float R, float G, float B) AdjustColor(float r, float g, float b, float brightness)
        {
            var multiplier = GetMultiplier(brightness);
            return (r * multiplier, g * multiplier, b * multiplier);
        }

        /// <summary>
        /// Clamps a colour bloom to BLOOM_MIN to BLOOM_MAX. Anything that isn't a number becomes BLOOM_DEFAULT.
        /// </summary>
        public static float ClampBloom(float bloom)
        {
            if (!float.IsFinite(bloom))
                return BLOOM_DEFAULT;

            return Math.Clamp(bloom, BLOOM_MIN, BLOOM_MAX);
        }

        /// <summary>
        /// How strongly the glow is added back for a colour bloom in percent: 0 for 0%, 1 for 100%.
        /// </summary>
        public static float GetBloomIntensity(float bloom)
        {
            return ClampBloom(bloom) / 100f;
        }

        /// <summary>
        /// How many times over the tight halo (the glow blurred at half size) is added back for a colour bloom in percent:
        /// 0 for 0%, up to BLOOM_MAX_GAIN times (1 - BLOOM_WIDE_SHARE) at 100%.
        /// </summary>
        public static float GetBloomTightGain(float bloom)
        {
            return GetBloomIntensity(bloom) * BLOOM_MAX_GAIN * (1f - BLOOM_WIDE_SHARE);
        }

        /// <summary>
        /// How many times over the wide halo (the glow blurred at quarter size) is added back for a colour bloom in
        /// percent: 0 for 0%, up to BLOOM_MAX_GAIN times BLOOM_WIDE_SHARE at 100%.
        /// </summary>
        public static float GetBloomWideGain(float bloom)
        {
            return GetBloomIntensity(bloom) * BLOOM_MAX_GAIN * BLOOM_WIDE_SHARE;
        }

        /// <summary>
        /// The Bloom shader's blur weights, centre first then one per texel outwards (each used on both sides),
        /// from a Gaussian of BLOOM_BLUR_SIGMA. All the samples together add up to 1, so the blur keeps brightness.
        /// </summary>
        public static float[] GetBlurWeights()
        {
            var weights = new float[BLOOM_BLUR_TAPS];
            var total = 0f;

            for (var i = 0; i < BLOOM_BLUR_TAPS; i++)
            {
                weights[i] = MathF.Exp(-(i * i) / (2f * BLOOM_BLUR_SIGMA * BLOOM_BLUR_SIGMA));
                total += i == 0 ? weights[i] : weights[i] * 2f;
            }

            for (var i = 0; i < BLOOM_BLUR_TAPS; i++)
                weights[i] /= total;

            return weights;
        }

        /// <summary>
        /// The back buffer size for windowed mode on a desktop of the given size: WINDOWED_SCREEN_FRACTION of it,
        /// keeping its aspect ratio, and never less than one pixel each way.
        /// </summary>
        public static (int Width, int Height) GetWindowedSize(int displayWidth, int displayHeight)
        {
            var width = Math.Max(1, (int)(displayWidth * WINDOWED_SCREEN_FRACTION));
            var height = Math.Max(1, (int)(displayHeight * WINDOWED_SCREEN_FRACTION));
            return (width, height);
        }
    }
}
