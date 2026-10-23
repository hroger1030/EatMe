using EatMeLibrary;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EatMeClient
{
    public class TypingTextControl
    {
        private const float DEFAULT_CHARS_PER_SECOND = 15f;
        private const int CURSOR_BLINK_MS = 250;
        private const string CURSOR_CHARACTER = "_";
        private const float TEXT_PADDING = 30f;
        private const float SCROLLBAR_WIDTH = 16f;
        private const float SCROLLBAR_GAP = 20f;
        private const float MIN_SCROLLBAR_THUMB_HEIGHT = 30f;
        private const float SCROLL_KEY_SPEED = 400f;
        private const float SCROLL_WHEEL_SPEED = 3f;


        private readonly SpriteFont _Font;
        private Texture2D _Pixel;
        private RasterizerState _ScissorRasterizerState;

        private readonly string _RawText;
        private string _WrappedText;

        private readonly float _CharsPerSecond;
        private float _WrappedTextWidth = -1f;
        private int _VisibleCharCount;
        private TimeSpan _CharTimer;
        private TimeSpan _CursorBlinkTimer;
        private float _ScrollOffset;
        private bool _IsDraggingScrollbar;
        private bool _CursorVisible = true;

        public bool IsComplete => _WrappedText != null && _VisibleCharCount >= _WrappedText.Length;

        public TypingTextControl(SpriteFont font, string text, float charsPerSecond = DEFAULT_CHARS_PER_SECOND)
        {
            _Font = font;
            _RawText = text;
            _CharsPerSecond = charsPerSecond;
        }

        public void Reset()
        {
            _VisibleCharCount = 0;
            _CharTimer = TimeSpan.Zero;
            _CursorBlinkTimer = TimeSpan.Zero;
            _CursorVisible = true;
            _ScrollOffset = 0f;
            _IsDraggingScrollbar = false;
        }

        /// <summary>
        /// in the event we want to skip all the aninmation, we can invoke this method.
        /// </summary>
        public void CompleteInstantly()
        {
            if (_WrappedText != null)
                _VisibleCharCount = _WrappedText.Length;
        }

        public void Update(GameTime gameTime, Rectangle area)
        {
            var keyboardState = InputManager.KeyboardState;
            var gamePadState = InputManager.GamePadState[(int)PlayerIndex.One];
            var mouseState = InputManager.MouseState;
            var previousMouseState = InputManager.PreviousMouseState;

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var textBounds = GetTextBounds(area);
            var wrappedText = GetWrappedText(textBounds.Width);

            if (_VisibleCharCount < wrappedText.Length)
            {
                var timePerChar = TimeSpan.FromSeconds(1.0 / _CharsPerSecond);
                var typedVisibleChar = false;
                _CharTimer += gameTime.ElapsedGameTime;

                while (_CharTimer >= timePerChar && _VisibleCharCount < wrappedText.Length)
                {
                    _CharTimer -= timePerChar;

                    if (!char.IsWhiteSpace(wrappedText[_VisibleCharCount]))
                        typedVisibleChar = true;

                    _VisibleCharCount++;
                }

                // One tick per frame at most, and none for spaces or line breaks
                if (typedVisibleChar)
                    SoundManager.PlaySound(Const.SFX_TICK);
            }

            _CursorBlinkTimer += gameTime.ElapsedGameTime;
            if (_CursorBlinkTimer.TotalMilliseconds >= CURSOR_BLINK_MS)
            {
                _CursorBlinkTimer -= TimeSpan.FromMilliseconds(CURSOR_BLINK_MS);
                _CursorVisible = !_CursorVisible;
            }

            var visibleHeight = GetVisibleTextHeight(wrappedText);
            var maxScroll = Math.Max(0f, visibleHeight - textBounds.Height);
            var scrollDelta = 0f;

            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S) || gamePadState.IsButtonDown(Buttons.DPadDown) || gamePadState.IsButtonDown(Buttons.LeftThumbstickDown))
                scrollDelta += SCROLL_KEY_SPEED * dt;

            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W) || gamePadState.IsButtonDown(Buttons.DPadUp) || gamePadState.IsButtonDown(Buttons.LeftThumbstickUp))
                scrollDelta -= SCROLL_KEY_SPEED * dt;

            var wheelDelta = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
            scrollDelta -= wheelDelta / 120f * SCROLL_WHEEL_SPEED;

            if (IsComplete && maxScroll > 0f)
                UpdateScrollbarDrag(area, textBounds, visibleHeight, maxScroll, mouseState, previousMouseState);
            else
                _IsDraggingScrollbar = false;

            if (!_IsDraggingScrollbar)
                _ScrollOffset = IsComplete ? _ScrollOffset + scrollDelta : maxScroll;

            _ScrollOffset = MathHelper.Clamp(_ScrollOffset, 0f, maxScroll);
        }

        private void UpdateScrollbarDrag(Rectangle area, Rectangle textBounds, float visibleHeight, float maxScroll, MouseState mouseState, MouseState previousMouseState)
        {
            var (trackBounds, thumbBounds) = GetScrollbarBounds(area, textBounds, visibleHeight, maxScroll);
            var mousePoint = new Point(mouseState.X, mouseState.Y);
            var justPressed = mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;

            if (justPressed && trackBounds.Contains(mousePoint))
                _IsDraggingScrollbar = true;

            if (mouseState.LeftButton == ButtonState.Released)
                _IsDraggingScrollbar = false;

            if (_IsDraggingScrollbar)
            {
                var travel = trackBounds.Height - thumbBounds.Height;
                var relativeY = mousePoint.Y - trackBounds.Y - thumbBounds.Height / 2f;
                var fraction = travel > 0f ? MathHelper.Clamp(relativeY / travel, 0f, 1f) : 0f;
                _ScrollOffset = fraction * maxScroll;
            }
        }

        private float GetVisibleTextHeight(string wrappedText)
        {
            var visibleText = wrappedText.Substring(0, _VisibleCharCount);
            var visibleLineCount = visibleText.Split('\n').Length;
            return visibleLineCount * _Font.LineSpacing;
        }

        private Rectangle GetTextBounds(Rectangle area)
        {
            var left = area.Left + TEXT_PADDING;
            var top = area.Top + TEXT_PADDING;
            var bottom = area.Bottom - TEXT_PADDING;
            var scrollbarX = area.Right - TEXT_PADDING - SCROLLBAR_WIDTH;
            var right = scrollbarX - SCROLLBAR_GAP;

            return new Rectangle((int)left, (int)top, (int)(right - left), (int)(bottom - top));
        }

        private (Rectangle Track, Rectangle Thumb) GetScrollbarBounds(Rectangle area, Rectangle textBounds, float visibleHeight, float maxScroll)
        {
            var scrollbarX = area.Right - TEXT_PADDING - SCROLLBAR_WIDTH;
            var trackBounds = new Rectangle((int)scrollbarX, textBounds.Top, (int)SCROLLBAR_WIDTH, textBounds.Height);

            var thumbHeight = Math.Max(MIN_SCROLLBAR_THUMB_HEIGHT, textBounds.Height * (textBounds.Height / visibleHeight));
            var thumbTravel = textBounds.Height - thumbHeight;
            var thumbY = textBounds.Top + thumbTravel * (maxScroll > 0f ? _ScrollOffset / maxScroll : 0f);
            var thumbBounds = new Rectangle((int)scrollbarX, (int)thumbY, (int)SCROLLBAR_WIDTH, (int)thumbHeight);

            return (trackBounds, thumbBounds);
        }

        private string GetWrappedText(float wrapWidth)
        {
            if (_WrappedText != null && Math.Abs(_WrappedTextWidth - wrapWidth) < 0.5f)
                return _WrappedText;

            var lines = WrapText(_RawText, wrapWidth);
            _WrappedText = string.Join("\n", lines);
            _WrappedTextWidth = wrapWidth;
            return _WrappedText;
        }

        private List<string> WrapText(string text, float maxWidth)
        {
            var lines = new List<string>();
            var paragraphs = text.Replace("\r\n", "\n").Split('\n');

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Trim().Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var words = paragraph.Split(' ');
                var currentLine = string.Empty;

                foreach (var word in words)
                {
                    var candidate = currentLine.Length == 0 ? word : $"{currentLine} {word}";
                    if (_Font.MeasureString(candidate).X > maxWidth && currentLine.Length > 0)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = candidate;
                    }
                }

                lines.Add(currentLine);
            }

            return lines;
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle area)
        {
            if (_Pixel == null)
                _Pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            spriteBatch.Draw(_Pixel, area, ClientColors.COLOR_TEXT_PANEL);

            var textBounds = GetTextBounds(area);
            var wrappedText = GetWrappedText(textBounds.Width);
            var visibleText = wrappedText.Substring(0, _VisibleCharCount);
            var visibleHeight = GetVisibleTextHeight(wrappedText);
            var maxScroll = Math.Max(0f, visibleHeight - textBounds.Height);
            var lineHeight = _Font.LineSpacing;

            var displayText = _CursorVisible ? visibleText + CURSOR_CHARACTER : visibleText;
            var displayLines = displayText.Split('\n');

            // Clip text drawing to textBounds so an out-of-sync scroll offset can never render past the edges of the control.
            var graphicsDevice = spriteBatch.GraphicsDevice;
            var previousScissorRect = graphicsDevice.ScissorRectangle;

            spriteBatch.End();
            graphicsDevice.ScissorRectangle = Rectangle.Intersect(textBounds, graphicsDevice.Viewport.Bounds);
            spriteBatch.Begin(rasterizerState: GetScissorRasterizerState());

            var y = textBounds.Top - _ScrollOffset;
            foreach (var line in displayLines)
            {
                if (y + lineHeight >= textBounds.Top && y <= textBounds.Bottom)
                    spriteBatch.DrawString(_Font, line, new Vector2(textBounds.Left, y), ClientColors.COLOR_PAC_WHITE);

                y += lineHeight;
            }

            spriteBatch.End();
            graphicsDevice.ScissorRectangle = previousScissorRect;
            spriteBatch.Begin();

            if (IsComplete && maxScroll > 0f)
                DrawScrollbar(spriteBatch, area, textBounds, visibleHeight, maxScroll);
        }

        private void DrawScrollbar(SpriteBatch spriteBatch, Rectangle area, Rectangle textBounds, float visibleHeight, float maxScroll)
        {
            if (_Pixel == null)
                _Pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var (trackBounds, thumbBounds) = GetScrollbarBounds(area, textBounds, visibleHeight, maxScroll);

            spriteBatch.Draw(_Pixel, trackBounds, ClientColors.COLOR_PAC_LIGHT_GREY);
            spriteBatch.Draw(_Pixel, thumbBounds, ClientColors.COLOR_PAC_WHITE);
        }

        private RasterizerState GetScissorRasterizerState()
        {
            _ScissorRasterizerState ??= new RasterizerState { ScissorTestEnable = true };
            return _ScissorRasterizerState;
        }
    }
}
