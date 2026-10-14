using EatMeLibrary;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EatMeClient
{
    /// <summary>
    /// Renders a slider with a default range of 0 to 100.
    /// </summary>
    public class SliderControl
    {
        private const float BAR_WIDTH = 400f;
        private const float BAR_HEIGHT = 32f;
        private const float GUTTER_WIDTH = 60f;
        private const float LABEL_TRACK_GAP = 20f;
        private const float INLINE_GAP = 30f;

        public SpriteFont Font { get; set; }
        public string Label { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float Value { get; set; }
        public Texture2D _pixel { get; set; }
        public bool IsDragging { get; set; }
        public bool IsHovered { get; set; }

        /// <summary>
        /// Draws the label, track and value on one line instead of the label over the track.
        /// </summary>
        public bool IsInline { get; set; }

        /// <summary>
        /// Width of the label column when inline, so several controls can line their tracks up. 0 uses the label's
        /// own width.
        /// </summary>
        public float LabelWidth { get; set; }

        public SliderControl(SpriteFont font, string label, float minValue = 0f, float maxValue = 100f)
        {
            Font = font;
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
            Value = maxValue;
        }

        private string FormatValue(float value)
        {
            return $"{(int)value}%";
        }

        private string GetControlLabel()
        {
            return $"{Label} {FormatValue(Value)}";
        }

        public float GetLabelColumnWidth()
        {
            return MathF.Max(LabelWidth, Font.MeasureString(Label).X);
        }

        /// <summary>
        /// Width of the widest value, so an inline slider doesn't change size as it moves.
        /// </summary>
        private float GetValueColumnWidth()
        {
            return MathF.Max(Font.MeasureString(FormatValue(MinValue)).X, Font.MeasureString(FormatValue(MaxValue)).X);
        }

        public float GetWidth()
        {
            if (IsInline)
                return GetLabelColumnWidth() + INLINE_GAP + BAR_WIDTH + INLINE_GAP + GetValueColumnWidth();

            var labelWidth = Font.MeasureString(GetControlLabel()).X;
            return MathF.Max(labelWidth, BAR_WIDTH);
        }

        public float GetHeight()
        {
            if (IsInline)
                return MathF.Max(Font.LineSpacing, BAR_HEIGHT);

            var labelHeight = Font.MeasureString(GetControlLabel()).Y;
            return labelHeight + LABEL_TRACK_GAP + BAR_HEIGHT;
        }

        private (Vector2 LabelPosition, Vector2 TrackPosition, Vector2 TrackSize) GetLayout(Vector2 position)
        {
            var totalWidth = GetWidth();

            if (IsInline)
            {
                // label right-aligned in its column, track after it, centred on the text line
                var height = GetHeight();
                var labelColumnWidth = GetLabelColumnWidth();
                var inlineLabelPosition = new Vector2(position.X + labelColumnWidth - Font.MeasureString(Label).X, position.Y + (height - Font.LineSpacing) / 2f);
                var inlineTrackPosition = new Vector2(position.X + labelColumnWidth + INLINE_GAP, position.Y + (height - BAR_HEIGHT) / 2f);
                return (inlineLabelPosition, inlineTrackPosition, new Vector2(BAR_WIDTH, BAR_HEIGHT));
            }

            var labelSize = Font.MeasureString(GetControlLabel());
            var labelPosition = new Vector2(position.X + (totalWidth - labelSize.X) / 2f, position.Y);

            var trackSize = new Vector2(BAR_WIDTH, BAR_HEIGHT);
            var trackX = position.X + (totalWidth - trackSize.X) / 2f;
            var trackPosition = new Vector2(trackX, position.Y + labelSize.Y + LABEL_TRACK_GAP);

            return (labelPosition, trackPosition, trackSize);
        }

        public void Update(Vector2 position)
        {
            var mouseState = InputManager.MouseState;
            var (_, trackPosition, trackSize) = GetLayout(position);
            var interactiveBounds = new Rectangle(
                (int)(trackPosition.X - GUTTER_WIDTH),
                (int)trackPosition.Y,
                (int)(trackSize.X + GUTTER_WIDTH * 2f),
                (int)trackSize.Y);
            var mousePoint = new Point(mouseState.X, mouseState.Y);

            if (InputManager.MouseButtonPressed(InputManager.MouseButton.Left) && interactiveBounds.Contains(mousePoint))
            {
                IsDragging = true;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (mouseState.LeftButton == ButtonState.Released)
                IsDragging = false;

            IsHovered = IsDragging || interactiveBounds.Contains(mousePoint);

            if (IsDragging)
            {
                var fraction = MathHelper.Clamp((mousePoint.X - trackPosition.X) / trackSize.X, 0f, 1f);
                SetValue(MinValue + fraction * (MaxValue - MinValue));
            }
        }

        public void AdjustValue(float delta)
        {
            SetValue(Value + delta);
        }

        private void SetValue(float value)
        {
            Value = MathHelper.Clamp(value, MinValue, MaxValue);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, bool isFocused)
        {
            var (labelPosition, trackPosition, trackSize) = GetLayout(position);

            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var isActive = isFocused || IsHovered;
            var textColor = isActive ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;

            if (IsInline)
            {
                spriteBatch.DrawString(Font, Label, labelPosition, textColor);
                var valuePosition = new Vector2(trackPosition.X + trackSize.X + INLINE_GAP, labelPosition.Y);
                spriteBatch.DrawString(Font, FormatValue(Value), valuePosition, textColor);
            }
            else
                spriteBatch.DrawString(Font, GetControlLabel(), labelPosition, textColor);

            var fillColor = isActive ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;
            var backgroundColor = isActive ? ClientColors.COLOR_DARK_MENU_SLIDER_SELECTED_BACKGROUND : ClientColors.COLOR_DARK_MENU_SLIDER_BACKGROUND;

            var trackBounds = new Rectangle((int)trackPosition.X, (int)trackPosition.Y, (int)trackSize.X, (int)trackSize.Y);
            spriteBatch.Draw(_pixel, trackBounds, backgroundColor);

            var fraction = (Value - MinValue) / (MaxValue - MinValue);
            var fillBounds = new Rectangle((int)trackPosition.X, (int)trackPosition.Y, (int)(trackSize.X * fraction), (int)trackSize.Y);
            spriteBatch.Draw(_pixel, fillBounds, fillColor);
        }
    }
}
