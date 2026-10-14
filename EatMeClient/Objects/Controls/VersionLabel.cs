using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// The client version ("v0.9") in the top right corner of the screen, in the menu font and the dark title color.
    /// </summary>
    public class VersionLabel
    {
        private const float MARGIN = 16f;

        private SpriteFont _font;
        private string _text = "";

        public void LoadContent()
        {
            _font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC48);
            _text = VersionLabelMath.GetVersionText(NetworkConstants.APP_VERSION);
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            var textWidth = _font.MeasureString(_text).X;
            var position = new Vector2(VersionLabelMath.GetTextX(viewport.Width, textWidth, MARGIN), MARGIN);
            spriteBatch.DrawString(_font, _text, position, ClientColors.COLOR_DARK_TITLE_ITEM);
        }
    }
}
