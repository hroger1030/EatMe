using System;

namespace EatMeLibrary
{
    /// <summary>
    /// How the client draws the Triforce's force field over a sealed flag (GameFlag.IsSealed): a gold bubble with a
    /// bright rim, pulsing, FIELD_CELLS wide; on the mini map the flag's triangle blinks gold.
    /// </summary>
    public static class ForceField
    {
        /// <summary>
        /// How wide the bubble is drawn, in cells.
        /// </summary>
        public const float FIELD_CELLS = 1.6f;

        /// <summary>
        /// The bubble's rim is this share of its radius.
        /// </summary>
        public const float RIM_SHARE = 0.12f;

        /// <summary>
        /// Opacity at the bubble's centre and just inside the rim; the fill brightens from one to the other.
        /// </summary>
        public const float CENTRE_ALPHA = 0.15f;
        public const float EDGE_ALPHA = 0.45f;

        /// <summary>
        /// One full pulse of the bubble's brightness takes this long.
        /// </summary>
        public const float PULSE_SECONDS = 0.8f;

        /// <summary>
        /// The bubble's brightness never pulses below this share of full.
        /// </summary>
        public const float PULSE_MIN = 0.6f;

        /// <summary>
        /// A sealed flag's triangle on the mini map switches to gold this often.
        /// </summary>
        public const float MINI_MAP_BLINK_SECONDS = 0.25f;

        /// <summary>
        /// Opacity (0 to 1) of pixel (x, y) of a bubble texture size pixels square: 0 outside the circle, 1 on the rim,
        /// and from CENTRE_ALPHA to EDGE_ALPHA inside it, brighter towards the edge.
        /// </summary>
        public static float GetAlpha(int x, int y, int size)
        {
            var radius = size * 0.5f;
            var dx = x + 0.5f - radius;
            var dy = y + 0.5f - radius;
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance > radius)
                return 0f;

            if (distance >= radius * (1f - RIM_SHARE))
                return 1f;

            var share = distance / (radius * (1f - RIM_SHARE));
            return CENTRE_ALPHA + (EDGE_ALPHA - CENTRE_ALPHA) * share * share;
        }

        /// <summary>
        /// The bubble's brightness at a time in seconds: a smooth pulse between PULSE_MIN and 1 every PULSE_SECONDS.
        /// </summary>
        public static float GetPulse(float seconds)
        {
            var wave = 0.5f - 0.5f * MathF.Cos(seconds * MathF.PI * 2f / PULSE_SECONDS);
            return PULSE_MIN + (1f - PULSE_MIN) * wave;
        }

        /// <summary>
        /// True if a sealed flag's mini map triangle is drawn gold at a time in seconds (otherwise its usual color).
        /// </summary>
        public static bool IsMiniMapGold(float seconds)
        {
            return seconds % (MINI_MAP_BLINK_SECONDS * 2f) < MINI_MAP_BLINK_SECONDS;
        }
    }
}
