using EatMeLibrary;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EatMeClient
{
    public class HighScoresScreen
    {
        private const int TABLE_COLUMNS = 2;
        private const float TABLE_WIDTH_RATIO = 0.84f;
        private const float COLUMN_GAP_RATIO = 0.06f;
        private const float RANK_COLUMN_RATIO = 0.12f;
        private const float NAME_COLUMN_RATIO = 0.46f;
        private const float SCORE_COLUMN_RATIO = 0.2f;
        private const float ROW_SPACING = 1.2f;
        private const float TITLE_Y_RATIO = 0.1f;
        private const float BACK_Y_RATIO = 0.88f;
        private const float SCROLLBAR_WIDTH = 12f;
        private const float SCROLLBAR_GAP = 20f;
        private const float MIN_SCROLLBAR_THUMB_HEIGHT = 30f;
        private const float SCROLL_KEY_SPEED = 600f;
        private const float SCROLL_WHEEL_ROWS = 3f;

        private SpriteFont _font;
        private SpriteFont _TitleFont;
        private SpriteFont _TableFont;
        private Texture2D _Pixel;
        private RasterizerState _ScissorRasterizerState;
        private readonly Action _onBack;

        private bool _isBackHovered;
        private bool _isBackSelected;
        private float _ScrollOffset;
        private bool _IsDraggingScrollbar;

        // the client's own list (HighScoreStore), highest first; only the title and Back are drawn while it is empty
        public List<HighScoreEntry> Scores { get; set; } = [];

        public HighScoresScreen(Action onBack)
        {
            _onBack = onBack;
        }

        public void LoadContent()
        {
            _font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC60);
            _TitleFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC96);
            _TableFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC30);
        }

        public void Activate()
        {
            _isBackSelected = false;
            _ScrollOffset = 0f;
            _IsDraggingScrollbar = false;
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            var backRequested =
                InputManager.KeyPressed(Keys.Escape) ||
                InputManager.KeyPressed(Keys.Enter) ||
                InputManager.KeyPressed(Keys.Space) ||
                InputManager.GamePadButtonPressed(Buttons.B) ||
                InputManager.GamePadButtonPressed(Buttons.A) ||
                InputManager.GamePadButtonPressed(Buttons.Back);

            // up and down scroll the table; down, left and right also select the Back option (the only one on this screen)
            if (InputManager.KeyPressed(Keys.A) || InputManager.KeyPressed(Keys.D) ||
                InputManager.KeyPressed(Keys.Left) || InputManager.KeyPressed(Keys.Right) ||
                InputManager.KeyPressed(Keys.S) || InputManager.KeyPressed(Keys.Down) ||
                InputManager.GamePadButtonPressed(Buttons.DPadLeft) || InputManager.GamePadButtonPressed(Buttons.DPadRight) ||
                InputManager.GamePadButtonPressed(Buttons.DPadDown) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickDown) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickLeft) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickRight))
            {
                if (!_isBackSelected)
                    SoundManager.PlaySound(Const.SFX_TICK);

                _isBackSelected = true;
            }

            UpdateScroll(gameTime, viewport);

            var backBounds = GetBackBounds(viewport);

            _isBackHovered = backBounds.Contains(new Point(InputManager.MouseState.X, InputManager.MouseState.Y));

            if (_isBackHovered && InputManager.MouseMoved())
                _isBackSelected = true;

            if (_isBackHovered && InputManager.MouseButtonPressed(InputManager.MouseButton.Left))
            {
                backRequested = true;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (backRequested)
                _onBack();
        }

        private void UpdateScroll(GameTime gameTime, Viewport viewport)
        {
            var keyboardState = InputManager.KeyboardState;
            var gamePadState = InputManager.GamePadState[(int)PlayerIndex.One];
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var rowsBounds = GetRowsBounds(viewport);
            var contentHeight = GetContentHeight();
            var viewHeight = rowsBounds.Height;

            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S) || gamePadState.IsButtonDown(Buttons.DPadDown) || gamePadState.IsButtonDown(Buttons.LeftThumbstickDown))
                _ScrollOffset += SCROLL_KEY_SPEED * dt;

            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W) || gamePadState.IsButtonDown(Buttons.DPadUp) || gamePadState.IsButtonDown(Buttons.LeftThumbstickUp))
                _ScrollOffset -= SCROLL_KEY_SPEED * dt;

            if (InputManager.KeyPressed(Keys.PageDown))
                _ScrollOffset += viewHeight;

            if (InputManager.KeyPressed(Keys.PageUp))
                _ScrollOffset -= viewHeight;

            if (InputManager.KeyPressed(Keys.Home))
                _ScrollOffset = 0f;

            if (InputManager.KeyPressed(Keys.End))
                _ScrollOffset = contentHeight;

            var wheelDelta = InputManager.MouseState.ScrollWheelValue - InputManager.PreviousMouseState.ScrollWheelValue;
            _ScrollOffset -= wheelDelta / 120f * SCROLL_WHEEL_ROWS * GetRowHeight();

            // dragging the scrollbar (only there when the rows don't fit)
            if (ScrollMath.GetMaxScroll(contentHeight, viewHeight) > 0f)
            {
                var trackBounds = GetTrackBounds(viewport);
                var mousePoint = new Point(InputManager.MouseState.X, InputManager.MouseState.Y);

                if (InputManager.MouseButtonPressed(InputManager.MouseButton.Left) && trackBounds.Contains(mousePoint))
                    _IsDraggingScrollbar = true;

                if (InputManager.MouseState.LeftButton == ButtonState.Released)
                    _IsDraggingScrollbar = false;

                if (_IsDraggingScrollbar)
                {
                    var thumbHeight = ScrollMath.GetThumbHeight(contentHeight, viewHeight, MIN_SCROLLBAR_THUMB_HEIGHT);
                    _ScrollOffset = ScrollMath.GetOffsetFromThumb(mousePoint.Y - trackBounds.Y, contentHeight, viewHeight, thumbHeight);
                }
            }
            else
            {
                _IsDraggingScrollbar = false;
            }

            _ScrollOffset = ScrollMath.ClampOffset(_ScrollOffset, contentHeight, viewHeight);
        }

        private Rectangle GetBackBounds(Viewport viewport)
        {
            var backSize = _font.MeasureString(Const.STRING_BACK);
            var backPosition = new Vector2((viewport.Width - backSize.X) / 2f, viewport.Height * BACK_Y_RATIO);
            return new Rectangle((int)backPosition.X, (int)backPosition.Y, (int)backSize.X, (int)backSize.Y);
        }

        private float GetRowHeight()
        {
            return _TableFont.LineSpacing * ROW_SPACING;
        }

        private float GetContentHeight()
        {
            var count = Scores?.Count ?? 0;
            return HighScoreRules.GetRowCount(count, TABLE_COLUMNS) * GetRowHeight();
        }

        private float GetTableLeft(Viewport viewport)
        {
            return (viewport.Width - viewport.Width * TABLE_WIDTH_RATIO) / 2f;
        }

        // the column headings, from just under the screen title
        private float GetHeaderY(Viewport viewport)
        {
            var titleHeight = _TitleFont.MeasureString(Const.STRING_HIGH_SCORES).Y;
            return viewport.Height * TITLE_Y_RATIO + titleHeight + _TableFont.LineSpacing;
        }

        // the scrolling rows: from under the headings to just above Back
        private Rectangle GetRowsBounds(Viewport viewport)
        {
            var left = GetTableLeft(viewport);
            var top = GetHeaderY(viewport) + GetRowHeight();
            var bottom = GetBackBounds(viewport).Top - _TableFont.LineSpacing;
            var width = viewport.Width * TABLE_WIDTH_RATIO;

            return new Rectangle((int)left, (int)top, (int)width, (int)Math.Max(0f, bottom - top));
        }

        private Rectangle GetTrackBounds(Viewport viewport)
        {
            var rowsBounds = GetRowsBounds(viewport);
            return new Rectangle((int)(rowsBounds.Right + SCROLLBAR_GAP), rowsBounds.Top, (int)SCROLLBAR_WIDTH, rowsBounds.Height);
        }

        private (float RankX, float NameX, float ScoreX, float DateX) GetColumnPositions(Viewport viewport, int column)
        {
            var tableWidth = viewport.Width * TABLE_WIDTH_RATIO;
            var gap = tableWidth * COLUMN_GAP_RATIO;
            var blockWidth = (tableWidth - gap * (TABLE_COLUMNS - 1)) / TABLE_COLUMNS;

            var rankX = GetTableLeft(viewport) + column * (blockWidth + gap);
            var nameX = rankX + blockWidth * RANK_COLUMN_RATIO;
            var scoreX = nameX + blockWidth * NAME_COLUMN_RATIO;
            var dateX = scoreX + blockWidth * SCORE_COLUMN_RATIO;

            return (rankX, nameX, scoreX, dateX);
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            spriteBatch.Begin();

            // screen title
            var headingSize = _TitleFont.MeasureString(Const.STRING_HIGH_SCORES);
            var headingPosition = new Vector2((viewport.Width - headingSize.X) / 2f, viewport.Height * TITLE_Y_RATIO);
            TextHelper.DrawShadowedString(spriteBatch, _TitleFont, Const.STRING_HIGH_SCORES, headingPosition, ClientColors.COLOR_DARK_TITLE_ITEM, ClientColors.COLOR_DARK_TITLE_SHADOW);

            // back option
            var backBounds = GetBackBounds(viewport);
            var backColor = _isBackSelected || _isBackHovered ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;
            TextHelper.DrawShadowedString(spriteBatch, _font, Const.STRING_BACK, new Vector2(backBounds.X, backBounds.Y), backColor);

            // high score table (ends the batch and begins a fresh one around its clipped rows)
            DrawTable(spriteBatch, viewport);

            spriteBatch.End();
        }

        private void DrawTable(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (Scores == null || Scores.Count == 0)
                return;

            var headerY = GetHeaderY(viewport);

            for (var column = 0; column < TABLE_COLUMNS; column++)
            {
                var (rankX, nameX, scoreX, dateX) = GetColumnPositions(viewport, column);
                TextHelper.DrawShadowedString(spriteBatch, _TableFont, Const.STRING_RANK, new Vector2(rankX, headerY), ClientColors.COLOR_DARK_MENU_HEADING);
                TextHelper.DrawShadowedString(spriteBatch, _TableFont, Const.STRING_NAME, new Vector2(nameX, headerY), ClientColors.COLOR_DARK_MENU_HEADING);
                TextHelper.DrawShadowedString(spriteBatch, _TableFont, Const.STRING_SCORE, new Vector2(scoreX, headerY), ClientColors.COLOR_DARK_MENU_HEADING);
                TextHelper.DrawShadowedString(spriteBatch, _TableFont, Const.STRING_DATE, new Vector2(dateX, headerY), ClientColors.COLOR_DARK_MENU_HEADING);
            }

            DrawRows(spriteBatch, viewport);
            DrawScrollbar(spriteBatch, viewport);
        }

        // the rows are clipped to GetRowsBounds so scrolled rows never draw over the headings or Back
        private void DrawRows(SpriteBatch spriteBatch, Viewport viewport)
        {
            var rowsBounds = GetRowsBounds(viewport);
            var rowHeight = GetRowHeight();
            var rowCount = HighScoreRules.GetRowCount(Scores.Count, TABLE_COLUMNS);
            var graphicsDevice = spriteBatch.GraphicsDevice;
            var previousScissorRect = graphicsDevice.ScissorRectangle;

            spriteBatch.End();
            graphicsDevice.ScissorRectangle = Rectangle.Intersect(rowsBounds, graphicsDevice.Viewport.Bounds);
            spriteBatch.Begin(rasterizerState: GetScissorRasterizerState());

            for (var row = 0; row < rowCount; row++)
            {
                var y = rowsBounds.Top + row * rowHeight - _ScrollOffset;

                if (y + rowHeight < rowsBounds.Top || y > rowsBounds.Bottom)
                    continue;

                for (var column = 0; column < TABLE_COLUMNS; column++)
                {
                    var index = HighScoreRules.GetEntryIndex(row, column, TABLE_COLUMNS);

                    if (index >= Scores.Count)
                        break;

                    var entry = Scores[index];
                    var (rankX, nameX, scoreX, dateX) = GetColumnPositions(viewport, column);

                    TextHelper.DrawShadowedString(spriteBatch, _TableFont, HighScoreRules.GetRankText(index), new Vector2(rankX, y), ClientColors.COLOR_DARK_MENU_ITEM);
                    TextHelper.DrawShadowedString(spriteBatch, _TableFont, entry.Name, new Vector2(nameX, y), ClientColors.COLOR_DARK_MENU_ITEM);
                    TextHelper.DrawShadowedString(spriteBatch, _TableFont, entry.Score.ToString(), new Vector2(scoreX, y), ClientColors.COLOR_DARK_MENU_ITEM);
                    TextHelper.DrawShadowedString(spriteBatch, _TableFont, entry.Date, new Vector2(dateX, y), ClientColors.COLOR_DARK_MENU_ITEM);
                }
            }

            spriteBatch.End();
            graphicsDevice.ScissorRectangle = previousScissorRect;
            spriteBatch.Begin();
        }

        private void DrawScrollbar(SpriteBatch spriteBatch, Viewport viewport)
        {
            var contentHeight = GetContentHeight();
            var trackBounds = GetTrackBounds(viewport);

            if (ScrollMath.GetMaxScroll(contentHeight, trackBounds.Height) <= 0f)
                return;

            _Pixel ??= TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var thumbHeight = ScrollMath.GetThumbHeight(contentHeight, trackBounds.Height, MIN_SCROLLBAR_THUMB_HEIGHT);
            var thumbY = trackBounds.Top + ScrollMath.GetThumbOffset(_ScrollOffset, contentHeight, trackBounds.Height, thumbHeight);
            var thumbBounds = new Rectangle(trackBounds.X, (int)thumbY, trackBounds.Width, (int)thumbHeight);

            spriteBatch.Draw(_Pixel, trackBounds, ClientColors.COLOR_DARK_MENU_SLIDER_BACKGROUND);
            spriteBatch.Draw(_Pixel, thumbBounds, ClientColors.COLOR_DARK_MENU_ITEM);
        }

        private RasterizerState GetScissorRasterizerState()
        {
            _ScissorRasterizerState ??= new RasterizerState { ScissorTestEnable = true };
            return _ScissorRasterizerState;
        }
    }
}
