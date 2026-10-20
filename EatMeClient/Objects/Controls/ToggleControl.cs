using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// Renders a control that cycles through a fixed set of string options.
    /// </summary>
    public class ToggleControl
    {
        private const float LABEL_GAP = 30f;

        public SpriteFont Font { get; set; }
        public string Label { get; set; }
        public string[] Options { get; set; }
        public int SelectedIndex { get; set; }
        public bool IsHovered { get; set; }

        /// <summary>
        /// Draws the label and value on one line instead of the value under the label.
        /// </summary>
        public bool IsInline { get; set; }

        /// <summary>
        /// Width of the label column when inline, so several controls can line their values up. 0 uses the label's
        /// own width.
        /// </summary>
        public float LabelWidth { get; set; }

        public ToggleControl(SpriteFont font, string label, string[] options)
        {
            Font = font;
            Label = label;
            Options = options;
        }

        public float GetLabelColumnWidth()
        {
            return MathHelper.Max(LabelWidth, Font.MeasureString(Label).X);
        }

        /// <summary>
        /// Width of the widest option, so an inline toggle doesn't change size as it cycles.
        /// </summary>
        public float GetWidestOptionWidth()
        {
            var width = 0f;

            foreach (var option in Options)
                width = MathHelper.Max(width, Font.MeasureString(option).X);

            return width;
        }

        public float GetWidth()
        {
            if (IsInline)
                return GetLabelColumnWidth() + LABEL_GAP + GetWidestOptionWidth();

            var labelWidth = Font.MeasureString(Label).X;
            var valueWidth = Font.MeasureString(Options[SelectedIndex]).X;
            return MathHelper.Max(labelWidth, valueWidth);
        }

        public float GetHeight()
        {
            var labelHeight = Font.MeasureString(Label).Y;
            var valueHeight = Font.MeasureString(Options[SelectedIndex]).Y;
            return IsInline ? MathHelper.Max(labelHeight, valueHeight) : labelHeight + valueHeight;
        }

        public void Update(Vector2 position)
        {
            var mouseState = InputManager.MouseState;
            var bounds = new Rectangle((int)position.X, (int)position.Y, (int)GetWidth(), (int)GetHeight());
            var mousePoint = new Point(mouseState.X, mouseState.Y);

            IsHovered = bounds.Contains(mousePoint);

            if (IsHovered && InputManager.MouseButtonPressed(InputManager.MouseButton.Left))
            {
                Next();
                SoundManager.PlaySound(Const.SFX_TICK);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, bool isFocused)
        {
            var isActive = isFocused || IsHovered;
            var textColor = isActive ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;

            var labelSize = Font.MeasureString(Label);
            var valueSize = Font.MeasureString(Options[SelectedIndex]);
            Vector2 labelPosition;
            Vector2 valuePosition;

            if (IsInline)
            {
                // label right-aligned in its column, value after it
                var labelColumnWidth = GetLabelColumnWidth();
                labelPosition = new Vector2(position.X + labelColumnWidth - labelSize.X, position.Y);
                valuePosition = new Vector2(position.X + labelColumnWidth + LABEL_GAP, position.Y);
            }
            else
            {
                var totalWidth = GetWidth();
                labelPosition = new Vector2(position.X + (totalWidth - labelSize.X) / 2f, position.Y);
                valuePosition = new Vector2(position.X + (totalWidth - valueSize.X) / 2f, position.Y + labelSize.Y);
            }

            TextHelper.DrawShadowedString(spriteBatch, Font, Label, labelPosition, textColor);
            TextHelper.DrawShadowedString(spriteBatch, Font, Options[SelectedIndex], valuePosition, textColor);
        }

        public void Next()
        {
            SelectedIndex = (SelectedIndex + 1) % Options.Length;
        }

        public void Previous()
        {
            SelectedIndex = (SelectedIndex - 1 + Options.Length) % Options.Length;
        }
    }
}
