using System;
using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// The player's power up effect, under the mini-map: the effect's name ("No power up" when there isn't one) above a meter,
    /// greyed out, with a yellow bar over it counting down the time left, and the player's size under that, then a hunger meter filling red as the pac nears starving, inside a
    /// yellow border with rounded corners like the mini-map's. The text is left aligned.
    /// </summary>
    public class PlayerState
    {
        private const int PADDING_PIXELS = 8;
        private const int METER_HEIGHT_PIXELS = 12;

        private readonly SpriteFont _font;
        private Texture2D _pixel;

        public PlayerState(SpriteFont font)
        {
            _font = font;
        }

        /// <summary>
        /// Height of the whole control, border included: padding, the label, padding, the meter, padding, the size, padding,
        /// the size bar, padding, the hunger meter and padding.
        /// </summary>
        public int GetHeight()
        {
            return MiniMap.BORDER_PIXELS * 2 + PADDING_PIXELS * 6 + _font.LineSpacing * 2 + METER_HEIGHT_PIXELS * 3;
        }

        /// <summary>
        /// Draws the control filling bounds (border included). fill is the share of the effect's time left, 0 to 1, and
        /// size is the player's pac size and hunger how close it is to starving, 0 to 1.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, ePowerUpEffect effect, float fill, int size, float hunger)
        {
            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var border = MiniMap.BORDER_PIXELS;
            TextureHelper.FillRounded(spriteBatch, _pixel, bounds, MiniMap.CORNER_RADIUS, ClientColors.COLOR_PAC_YELLOW);

            var inner = new Rectangle(bounds.X + border, bounds.Y + border, bounds.Width - border * 2, bounds.Height - border * 2);
            TextureHelper.FillRounded(spriteBatch, _pixel, inner, MiniMap.CORNER_RADIUS - border, Color.Black);

            var label = PowerUpEffects.GetLabel(effect);
            var labelColor = effect == ePowerUpEffect.None ? ClientColors.COLOR_PAC_LIGHT_GREY : ClientColors.COLOR_PAC_WHITE;
            var textX = inner.X + PADDING_PIXELS;
            spriteBatch.DrawString(_font, label, new Vector2(textX, inner.Y + PADDING_PIXELS), labelColor);

            var meter = new Rectangle(inner.X + PADDING_PIXELS, inner.Y + PADDING_PIXELS * 2 + _font.LineSpacing, inner.Width - PADDING_PIXELS * 2, METER_HEIGHT_PIXELS);
            spriteBatch.Draw(_pixel, meter, ClientColors.COLOR_POWER_UP_BAR_EMPTY);

            // the yellow part shrinks towards the left as the effect runs out
            var fillWidth = (int)Math.Round(meter.Width * Math.Clamp(fill, 0f, 1f));

            if (effect != ePowerUpEffect.None && fillWidth > 0)
                spriteBatch.Draw(_pixel, new Rectangle(meter.X, meter.Y, fillWidth, meter.Height), ClientColors.COLOR_PAC_YELLOW);

            var sizeText = $"{Const.STRING_SIZE} {size}";
            var sizeY = meter.Bottom + PADDING_PIXELS;
            spriteBatch.DrawString(_font, sizeText, new Vector2(textX, sizeY), ClientColors.COLOR_PAC_WHITE);

            // the size bar fills green up to 100, then turns orange above 100 and red above 150
            var sizeBar = new Rectangle(meter.X, sizeY + _font.LineSpacing + PADDING_PIXELS, meter.Width, METER_HEIGHT_PIXELS);
            spriteBatch.Draw(_pixel, sizeBar, ClientColors.COLOR_POWER_UP_BAR_EMPTY);

            var sizeWidth = (int)Math.Round(sizeBar.Width * SizeBar.GetFill(size));

            if (sizeWidth > 0)
                spriteBatch.Draw(_pixel, new Rectangle(sizeBar.X, sizeBar.Y, sizeWidth, sizeBar.Height), ClientColors.GetSizeBarColor(SizeBar.GetLevel(size)));

            // the red part grows towards the right as the pac gets hungrier, and empties when it eats
            var hungerMeter = new Rectangle(meter.X, sizeBar.Bottom + PADDING_PIXELS, meter.Width, METER_HEIGHT_PIXELS);
            spriteBatch.Draw(_pixel, hungerMeter, ClientColors.COLOR_POWER_UP_BAR_EMPTY);

            var hungerWidth = (int)Math.Round(hungerMeter.Width * Math.Clamp(hunger, 0f, 1f));

            if (hungerWidth > 0)
                spriteBatch.Draw(_pixel, new Rectangle(hungerMeter.X, hungerMeter.Y, hungerWidth, hungerMeter.Height), ClientColors.COLOR_PAC_RED);
        }
    }
}
