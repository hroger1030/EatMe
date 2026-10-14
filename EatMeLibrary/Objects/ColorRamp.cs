using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Smooth color ramps for showing how good or bad a value is.
    /// </summary>
    public static class ColorRamp
    {
        /// <summary>
        /// Pings at or below this are fully green.
        /// </summary>
        public const int PING_GOOD_MS = 250;

        /// <summary>
        /// Pings at or above this are fully red.
        /// </summary>
        public const int PING_BAD_MS = 750;

        private static readonly FastColor[] HEAT_STOPS =
        {
            FastColor.FromRgb(0, 255, 0),   // 0: green
            FastColor.FromRgb(255, 255, 0), // 1/3: yellow
            FastColor.FromRgb(255, 165, 0), // 2/3: orange
            FastColor.FromRgb(255, 0, 0),   // 1: red
        };

        /// <summary>
        /// A color from green (0) through yellow and orange to red (1), blended smoothly between the stops.
        /// Values outside 0 to 1 are clamped.
        /// </summary>
        public static FastColor GetHeatColor(float value)
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            var scaled = clamped * (HEAT_STOPS.Length - 1);
            var index = Math.Min((int)scaled, HEAT_STOPS.Length - 2);
            var t = scaled - index;

            var from = HEAT_STOPS[index];
            var to = HEAT_STOPS[index + 1];
            return FastColor.FromRgb(Lerp(from.R, to.R, t), Lerp(from.G, to.G, t), Lerp(from.B, to.B, t));
        }

        /// <summary>
        /// The color for a ping in ms: green at PING_GOOD_MS or less, red at PING_BAD_MS or more, the heat ramp in between.
        /// </summary>
        public static FastColor GetPingColor(long pingMs)
        {
            var clamped = Math.Clamp(pingMs, PING_GOOD_MS, PING_BAD_MS);
            return GetHeatColor((clamped - PING_GOOD_MS) / (float)(PING_BAD_MS - PING_GOOD_MS));
        }

        private static byte Lerp(byte from, byte to, float t)
        {
            return (byte)Math.Round(from + (to - from) * t);
        }
    }
}
