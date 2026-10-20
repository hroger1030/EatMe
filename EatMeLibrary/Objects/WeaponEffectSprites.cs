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
        public const float FRAME_SECONDS = 0.25f;

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
        /// How far (radians, clockwise) to turn a sprite drawn pointing right, such as the sword, to point facing.
        /// </summary>
        public static float GetRotation(eFacing facing)
        {
            return facing switch
            {
                eFacing.Down => MathF.PI * 0.5f,
                eFacing.Left => MathF.PI,
                eFacing.Up => -MathF.PI * 0.5f,
                _ => 0f,
            };
        }

        /// <summary>
        /// True for effects lying in the maze (holy flames, banana peels, Tetromino blocks), drawn before the rest so
        /// flying ones go over them.
        /// </summary>
        public static bool IsOnFloor(eWeaponEffect effect)
        {
            return effect == eWeaponEffect.HolyFlame || effect == eWeaponEffect.BananaPeel || effect == eWeaponEffect.TetrisBlock;
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
