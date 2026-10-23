using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Where each frame of the animated title logo is on the logo sheet (Const.IMAGE_LOGO): FRAME_COUNT frames of
    /// FRAME_WIDTH by FRAME_HEIGHT, COLUMNS to a row, left to right then down. The frames loop: the dots scroll one
    /// spacing to the left and the pac's four chomp frames play once, opening as a dot comes and snapping shut as it
    /// reaches the pac.
    /// </summary>
    public static class LogoSprites
    {
        public const int FRAME_WIDTH = 800;
        public const int FRAME_HEIGHT = 200;
        public const int FRAME_COUNT = 4;
        public const int COLUMNS = 1;

        /// <summary>
        /// How long each animation frame shows.
        /// </summary>
        public const float FRAME_SECONDS = 0.10f;

        /// <summary>
        /// Which animation frame shows a time in seconds, looping through FRAME_COUNT.
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

            return frame % COLUMNS * FRAME_WIDTH;
        }

        /// <summary>
        /// Top edge of a frame on the sheet, in pixels. Throws for a frame outside 0 to FRAME_COUNT - 1.
        /// </summary>
        public static int GetCellY(int frame)
        {
            if (frame < 0 || frame >= FRAME_COUNT)
                throw new ArgumentOutOfRangeException(nameof(frame));

            return frame / COLUMNS * FRAME_HEIGHT;
        }
    }
}
