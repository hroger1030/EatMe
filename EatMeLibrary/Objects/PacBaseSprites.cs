using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Where each floor frame is on the base sheet (Const.IMAGE_PAC_BASE): a 2x3 grid of CELL_PIXELS squares,
    /// red's row on top, blue's below and the ghost pen's at the bottom, with FRAME_COUNT animation frames left to right.
    /// Every spawn area and ghost pen blinks between its row's two frames, each shown FRAME_SECONDS. The sprites are drawn
    /// in their own colors (not tinted).
    /// </summary>
    public static class PacBaseSprites
    {
        public const int CELL_PIXELS = 48;
        public const int FRAME_COUNT = 2;

        /// <summary>
        /// How long each animation frame shows.
        /// </summary>
        public const float FRAME_SECONDS = 0.5f;

        /// <summary>
        /// Which animation frame shows at a time in seconds, looping through FRAME_COUNT.
        /// </summary>
        public static int GetFrame(float seconds)
        {
            if (seconds <= 0f)
                return 0;

            return (int)(seconds / FRAME_SECONDS) % FRAME_COUNT;
        }

        /// <summary>
        /// Left edge of a frame on the sheet, in pixels. Throws for a frame outside 0 to FRAME_COUNT - 1.
        /// </summary>
        public static int GetCellX(int frame)
        {
            if (frame < 0 || frame >= FRAME_COUNT)
                throw new ArgumentOutOfRangeException(nameof(frame));

            return frame * CELL_PIXELS;
        }

        /// <summary>
        /// Top edge of a team's row on the sheet, in pixels: red on top, blue below.
        /// </summary>
        public static int GetCellY(eTeam team)
        {
            return team == eTeam.Blue ? CELL_PIXELS : 0;
        }

        /// <summary>
        /// Top edge of the ghost pen's row on the sheet, in pixels: below both teams' rows.
        /// </summary>
        public static int GetGhostCellY()
        {
            return 2 * CELL_PIXELS;
        }
    }
}
