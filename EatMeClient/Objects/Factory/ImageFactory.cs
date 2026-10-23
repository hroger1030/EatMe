using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    public static class ImageFactory
    {
        private const int DEATH_FRAME_COLUMNS = 11;
        private const int DEATH_DURATION_MS = 1100;

        /// <summary>
        /// The pac death animation for a shape (its row of the death sheet, in ePacShape order, like the pac sheet),
        /// played once at a world location, drawn the given width in pixels and tinted.
        /// </summary>
        public static AnimatedTexture GeneratePlayerDeath(Texture2D texture, ePacShape shape, FastG.Point2 location, float width, Color tint)
        {
            var sheetRows = PacShapes.ALL.Length;
            var row = PacShapes.GetCellY(shape);
            var frameWidth = texture.Width / DEATH_FRAME_COLUMNS;
            var frameHeight = texture.Height / sheetRows;

            var output = new AnimatedTexture()
            {
                Texture = texture,
                Rows = 1,
                SheetRows = sheetRows,
                StartRow = row,
                Columns = DEATH_FRAME_COLUMNS,
                AnimationDuration = TimeSpan.FromMilliseconds(DEATH_DURATION_MS),
                Scale = width / frameWidth,
                Tint = tint,
                Loop = false,
                Location = location,
                SpriteSource = new Rectangle(0, row * frameHeight, frameWidth, frameHeight),
            };

            return output;
        }

        /// <summary>
        /// A copy of the white ghost sheet with its body in the given color; the outline and eyes are unchanged.
        /// </summary>
        public static Texture2D GenerateGhostSheet(Texture2D whiteSheet, Color bodyColor)
        {
            var pixels = new uint[whiteSheet.Width * whiteSheet.Height];
            whiteSheet.GetData(pixels);

            var output = new Texture2D(whiteSheet.GraphicsDevice, whiteSheet.Width, whiteSheet.Height);
            output.SetData(GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, ClientColors.ToFastColor(bodyColor)));

            return output;
        }

        /// <summary>
        /// The ghost sheet with its white body made transparent, leaving only the eyes, for dead ghosts (GhostEyes).
        /// </summary>
        public static Texture2D GenerateGhostEyesSheet(Texture2D whiteSheet)
        {
            var pixels = new uint[whiteSheet.Width * whiteSheet.Height];
            whiteSheet.GetData(pixels);

            var output = new Texture2D(whiteSheet.GraphicsDevice, whiteSheet.Width, whiteSheet.Height);
            output.SetData(GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, FastColor.TRANSPARENT));

            return output;
        }
    }
}
