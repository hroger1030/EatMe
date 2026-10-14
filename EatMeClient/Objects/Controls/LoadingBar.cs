using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// A progress bar spanning the width of the screen, with a label on the left.
    /// Draws nothing once Progress reaches 100%.
    /// </summary>
    public class LoadingBar
    {
        private const float BAR_HEIGHT = 32f;
        private const float LABEL_BAR_GAP = 20f;
        private const float SIDE_MARGIN = 60f;

        private Texture2D _pixel;

        public Vector2 Position { get; set; }
        public SpriteFont Font { get; set; }
        public string Label { get; set; }
        public int Total { get; set; }
        public int CurrentCount { get; set; }
        public Color BarColor { get; set; }

        public LoadingBar(SpriteFont font, string label, Vector2 position, int total, Color barColor)
        {
            ArgumentNullException.ThrowIfNull(font);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(total, 0);

            Font = font;
            Label = label;
            Position = position;
            Total = total;
            BarColor = barColor;
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (CurrentCount >= Total)
                return;

            // Draw label
            var labelSize = Font.MeasureString(Label);
            var labelPosition = new Vector2(Position.X, Position.Y + (BAR_HEIGHT - labelSize.Y) / 2f);

            // draw bar
            var barX = Position.X + labelSize.X + LABEL_BAR_GAP;
            var barWidth = viewport.Width - SIDE_MARGIN - barX;

            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var trackBounds = new Rectangle((int)barX, (int)Position.Y, (int)barWidth, (int)BAR_HEIGHT);
            spriteBatch.Draw(_pixel, trackBounds, ClientColors.COLOR_PAC_LIGHT_GREY);

            var fillWidth = barWidth * MathHelper.Clamp((float)CurrentCount / Total, 0f, 1f);
            var fillBounds = new Rectangle((int)barX, (int)Position.Y, (int)fillWidth, (int)BAR_HEIGHT);
            spriteBatch.Draw(_pixel, fillBounds, ClientColors.COLOR_PAC_YELLOW);

            spriteBatch.DrawString(Font, Label, labelPosition, ClientColors.COLOR_PAC_WHITE);
        }
    }
}
