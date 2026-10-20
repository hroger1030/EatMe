using EatMeLibrary;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EatMeClient
{
    public class InGameMenu
    {
        private const float OPTION_SPACING = 1.75f;
        private const float TEXT_PADDING = 30f;

        private static readonly string[] MENU_OPTIONS = [Const.STRING_RESUME_GAME, Const.STRING_GAME_SETTINGS, Const.STRING_QUIT_GAME];

        private readonly SpriteFont _Font;
        private readonly Action _OnResume;
        private readonly Action _OnSettings;
        private readonly Action _OnQuit;
        private Texture2D _Pixel;
        private int _SelectedIndex;

        /// <summary>
        /// onSettings is called with the game still paused, so the settings screen opens over it.
        /// </summary>
        public InGameMenu(SpriteFont font, Action onResume, Action onSettings, Action onQuit)
        {
            _Font = font;
            _OnResume = onResume;
            _OnSettings = onSettings;
            _OnQuit = onQuit;
        }

        public void Open()
        {
            ConfigManager.Settings.Paused = true;
            _SelectedIndex = 0;
        }

        public void Close()
        {
            ConfigManager.Settings.Paused = false;
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            if (!ConfigManager.Settings.Paused)
                return;

            if (InputManager.KeyPressed(Keys.Escape)
                || InputManager.GamePadButtonPressed(Buttons.B)
                || InputManager.GamePadButtonPressed(Buttons.Back))
            {
                Close();
                _OnResume();
                return;
            }

            if (InputManager.KeyPressed(Keys.Down) || InputManager.KeyPressed(Keys.S)
                || InputManager.GamePadButtonPressed(Buttons.DPadDown)
                || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickDown))
            {
                _SelectedIndex = (_SelectedIndex + 1) % MENU_OPTIONS.Length;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (InputManager.KeyPressed(Keys.Up) || InputManager.KeyPressed(Keys.W)
                || InputManager.GamePadButtonPressed(Buttons.DPadUp)
                || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickUp))
            {
                _SelectedIndex = (_SelectedIndex - 1 + MENU_OPTIONS.Length) % MENU_OPTIONS.Length;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            var activateRequested =
                InputManager.KeyPressed(Keys.Enter) ||
                InputManager.KeyPressed(Keys.Space) ||
                InputManager.GamePadButtonPressed(Buttons.A);

            var optionBounds = GetOptionBounds(viewport);
            var mouseState = InputManager.MouseState;
            var mousePoint = new Point(mouseState.X, mouseState.Y);

            for (var i = 0; i < optionBounds.Length; i++)
            {
                if (!optionBounds[i].Contains(mousePoint))
                    continue;

                var clicked = InputManager.MouseButtonPressed(InputManager.MouseButton.Left);

                if (clicked || InputManager.MouseMoved())
                    _SelectedIndex = i;

                if (clicked)
                {
                    activateRequested = true;
                    SoundManager.PlaySound(Const.SFX_TICK);
                }

                break;
            }

            if (activateRequested)
                SelectOption(_SelectedIndex);
        }

        private void SelectOption(int index)
        {
            // the game stays paused behind the settings screen
            if (MENU_OPTIONS[index] == Const.STRING_GAME_SETTINGS)
            {
                _OnSettings();
                return;
            }

            Close();

            if (MENU_OPTIONS[index] == Const.STRING_QUIT_GAME)
                _OnQuit();
            else
                _OnResume();
        }

        private Rectangle[] GetOptionBounds(Viewport viewport)
        {
            var bounds = new Rectangle[MENU_OPTIONS.Length];
            var totalHeight = _Font.LineSpacing * OPTION_SPACING * (MENU_OPTIONS.Length - 1) + _Font.LineSpacing;
            var y = (viewport.Height - totalHeight) / 2f;

            for (var i = 0; i < MENU_OPTIONS.Length; i++)
            {
                var optionSize = _Font.MeasureString(MENU_OPTIONS[i]);
                var optionPosition = new Vector2((viewport.Width - optionSize.X) / 2f, y);
                bounds[i] = new Rectangle((int)optionPosition.X, (int)optionPosition.Y, (int)optionSize.X, (int)optionSize.Y);
                y += _Font.LineSpacing * OPTION_SPACING;
            }

            return bounds;
        }

        private Rectangle GetPanelBounds(Viewport viewport)
        {
            var optionBounds = GetOptionBounds(viewport);

            var left = optionBounds[0].Left;
            var top = optionBounds[0].Top;
            var right = optionBounds[0].Right;
            var bottom = optionBounds[0].Bottom;

            foreach (var bounds in optionBounds)
            {
                left = Math.Min(left, bounds.Left);
                top = Math.Min(top, bounds.Top);
                right = Math.Max(right, bounds.Right);
                bottom = Math.Max(bottom, bounds.Bottom);
            }

            return new Rectangle(
                (int)(left - TEXT_PADDING),
                (int)(top - TEXT_PADDING),
                (int)(right - left + TEXT_PADDING * 2f),
                (int)(bottom - top + TEXT_PADDING * 2f));
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (!ConfigManager.Settings.Paused)
                return;

            if (_Pixel == null)
                _Pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var panelBounds = GetPanelBounds(viewport);

            spriteBatch.Begin();
            TextureHelper.FillRounded(spriteBatch, _Pixel, panelBounds, MiniMap.CORNER_RADIUS, ClientColors.COLOR_LIGHT_MENU_PANEL);

            var optionBounds = GetOptionBounds(viewport);

            // draw the game option
            for (var i = 0; i < MENU_OPTIONS.Length; i++)
            {
                var color = i == _SelectedIndex ? ClientColors.COLOR_LIGHT_MENU_SELECTED : ClientColors.COLOR_LIGHT_MENU_ITEM;
                spriteBatch.DrawString(_Font, MENU_OPTIONS[i], new Vector2(optionBounds[i].X, optionBounds[i].Y), color);
            }

            spriteBatch.End();
        }
    }
}
