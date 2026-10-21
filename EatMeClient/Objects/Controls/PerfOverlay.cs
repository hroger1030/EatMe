using System;
using System.Collections.Generic;
using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// The performance overlay toggled by F1: ClientPerf's lines in Arial20 with a drop shadow, on a half-transparent
    /// panel with rounded corners fitted to them, with its top left corner at the given position.
    /// </summary>
    public class PerfOverlay
    {
        private const int PADDING_PIXELS = 12;

        private SpriteFont _font;
        private Texture2D _pixel;

        public void LoadContent(GraphicsDevice graphicsDevice)
        {
            _font = AssetCache.GetFont(Const.FONT_ARIAL20);
            _pixel = TextureHelper.GetPixel(graphicsDevice);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, List<string> lines)
        {
            if (lines.Count == 0)
                return;

            var width = 0f;

            foreach (var line in lines)
                width = Math.Max(width, _font.MeasureString(line).X);

            var panel = new Rectangle((int)position.X, (int)position.Y, (int)Math.Ceiling(width) + PADDING_PIXELS * 2, lines.Count * _font.LineSpacing + PADDING_PIXELS * 2);
            TextureHelper.FillRounded(spriteBatch, _pixel, panel, MiniMap.CORNER_RADIUS, ClientColors.COLOR_TEXT_PANEL);

            for (var i = 0; i < lines.Count; i++)
                TextHelper.DrawShadowedString(spriteBatch, _font, lines[i], new Vector2(panel.X + PADDING_PIXELS, panel.Y + PADDING_PIXELS + i * _font.LineSpacing), Color.White);
        }
    }
}
