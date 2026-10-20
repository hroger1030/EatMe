using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;

namespace EatMeClient
{
    public class TitleScreen : Game
    {
        private const float MENU_OPTION_SPACING = 1.25f;
        private const float EXIT_DELAY_SECONDS = 1.5f;
        private const float LOGO_WIDTH_RATIO = 0.5f;
        private const float TITLE_CENTER_Y_RATIO = 1f / 4f;
        private const float DISPLAY_SETTLE_SECONDS = 0.25f;

        /// <summary>
        /// Times the bloom glow is blurred each way; more makes it wider and softer.
        /// </summary>
        private const int BLOOM_BLUR_PASSES = 2;

        /// <summary>
        /// Adds the glow straight onto the frame. BlendState.Additive would also multiply by the tint's alpha,
        /// applying the bloom strength twice.
        /// </summary>
        private static readonly BlendState BLEND_ADD_GLOW = new()
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private static readonly string[] MENU_OPTIONS = [Const.STRING_JOIN_GAME, Const.STRING_STORY, Const.STRING_GAME_SETTINGS, Const.STRING_HIGH_SCORES, Const.STRING_QUIT];

        private GraphicsDeviceManager _Graphics;

        // Screens
        private SettingsScreen _GameSettingsScreen;
        private StoryScreen _StoryScreen;
        private HighScoresScreen _HighScoresScreen;
        private MultiPlayerGameScreen _GameScreen;
        private JoinGameScreen _JoinGameScreen;
        private MessageScroller _MessageScroller;
        private VersionLabel _VersionLabel;

        private SpriteBatch _SpriteBatch;
        private SpriteFont _MenuFont;
        private Texture2D _Background;
        private Texture2D _Logo;
        private Effect _Brightness;
        private Effect _Bloom;
        private RenderTarget2D _SceneTarget;
        private RenderTarget2D _BloomTarget;
        private RenderTarget2D _BloomBlurTarget;
        private eScreenState _CurrentScreen = eScreenState.TitleMenu;
        private int _SelectedMenuIndex;
        private float? _ExitTimeRemaining;
        private float _DisplayStableSeconds;

        public TitleScreen()
        {
            _Graphics = new GraphicsDeviceManager(this);

            // Program.cs has loaded the settings, so the window is created in the saved mode
            SetDisplayMode();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = Const.GAME_TITLE;
            Window.TextInput += (_, e) => InputManager.AddTypedCharacter(e.Character);
            Window.ClientSizeChanged += (_, _) => FollowWindowSize();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _SpriteBatch = new SpriteBatch(GraphicsDevice);

            MusicManager.Initialize();
            SoundManager.Initialize();

            ConfigManager.ApplyVolumes();
            HighScoreStore.Load();
            AssetCache.Initialize(Content);

            LoadAllAssets();

            _MenuFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC60);
            _Background = AssetCache.GetTexture(Const.IMAGE_TITLE_BACKGROUND);
            _Logo = AssetCache.GetTexture(Const.IMAGE_LOGO);
            _Brightness = AssetCache.GetEffect(Const.EFFECT_BRIGHTNESS);
            _Bloom = AssetCache.GetEffect(Const.EFFECT_BLOOM);
            _Bloom.Parameters["Threshold"].SetValue(DisplayAdjust.BLOOM_THRESHOLD);
            _Bloom.Parameters["Weights"].SetValue(DisplayAdjust.GetBlurWeights());

            _GameSettingsScreen = new SettingsScreen(ReturnToTitleMenu);
            _StoryScreen = new StoryScreen(ReturnToTitleMenu);
            _HighScoresScreen = new HighScoresScreen(ReturnToTitleMenu);
            _GameScreen = new MultiPlayerGameScreen(ReturnFromGame);
            _JoinGameScreen = new JoinGameScreen(StartGame, ReturnToTitleMenu);

            _GameSettingsScreen.LoadContent();
            _StoryScreen.LoadContent();
            _HighScoresScreen.LoadContent();
            _GameScreen.LoadContent();
            _JoinGameScreen.LoadContent();

            _MessageScroller = new MessageScroller();
            _MessageScroller.LoadContent();
            _MessageScroller.SetMessage(Const.MOTD_DEFAULT);

            _VersionLabel = new VersionLabel();
            _VersionLabel.LoadContent();

            ActivateTitleMenu();
        }

        /// <summary>
        /// Loads every asset the game uses into AssetCache, all in one place, up front.
        /// </summary>
        private void LoadAllAssets()
        {
            // font
            AssetCache.LoadFonts();

            // images
            AssetCache.LoadTextures([
                Const.IMAGE_TITLE_BACKGROUND, Const.IMAGE_LOGO, Const.IMAGE_WIN_BLUE, Const.IMAGE_WIN_RED, Const.IMAGE_DOT, Const.IMAGE_POWER_PILL,
                Const.IMAGE_FLAG0, Const.IMAGE_FLAG_BASE, Const.IMAGE_PAC_SHEET, Const.IMAGE_PAC_DEATH_SHEET,
                Const.IMAGE_GHOST, Const.IMAGE_PAC_BASE, Const.IMAGE_GHOST_BASE, Const.IMAGE_POWER_UP_SHEET, Const.IMAGE_WEAPON_EFFECTS,
                Const.IMAGE_WALL, Const.IMAGE_WALL_FILL, Const.IMAGE_SPACE_FILL, Const.IMAGE_POINTER]);

            // music
            var trackFolder = Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, MusicPlaylist.TRACK_FOLDER);
            MusicManager.LevelTracks = Directory.Exists(trackFolder) ? MusicPlaylist.GetTrackNames(Directory.GetFiles(trackFolder, "*.xnb")) : [];
            AssetCache.LoadSongs([Const.SONG_TITLE_THEME, .. MusicManager.LevelTracks]);

            // sound effects
            AssetCache.LoadSfx([
                Const.SFX_TICK, Const.SFX_EAT, Const.SFX_SIREN, Const.SFX_DEATH,
                Const.SFX_EAT_GHOST, Const.SFX_FLAG_CAP, Const.SFX_LEVEL_LOST, Const.SFX_LEVEL_WON, Const.SFX_POWER_PILL, Const.SFX_POWER_UP_EATEN, Const.SFX_POWER_UP, Const.SFX_POW, Const.SFX_ANGELIC_CHORUS, Const.SFX_BLOOP, Const.SFX_BFG_BURST,
                Const.SFX_MSG_RED_FLAG_DROPPED, Const.SFX_MSG_BLUE_FLAG_DROPPED, Const.SFX_MSG_RED_FLAG_RETURNED, Const.SFX_MSG_BLUE_FLAG_RETURNED,
                Const.SFX_MSG_RED_FLAG_CAPTURED, Const.SFX_MSG_BLUE_FLAG_CAPTURED, Const.SFX_MSG_RED_ABOUT_TO_WIN, Const.SFX_MSG_BLUE_ABOUT_TO_WIN]);

            // text
            AssetCache.LoadTexts([Const.TEXT_STORY]);

            // shaders
            AssetCache.LoadEffects([Const.EFFECT_BRIGHTNESS, Const.EFFECT_PIXEL_ART, Const.EFFECT_BLOOM]);
        }

        /// <summary>
        /// Leaves the menus and connects to a server.
        /// </summary>
        private void StartGame(string host, int port)
        {
            _CurrentScreen = eScreenState.Game;
            MusicManager.Stop();
            IsMouseVisible = ConfigManager.Settings.ControllerType == eControllerType.MouseAndKeyboard;
            _GameScreen.Activate(GraphicsDevice.Viewport, host, port);
        }

        /// <summary>
        /// Back from a game: the title scroller shows the server's message of the day, if it sent one.
        /// </summary>
        private void ReturnFromGame()
        {
            if (!string.IsNullOrEmpty(_GameScreen.MessageOfTheDay))
                _MessageScroller.SetMessage(_GameScreen.MessageOfTheDay);

            ReturnToTitleMenu();
        }

        private void ReturnToTitleMenu()
        {
            _CurrentScreen = eScreenState.TitleMenu;
            IsMouseVisible = true;
            ActivateTitleMenu();
        }

        private void ActivateTitleMenu()
        {
            _SelectedMenuIndex = 0;

            // Loops until a game starts; Play is a no-op if the theme is already playing
            MusicManager.Play(AssetCache.GetSong(Const.SONG_TITLE_THEME));
        }

        protected override void Update(GameTime gameTime)
        {
            InputManager.Update();

            // the display mode can be changed from either settings screen, and takes effect straight away
            if (ConfigManager.Settings.FullScreen != _Graphics.IsFullScreen)
                ApplyDisplayMode();

            // Restart the settle timer whenever the window isn't at full screen size yet
            if (IsDisplaySettled(Window.ClientBounds, _Graphics.PreferredBackBufferWidth, _Graphics.PreferredBackBufferHeight))
                _DisplayStableSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
            else
                _DisplayStableSeconds = 0f;

            if (_ExitTimeRemaining.HasValue)
            {
                _ExitTimeRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_ExitTimeRemaining <= 0f)
                    Exit();

                base.Update(gameTime);
                return;
            }

            if (_CurrentScreen == eScreenState.GameSettings)
            {
                _GameSettingsScreen.Update(gameTime, GraphicsDevice.Viewport);
                base.Update(gameTime);
                return;
            }

            if (_CurrentScreen == eScreenState.Game)
            {
                _GameScreen.Update(gameTime, GraphicsDevice.Viewport);

                // the controls can be changed from the in-game settings
                IsMouseVisible = ConfigManager.Settings.ControllerType == eControllerType.MouseAndKeyboard;
                base.Update(gameTime);
                return;
            }

            if (_CurrentScreen == eScreenState.JoinGame)
            {
                _JoinGameScreen.Update(gameTime, GraphicsDevice.Viewport);
                base.Update(gameTime);
                return;
            }

            if (_CurrentScreen == eScreenState.Story)
            {
                _StoryScreen.Update(gameTime, GraphicsDevice.Viewport);
                base.Update(gameTime);
                return;
            }

            if (_CurrentScreen == eScreenState.HighScores)
            {
                _HighScoresScreen.Update(gameTime, GraphicsDevice.Viewport);
                base.Update(gameTime);
                return;
            }

            if (InputManager.GamePadButtonPressed(Buttons.Back) || InputManager.KeyPressed(Keys.Escape))
                RequestExit();

            if (InputManager.KeyPressed(Keys.Down) ||
                InputManager.KeyPressed(Keys.S) ||
                InputManager.GamePadButtonPressed(Buttons.DPadDown) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickDown))
            {
                _SelectedMenuIndex = (_SelectedMenuIndex + 1) % MENU_OPTIONS.Length;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (InputManager.KeyPressed(Keys.Up) ||
                InputManager.KeyPressed(Keys.W) ||
                InputManager.GamePadButtonPressed(Buttons.DPadUp) ||
                InputManager.GamePadButtonPressed(Buttons.LeftThumbstickUp))
            {
                _SelectedMenuIndex = (_SelectedMenuIndex - 1 + MENU_OPTIONS.Length) % MENU_OPTIONS.Length;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (InputManager.KeyPressed(Keys.Enter) ||
                InputManager.KeyPressed(Keys.Space) ||
                InputManager.GamePadButtonPressed(Buttons.A))
                SelectMenuOption(_SelectedMenuIndex);

            UpdateMouseSelection();
            _MessageScroller.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Switches to full screen at the desktop's resolution, or to a window DisplayAdjust.GetWindowedSize big,
        /// as GameSettings.FullScreen says.
        /// </summary>
        private void ApplyDisplayMode()
        {
            SetDisplayMode();
            _Graphics.ApplyChanges();

            // SDL ignores a resizable change on a full screen window, so set it again now the window has left full screen
            Window.AllowUserResizing = !_Graphics.IsFullScreen;
        }

        /// <summary>
        /// Sets the size and mode ApplyDisplayMode applies, without applying them, so the constructor can use it before
        /// the graphics device exists.
        /// </summary>
        private void SetDisplayMode()
        {
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var fullScreen = ConfigManager.Settings.FullScreen;
            var (width, height) = fullScreen ? (displayMode.Width, displayMode.Height) : DisplayAdjust.GetWindowedSize(displayMode.Width, displayMode.Height);

            _Graphics.PreferredBackBufferWidth = width;
            _Graphics.PreferredBackBufferHeight = height;
            _Graphics.IsFullScreen = fullScreen;

            // a window can be maximized or dragged to any size; FollowWindowSize keeps the back buffer in step
            Window.AllowUserResizing = !fullScreen;
        }

        /// <summary>
        /// Resizes the back buffer to the window after it is maximized, restored or dragged, in windowed mode only.
        /// A minimized window (no size) is left alone.
        /// </summary>
        private void FollowWindowSize()
        {
            var bounds = Window.ClientBounds;

            if (_Graphics.IsFullScreen || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            if (bounds.Width == _Graphics.PreferredBackBufferWidth && bounds.Height == _Graphics.PreferredBackBufferHeight)
                return;

            _Graphics.PreferredBackBufferWidth = bounds.Width;
            _Graphics.PreferredBackBufferHeight = bounds.Height;
            _Graphics.ApplyChanges();
        }

        /// <summary>
        /// True once the window has reached the size it was asked for.
        /// </summary>
        public static bool IsDisplaySettled(Rectangle clientBounds, int width, int height)
        {
            return clientBounds.Width == width && clientBounds.Height == height;
        }

        private void RequestExit()
        {
            _ExitTimeRemaining = EXIT_DELAY_SECONDS;
        }

        private void UpdateMouseSelection()
        {
            var mouseState = InputManager.MouseState;
            var mousePoint = new Point(mouseState.X, mouseState.Y);
            var menuStartY = GetMenuStartY(GraphicsDevice.Viewport);
            var optionBounds = GetMenuOptionBounds(GraphicsDevice.Viewport, menuStartY);

            for (var i = 0; i < optionBounds.Length; i++)
            {
                if (!optionBounds[i].Contains(mousePoint))
                    continue;

                var clicked = InputManager.MouseButtonPressed(InputManager.MouseButton.Left);

                // Only follow the cursor when it moves, so a resting cursor doesn't undo keyboard selection
                if (clicked || InputManager.MouseMoved())
                    _SelectedMenuIndex = i;

                if (clicked)
                {
                    SoundManager.PlaySound(Const.SFX_TICK);
                    SelectMenuOption(i);
                }

                break;
            }
        }

        /// <summary>
        /// Sets up logo position and size: half the screen wide, centred a quarter of the way down
        /// </summary>
        private (Vector2 Position, Vector2 Size) GetTitleLayout(Viewport viewport)
        {
            var logoWidth = viewport.Width * LOGO_WIDTH_RATIO;
            var titleSize = new Vector2(logoWidth, logoWidth * _Logo.Height / _Logo.Width);
            var titlePosition = new Vector2((viewport.Width - titleSize.X) / 2f, viewport.Height * TITLE_CENTER_Y_RATIO - titleSize.Y / 2f);
            return (titlePosition, titleSize);
        }

        /// <summary>
        /// Scales an image to fill the area without stretching, centred, cropping whatever overflows.
        /// </summary>
        public static Rectangle GetCoverRectangle(int imageWidth, int imageHeight, Rectangle area)
        {
            var scale = Math.Max((float)area.Width / imageWidth, (float)area.Height / imageHeight);
            var width = (int)Math.Ceiling(imageWidth * scale);
            var height = (int)Math.Ceiling(imageHeight * scale);
            return new Rectangle(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);
        }

        private float GetMenuStartY(Viewport viewport)
        {
            var (titlePosition, titleSize) = GetTitleLayout(viewport);
            return titlePosition.Y + titleSize.Y + _MenuFont.LineSpacing * (MENU_OPTION_SPACING - 1f);
        }

        private Rectangle[] GetMenuOptionBounds(Viewport viewport, float menuStartY)
        {
            var bounds = new Rectangle[MENU_OPTIONS.Length];
            var menuY = menuStartY;

            for (var i = 0; i < MENU_OPTIONS.Length; i++)
            {
                var optionSize = _MenuFont.MeasureString(MENU_OPTIONS[i]);
                var optionPosition = new Vector2((viewport.Width - optionSize.X) / 2f, menuY);
                bounds[i] = new Rectangle((int)optionPosition.X, (int)optionPosition.Y, (int)optionSize.X, (int)optionSize.Y);
                menuY += _MenuFont.LineSpacing * MENU_OPTION_SPACING;
            }

            return bounds;
        }

        private void SelectMenuOption(int menuIndex)
        {
            switch (MENU_OPTIONS[menuIndex])
            {
                case Const.STRING_JOIN_GAME:
                    _CurrentScreen = eScreenState.JoinGame;
                    _JoinGameScreen.Activate();
                    break;

                case Const.STRING_STORY:
                    _CurrentScreen = eScreenState.Story;
                    _StoryScreen.Activate(GraphicsDevice.Viewport);
                    break;

                case Const.STRING_HIGH_SCORES:
                    _CurrentScreen = eScreenState.HighScores;
                    _HighScoresScreen.Scores = HighScoreStore.Scores;
                    _HighScoresScreen.Activate();
                    break;

                case Const.STRING_GAME_SETTINGS:
                    _CurrentScreen = eScreenState.GameSettings;
                    _GameSettingsScreen.Activate();
                    break;

                case Const.STRING_QUIT:
                    RequestExit();
                    break;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Draw nothing but black until the switch to full screen has finished
            if (_DisplayStableSeconds < DISPLAY_SETTLE_SECONDS)
            {
                base.Draw(gameTime);
                return;
            }

            // Every screen is drawn into the scene target, the bloom glow is made from it, then both are copied to the
            // screen through the Brightness shader
            var sceneTarget = GetSceneTarget();
            GraphicsDevice.SetRenderTarget(sceneTarget);
            GraphicsDevice.Clear(Color.Black);
            DrawScreens(GraphicsDevice.Viewport);

            var bloomIntensity = DisplayAdjust.GetBloomIntensity(ConfigManager.Settings.Bloom);
            var bloomTarget = bloomIntensity > 0f ? DrawBloom(sceneTarget) : null;

            GraphicsDevice.SetRenderTarget(null);

            DrawWithBrightness(sceneTarget, bloomTarget, bloomIntensity);

            base.Draw(gameTime);
        }

        /// <summary>
        /// The off screen target the frame is drawn into, remade whenever the back buffer changes size.
        /// </summary>
        private RenderTarget2D GetSceneTarget()
        {
            var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height = GraphicsDevice.PresentationParameters.BackBufferHeight;

            if (_SceneTarget == null || _SceneTarget.Width != width || _SceneTarget.Height != height)
            {
                _SceneTarget?.Dispose();
                _SceneTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            }

            return _SceneTarget;
        }

        /// <summary>
        /// Makes the bloom glow from the finished frame: the bright parts, at half size, blurred twice each way.
        /// Returns the target holding it.
        /// </summary>
        private RenderTarget2D DrawBloom(RenderTarget2D sceneTarget)
        {
            var width = Math.Max(1, sceneTarget.Width / 2);
            var height = Math.Max(1, sceneTarget.Height / 2);

            if (_BloomTarget == null || _BloomTarget.Width != width || _BloomTarget.Height != height)
            {
                _BloomTarget?.Dispose();
                _BloomBlurTarget?.Dispose();
                _BloomTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                _BloomBlurTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            }

            _Bloom.CurrentTechnique = _Bloom.Techniques["BrightPass"];
            DrawBloomPass(sceneTarget, _BloomTarget);

            _Bloom.CurrentTechnique = _Bloom.Techniques["Blur"];
            for (var i = 0; i < BLOOM_BLUR_PASSES; i++)
            {
                _Bloom.Parameters["TexelStep"].SetValue(new Vector2(1f / width, 0f));
                DrawBloomPass(_BloomTarget, _BloomBlurTarget);

                _Bloom.Parameters["TexelStep"].SetValue(new Vector2(0f, 1f / height));
                DrawBloomPass(_BloomBlurTarget, _BloomTarget);
            }

            return _BloomTarget;
        }

        /// <summary>
        /// Draws one texture over the whole of another target through the Bloom shader's current technique.
        /// </summary>
        private void DrawBloomPass(Texture2D source, RenderTarget2D destination)
        {
            GraphicsDevice.SetRenderTarget(destination);
            _SpriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.LinearClamp, effect: _Bloom);
            _SpriteBatch.Draw(source, destination.Bounds, Color.White);
            _SpriteBatch.End();
        }

        /// <summary>
        /// Copies the finished frame to the screen pixel for pixel, adds any bloom glow over it at bloomIntensity,
        /// all at the player's brightness.
        /// </summary>
        private void DrawWithBrightness(RenderTarget2D sceneTarget, RenderTarget2D bloomTarget, float bloomIntensity)
        {
            _Brightness.Parameters["Brightness"].SetValue(DisplayAdjust.GetMultiplier(ConfigManager.Settings.Brightness));

            _SpriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp, effect: _Brightness);
            _SpriteBatch.Draw(sceneTarget, GraphicsDevice.Viewport.Bounds, Color.White);
            _SpriteBatch.End();

            if (bloomTarget == null)
                return;

            // the glow is scaled up from half size, so sampled linearly; the tint carries its strength
            _SpriteBatch.Begin(blendState: BLEND_ADD_GLOW, samplerState: SamplerState.LinearClamp, effect: _Brightness);
            _SpriteBatch.Draw(bloomTarget, GraphicsDevice.Viewport.Bounds, Color.White * bloomIntensity);
            _SpriteBatch.End();
        }

        /// <summary>
        /// Draws the active screen, and the title background behind it unless it's the game.
        /// </summary>
        private void DrawScreens(Viewport viewport)
        {
            // Background behind every screen except the game, point sampled so the pixel art stays crisp
            if (_CurrentScreen != eScreenState.Game)
            {
                _SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _SpriteBatch.Draw(_Background, GetCoverRectangle(_Background.Width, _Background.Height, viewport.Bounds), Color.White);
                _SpriteBatch.End();

                // client version in the top right corner of every menu screen
                _SpriteBatch.Begin();
                _VersionLabel.Draw(_SpriteBatch, viewport);
                _SpriteBatch.End();
            }

            if (_CurrentScreen == eScreenState.GameSettings)
            {
                _GameSettingsScreen.Draw(_SpriteBatch, viewport);
                return;
            }

            if (_CurrentScreen == eScreenState.Game)
            {
                _GameScreen.Draw(_SpriteBatch, viewport);
                return;
            }

            if (_CurrentScreen == eScreenState.JoinGame)
            {
                _JoinGameScreen.Draw(_SpriteBatch, viewport);
                return;
            }

            if (_CurrentScreen == eScreenState.Story)
            {
                _StoryScreen.Draw(_SpriteBatch, viewport);
                return;
            }

            if (_CurrentScreen == eScreenState.HighScores)
            {
                _HighScoresScreen.Draw(_SpriteBatch, viewport);
                return;
            }

            // Logo in place of the title text, point sampled so the pixel art stays crisp
            _SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            var (titlePosition, titleSize) = GetTitleLayout(viewport);
            _SpriteBatch.Draw(_Logo, new Rectangle((int)titlePosition.X, (int)titlePosition.Y, (int)titleSize.X, (int)titleSize.Y), Color.White);
            _SpriteBatch.End();

            _SpriteBatch.Begin();

            // add in the menu options
            var menuStartY = GetMenuStartY(viewport);
            var optionBounds = GetMenuOptionBounds(viewport, menuStartY);

            for (var i = 0; i < MENU_OPTIONS.Length; i++)
            {
                var optionColor = i == _SelectedMenuIndex ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;
                var optionPosition = new Vector2(optionBounds[i].X, optionBounds[i].Y);
                _SpriteBatch.DrawString(_MenuFont, MENU_OPTIONS[i], optionPosition, optionColor);
            }

            // message scroller along the bottom
            _MessageScroller.Draw(_SpriteBatch, viewport);

            _SpriteBatch.End();
        }
    }
}