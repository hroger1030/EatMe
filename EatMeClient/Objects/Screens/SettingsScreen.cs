using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EatMeClient
{
    public class SettingsScreen
    {
        private const float VOLUME_ADJUST_RATE = 60f;
        private const float SLIDER_SPACING = 30f;

        /// <summary>
        /// Gap between the pac toggle's value column and the chomping preview of the picked shape.
        /// </summary>
        private const float PAC_PREVIEW_GAP = 30f;

        private enum SettingsItem { MusicVolume, SoundVolume, Brightness, Bloom, Controls, Display, PacShape, Back }

        private static readonly SettingsItem[] SETTINGS_ITEMS = [SettingsItem.MusicVolume, SettingsItem.SoundVolume, SettingsItem.Brightness, SettingsItem.Bloom, SettingsItem.Controls, SettingsItem.Display, SettingsItem.PacShape, SettingsItem.Back];
        private static readonly string[] CONTROL_SCHEME_OPTIONS = [Const.STRING_CONTROLS_MOUSE_KEYBOARD, Const.STRING_CONTROLS_GAME_PAD];
        private static readonly string[] DISPLAY_OPTIONS = [Const.STRING_DISPLAY_FULL_SCREEN, Const.STRING_DISPLAY_WINDOWED];

        private SpriteFont _Font;
        private SpriteFont _TitleFont;
        private SliderControl _MusicVolumeSlider;
        private SliderControl _SoundVolumeSlider;
        private ToggleControl _ControlsToggle;
        private ToggleControl _DisplayToggle;
        private ToggleControl _PacShapeToggle;
        private Texture2D _PacSheet;
        private float _AnimationTime;
        private SliderControl _BrightnessSlider;
        private SliderControl _BloomSlider;
        private readonly Action _OnBack;

        private SettingsItem _SelectedItem = SettingsItem.MusicVolume;

        public SettingsScreen(Action onBack)
        {
            _OnBack = onBack;
        }

        public void LoadContent()
        {
            _Font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC60);
            _TitleFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC96);
            _MusicVolumeSlider = new SliderControl(_Font, Const.STRING_MUSIC_VOLUME);
            _SoundVolumeSlider = new SliderControl(_Font, Const.STRING_SOUND_VOLUME);
            _ControlsToggle = new ToggleControl(_Font, Const.STRING_CONTROLS, CONTROL_SCHEME_OPTIONS);
            _DisplayToggle = new ToggleControl(_Font, Const.STRING_DISPLAY, DISPLAY_OPTIONS);
            _PacShapeToggle = new ToggleControl(_Font, Const.STRING_PAC_SHAPE, Array.ConvertAll(PacShapes.ALL, PacShapes.GetName));
            _PacSheet = AssetCache.GetTexture(Const.IMAGE_PAC_SHEET);
            _BrightnessSlider = new SliderControl(_Font, Const.STRING_BRIGHTNESS, DisplayAdjust.BRIGHTNESS_MIN, DisplayAdjust.BRIGHTNESS_MAX);
            _BloomSlider = new SliderControl(_Font, Const.STRING_BLOOM, DisplayAdjust.BLOOM_MIN, DisplayAdjust.BLOOM_MAX);

            // one line each, labels right-aligned in a shared column so the tracks and the toggle's value line up
            var sliders = new[] { _MusicVolumeSlider, _SoundVolumeSlider, _BrightnessSlider, _BloomSlider };
            var labelWidth = MathF.Max(_ControlsToggle.GetLabelColumnWidth(), _DisplayToggle.GetLabelColumnWidth());
            labelWidth = MathF.Max(labelWidth, _PacShapeToggle.GetLabelColumnWidth());

            foreach (var slider in sliders)
                labelWidth = MathF.Max(labelWidth, slider.GetLabelColumnWidth());

            foreach (var slider in sliders)
            {
                slider.IsInline = true;
                slider.LabelWidth = labelWidth;
            }

            _ControlsToggle.IsInline = true;
            _ControlsToggle.LabelWidth = labelWidth;
            _DisplayToggle.IsInline = true;
            _DisplayToggle.LabelWidth = labelWidth;
            _PacShapeToggle.IsInline = true;
            _PacShapeToggle.LabelWidth = labelWidth;
        }

        public void Activate()
        {
            _SelectedItem = SettingsItem.MusicVolume;
            _MusicVolumeSlider.Value = MusicManager.GetVolume() * 100f;
            _SoundVolumeSlider.Value = SoundManager.GetVolume() * 100f;
            _ControlsToggle.SelectedIndex = (int)ConfigManager.Settings.ControllerType;
            _DisplayToggle.SelectedIndex = ConfigManager.Settings.FullScreen ? 0 : 1;
            _PacShapeToggle.SelectedIndex = Math.Max(0, Array.IndexOf(PacShapes.ALL, ConfigManager.Settings.PacShape));
            _BrightnessSlider.Value = ConfigManager.Settings.Brightness;
            _BloomSlider.Value = ConfigManager.Settings.Bloom;
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            _AnimationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // exit screen?
            if (InputManager.KeyPressed(Keys.Escape) || InputManager.GamePadButtonPressed(Buttons.B) || InputManager.GamePadButtonPressed(Buttons.Back))
                ExitScreen();

            // which item we looking at?
            var selectedIndex = Array.IndexOf(SETTINGS_ITEMS, _SelectedItem);

            if (InputManager.KeyPressed(Keys.Down) || 
                InputManager.KeyPressed(Keys.S) || 
                InputManager.GamePadButtonPressed(Buttons.DPadDown) || 
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickDown))
            {
                _SelectedItem = SETTINGS_ITEMS[(selectedIndex + 1) % SETTINGS_ITEMS.Length];
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (InputManager.KeyPressed(Keys.Up) ||
                InputManager.KeyPressed(Keys.W) ||
                InputManager.GamePadButtonPressed(Buttons.DPadUp) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickUp))
            {
                _SelectedItem = SETTINGS_ITEMS[(selectedIndex - 1 + SETTINGS_ITEMS.Length) % SETTINGS_ITEMS.Length];
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            var leftPressed = InputManager.KeyPressed(Keys.Left) ||
                InputManager.KeyPressed(Keys.A) ||
                InputManager.GamePadButtonPressed(Buttons.DPadLeft) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickLeft);

            var rightPressed = InputManager.KeyPressed(Keys.Right) ||
                InputManager.KeyPressed(Keys.D) ||
                InputManager.GamePadButtonPressed(Buttons.DPadRight) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickRight);

            // If we are hovering a slider, figure out which one and process input for it.
            var selectedSlider = _SelectedItem switch
            {
                SettingsItem.MusicVolume => _MusicVolumeSlider,
                SettingsItem.SoundVolume => _SoundVolumeSlider,
                SettingsItem.Brightness => _BrightnessSlider,
                SettingsItem.Bloom => _BloomSlider,
                _ => null
            };

            if (selectedSlider != null)
            {
                var adjust = (float)gameTime.ElapsedGameTime.TotalSeconds * VOLUME_ADJUST_RATE;

                // once per press, not every frame the key is held
                if (leftPressed || rightPressed)
                    SoundManager.PlaySound(Const.SFX_TICK);

                if (InputManager.KeyboardState.IsKeyDown(Keys.Right) || 
                    InputManager.KeyboardState.IsKeyDown(Keys.D) ||
                    InputManager.GamePadState[(int)PlayerIndex.One].IsButtonDown(Buttons.DPadRight) ||
                    InputManager.GamePadState[(int)PlayerIndex.One].IsButtonDown(Buttons.LeftThumbstickRight))
                    selectedSlider.AdjustValue(adjust);

                if (InputManager.KeyboardState.IsKeyDown(Keys.Left) || 
                    InputManager.KeyboardState.IsKeyDown(Keys.A) ||
                    InputManager.GamePadState[(int)PlayerIndex.One].IsButtonDown(Buttons.DPadLeft) ||
                    InputManager.GamePadState[(int)PlayerIndex.One].IsButtonDown(Buttons.LeftThumbstickLeft))
                    selectedSlider.AdjustValue(-adjust);
            }

            // If we are toggling between controls, manage input
            if (_SelectedItem == SettingsItem.Controls)
            {
                if (rightPressed)
                    _ControlsToggle.Next();

                if (leftPressed)
                    _ControlsToggle.Previous();

                if (leftPressed || rightPressed)
                    SoundManager.PlaySound(Const.SFX_TICK);
            }

            // and between full screen and windowed
            if (_SelectedItem == SettingsItem.Display)
            {
                if (rightPressed)
                    _DisplayToggle.Next();

                if (leftPressed)
                    _DisplayToggle.Previous();

                if (leftPressed || rightPressed)
                    SoundManager.PlaySound(Const.SFX_TICK);
            }

            // and between pac shapes
            if (_SelectedItem == SettingsItem.PacShape)
            {
                if (rightPressed)
                    _PacShapeToggle.Next();

                if (leftPressed)
                    _PacShapeToggle.Previous();

                if (leftPressed || rightPressed)
                    SoundManager.PlaySound(Const.SFX_TICK);
            }

            // Unneeded local var? can we just store this in config manager?
            _MusicVolumeSlider.Update(GetMusicSliderPosition(viewport));
            MusicManager.SetVolume(_MusicVolumeSlider.Value / 100f);

            // Unneeded local var? can we just store this in config manager?
            _SoundVolumeSlider.Update(GetSoundSliderPosition(viewport));
            SoundManager.SetVolume(_SoundVolumeSlider.Value / 100f);

            // hover tracking, and a click moves to the next option
            _ControlsToggle.Update(GetControlsPosition(viewport));
            _DisplayToggle.Update(GetDisplayPosition(viewport));
            _PacShapeToggle.Update(GetPacShapePosition(viewport));

            // Brightness takes effect straight away, so this screen previews it
            _BrightnessSlider.Update(GetBrightnessSliderPosition(viewport));
            ConfigManager.Settings.Brightness = _BrightnessSlider.Value;

            // so does bloom
            _BloomSlider.Update(GetBloomSliderPosition(viewport));
            ConfigManager.Settings.Bloom = _BloomSlider.Value;

            // user wants to go back?
            if (_SelectedItem == SettingsItem.Back && (InputManager.KeyPressed(Keys.Enter) ||
                InputManager.KeyPressed(Keys.Space) ||
                InputManager.GamePadButtonPressed(Buttons.A)))
                ExitScreen();

            // Update config manager
            ConfigManager.Settings.ControllerType = (eControllerType)_ControlsToggle.SelectedIndex;

            // TitleScreen switches the display mode as soon as this changes
            ConfigManager.Settings.FullScreen = _DisplayToggle.SelectedIndex == 0;

            // sent to the server the next time the player joins a game
            ConfigManager.Settings.PacShape = PacShapes.ALL[_PacShapeToggle.SelectedIndex];

            var backBounds = GetBackBounds(viewport);
            var mousePoint = new Point(InputManager.MouseState.X, InputManager.MouseState.Y);
            var isBackHovered = backBounds.Contains(mousePoint);

            var hoveredItem = true switch
            {
                _ when _MusicVolumeSlider.IsHovered => SettingsItem.MusicVolume,
                _ when _SoundVolumeSlider.IsHovered => SettingsItem.SoundVolume,
                _ when _ControlsToggle.IsHovered => SettingsItem.Controls,
                _ when _DisplayToggle.IsHovered => SettingsItem.Display,
                _ when _PacShapeToggle.IsHovered => SettingsItem.PacShape,
                _ when _BrightnessSlider.IsHovered => SettingsItem.Brightness,
                _ when _BloomSlider.IsHovered => SettingsItem.Bloom,
                _ when isBackHovered => SettingsItem.Back,
                _ => (SettingsItem?)null,
            };

            var mouseActive = InputManager.MouseMoved() || InputManager.MouseButtonPressed(InputManager.MouseButton.Left);

            if (mouseActive && hoveredItem.HasValue && hoveredItem.Value != _SelectedItem)
                _SelectedItem = hoveredItem.Value;

            if (isBackHovered && InputManager.MouseButtonPressed(InputManager.MouseButton.Left))
            {
                SoundManager.PlaySound(Const.SFX_TICK);
                ExitScreen();
            }
        }

        private void ExitScreen()
        {
            ConfigManager.Save();
            _OnBack();
        }

        private Rectangle GetBackBounds(Viewport viewport)
        {
            var backSize = _Font.MeasureString(Const.STRING_BACK);
            // Normally 80% of the way down, but never over the last setting
            var pacShapeBottom = GetPacShapePosition(viewport).Y + _PacShapeToggle.GetHeight() + SLIDER_SPACING;
            var backPosition = new Vector2((viewport.Width - backSize.X) / 2f, MathF.Max(viewport.Height * 0.8f, pacShapeBottom));
            return new Rectangle((int)backPosition.X, (int)backPosition.Y, (int)backSize.X, (int)backSize.Y);
        }

        /// <summary>
        /// Left edge shared by every setting, so the block is centred as a whole and the label column lines up.
        /// </summary>
        private float GetControlsLeft(Viewport viewport)
        {
            var width = MathF.Max(_MusicVolumeSlider.GetWidth(), _SoundVolumeSlider.GetWidth());
            width = MathF.Max(width, MathF.Max(_BrightnessSlider.GetWidth(), _BloomSlider.GetWidth()));
            width = MathF.Max(width, MathF.Max(_ControlsToggle.GetWidth(), _DisplayToggle.GetWidth()));
            width = MathF.Max(width, _PacShapeToggle.GetWidth());
            return (viewport.Width - width) / 2f;
        }

        private Vector2 GetMusicSliderPosition(Viewport viewport)
        {
            var headingSize = _TitleFont.MeasureString(Const.STRING_GAME_SETTINGS);
            var headingPosition = new Vector2((viewport.Width - headingSize.X) / 2f, viewport.Height * 0.2f);
            var sliderX = GetControlsLeft(viewport);
            return new Vector2(sliderX, headingPosition.Y + headingSize.Y + _Font.LineSpacing);
        }

        private Vector2 GetSoundSliderPosition(Viewport viewport)
        {
            var musicPosition = GetMusicSliderPosition(viewport);
            var sliderX = GetControlsLeft(viewport);
            return new Vector2(sliderX, musicPosition.Y + _MusicVolumeSlider.GetHeight() + SLIDER_SPACING);
        }

        private Vector2 GetControlsPosition(Viewport viewport)
        {
            var bloomPosition = GetBloomSliderPosition(viewport);
            var controlsX = GetControlsLeft(viewport);
            return new Vector2(controlsX, bloomPosition.Y + _BloomSlider.GetHeight() + SLIDER_SPACING);
        }

        private Vector2 GetDisplayPosition(Viewport viewport)
        {
            var controlsPosition = GetControlsPosition(viewport);
            var displayX = GetControlsLeft(viewport);
            return new Vector2(displayX, controlsPosition.Y + _ControlsToggle.GetHeight() + SLIDER_SPACING);
        }

        private Vector2 GetPacShapePosition(Viewport viewport)
        {
            var displayPosition = GetDisplayPosition(viewport);
            return new Vector2(GetControlsLeft(viewport), displayPosition.Y + _DisplayToggle.GetHeight() + SLIDER_SPACING);
        }

        /// <summary>
        /// Draws the picked pac shape chomping, one line tall, to the right of the pac toggle's value column.
        /// </summary>
        private void DrawPacShapePreview(SpriteBatch spriteBatch, Viewport viewport)
        {
            var position = GetPacShapePosition(viewport);
            var size = (int)_PacShapeToggle.GetHeight();
            var left = (int)(position.X + _PacShapeToggle.GetWidth() + PAC_PREVIEW_GAP);
            var source = MultiPlayerGameScreen.GetPacSource(_PacSheet, PacShapes.ALL[_PacShapeToggle.SelectedIndex], MultiPlayerGameScreen.GetChompFrame(_AnimationTime));
            spriteBatch.Draw(_PacSheet, new Rectangle(left, (int)position.Y, size, size), source, ClientColors.COLOR_PAC_YELLOW);
        }

        private Vector2 GetBrightnessSliderPosition(Viewport viewport)
        {
            var soundPosition = GetSoundSliderPosition(viewport);
            var sliderX = GetControlsLeft(viewport);
            return new Vector2(sliderX, soundPosition.Y + _SoundVolumeSlider.GetHeight() + SLIDER_SPACING);
        }

        private Vector2 GetBloomSliderPosition(Viewport viewport)
        {
            var brightnessPosition = GetBrightnessSliderPosition(viewport);
            var sliderX = GetControlsLeft(viewport);
            return new Vector2(sliderX, brightnessPosition.Y + _BrightnessSlider.GetHeight() + SLIDER_SPACING);
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            // point sampled so the scaled up pac preview stays crisp; text is never scaled
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            var headingSize = _TitleFont.MeasureString(Const.STRING_GAME_SETTINGS);
            var headingPosition = new Vector2((viewport.Width - headingSize.X) / 2f, viewport.Height * 0.2f);
            TextHelper.DrawShadowedString(spriteBatch, _TitleFont, Const.STRING_GAME_SETTINGS, headingPosition, ClientColors.COLOR_DARK_TITLE_ITEM, ClientColors.COLOR_DARK_TITLE_SHADOW);

            _MusicVolumeSlider.Draw(spriteBatch, GetMusicSliderPosition(viewport), _SelectedItem == SettingsItem.MusicVolume);
            _SoundVolumeSlider.Draw(spriteBatch, GetSoundSliderPosition(viewport), _SelectedItem == SettingsItem.SoundVolume);
            _ControlsToggle.Draw(spriteBatch, GetControlsPosition(viewport), _SelectedItem == SettingsItem.Controls);
            _DisplayToggle.Draw(spriteBatch, GetDisplayPosition(viewport), _SelectedItem == SettingsItem.Display);
            _PacShapeToggle.Draw(spriteBatch, GetPacShapePosition(viewport), _SelectedItem == SettingsItem.PacShape);
            DrawPacShapePreview(spriteBatch, viewport);
            _BrightnessSlider.Draw(spriteBatch, GetBrightnessSliderPosition(viewport), _SelectedItem == SettingsItem.Brightness);
            _BloomSlider.Draw(spriteBatch, GetBloomSliderPosition(viewport), _SelectedItem == SettingsItem.Bloom);

            var backBounds = GetBackBounds(viewport);
            var backColor = (_SelectedItem == SettingsItem.Back) ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;
            TextHelper.DrawShadowedString(spriteBatch, _Font, Const.STRING_BACK, new Vector2(backBounds.X, backBounds.Y), backColor);

            spriteBatch.End();
        }
    }
}
