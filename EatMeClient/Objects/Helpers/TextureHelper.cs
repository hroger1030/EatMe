using System;
using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public static class TextureHelper
    {
        public static Texture2D GetPixel(GraphicsDevice graphicsDevice)
        {
            var pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData([Color.White]);

            return pixel;
        }

        /// <summary>
        /// Fills a rectangle with its corners rounded to radius: the middle is one rectangle, and the curved rows at the
        /// top and bottom are one-pixel strips cut in at each side.
        /// </summary>
        public static void FillRounded(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int radius, Color color)
        {
            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + radius, rect.Width, rect.Height - radius * 2), color);

            for (var row = 0; row < radius; row++)
            {
                var inset = RoundedCorners.GetRowInset(radius, row);
                spriteBatch.Draw(pixel, new Rectangle(rect.X + inset, rect.Y + row, rect.Width - inset * 2, 1), color);
                spriteBatch.Draw(pixel, new Rectangle(rect.X + inset, rect.Bottom - 1 - row, rect.Width - inset * 2, 1), color);
            }
        }
    }
}
