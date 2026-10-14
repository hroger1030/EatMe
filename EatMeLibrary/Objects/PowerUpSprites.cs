using System;
using System.Numerics;

namespace EatMeLibrary
{
    /// <summary>
    /// Where each bonus item is on the power up sheet (Const.IMAGE_POWER_UP_SHEET): one CELL_PIXELS square cell per
    /// ePowerUp flag, in bit order (Cherry is cell 0), COLUMNS cells to a row, left to right then top to bottom.
    /// </summary>
    public static class PowerUpSprites
    {
        public const int CELL_PIXELS = 60;
        public const int COLUMNS = 8;
        public const int ROWS = 4;

        /// <summary>
        /// The item's cell number: the position of its flag bit. Throws unless the value is exactly one item.
        /// </summary>
        public static int GetCellIndex(ePowerUp item)
        {
            var bits = (ulong)item;

            if (bits == 0 || (bits & (bits - 1)) != 0 || (item & ePowerUp.Any) != item)
                throw new ArgumentException("Expected exactly one bonus item", nameof(item));

            return BitOperations.TrailingZeroCount(bits);
        }

        /// <summary>
        /// Left edge of the item's cell on the sheet, in pixels.
        /// </summary>
        public static int GetCellX(ePowerUp item)
        {
            return (GetCellIndex(item) % COLUMNS) * CELL_PIXELS;
        }

        /// <summary>
        /// Top edge of the item's cell on the sheet, in pixels.
        /// </summary>
        public static int GetCellY(ePowerUp item)
        {
            return (GetCellIndex(item) / COLUMNS) * CELL_PIXELS;
        }
    }
}
