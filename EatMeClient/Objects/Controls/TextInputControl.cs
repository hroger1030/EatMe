using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// A labelled text box on one line: the label, then the value. Takes typed characters from InputManager while focused.
    /// </summary>
    public class TextInputControl
    {
        private const float CURSOR_BLINK_SECONDS = 0.5f;
        private const string CURSOR = "_";
        private const float LABEL_GAP = 30f;
        private const float BOX_PADDING = 8f;
        private const char FIRST_PRINTABLE = ' ';
        private const char LAST_PRINTABLE = '~';

        private float _BlinkTime;
        private float _AverageCharacterWidth = -1f;
        private Texture2D _Pixel;

        public SpriteFont Font { get; set; }
        public string Label { get; set; }
        public string Value { get; set; } = string.Empty;
        public int MaxLength { get; set; }
        public bool IsHovered { get; set; }

        /// <summary>
        /// Width of the label column, so several controls can line their values up. 0 uses the label's own width.
        /// </summary>
        public float LabelWidth { get; set; }

        /// <summary>
        /// Draws a box behind the value, MaxLength average characters wide.
        /// </summary>
        public bool ShowBox { get; set; }

        /// <summary>
        /// Plays a tick for every key typed while focused, backspace included.
        /// </summary>
        public bool PlayTypingSound { get; set; }

        /// <summary>
        /// Which characters may be typed. Anything else is ignored.
        /// </summary>
        public Func<char, bool> AllowCharacter { get; set; }

        public TextInputControl(SpriteFont font, string label, int maxLength, Func<char, bool> allowCharacter)
        {
            Font = font;
            Label = label;
            MaxLength = maxLength;
            AllowCharacter = allowCharacter;
        }

        public float GetLabelColumnWidth()
        {
            return MathHelper.Max(LabelWidth, Font.MeasureString(Label).X);
        }

        /// <summary>
        /// Width of the value area: with a box, wide enough for MaxLength of the average allowed character, so it
        /// doesn't change as text is typed; without one, the current text.
        /// </summary>
        public float GetValueWidth()
        {
            if (!ShowBox)
                return Font.MeasureString(Value + CURSOR).X;

            return MaxLength * GetAverageCharacterWidth() + Font.MeasureString(CURSOR).X + BOX_PADDING * 2f;
        }

        private float GetAverageCharacterWidth()
        {
            if (_AverageCharacterWidth >= 0f)
                return _AverageCharacterWidth;

            var total = 0f;
            var count = 0;

            for (var c = FIRST_PRINTABLE; c <= LAST_PRINTABLE; c++)
            {
                if (AllowCharacter(c))
                {
                    total += Font.MeasureString(c.ToString()).X;
                    count++;
                }
            }

            _AverageCharacterWidth = count > 0 ? total / count : 0f;
            return _AverageCharacterWidth;
        }

        public float GetWidth()
        {
            return GetLabelColumnWidth() + LABEL_GAP + GetValueWidth();
        }

        public float GetHeight()
        {
            return Font.LineSpacing;
        }

        public void Update(GameTime gameTime, Vector2 position, bool isFocused)
        {
            var bounds = new Rectangle((int)position.X, (int)position.Y, (int)GetWidth(), (int)GetHeight());
            IsHovered = bounds.Contains(new Point(InputManager.MouseState.X, InputManager.MouseState.Y));

            _BlinkTime = (_BlinkTime + (float)gameTime.ElapsedGameTime.TotalSeconds) % (CURSOR_BLINK_SECONDS * 2f);

            if (!isFocused)
                return;

            Value = ApplyTyping(Value, InputManager.TypedCharacters, MaxLength, AllowCharacter);

            if (PlayTypingSound)
            {
                foreach (var _ in InputManager.TypedCharacters)
                    SoundManager.PlaySound(Const.SFX_TICK);
            }
        }

        /// <summary>
        /// Returns text after the typed characters are applied: allowed characters are added up to maxLength
        /// and '\b' deletes the last character.
        /// </summary>
        public static string ApplyTyping(string text, string typed, int maxLength, Func<char, bool> allowCharacter)
        {
            foreach (var c in typed)
            {
                if (c == '\b')
                {
                    if (text.Length > 0)
                        text = text[..^1];
                }
                else if (text.Length < maxLength && allowCharacter(c))
                {
                    text += c;
                }
            }

            return text;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, bool isFocused)
        {
            var isActive = isFocused || IsHovered;
            var textColor = isActive ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;
            var labelColumnWidth = GetLabelColumnWidth();

            // label right-aligned in its column
            var labelSize = Font.MeasureString(Label);
            TextHelper.DrawShadowedString(spriteBatch, Font, Label, new Vector2(position.X + labelColumnWidth - labelSize.X, position.Y), textColor);

            var valueX = position.X + labelColumnWidth + LABEL_GAP;

            if (ShowBox)
            {
                _Pixel ??= TextureHelper.GetPixel(spriteBatch.GraphicsDevice);
                var boxColor = isActive ? ClientColors.COLOR_DARK_MENU_FIELD_SELECTED_BACKGROUND : ClientColors.COLOR_DARK_MENU_FIELD_BACKGROUND;
                spriteBatch.Draw(_Pixel, new Rectangle((int)valueX, (int)position.Y, (int)GetValueWidth(), (int)GetHeight()), boxColor);
                valueX += BOX_PADDING;
            }

            var showCursor = isFocused && _BlinkTime < CURSOR_BLINK_SECONDS;
            TextHelper.DrawShadowedString(spriteBatch, Font, showCursor ? Value + CURSOR : Value, new Vector2(valueX, position.Y), textColor);
        }
    }
}
