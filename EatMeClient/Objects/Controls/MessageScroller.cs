using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// A black bar across the bottom of the screen with a message in yellow scrolling through it from right to left,
    /// in the same font as the menu options.
    /// </summary>
    public class MessageScroller
    {
        private const float SCROLL_PIXELS_PER_SECOND = 200f;
        private const int SCROLLER_PADDING = 8;

        private SpriteFont _font;
        private Texture2D _pixel;
        private string _message = "";
        private TimeSpan _elapsed;

        public void LoadContent()
        {
            _font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC48);
        }

        /// <summary>
        /// Shows a new message, starting again from the right edge.
        /// </summary>
        public void SetMessage(string message)
        {
            _message = message ?? "";
            _elapsed = TimeSpan.Zero;
        }

        public void Update(GameTime gameTime)
        {
            _elapsed += gameTime.ElapsedGameTime;
        }

        /// <summary>
        /// The bar's height: one line of text plus padding above and below.
        /// </summary>
        public int GetHeight()
        {
            return _font.LineSpacing + SCROLLER_PADDING * 2;
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var barHeight = GetHeight();
            var barTop = viewport.Height - barHeight;
            spriteBatch.Draw(_pixel, new Rectangle(0, barTop, viewport.Width, barHeight), Color.Black);

            var textWidth = _font.MeasureString(_message).X;
            var x = ScrollerMath.GetTextX(viewport.Width, textWidth, _elapsed, SCROLL_PIXELS_PER_SECOND);
            spriteBatch.DrawString(_font, _message, new Vector2(x, barTop + SCROLLER_PADDING), ClientColors.COLOR_PAC_YELLOW);
        }
    }
}
