using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EatMeClient
{
    public class StoryScreen
    {
        private const float TEXT_AREA_WIDTH_RATIO = 0.5f;

        private SpriteFont _Font;
        private SpriteFont _TitleFont;
        private SpriteFont _TextFont;
        private readonly Action _OnBack;
        private TypingTextControl _TypingText;
        private bool _IsBackHovered;
        private bool _IsBackSelected;

        public StoryScreen(Action onBack)
        {
            _OnBack = onBack;
        }

        public void LoadContent()
        {
            _Font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC60);
            _TitleFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC96);
            _TextFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC36);
        }

        public void Activate(Viewport viewport)
        {
            _TypingText = new TypingTextControl(_TextFont, AssetCache.GetText(Const.TEXT_STORY));
            _IsBackSelected = false;
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

            // W/S and up/down scroll the story text; down, A/D and left/right also select the Back option
            if (InputManager.KeyPressed(Keys.A) || InputManager.KeyPressed(Keys.D) ||
                InputManager.KeyPressed(Keys.Left) || InputManager.KeyPressed(Keys.Right) ||
                InputManager.KeyPressed(Keys.S) || InputManager.KeyPressed(Keys.Down) ||
                InputManager.GamePadButtonPressed(Buttons.DPadLeft) || InputManager.GamePadButtonPressed(Buttons.DPadRight) ||
                InputManager.GamePadButtonPressed(Buttons.DPadDown) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickDown) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickLeft) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickRight))
            {
                if (!_IsBackSelected)
                    SoundManager.PlaySound(Const.SFX_TICK);

                _IsBackSelected = true;
            }

            var backBounds = GetBackBounds(viewport);

            _IsBackHovered = backBounds.Contains(new Point(InputManager.MouseState.X, InputManager.MouseState.Y));

            if (_IsBackHovered && InputManager.MouseMoved())
                _IsBackSelected = true;

            if (_IsBackHovered && InputManager.MouseButtonPressed(InputManager.MouseButton.Left))
            {
                backRequested = true;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (backRequested)
                _OnBack();

            _TypingText.Update(gameTime, GetTextAreaBounds(viewport));
        }

        private Rectangle GetBackBounds(Viewport viewport)
        {
            var backSize = _Font.MeasureString(Const.STRING_BACK);
            var backPosition = new Vector2((viewport.Width - backSize.X) / 2f, viewport.Height * 0.8f);
            return new Rectangle((int)backPosition.X, (int)backPosition.Y, (int)backSize.X, (int)backSize.Y);
        }

        private Rectangle GetTextAreaBounds(Viewport viewport)
        {
            var headingSize = _TitleFont.MeasureString(Const.STRING_STORY);

            var top = viewport.Height * 0.2f + headingSize.Y + _Font.LineSpacing;
            var bottom = viewport.Height * 0.8f - _Font.LineSpacing;
            var columnWidth = viewport.Width * TEXT_AREA_WIDTH_RATIO;
            var left = (viewport.Width - columnWidth) / 2f;
            var right = left + columnWidth;

            return new Rectangle((int)left, (int)top, (int)(right - left), (int)(bottom - top));
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            spriteBatch.Begin();

            // screen title
            var headingSize = _TitleFont.MeasureString(Const.STRING_STORY);
            var headingPosition = new Vector2((viewport.Width - headingSize.X) / 2f, viewport.Height * 0.2f);
            TextHelper.DrawShadowedString(spriteBatch, _TitleFont, Const.STRING_STORY, headingPosition, ClientColors.COLOR_DARK_TITLE_ITEM, ClientColors.COLOR_DARK_TITLE_SHADOW);

            // story text
            _TypingText.Draw(spriteBatch, GetTextAreaBounds(viewport));

            // back option
            var backBounds = GetBackBounds(viewport);
            var backColor = _IsBackSelected || _IsBackHovered ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;
            TextHelper.DrawShadowedString(spriteBatch, _Font, Const.STRING_BACK, new Vector2(backBounds.X, backBounds.Y), backColor);

            spriteBatch.End();
        }
    }
}
