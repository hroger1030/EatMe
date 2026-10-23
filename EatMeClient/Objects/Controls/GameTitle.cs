using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    public class GameTitle
    {
        private readonly string _TextMessage;
        private readonly Color _TextColor;
        private readonly TimeSpan _FadeDuration;
        private readonly SpriteFont _Font;
        private readonly eElementAlignment _Alignment;

        private TimeSpan _Elapsed;
        private Texture2D _Texture;

        public bool IsComplete => (_Elapsed >= _FadeDuration);

        public GameTitle(string textMessage, Color textColor, TimeSpan fadeDuration, SpriteFont font, eElementAlignment alignment)
        {
            _TextMessage = textMessage;
            _TextColor = textColor;
            _FadeDuration = fadeDuration;
            _Font = font;
            _Alignment = alignment;
        }

        public void Update(TimeSpan elapsed)
        {
            if (IsComplete)
                return;

            _Elapsed += elapsed;
        }

        public void Skip()
        {
            _Elapsed = _FadeDuration;
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_Texture == null)
                _Texture = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var textSize = _Font.MeasureString(_TextMessage);
            var position = AlignmentHelper.GetPosition(viewport, textSize, _Alignment);

            spriteBatch.Begin();
            spriteBatch.Draw(_Texture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            spriteBatch.DrawString(_Font, _TextMessage, position, _TextColor);
            spriteBatch.End();
        }
    }
}
