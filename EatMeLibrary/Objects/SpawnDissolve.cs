using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Pacs and ghosts materialise when they spawn: for SPAWN_IN_MS each texel of the sprite shows once the threshold
    /// passes its noise value (GetNoise), with a bright rim on the texels just appearing. The client's Dissolve shader
    /// does the same sums; these mirror them.
    /// </summary>
    public static class SpawnDissolve
    {
        /// <summary>
        /// How long a pac or ghost takes to materialise.
        /// </summary>
        public const int SPAWN_IN_MS = 500;

        /// <summary>
        /// The bright rim's share of the noise range.
        /// </summary>
        public const float EDGE_WIDTH = 0.15f;

        /// <summary>
        /// True while something spawned this long ago is still materialising.
        /// </summary>
        public static bool IsSpawning(TimeSpan sinceSpawn)
        {
            return sinceSpawn >= TimeSpan.Zero && sinceSpawn.TotalMilliseconds < SPAWN_IN_MS;
        }

        /// <summary>
        /// The shader's threshold this long after spawning: 0 (nothing shows) at the start, up to 1 + EDGE_WIDTH (all of
        /// it, with no rim left) at SPAWN_IN_MS.
        /// </summary>
        public static float GetThreshold(TimeSpan sinceSpawn)
        {
            return Math.Clamp((float)(sinceSpawn.TotalMilliseconds / SPAWN_IN_MS), 0f, 1f) * (1f + EDGE_WIDTH);
        }

        /// <summary>
        /// A texel's noise value, 0 to 1, from its position on the sheet, as the shader does.
        /// </summary>
        public static float GetNoise(float texelX, float texelY)
        {
            var value = MathF.Sin(texelX * 12.9898f + texelY * 78.233f) * 43758.5453f;
            return value - MathF.Floor(value);
        }

        /// <summary>
        /// True if a texel with this noise value shows at this threshold.
        /// </summary>
        public static bool IsVisible(float noise, float threshold)
        {
            return noise < threshold;
        }

        /// <summary>
        /// True if a showing texel is on the bright rim: it only just appeared.
        /// </summary>
        public static bool IsEdge(float noise, float threshold)
        {
            return IsVisible(noise, threshold) && noise >= threshold - EDGE_WIDTH;
        }
    }
}
