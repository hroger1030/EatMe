using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Colors for rainbow levels (LevelInfo.IsRainbow): dots in diagonal stripes of seven shades, walls in a gradient
    /// running the same way, and ghosts in a left to right gradient that scrolls as they move.
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
        /// How many cells a moving ghost travels for its rainbow to scroll once all the way round.
        /// </summary>
        public const float GHOST_SCROLL_CELLS = 2f;

        private const float INV_COLOR_COUNT = 1f / 7f;
        private const float INV_GHOST_SCROLL_CELLS = 1f / GHOST_SCROLL_CELLS;

        /// <summary>
        /// A dot's color: diagonal stripes from the top left corner towards the bottom right, one shade per diagonal
        /// (x + y), repeating every seven diagonals.
        /// </summary>
        public static FastColor GetBandColor(int x, int y)
        {
            return COLORS[(x + y) % COLORS.Length];
        }

        /// <summary>
        /// A width x height grid of dot colors in GetBandColor's diagonal stripes.
        /// </summary>
        public static FastColor[,] BuildDotColors(int width, int height)
        {
            var colors = new FastColor[width, height];

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    colors[x, y] = GetBandColor(x, y);

            return colors;
        }

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
        /// The tint for one quarter of a wall cell (quarters run clockwise from the top left): the gradient along the
        /// dots' diagonal, so a wall cell's centre is the same shade as a dot in that cell would be.
        /// </summary>
        public static FastColor GetWallColor(int x, int y, int quarter)
        {
            // how far the quarter's centre is along the diagonal from the cell's centre
            var offset = quarter switch
            {
                0 => -0.5f,
                2 => 0.5f,
                _ => 0f,
            };

            return GetGradientColor((x + y + offset) * INV_COLOR_COUNT);
        }

        /// <summary>
        /// Which of phaseCount scrolled ghost sheets to draw for a ghost at (x, y). Built from x + y, so the rainbow
        /// scrolls whichever way the ghost moves and stands still when it does, once round every GHOST_SCROLL_CELLS.
        /// </summary>
        public static int GetScrollPhase(float x, float y, int phaseCount)
        {
            var cycles = (x + y) * INV_GHOST_SCROLL_CELLS;
            var phase = (int)((cycles - MathF.Floor(cycles)) * phaseCount);

            return phase >= phaseCount ? 0 : phase;
        }

        /// <summary>
        /// A copy of a sprite sheet's pixels (packed like FastColor, width pixels across, frames frameWidth wide side
        /// by side) with every pixel exactly <paramref name="from"/> set to the rainbow gradient running left to right
        /// across its frame, moved round by phase (0 to 1). Every other pixel is copied unchanged.
        /// </summary>
        public static uint[] ReplaceWithGradient(uint[] pixels, int width, int frameWidth, FastColor from, float phase)
        {
            var result = new uint[pixels.Length];
            var invFrameWidth = 1f / frameWidth;

            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] != from.PackedValue)
                {
                    result[i] = pixels[i];
                    continue;
                }

                var frameX = (i % width) % frameWidth;
                result[i] = GetGradientColor((frameX + 0.5f) * invFrameWidth + phase).PackedValue;
            }

            return result;
        }

        private static byte Lerp(byte from, byte to, float amount)
        {
            return (byte)MathF.Round(from + (to - from) * amount);
        }
    }
}
