using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Colors for rainbow levels (LevelInfo.IsRainbow): walls, dots and power pills in a smooth gradient of seven shades
    /// along the diagonal from the top left corner towards the bottom right, flowing as time passes.
    /// </summary>
    public static class Rainbow
    {
        /// <summary>
        /// The seven shades, in order: red, orange, yellow, green, blue, indigo, violet.
        /// </summary>
        public static readonly FastColor[] COLORS =
        [
            FastColor.FromRgb(255, 0, 0),
            FastColor.FromRgb(255, 127, 0),
            FastColor.FromRgb(255, 255, 0),
            FastColor.FromRgb(0, 255, 0),
            FastColor.FromRgb(0, 96, 255),
            FastColor.FromRgb(75, 0, 130),
            FastColor.FromRgb(148, 0, 211),
        ];

        /// <summary>
        /// How far along the diagonal (x + y, in cells) the gradient goes once round the seven colors.
        /// </summary>
        public const float CYCLE_CELLS = 7f;

        /// <summary>
        /// How fast the gradient flows along the diagonal, towards the bottom right, in cells a second.
        /// </summary>
        public const float FLOW_CELLS_PER_SECOND = 2f;

        private const float INV_CYCLE_CELLS = 1f / CYCLE_CELLS;

        /// <summary>
        /// A smooth blend round the seven colors. t wraps, so 0 and 1 are both red, and t = i / 7 is exactly COLORS[i].
        /// </summary>
        public static FastColor GetGradientColor(float t)
        {
            var position = (t - MathF.Floor(t)) * COLORS.Length;
            var index = (int)position;

            if (index >= COLORS.Length)
                index = 0;

            var amount = position - index;
            var from = COLORS[index];
            var to = COLORS[(index + 1) % COLORS.Length];

            return FastColor.FromRgb(Lerp(from.R, to.R, amount), Lerp(from.G, to.G, amount), Lerp(from.B, to.B, amount));
        }

        /// <summary>
        /// How far round the colors the gradient has flowed after a time, 0 to 1. The RainbowWall shader takes it as
        /// its Phase.
        /// </summary>
        public static float GetFlowPhase(TimeSpan time)
        {
            var phase = (float)time.TotalSeconds * FLOW_CELLS_PER_SECOND * INV_CYCLE_CELLS;

            return phase - MathF.Floor(phase);
        }

        /// <summary>
        /// The rainbow at world position (x, y), in cells, after a time: the gradient along the diagonal, one round
        /// every CYCLE_CELLS, flowed on by GetFlowPhase. The RainbowWall shader works this out for every pixel.
        /// </summary>
        public static FastColor GetShade(float x, float y, TimeSpan time)
        {
            return GetGradientColor((x + y) * INV_CYCLE_CELLS - GetFlowPhase(time));
        }

        private static byte Lerp(byte from, byte to, float amount)
        {
            return (byte)MathF.Round(from + (to - from) * amount);
        }
    }
}
