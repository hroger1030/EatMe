using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Where each icon is on the mini map icon sheet (Const.IMAGE_MINI_MAP_ICONS): one CELL_PIXELS square cell per
    /// eMiniMapIcon, in enum order, in a single row. Icons are white on transparent, centred in their cells, and tinted
    /// when drawn.
    /// </summary>
    public static class MiniMapSprites
    {
        public const int CELL_PIXELS = 16;
        public const int COUNT = 10;

        /// <summary>
        /// Left edge of the icon's cell on the sheet, in pixels.
        /// </summary>
        public static int GetCellX(eMiniMapIcon icon)
        {
            return (int)icon * CELL_PIXELS;
        }

        /// <summary>
        /// Top edge of the icon's cell on the sheet, in pixels. The sheet is one row.
        /// </summary>
        public static int GetCellY(eMiniMapIcon icon)
        {
            return 0;
        }

        /// <summary>
        /// The player's own Pac-Man icon facing the given way.
        /// </summary>
        public static eMiniMapIcon GetOwnPacIcon(eFacing facing)
        {
            return facing switch
            {
                eFacing.Up => eMiniMapIcon.OwnPacUp,
                eFacing.Down => eMiniMapIcon.OwnPacDown,
                eFacing.Left => eMiniMapIcon.OwnPacLeft,
                eFacing.Right => eMiniMapIcon.OwnPacRight,
                _ => throw new ArgumentOutOfRangeException(nameof(facing)),
            };
        }
    }
}
