using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The pac shapes a player can pick from: their names and where each is on the pac sheet (Const.IMAGE_PAC_SHEET,
    /// one row of chomp frames per shape, in ePacShape order).
    /// </summary>
    public static class PacShapes
    {
        public const ePacShape DEFAULT = ePacShape.Chomper;

        /// <summary>
        /// Every bot's shape.
        /// </summary>
        public const ePacShape BOT = ePacShape.Gears;

        /// <summary>
        /// Every shape in sheet order, as the settings screen cycles through them.
        /// </summary>
        public static readonly ePacShape[] ALL = (ePacShape[])Enum.GetValues(typeof(ePacShape));

        /// <summary>
        /// The name each shape is shown by. Printable ASCII only, since sprite fonts are.
        /// </summary>
        public static string GetName(ePacShape shape)
        {
            return shape switch
            {
                ePacShape.OG => "O-gee",
                ePacShape.Chomper => "Chomper",
                ePacShape.Hex => "Hex",
                ePacShape.Boxie => "Boxie",
                ePacShape.Doc => "Doc",
                ePacShape.Gears => "Gears",
                ePacShape.LadyDi => "LadyDi",
                ePacShape.Go => "Go",
                _ => GetName(DEFAULT),
            };
        }

        /// <summary>
        /// The shape's row on the pac sheet.
        /// </summary>
        public static int GetCellY(ePacShape shape)
        {
            return (int)Clamp(shape);
        }

        /// <summary>
        /// The shape, or DEFAULT when it isn't one (a bad settings file or message).
        /// </summary>
        public static ePacShape Clamp(ePacShape shape)
        {
            return Enum.IsDefined(typeof(ePacShape), shape) ? shape : DEFAULT;
        }
    }
}
