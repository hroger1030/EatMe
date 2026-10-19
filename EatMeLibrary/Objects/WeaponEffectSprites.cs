using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Where each weapon effect's frames are on the weapon effects sheet (Const.IMAGE_WEAPON_EFFECTS): one row per
    /// eWeaponEffect, in enum order, with its FRAME_COUNT animation frames left to right, each a CELL_PIXELS square.
    /// The sprites are drawn in their own colors (not tinted), on magenta that is keyed out.
    /// </summary>
    public static class WeaponEffectSprites
    {
        public const int CELL_PIXELS = 60;
        public const int FRAME_COUNT = 2;
        public const int ROWS = 10;

        /// <summary>
        /// How long each animation frame shows.
        /// </summary>
        public const float FRAME_SECONDS = 0.15f;

        /// <summary>
        /// Which animation frame shows a time in seconds after the effect appeared, looping through FRAME_COUNT.
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
        /// Top edge of an effect's row on the sheet, in pixels.
        /// </summary>
        public static int GetCellY(eWeaponEffect effect)
        {
            return (int)effect * CELL_PIXELS;
        }
    }
}
