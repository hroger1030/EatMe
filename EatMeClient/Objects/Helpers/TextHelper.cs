using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public static class TextHelper
    {
        /// <summary>
        /// How far down and to the right a drop shadow sits under its text.
        /// </summary>
        public const float SHADOW_PIXELS = 2f;

        /// <summary>
        /// Draws text with a COLOR_TEXT_SHADOW drop shadow SHADOW_PIXELS down and to the right, so it reads on any picture.
        /// </summary>
        public static void DrawShadowedString(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, float scale = 1f)
        {
            DrawShadowedString(spriteBatch, font, text, position, color, ClientColors.COLOR_TEXT_SHADOW, scale);
        }

        /// <summary>
        /// Draws text with a drop shadow in shadowColor SHADOW_PIXELS down and to the right.
        /// </summary>
        public static void DrawShadowedString(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, Color shadowColor, float scale = 1f)
        {
            var shadow = position + new Vector2(SHADOW_PIXELS, SHADOW_PIXELS);
            spriteBatch.DrawString(font, text, shadow, shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
