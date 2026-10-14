using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public class GameTitle
    {
        private readonly string _TextMessage;
        private readonly Color _TextColor;
        private readonly float _FadeDuration;
        private readonly SpriteFont _Font;
        private readonly eElementAlignment _Alignment;

        private float _Elapsed;
        private Texture2D _Texture;

        public bool IsComplete => (_Elapsed >= _FadeDuration);

        public GameTitle(string textMessage, Color textColor, float fadeDuration, SpriteFont font, eElementAlignment alignment)
        {
            _TextMessage = textMessage;
            _TextColor = textColor;
            _FadeDuration = fadeDuration;
            _Font = font;
            _Alignment = alignment;
        }

        public void Update(float deltaTime)
        {
            if (IsComplete)
                return;

            _Elapsed += deltaTime;
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
