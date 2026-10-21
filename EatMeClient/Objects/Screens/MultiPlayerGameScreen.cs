using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EatMeClient
{
    /// <summary>
    /// The networked game: connects to a server, sends the player's input and draws what the server reports.
    /// </summary>
    public class MultiPlayerGameScreen
    {
        /// <summary>
        /// Screen pixels per map cell.
        /// </summary>
        public const int CELL_PIXELS = 48;

        /// <summary>
        /// The power pill's cell on the power up sheet, used for the map's power pills too (tinted the dot color).
        /// </summary>
        private static readonly Rectangle POWER_PILL_SOURCE = new Rectangle(PowerUpSprites.GetCellX(ePowerUp.PowerPill), PowerUpSprites.GetCellY(ePowerUp.PowerPill),
            PowerUpSprites.CELL_PIXELS, PowerUpSprites.CELL_PIXELS);

        private const float PING_INTERVAL_SECONDS = 2f;
        private const float PAC_FRAME_SECONDS = 0.08f;

        // space between the respawn countdown's text and the edge of its panel
        private const float RESPAWN_PANEL_PADDING = 24f;
        private const int PAC_FRAME_COUNT = 4;
        private const float HUD_MARGIN = 20f;

        /// <summary>
        /// Space above and below the score text in the bar across the top of the game.
        /// </summary>
        private const int HUD_BAR_PADDING = 8;

        /// <summary>
        /// The mini-map is a square this share of the smaller screen side.
        /// </summary>
        private const float MINI_MAP_SCREEN_FRACTION = 0.2f;

        /// <summary>
        /// Power pills blink: shown for this long, then hidden for this long.
        /// </summary>
        private const float POWER_PILL_BLINK_SECONDS = 0.5f;

        /// <summary>
        /// Flag bases blink between their team's color and yellow, showing each for this long.
        /// </summary>
        private const float FLAG_BASE_BLINK_SECONDS = 0.5f;

        private const int GHOST_FRAME_COUNT = 4;

        // rainbow ghost sheets, each moved round the gradient a little further, so the rainbow scrolls as ghosts move
        private const int RAINBOW_GHOST_PHASES = 16;

        /// <summary>
        /// The results screen's title and countdown are the UI font drawn this many times bigger.
        /// Whole numbers keep the pixel font crisp.
        /// </summary>
        private const float RESULTS_TITLE_SCALE = 2f;
        private const float RESULTS_COUNTDOWN_SCALE = 2f;

        /// <summary>
        /// Gap between the results screen's sections, between columns of player names, and between a name and its score.
        /// </summary>
        private const float RESULTS_GAP = 24f;
        private const int RESULTS_PANEL_RADIUS = 12;

        /// <summary>
        /// The results screen's countdown and tip sit straight on the picture, so each gets a black drop shadow
        /// this many pixels down and to the right to stay readable.
        /// </summary>

        /// <summary>
        /// A carried flag is drawn this fraction of its carrier's width, over the carrier's shoulder.
        /// </summary>
        private const float CARRIED_FLAG_SCALE = 0.75f;

        /// <summary>
        /// A carried flag's centre sits this fraction of the carrier's radius above and behind the carrier's centre
        /// (behind is 3/4 of the way back from the front), measured on the sprite facing right, so it looks carried
        /// over the shoulder. The point turns with the pac.
        /// </summary>
        private const float CARRIED_FLAG_OFFSET = 0.5f;

        /// <summary>
        /// Gap between the bottom of a pac and the top of its player's name.
        /// </summary>
        private const float NAME_OFFSET_PIXELS = 2f;

        /// <summary>
        /// Flag pointers, drawn on a circle around the player for every flag, are this many pixels across.
        /// </summary>
        private const int FLAG_POINTER_PIXELS = 96;

        private readonly Action _OnQuit;

        // Assets
        private SpriteFont _Font;
        private SpriteFont _NameFont;
        private Texture2D _Pixel;
        private Texture2D _DotTexture;
        private Texture2D _PacBaseTexture;
        private eGridDef[,] _FloorBoxesMap;
        private List<CellBox> _GhostPenBoxes = new List<CellBox>();
        private List<CellBox> _RedSpawnBoxes = new List<CellBox>();
        private List<CellBox> _BlueSpawnBoxes = new List<CellBox>();
        private Texture2D _FlagBaseTexture;
        private Texture2D _WinBlueTexture;
        private Texture2D _WinRedTexture;
        private Texture2D _FlagTexture;

        // the Triforce's force field, built from ForceField.GetAlpha (white, tinted gold when drawn)
        private Texture2D _ForceFieldTexture;
        private const int FORCE_FIELD_TEXTURE_PIXELS = 96;
        private Texture2D _PacSheet;
        private Texture2D _PowerUpSheet;

        // the oval shadow under each power up, built from PowerUpRules.GetShadowAlpha (black)
        private Texture2D _PowerUpShadowTexture;
        private const int POWER_UP_SHADOW_TEXTURE_WIDTH = 64;
        private const int POWER_UP_SHADOW_TEXTURE_HEIGHT = 24;
        private Texture2D _WeaponEffectSheet;
        private Texture2D _PacDeathSheet;
        private Texture2D[] _GhostSheets;
        private Texture2D[] _RainbowGhostSheets;
        private Texture2D _GhostEyesSheet;

        /// <summary>
        /// Every draw layer, bottom to top, in the order Draw draws them.
        /// </summary>
        private static readonly eDrawLayer[] DRAW_LAYERS = Enum.GetValues<eDrawLayer>();
        private Texture2D _WallTexture;
        private Texture2D _WallFillTexture;
        private Texture2D _PointerTexture;
        private Texture2D _Background;
        private PixelArtEffect _PixelArt;
        private SoundEffectInstance _EatLoop;
        private SoundEffectInstance _SirenLoop;
        private SoundEffectInstance _PowerPillLoop;
        private SoundEffectInstance _SpawnChargeLoop;

        /// <summary>
        /// The power up sound, a global sound: one loop at full volume while any power up is in the level, however many.
        /// </summary>
        private SoundEffectInstance _PowerUpLoop;

        /// <summary>
        /// The one global sound playing (announcer clip, capture sting or level end fanfare), so they never overlap.
        /// </summary>
        private SoundEffectInstance _Announcer;
        private readonly AnnouncementQueue _Announcements = new();

        // Controls
        private InGameMenu _InGameMenu;

        /// <summary>
        /// The settings screen opened from the in-game menu; the game stays connected, paused and hidden behind it.
        /// </summary>
        private SettingsScreen _SettingsScreen;
        private bool _SettingsOpen;

        /// <summary>
        /// No music starts while paused, so this remembers that the next level's track is owed when play resumes.
        /// </summary>
        private bool _LevelStartedWhilePaused;
        private readonly MiniMap _MiniMap = new();
        private PlayerState _PlayerState;
        private PlayerList _PlayerList;

        /// <summary>
        /// True while TAB or a gamepad shoulder button is held, showing the player list.
        /// </summary>
        private bool _PlayerListHeld;

        // Game Elements
        private Camera _Camera = new();

        /// <summary>
        /// Size in pixels of the board's viewport last frame, for how loud in-world sounds play.
        /// </summary>
        private Vector2 _BoardSize;
        private readonly PacTrails _Trails = new();
        private NetworkClient _Client;
        private ClientGameState _State;

        /// <summary>
        /// The message of the day from the last server joined, empty if none has sent one yet.
        /// </summary>
        public string MessageOfTheDay => _State?.MessageOfTheDay ?? string.Empty;

        /// <summary>
        /// Snapshots received this game, for the perf overlay.
        /// </summary>
        public long SnapshotsReceived => _State?.SnapshotsReceived ?? 0;

        /// <summary>
        /// The stats bar's ping text, for the perf overlay.
        /// </summary>
        public string PingText => _State?.GetPingText() ?? string.Empty;

        /// <summary>
        /// Height of the score bar across the top, so the perf overlay can sit under it.
        /// </summary>
        public int HudBarHeight => GetHudBarHeight();
        private readonly Stopwatch _Clock = Stopwatch.StartNew();
        private float _PingTimer;
        private float _AnimationTime;

        /// <summary>
        /// The board's shake (ShakeScreen), added to the camera every frame.
        /// </summary>
        private readonly ScreenShake _Shake = new();
        private eDirection _LastDirection;
        private uint _InputSequence;

        public List<AnimatedTexture> DeathAnimations { get; set; }

        /// <summary>
        /// Pieces of smashed walls flying off the board (WallDebris), taken from ClientGameState.NewDebris.
        /// </summary>
        private readonly List<WallDebrisPiece> _Debris = [];

        public MultiPlayerGameScreen(Action onQuit)
        {
            _OnQuit = onQuit;
        }

        public void LoadContent()
        {
            _Font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC48);
            _NameFont = AssetCache.GetFont(Const.FONT_ARIAL20);
            _PlayerState = new PlayerState(AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC30),
                AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC48));
            _PlayerList = new PlayerList(AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC30));
            _DotTexture = AssetCache.GetTexture(Const.IMAGE_DOT);
            _PacBaseTexture = AssetCache.GetTexture(Const.IMAGE_PAC_BASE);
            _FlagBaseTexture = AssetCache.GetTexture(Const.IMAGE_FLAG_BASE);
            _WinBlueTexture = AssetCache.GetTexture(Const.IMAGE_WIN_BLUE);
            _WinRedTexture = AssetCache.GetTexture(Const.IMAGE_WIN_RED);
            _FlagTexture = AssetCache.GetTexture(Const.IMAGE_FLAG0);
            _PacSheet = AssetCache.GetTexture(Const.IMAGE_PAC_SHEET);
            _PacDeathSheet = AssetCache.GetTexture(Const.IMAGE_PAC_DEATH_SHEET);
            _PowerUpSheet = AssetCache.GetTexture(Const.IMAGE_POWER_UP_SHEET);
            _WeaponEffectSheet = AssetCache.GetTexture(Const.IMAGE_WEAPON_EFFECTS);

            _WallTexture = AssetCache.GetTexture(Const.IMAGE_WALL);
            _WallFillTexture = AssetCache.GetTexture(Const.IMAGE_WALL_FILL);
            _PointerTexture = AssetCache.GetTexture(Const.IMAGE_POINTER);
            _Background = AssetCache.GetTexture(Const.IMAGE_TITLE_BACKGROUND);
            _PixelArt = new PixelArtEffect(AssetCache.GetEffect(Const.EFFECT_PIXEL_ART));

            var whiteGhost = AssetCache.GetTexture(Const.IMAGE_GHOST);
            // one sheet per personality, indexed by GhostPersonality.Id
            _GhostSheets = new Texture2D[GhostPersonalities.ALL.Count];
            foreach (var personality in GhostPersonalities.ALL)
                _GhostSheets[personality.Id] = ImageFactory.GenerateGhostSheet(whiteGhost, ClientColors.ToColor(personality.Color));

            _GhostEyesSheet = ImageFactory.GenerateGhostEyesSheet(whiteGhost);

            _RainbowGhostSheets = new Texture2D[RAINBOW_GHOST_PHASES];
            for (var phase = 0; phase < RAINBOW_GHOST_PHASES; phase++)
                _RainbowGhostSheets[phase] = ImageFactory.GenerateRainbowGhostSheet(whiteGhost, GHOST_FRAME_COUNT, phase * (1f / RAINBOW_GHOST_PHASES));

            _EatLoop = AssetCache.GetSfx(Const.SFX_EAT).CreateInstance();
            _EatLoop.IsLooped = true;

            _SirenLoop = AssetCache.GetSfx(Const.SFX_SIREN).CreateInstance();
            _SirenLoop.IsLooped = true;

            _PowerPillLoop = AssetCache.GetSfx(Const.SFX_POWER_PILL).CreateInstance();
            _PowerPillLoop.IsLooped = true;

            _SpawnChargeLoop = AssetCache.GetSfx(Const.SFX_SPAWN_CHARGE).CreateInstance();
            _SpawnChargeLoop.IsLooped = true;

            _PowerUpLoop = AssetCache.GetSfx(Const.SFX_POWER_UP).CreateInstance();
            _PowerUpLoop.IsLooped = true;

            _InGameMenu = new InGameMenu(_Font, ResumeGame, OpenSettings, QuitToTitle);

            _SettingsScreen = new SettingsScreen(CloseSettings);
            _SettingsScreen.LoadContent();
        }

        /// <summary>
        /// Starts connecting to a server with the player's saved name and team.
        /// </summary>
        public void Activate(Viewport viewport, string host, int port)
        {
            _InGameMenu.Close();
            _SettingsOpen = false;
            _LevelStartedWhilePaused = false;

            DeathAnimations = new List<AnimatedTexture>();
            _Debris.Clear();
            _Trails.Clear();
            StopAnnouncements();

            _State = new ClientGameState
            {
                BlueDotColor = ClientColors.ToFastColor(ClientColors.COLOR_PAC_DOTS_BLUE),
                RedDotColor = ClientColors.ToFastColor(ClientColors.COLOR_PAC_DOTS_RED),
            };
            _Client = new NetworkClient();
            _PingTimer = 0f;
            _LastDirection = eDirection.None;
            _InputSequence = 0;

            var settings = ConfigManager.Settings;
            _Client.Connect(host, port, new HelloMessage { PlayerName = settings.PlayerName, PreferredTeam = settings.PreferredTeam });

            _Camera.Position = Vector2.Zero;
            _Camera.ShakeOffset = Vector2.Zero;
            _Shake.Stop();
        }

        /// <summary>
        /// Opens the in-game menu and silences the game: music, loops and announcements stop, and nothing new plays
        /// until it is resumed.
        /// </summary>
        private void PauseGame()
        {
            _InGameMenu.Open();
            MusicManager.Pause();
            StopLoopSounds();
            StopAnnouncements();
        }

        /// <summary>
        /// Action Delegate to hook into on resume after a Pause
        /// </summary>
        private void ResumeGame()
        {
            switch (MusicPlaylist.GetResumeMusic(_LevelStartedWhilePaused, _State.IsIntermission))
            {
                case eResumeMusic.Resume:
                    MusicManager.Resume();
                    break;

                case eResumeMusic.PlayLevelTrack:
                    MusicManager.PlayLevelTrack(_State.Level);
                    break;
            }

            _LevelStartedWhilePaused = false;
        }

        private void OpenSettings()
        {
            _SettingsOpen = true;
            _SettingsScreen.Activate();
        }

        /// <summary>
        /// Back from the settings screen goes back to the in-game menu, still paused.
        /// </summary>
        private void CloseSettings()
        {
            _SettingsOpen = false;
            _InGameMenu.Open();
        }

        private void QuitToTitle()
        {
            _Client?.Disconnect();
            _SettingsOpen = false;
            _LevelStartedWhilePaused = false;
            MusicManager.Stop();
            StopLoopSounds();
            StopAnnouncements();
            _OnQuit();
        }

        /// <summary>
        /// True when the game can't go on: the server refused us or the connection failed or dropped.
        /// </summary>
        private bool IsStopped => _State.RejectReason.HasValue || _Client.State is eConnectionState.Failed or eConnectionState.Disconnected;

        public void Update(GameTime gameTime, Viewport viewport)
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _AnimationTime += deltaTime;

            var board = GetBoardViewport(viewport);
            _BoardSize = new Vector2(board.Width, board.Height);

            // the server keeps going while the menu is open, so keep up with it
            ReceiveMessages();
            _Trails.Update(_State.Snapshot?.Pacs, deltaTime);
            UpdateDebris(deltaTime);
            _State.UpdateGhostEyes(deltaTime);

            // paused, the game is silent: only the menus' own sounds play
            if (ConfigManager.Settings.Paused)
            {
                _PlayerListHeld = false;
                StopLoopSounds();

                if (_SettingsOpen)
                    _SettingsScreen.Update(gameTime, viewport);
                else
                    _InGameMenu.Update(gameTime, viewport);

                return;
            }

            UpdateAnnouncements();

            if (IsStopped)
            {
                StopLoopSounds();
                _PlayerListHeld = false;

                if (InputManager.KeyPressed(Keys.Escape) || InputManager.GamePadButtonPressed(Buttons.Back) || InputManager.GamePadButtonPressed(Buttons.B))
                    QuitToTitle();

                return;
            }

            if (InputManager.KeyPressed(Keys.Escape) || InputManager.GamePadButtonPressed(Buttons.Back))
                PauseGame();

            SendInput();

            UpdatePlayerList();

            // the action button triggers whatever the player's power up effect does on action
            if (InputManager.KeyPressed(Keys.Space) || InputManager.GamePadButtonPressed(Buttons.A))
                _Client.Send(new ActionMessage());

            UpdateLoopSound(_EatLoop, !_State.IsIntermission && _State.IsOwnPacEating());
            UpdateLoopSound(_SirenLoop, !_State.IsIntermission && _State.IsOwnPacNotEating());
            UpdateLoopSound(_PowerPillLoop, !_State.IsIntermission && _State.IsOwnPacPowered());
            UpdateLoopSound(_SpawnChargeLoop, !_State.IsIntermission && _State.IsOwnPacSpawnFeeding());
            UpdatePowerUpLoop();

            _PingTimer += deltaTime;
            if (_PingTimer >= PING_INTERVAL_SECONDS)
            {
                _PingTimer = 0f;
                _Client.Send(new PingMessage { ClientTime = _Clock.ElapsedMilliseconds });
            }

            var ownPac = _State.GetOwnPac();
            if (ownPac.HasValue)
                _Camera.Position = CellToWorld(ownPac.Value.X, ownPac.Value.Y);

            _Shake.Update(deltaTime);
            var (shakeX, shakeY) = _Shake.GetOffset();
            _Camera.ShakeOffset = new Vector2(shakeX, shakeY);

            for (int i = DeathAnimations.Count - 1; i > -1; i--)
            {
                DeathAnimations[i].Update(deltaTime);

                if (DeathAnimations[i].IsComplete)
                    DeathAnimations.RemoveAt(i);
            }
        }

        /// <summary>
        /// Takes the pieces of newly smashed walls, moves every piece along its arc and drops those that have fallen off
        /// the bottom of the board.
        /// </summary>
        private void UpdateDebris(float deltaTime)
        {
            _Debris.AddRange(_State.NewDebris);
            _State.NewDebris.Clear();

            var bottomY = (_Camera.Position.Y + _BoardSize.Y * 0.5f) / CELL_PIXELS;

            for (int i = _Debris.Count - 1; i > -1; i--)
            {
                _Debris[i].AgeSeconds += deltaTime;

                if (WallDebris.IsGone(_Debris[i], bottomY))
                    _Debris.RemoveAt(i);
            }
        }

        /// <summary>
        /// Plays a looping sound while its condition holds and stops it as soon as it doesn't:
        /// the eat sound while our pac is eating, the siren while it's alive and not eating, the power pill loop while ours has one working, the power up sound while
        /// a power up can be heard (UpdatePowerUpLoop). While it plays its volume follows the sound volume setting, times volume.
        /// </summary>
        private static void UpdateLoopSound(SoundEffectInstance loop, bool playing, float volume = 1f)
        {
            if (playing)
                loop.Volume = SoundManager.GetVolume() * volume;

            if (playing && loop.State != SoundState.Playing)
                loop.Play();
            else if (!playing && loop.State == SoundState.Playing)
                loop.Stop();
        }

        /// <summary>
        /// The power up loop is an in-world sound: it plays at the volume of the nearest power up (SoundMath.GetOnScreenVolume),
        /// fading towards the board's edge, and stops while none is on the board.
        /// </summary>
        private void UpdatePowerUpLoop()
        {
            var volume = 0f;
            var powerUps = _State.Snapshot?.PowerUps;

            if (!_State.IsIntermission && powerUps != null)
            {
                foreach (var powerUp in powerUps)
                {
                    var offset = CellToWorld(powerUp.X, powerUp.Y) - _Camera.Position;
                    volume = Math.Max(volume, SoundMath.GetOnScreenVolume(offset.X, offset.Y, _BoardSize.X, _BoardSize.Y));
                }
            }

            UpdateLoopSound(_PowerUpLoop, volume > 0f, volume);
        }

        private void StopLoopSounds()
        {
            UpdateLoopSound(_EatLoop, false);
            UpdateLoopSound(_SirenLoop, false);
            UpdateLoopSound(_PowerPillLoop, false);
            UpdateLoopSound(_SpawnChargeLoop, false);
            UpdateLoopSound(_PowerUpLoop, false);
        }

        /// <summary>
        /// Starts the next waiting global sound once the one playing has finished.
        /// </summary>
        private void UpdateAnnouncements()
        {
            if (_Announcer != null && _Announcer.State != SoundState.Stopped)
                return;

            if (_Announcements.TryDequeue(_Clock.ElapsedMilliseconds, out var name))
                PlayAnnouncer(name);
        }

        /// <summary>
        /// Plays a global sound on the announcer channel at full volume, cutting off whatever was playing there.
        /// </summary>
        private void PlayAnnouncer(string name)
        {
            _Announcer?.Stop();
            _Announcer?.Dispose();
            _Announcer = AssetCache.GetSfx(name).CreateInstance();
            _Announcer.Volume = SoundManager.GetVolume();
            _Announcer.Play();
        }

        private void StopAnnouncements()
        {
            _Announcements.Clear();
            _Announcer?.Stop();
        }

        private void ReceiveMessages()
        {
            // paused, nothing plays: sounds are skipped rather than saved up, and the level track waits for resume
            var paused = ConfigManager.Settings.Paused;

            while (_Client.TryGetMessage(out var message))
            {
                _State.Apply(message, _Clock.ElapsedMilliseconds);

                switch (message)
                {
                    case LevelStartMessage:
                        _Debris.Clear();

                        if (paused)
                            _LevelStartedWhilePaused = true;
                        else
                            MusicManager.PlayLevelTrack(_State.Level);
                        break;

                    case GameEventMessage gameEvent:
                        if (!paused)
                        {
                            PlayEventSound(gameEvent);

                            // an area effect's shockwave shakes the board for everyone inside its radius, the pac that set it off included
                            if (PowerUpEffects.ShakesScreen(gameEvent.EventType) && _State.IsOwnPacInEffectRange(gameEvent))
                                ShakeScreen(ScreenShake.POW_AMPLITUDE_PIXELS, ScreenShake.POW_DURATION_SECONDS);

                            // smashed walls shake every board, wherever the player is
                            if (PowerUpEffects.ShakesEveryScreen(gameEvent.EventType))
                                ShakeScreen(ScreenShake.POW_AMPLITUDE_PIXELS, ScreenShake.POW_DURATION_SECONDS);
                        }

                        StartDeathAnimation(gameEvent);
                        break;

                    // the results are shown until the next level's LevelStart, which starts its music
                    case LevelWonMessage won:
                        MusicManager.Stop();
                        StopLoopSounds();
                        DeathAnimations.Clear();
                        _Debris.Clear();
                        StopAnnouncements();
                        _LevelStartedWhilePaused = false;

                        if (!paused)
                            PlayAnnouncer(Announcements.GetLevelEndSound(won.WinningTeam, _State.Team));
                        break;

                    // every server's scores go into the client's own list, which the High Scores screen shows
                    case HighScoresMessage:
                        HighScoreStore.Merge(_State.HighScores);
                        break;

                    // answered at once, so the server's measure of our lag (for late turns) is the lag we feel
                    case ServerPingMessage serverPing:
                        _Client.Send(new ServerPongMessage { ServerTime = serverPing.ServerTime });
                        break;
                }
            }
        }

        /// <summary>
        /// Shakes the game board (not the bars, mini map or panel) for any effect that should jolt the player's view:
        /// a bounce amplitudePixels high that dies away over durationSeconds. A stronger shake already running
        /// carries on instead (ScreenShake.Start).
        /// </summary>
        public void ShakeScreen(float amplitudePixels = ScreenShake.DEFAULT_AMPLITUDE_PIXELS, float durationSeconds = ScreenShake.DEFAULT_DURATION_SECONDS)
        {
            _Shake.Start(amplitudePixels, durationSeconds);
        }

        private void PlayEventSound(GameEventMessage gameEvent)
        {
            var sound = gameEvent.EventType switch
            {
                eGameEvent.PacEaten => Const.SFX_DEATH,
                eGameEvent.GhostKilled => Const.SFX_EAT_GHOST,
                eGameEvent.PowerUpEaten => Const.SFX_POWER_UP_EATEN,
                eGameEvent.Pow => Const.SFX_POW,
                eGameEvent.PokeBallCapture or eGameEvent.PokeBallRelease => Const.SFX_BLOOP,
                eGameEvent.BfgBurst => Const.SFX_BFG_BURST,
                _ => null,
            };

            // the server only sends this to the pac that respawned thanks to a teammate's Reinforcements: heard at full volume
            if (gameEvent.EventType == eGameEvent.AngelicRespawn)
                SoundManager.PlaySound(Const.SFX_ANGELIC_CHORUS);

            // smashed walls (Super Size) boom for everyone at full volume
            if (gameEvent.EventType == eGameEvent.WallSmash)
                SoundManager.PlaySound(Const.SFX_POW);

            if (sound != null)
            {
                // in-world sounds fade towards the board's edge and are silent off it
                var offset = CellToWorld(gameEvent.X, gameEvent.Y) - _Camera.Position;
                SoundManager.PlaySound(sound, SoundMath.GetEventVolume(gameEvent.EventType, offset.X, offset.Y, _BoardSize.X, _BoardSize.Y));
            }

            // global sounds are heard everywhere at full volume, queued so they play one at a time
            var now = _Clock.ElapsedMilliseconds;

            // the capture sting is only for the player who made the capture
            if (gameEvent.EventType == eGameEvent.FlagCaptured && gameEvent.ActorId == _State.PlayerId)
                _Announcements.Enqueue(Const.SFX_FLAG_CAP, now);

            _Announcements.Enqueue(Announcements.GetSound(gameEvent.EventType, gameEvent.Team), now);
        }

        /// <summary>
        /// Plays the death animation where a pac died, in its team color and at its size.
        /// </summary>
        private void StartDeathAnimation(GameEventMessage gameEvent)
        {
            if (gameEvent.EventType != eGameEvent.PacEaten)
                return;

            var size = PacRules.START_SIZE;
            var color = Color.White;

            foreach (var pac in _State.Snapshot?.Pacs ?? [])
            {
                if (pac.PlayerId != gameEvent.TargetId)
                    continue;

                size = pac.Size;
                color = ClientColors.GetPacColor(pac.Team, pac.PlayerId == _State.PlayerId);
                break;
            }

            var location = CellToWorld(gameEvent.X, gameEvent.Y).ToGeometryPoint();
            var death = ImageFactory.GeneratePlayerDeath(_PacDeathSheet, location, CELL_PIXELS * PacRules.GetDiameter(size), color);
            death.Update(0f);
            DeathAnimations.Add(death);
        }

        /// <summary>
        /// Sends the held direction to the server whenever it changes.
        /// </summary>
        private void SendInput()
        {
            var direction = GetHeldDirection(InputManager.KeyboardState, InputManager.GamePadState[(int)PlayerIndex.One]);

            if (direction == _LastDirection)
                return;

            _LastDirection = direction;
            _Client.Send(new InputMessage { Sequence = ++_InputSequence, Direction = direction });
        }

        /// <summary>
        /// Direction held on the keyboard (arrows or WASD) or gamepad (d-pad or left stick). Keyboard wins if both are used.
        /// </summary>
        public static eDirection GetHeldDirection(KeyboardState keyboard, GamePadState gamePad)
        {
            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W)) return eDirection.Up;
            if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S)) return eDirection.Down;
            if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A)) return eDirection.Left;
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D)) return eDirection.Right;

            if (gamePad.IsButtonDown(Buttons.DPadUp) || gamePad.IsButtonDown(Buttons.LeftThumbstickUp)) return eDirection.Up;
            if (gamePad.IsButtonDown(Buttons.DPadDown) || gamePad.IsButtonDown(Buttons.LeftThumbstickDown)) return eDirection.Down;
            if (gamePad.IsButtonDown(Buttons.DPadLeft) || gamePad.IsButtonDown(Buttons.LeftThumbstickLeft)) return eDirection.Left;
            if (gamePad.IsButtonDown(Buttons.DPadRight) || gamePad.IsButtonDown(Buttons.LeftThumbstickRight)) return eDirection.Right;

            return eDirection.None;
        }

        /// <summary>
        /// Converts a position in map cells to world pixels.
        /// </summary>
        public static Vector2 CellToWorld(float cellX, float cellY)
        {
            return new Vector2(cellX * CELL_PIXELS, cellY * CELL_PIXELS);
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            _Pixel ??= TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            // the settings screen hides the game, over the same background as the title's menus
            if (_SettingsOpen)
            {
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(_Background, TitleScreen.GetCoverRectangle(_Background.Width, _Background.Height, viewport.Bounds), Color.White);
                spriteBatch.End();

                _SettingsScreen.Draw(spriteBatch, viewport);
                return;
            }

            if (_State.IsIntermission && !IsStopped)
            {
                // point sampled so the scaled up pixel font stays crisp
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawResults(spriteBatch, viewport);
                spriteBatch.End();

                _InGameMenu.Draw(spriteBatch, viewport);
                return;
            }

            var status = GetStatusText();

            foreach (var layer in DRAW_LAYERS)
                DrawLayer(spriteBatch, viewport, layer, status);
        }

        /// <summary>
        /// Draws one layer of the game screen (eDrawLayer). The layers up to Overhead are the board, drawn into its own
        /// viewport below the score bar, which also clips them; status is the connection text shown instead of the HUD,
        /// or null.
        /// </summary>
        private void DrawLayer(SpriteBatch spriteBatch, Viewport viewport, eDrawLayer layer, string status)
        {
            var showBoard = _State.Map != null && !IsStopped;

            switch (layer)
            {
                case eDrawLayer.Background:
                case eDrawLayer.Maze:
                case eDrawLayer.Items:
                case eDrawLayer.Deaths:
                case eDrawLayer.Actors:
                case eDrawLayer.Effects:
                case eDrawLayer.Overhead:
                    if (!showBoard)
                        return;

                    var board = GetBoardViewport(viewport);
                    spriteBatch.GraphicsDevice.Viewport = board;

                    // sharp bilinear, so the pixel art stays crisp but scaled, turned and moving sprites get smooth edges
                    spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _PixelArt);
                    DrawBoardLayer(spriteBatch, board, layer);
                    spriteBatch.End();
                    spriteBatch.GraphicsDevice.Viewport = viewport;
                    break;

                case eDrawLayer.Hud:
                    spriteBatch.Begin();

                    if (status != null)
                        DrawStatus(spriteBatch, viewport, status);
                    else
                    {
                        DrawHud(spriteBatch, viewport);
                        DrawStatsBar(spriteBatch, viewport);
                    }

                    spriteBatch.End();

                    // over the bars, under the mini-map; the board's viewport keeps them off the bars
                    if (status == null && showBoard)
                    {
                        var pointerBoard = GetBoardViewport(viewport);
                        spriteBatch.GraphicsDevice.Viewport = pointerBoard;
                        spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _PixelArt);
                        DrawFlagPointers(spriteBatch, pointerBoard, GetMiniMapBounds(viewport));
                        spriteBatch.End();
                        spriteBatch.GraphicsDevice.Viewport = viewport;
                    }
                    break;

                case eDrawLayer.Ui:
                    if (status == null)
                    {
                        spriteBatch.Begin();
                        _MiniMap.Draw(spriteBatch, GetMiniMapBounds(viewport), _State.Snapshot, _State.Map, _State.PlayerId, _AnimationTime);
                        var ownPac = _State.GetOwnPac();
                        var ownSize = ownPac.HasValue ? (int)Math.Round(ownPac.Value.Size) : 0;
                        _PlayerState.Draw(spriteBatch, GetPlayerStateBounds(viewport), _State.GetOwnPowerUpEffect(), _State.GetOwnPowerUpFill(), ownSize, _State.GetOwnHungerFill(), _State.IsOwnPacStunned());
                        DrawRespawnCountdown(spriteBatch, viewport);

                        if (_PlayerListHeld && _State.HasPlayerList)
                            _PlayerList.Draw(spriteBatch, GetBoardViewport(viewport).Bounds, _State.BluePlayerList, _State.RedPlayerList, _State.PlayerId);

                        spriteBatch.End();
                    }

                    _InGameMenu.Draw(spriteBatch, viewport);
                    break;
            }
        }

        /// <summary>
        /// Draws one of the board's layers, inside the board's viewport.
        /// </summary>
        private void DrawBoardLayer(SpriteBatch spriteBatch, Viewport board, eDrawLayer layer)
        {
            switch (layer)
            {
                case eDrawLayer.Background:
                    spriteBatch.Draw(_Pixel, new Rectangle(0, 0, board.Width, board.Height), ClientColors.ToColor(_State.BackgroundColor));
                    DrawBackgroundImage(spriteBatch, board);
                    break;

                case eDrawLayer.Maze:
                    DrawMaze(spriteBatch, board);
                    break;

                case eDrawLayer.Items:
                    DrawLooseFlags(spriteBatch, board);
                    DrawPowerUps(spriteBatch, board);
                    DrawWeaponEffects(spriteBatch, board);
                    break;

                case eDrawLayer.Deaths:
                    foreach (var death in DeathAnimations)
                    {
                        if (!_Camera.IsVisible(death.GetBoundingBox(), board))
                            continue;

                        var screenPosition = _Camera.ToScreenPosition(death.Location.ToXnaVector(), board);
                        death.Draw(spriteBatch, screenPosition);
                    }
                    break;

                case eDrawLayer.Actors:
                    DrawPacs(spriteBatch, board);
                    DrawGhosts(spriteBatch, board);
                    break;

                case eDrawLayer.Effects:
                    DrawDebris(spriteBatch, board);
                    break;

                case eDrawLayer.Overhead:
                    DrawGhostEyes(spriteBatch, board);
                    break;
            }
        }

        /// <summary>
        /// While our own pac is dead: "Respawning in n seconds", or "Locked out!" while an enemy's Lockdown works
        /// (ClientGameState.GetOwnRespawnText), in white in the game font, centred on the board on a half-transparent
        /// panel with rounded corners.
        /// </summary>
        /// <summary>
        /// Shows the player list while TAB or a gamepad shoulder button is held (not during the results). Each press asks
        /// the server for a fresh list, which then stays as it was until the button is let go.
        /// </summary>
        private void UpdatePlayerList()
        {
            var gamePad = InputManager.GamePadState[(int)PlayerIndex.One];
            var held = !_State.IsIntermission
                && (InputManager.KeyboardState.IsKeyDown(Keys.Tab) || gamePad.IsButtonDown(Buttons.LeftShoulder) || gamePad.IsButtonDown(Buttons.RightShoulder));

            if (held && !_PlayerListHeld)
            {
                _State.ClearPlayerList();
                _Client.Send(new PlayerListRequestMessage());
            }

            _PlayerListHeld = held;
        }

        private void DrawRespawnCountdown(SpriteBatch spriteBatch, Viewport viewport)
        {
            var text = _State.GetOwnRespawnText();
            if (text == null)
                return;

            var board = GetBoardViewport(viewport);
            var size = _Font.MeasureString(text);
            var position = new Vector2(
                (float)Math.Round(board.X + (board.Width - size.X) * 0.5f),
                (float)Math.Round(board.Y + (board.Height - size.Y) * 0.5f));
            var panel = new Rectangle(
                (int)(position.X - RESPAWN_PANEL_PADDING),
                (int)(position.Y - RESPAWN_PANEL_PADDING),
                (int)(size.X + RESPAWN_PANEL_PADDING * 2f),
                (int)(size.Y + RESPAWN_PANEL_PADDING * 2f));

            TextureHelper.FillRounded(spriteBatch, _Pixel, panel, MiniMap.CORNER_RADIUS, ClientColors.COLOR_TEXT_PANEL);
            spriteBatch.DrawString(_Font, text, position, Color.White);
        }

        /// <summary>
        /// Message to show in place of the game, or null once we are playing.
        /// </summary>
        private string GetStatusText()
        {
            if (_State.RejectReason.HasValue)
            {
                return _State.RejectReason.Value switch
                {
                    eRejectReason.ServerFull => Const.STRING_REJECT_SERVER_FULL,
                    eRejectReason.InvalidName => Const.STRING_REJECT_INVALID_NAME,
                    eRejectReason.VersionMismatch => Const.STRING_REJECT_VERSION,
                    eRejectReason.Kicked => Const.STRING_REJECT_KICKED,
                    _ => _State.RejectReason.Value.ToString(),
                };
            }

            return _Client.State switch
            {
                eConnectionState.Connecting => Const.STRING_CONNECTING,
                eConnectionState.Failed or eConnectionState.Disconnected => AssetCache.ToAscii(_Client.Error ?? string.Empty),
                _ when _State.Map == null => Const.STRING_JOINING,
                _ => null,
            };
        }

        private void DrawStatus(SpriteBatch spriteBatch, Viewport viewport, string status)
        {
            var size = _Font.MeasureString(status);
            var y = (viewport.Height - size.Y) * 0.5f;
            spriteBatch.DrawString(_Font, status, new Vector2((viewport.Width - size.X) * 0.5f, y), ClientColors.COLOR_PAC_WHITE);

            if (!IsStopped)
                return;

            var hintSize = _Font.MeasureString(Const.STRING_PRESS_ESCAPE);
            spriteBatch.DrawString(_Font, Const.STRING_PRESS_ESCAPE, new Vector2((viewport.Width - hintSize.X) * 0.5f, y + _Font.LineSpacing * 2f), ClientColors.COLOR_PAC_LIGHT_GREY);
        }

        /// <summary>
        /// Height of the score and message bar across the top of the game: the score text plus padding.
        /// </summary>
        private int GetHudBarHeight()
        {
            return _Font.LineSpacing + HUD_BAR_PADDING * 2;
        }

        /// <summary>
        /// The part of the screen the board is drawn in: everything between the HUD bar and the stats bar.
        /// </summary>
        private Viewport GetBoardViewport(Viewport viewport)
        {
            var barHeight = GetHudBarHeight();
            return new Viewport(viewport.X, viewport.Y + barHeight, viewport.Width, Math.Max(1, viewport.Height - barHeight * 2));
        }

        /// <summary>
        /// The mini-map's square: MINI_MAP_SCREEN_FRACTION of the smaller screen side, in the top right corner just
        /// under the HUD bar.
        /// </summary>
        private Rectangle GetMiniMapBounds(Viewport viewport)
        {
            var side = (int)(Math.Min(viewport.Width, viewport.Height) * MINI_MAP_SCREEN_FRACTION);
            var margin = (int)HUD_MARGIN;
            return new Rectangle(viewport.Width - margin - side, GetHudBarHeight() + margin, side, side);
        }

        /// <summary>
        /// The player state panel: as wide as the mini-map, HUD_MARGIN under it.
        /// </summary>
        private Rectangle GetPlayerStateBounds(Viewport viewport)
        {
            var miniMap = GetMiniMapBounds(viewport);
            return new Rectangle(miniMap.X, miniMap.Bottom + (int)HUD_MARGIN, miniMap.Width, _PlayerState.GetHeight());
        }

        /// <summary>
        /// The bar across the top: team scores (blue on the left, red on the right) and the level name between them.
        /// </summary>
        private void DrawHud(SpriteBatch spriteBatch, Viewport viewport)
        {
            var barHeight = GetHudBarHeight();
            spriteBatch.Draw(_Pixel, new Rectangle(0, 0, viewport.Width, barHeight), Color.Black);

            var textY = (float)Math.Round((barHeight - _Font.LineSpacing) * 0.5f);
            var snapshot = _State.Snapshot;
            var blueScore = $"{Const.STRING_TEAM_BLUE} {snapshot?.BlueScore ?? 0}";
            var redScore = $"{Const.STRING_TEAM_RED} {snapshot?.RedScore ?? 0}";

            spriteBatch.DrawString(_Font, blueScore, new Vector2(HUD_MARGIN, textY), ClientColors.COLOR_PAC_BLUE);

            var redSize = _Font.MeasureString(redScore);
            spriteBatch.DrawString(_Font, redScore, new Vector2(viewport.Width - HUD_MARGIN - redSize.X, textY), ClientColors.COLOR_PAC_RED);

            var message = AssetCache.ToAscii(_State.GetLevelTitle());
            var messageSize = _Font.MeasureString(message);
            spriteBatch.DrawString(_Font, message, new Vector2((float)Math.Round((viewport.Width - messageSize.X) * 0.5f), textY), ClientColors.COLOR_PAC_WHITE);
        }

        /// <summary>
        /// The results screen between levels, over the winning team's picture (winblue or winred, scaled to cover the
        /// screen): the winning team at the top, then each team's score and its players ranked by score (blue on the
        /// left, red on the right), "Next Level in n" near the bottom and, under it at the bottom centre, a loading screen
        /// tip in Arial and in quotes if the server sent any, drawn straight onto the picture. Each team's score and
        /// list sit on their own black panel, fitted tightly around them, so they can be read over the picture.
        /// </summary>
        private void DrawResults(SpriteBatch spriteBatch, Viewport viewport)
        {
            var winner = _State.LevelWinner.Value;
            var background = winner == eTeam.Blue ? _WinBlueTexture : _WinRedTexture;
            spriteBatch.Draw(_Pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black);
            spriteBatch.Draw(background, TitleScreen.GetCoverRectangle(background.Width, background.Height, viewport.Bounds), Color.White);

            var title = winner == eTeam.Blue ? Const.STRING_BLUE_WINS : Const.STRING_RED_WINS;
            var titleTop = RESULTS_GAP;
            var titleHeight = _Font.LineSpacing * RESULTS_TITLE_SCALE;
            var titleWidth = _Font.MeasureString(title).X * RESULTS_TITLE_SCALE;
            var titlePosition = new Vector2((float)Math.Round(viewport.Width * 0.5f - titleWidth * 0.5f), (float)Math.Round(titleTop));
            TextHelper.DrawShadowedString(spriteBatch, _Font, title, titlePosition, ClientColors.COLOR_PAC_WHITE, RESULTS_TITLE_SCALE);

            var countdown = $"{Const.STRING_NEXT_LEVEL_IN} {_State.GetIntermissionSecondsLeft(_Clock.ElapsedMilliseconds)}";
            var countdownHeight = _Font.LineSpacing * RESULTS_COUNTDOWN_SCALE;
            var countdownBottom = viewport.Height - RESULTS_GAP;

            var tip = AssetCache.ToAscii(_State.IntermissionTip);
            if (tip.Length > 0)
            {
                tip = $"\"{tip}\"";
                var tipSize = _NameFont.MeasureString(tip);
                var tipTop = viewport.Height - RESULTS_GAP - tipSize.Y;
                var tipPosition = new Vector2((float)Math.Round(viewport.Width * 0.5f - tipSize.X * 0.5f), (float)Math.Round(tipTop));
                TextHelper.DrawShadowedString(spriteBatch, _NameFont, tip, tipPosition, ClientColors.COLOR_PAC_WHITE);
                countdownBottom = tipTop - RESULTS_GAP;
            }

            var countdownTop = countdownBottom - countdownHeight;
            var countdownWidth = _Font.MeasureString(countdown).X * RESULTS_COUNTDOWN_SCALE;
            var countdownPosition = new Vector2((float)Math.Round(viewport.Width * 0.5f - countdownWidth * 0.5f), (float)Math.Round(countdownTop));
            TextHelper.DrawShadowedString(spriteBatch, _Font, countdown, countdownPosition, ClientColors.COLOR_PAC_WHITE, RESULTS_COUNTDOWN_SCALE);

            var halfWidth = viewport.Width * 0.5f;
            var blockTop = titleTop + titleHeight + RESULTS_GAP;
            var blockHeight = countdownTop - RESULTS_GAP - blockTop;
            var blockWidth = halfWidth - RESULTS_GAP * 1.5f;
            DrawTeamResults(spriteBatch, $"{Const.STRING_TEAM_BLUE} {_State.LevelBlueScore}", _State.BlueResults, halfWidth * 0.5f, blockTop, blockWidth, blockHeight, ClientColors.COLOR_PAC_BLUE);
            DrawTeamResults(spriteBatch, $"{Const.STRING_TEAM_RED} {_State.LevelRedScore}", _State.RedResults, halfWidth * 1.5f, blockTop, blockWidth, blockHeight, ClientColors.COLOR_PAC_RED);
        }

        /// <summary>
        /// One team's score, then its ranked players as "rank. name" with the score on the right, in as many columns
        /// as it takes to fit the height (a team can have 64 or more players), centred on centreX on a black panel
        /// fitted around them. Columns are as wide as their widest line, unless that would be wider than maxWidth,
        /// when they share maxWidth. Our own line is yellow.
        /// </summary>
        private void DrawTeamResults(SpriteBatch spriteBatch, string header, List<PlayerResult> results, float centreX, float top, float maxWidth, float maxHeight, Color teamColor)
        {
            var headerWidth = _Font.MeasureString(header).X;
            var headerHeight = (float)_Font.LineSpacing;

            var lineHeight = _NameFont.LineSpacing;
            var listTop = top + headerHeight + RESULTS_GAP;
            var rowsPerColumn = Math.Max(1, (int)((maxHeight - headerHeight - RESULTS_GAP) / lineHeight));
            var columns = (results.Count + rowsPerColumn - 1) / rowsPerColumn;
            var rowsUsed = Math.Min(results.Count, rowsPerColumn);

            var names = new string[results.Count];
            var scores = new string[results.Count];
            var nameWidth = 0f;
            var scoreWidth = 0f;
            for (var i = 0; i < results.Count; i++)
            {
                names[i] = $"{i + 1}. {AssetCache.ToAscii(results[i].Name)}";
                scores[i] = results[i].Score.ToString();
                nameWidth = Math.Max(nameWidth, _NameFont.MeasureString(names[i]).X);
                scoreWidth = Math.Max(scoreWidth, _NameFont.MeasureString(scores[i]).X);
            }

            var columnWidth = nameWidth + RESULTS_GAP + scoreWidth;
            var listWidth = columns > 0 ? columns * columnWidth + (columns - 1) * RESULTS_GAP : 0f;
            if (listWidth > maxWidth)
            {
                columnWidth = (maxWidth - RESULTS_GAP * (columns - 1)) / columns;
                listWidth = maxWidth;
            }

            var panelWidth = Math.Max(headerWidth, listWidth);
            var panelHeight = headerHeight + (results.Count > 0 ? RESULTS_GAP + rowsUsed * lineHeight : 0f);
            var panelInset = RESULTS_GAP * 0.5f;
            DrawPanel(spriteBatch, centreX - panelWidth * 0.5f - panelInset, top - panelInset, panelWidth + panelInset * 2f, panelHeight + panelInset * 2f);
            DrawCentredString(spriteBatch, header, centreX, top, 1f, teamColor);

            var listLeft = centreX - listWidth * 0.5f;
            for (var i = 0; i < results.Count; i++)
            {
                var x = (float)Math.Round(listLeft + i / rowsPerColumn * (columnWidth + RESULTS_GAP));
                var y = (float)Math.Round(listTop + i % rowsPerColumn * lineHeight);
                var color = results[i].PlayerId == _State.PlayerId ? ClientColors.COLOR_PAC_YELLOW : teamColor;

                var width = _NameFont.MeasureString(scores[i]).X;
                spriteBatch.DrawString(_NameFont, scores[i], new Vector2((float)Math.Round(x + columnWidth - width), y), color);
                spriteBatch.DrawString(_NameFont, names[i], new Vector2(x, y), color);
            }
        }

        /// <summary>
        /// An opaque black panel for text over a picture, with corners rounded to RESULTS_PANEL_RADIUS: the middle is
        /// one rectangle, and the curved rows at the top and bottom are one-pixel strips cut in at each side.
        /// </summary>
        private void DrawPanel(SpriteBatch spriteBatch, float left, float top, float width, float height)
        {
            var x = (int)Math.Round(left);
            var y = (int)Math.Round(top);
            var w = (int)Math.Round(width);
            var h = (int)Math.Round(height);
            var radius = Math.Min(RESULTS_PANEL_RADIUS, Math.Min(w, h) / 2);

            spriteBatch.Draw(_Pixel, new Rectangle(x, y + radius, w, h - radius * 2), Color.Black);

            for (var row = 0; row < radius; row++)
            {
                var inset = RoundedCorners.GetRowInset(radius, row);
                spriteBatch.Draw(_Pixel, new Rectangle(x + inset, y + row, w - inset * 2, 1), Color.Black);
                spriteBatch.Draw(_Pixel, new Rectangle(x + inset, y + h - 1 - row, w - inset * 2, 1), Color.Black);
            }
        }

        /// <summary>
        /// Draws text in the UI font at a scale, centred on centreX with its top at top.
        /// </summary>
        private void DrawCentredString(SpriteBatch spriteBatch, string text, float centreX, float top, float scale, Color color)
        {
            var width = _Font.MeasureString(text).X * scale;
            var position = new Vector2((float)Math.Round(centreX - width * 0.5f), (float)Math.Round(top));
            spriteBatch.DrawString(_Font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// The bar across the bottom: our pac's running total score, flag captures, ghost kills, enemies eaten, flag rescues
        /// and power ups eaten on the left and its ping on the right.
        /// </summary>
        private void DrawStatsBar(SpriteBatch spriteBatch, Viewport viewport)
        {
            var barHeight = GetHudBarHeight();
            var barTop = viewport.Height - barHeight;
            spriteBatch.Draw(_Pixel, new Rectangle(0, barTop, viewport.Width, barHeight), Color.Black);

            var ownPac = _State.GetOwnPac();
            if (!ownPac.HasValue)
                return;

            var pac = ownPac.Value;
            var textY = barTop + (float)Math.Round((barHeight - _Font.LineSpacing) * 0.5f);
            var size = $"{Const.STRING_SCORE} {pac.TotalScore}  ";
            var stats = $"{Const.STRING_FLAGS} {pac.FlagCaps}  {Const.STRING_GHOSTS} {pac.GhostKills}  {Const.STRING_CHOMPS} {pac.PlayerKills}  {Const.STRING_RESCUES} {pac.Rescues}  {Const.STRING_POWER_UPS} {pac.PowerUps}";

            spriteBatch.DrawString(_Font, size + stats, new Vector2(HUD_MARGIN, textY), ClientColors.COLOR_PAC_WHITE);

            var pingColor = ClientColors.COLOR_PAC_WHITE;
            if (_State.PingMs >= 0)
            {
                var heat = ColorRamp.GetPingColor(_State.PingMs);
                pingColor = ClientColors.ToColor(heat);
            }
            var ping = _State.GetPingText();
            var pingSize = _Font.MeasureString(ping);
            spriteBatch.DrawString(_Font, ping, new Vector2(viewport.Width - HUD_MARGIN - pingSize.X, textY), pingColor);
        }

        /// <summary>
        /// Tiles the level's background image (LevelInfo.BackgroundImage), if it has one, over the board.
        /// The tiles are fixed to the world, so they scroll with the maze; only the ones on screen are drawn.
        /// </summary>
        private void DrawBackgroundImage(SpriteBatch spriteBatch, Viewport viewport)
        {
            var imageName = _State.LevelInfo?.BackgroundImage;
            if (imageName == null)
                return;

            var image = AssetCache.GetTexture(imageName);
            var topLeft = _Camera.Position - new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
            var firstX = (int)Math.Floor(topLeft.X / image.Width);
            var firstY = (int)Math.Floor(topLeft.Y / image.Height);
            var lastX = (int)Math.Floor((topLeft.X + viewport.Width) / image.Width);
            var lastY = (int)Math.Floor((topLeft.Y + viewport.Height) / image.Height);

            for (var x = firstX; x <= lastX; x++)
            {
                for (var y = firstY; y <= lastY; y++)
                {
                    var screen = _Camera.ToScreenPosition(new Vector2(x * image.Width, y * image.Height), viewport);
                    spriteBatch.Draw(image, new Rectangle((int)screen.X, (int)screen.Y, image.Width, image.Height), Color.White);
                }
            }
        }

        /// <summary>
        /// Draws only the cells on screen: walls in the level's colors, the ghost pen floor and flag bases, then dots and power pills.
        /// </summary>
        private void DrawMaze(SpriteBatch spriteBatch, Viewport viewport)
        {
            var map = _State.Map;
            var wallColor = ClientColors.ToColor(_State.WallColor);
            var wallFillColor = ClientColors.ToColor(_State.WallFillColor);
            var isRainbow = _State.LevelInfo != null && _State.LevelInfo.IsRainbow;
            var dotColors = _State.DotColors;

            var topLeft = _Camera.Position - new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
            var firstX = Math.Max(0, (int)Math.Floor(topLeft.X / CELL_PIXELS));
            var firstY = Math.Max(0, (int)Math.Floor(topLeft.Y / CELL_PIXELS));
            var lastX = Math.Min(map.GetLength(0) - 1, (int)Math.Ceiling((topLeft.X + viewport.Width) / CELL_PIXELS));
            var lastY = Math.Min(map.GetLength(1) - 1, (int)Math.Ceiling((topLeft.Y + viewport.Height) / CELL_PIXELS));
            var showPowerPills = _AnimationTime % (POWER_PILL_BLINK_SECONDS * 2f) < POWER_PILL_BLINK_SECONDS;
            var showBaseTeamColor = _AnimationTime % (FLAG_BASE_BLINK_SECONDS * 2f) < FLAG_BASE_BLINK_SECONDS;

            DrawFloors(spriteBatch, viewport, map);

            for (var x = firstX; x <= lastX; x++)
            {
                for (var y = firstY; y <= lastY; y++)
                {
                    var cell = map[x, y];
                    var screen = _Camera.ToScreenPosition(CellToWorld(x, y), viewport);
                    var bounds = new Rectangle((int)screen.X, (int)screen.Y, CELL_PIXELS, CELL_PIXELS);

                    // a Tetromino block is drawn as its weapon effect sprite, not a wall
                    if (cell.HasFlag(eGridDef.TetrisBlock))
                        continue;

                    if (cell.HasFlag(eGridDef.Wall))
                    {
                        DrawWall(spriteBatch, map, x, y, bounds, wallColor, wallFillColor, isRainbow);
                        continue;
                    }

                    if (cell.HasFlag(eGridDef.FlagBaseRed))
                        spriteBatch.Draw(_FlagBaseTexture, bounds, showBaseTeamColor ? ClientColors.GetTeamColor(eTeam.Red) : ClientColors.COLOR_PAC_YELLOW);
                    else if (cell.HasFlag(eGridDef.FlagBaseBlue))
                        spriteBatch.Draw(_FlagBaseTexture, bounds, showBaseTeamColor ? ClientColors.GetTeamColor(eTeam.Blue) : ClientColors.COLOR_PAC_YELLOW);

                    if (cell.HasFlag(eGridDef.Food))
                        spriteBatch.Draw(_DotTexture, bounds, ClientColors.ToColor(dotColors[x, y]));
                    else if (cell.HasFlag(eGridDef.PowerPill) && showPowerPills)
                        spriteBatch.Draw(_PowerUpSheet, bounds, POWER_PILL_SOURCE, ClientColors.ToColor(dotColors[x, y]));
                }
            }
        }

        /// <summary>
        /// Draws the ghost pen and spawn room floors, each texture stretched once over its whole room.
        /// </summary>
        private void DrawFloors(SpriteBatch spriteBatch, Viewport viewport, eGridDef[,] map)
        {
            // a new map instance means a new level, so find its rooms again
            if (map != _FloorBoxesMap)
            {
                _GhostPenBoxes = MiniMapMath.GetCellBoxes(map, eGridDef.GhostGenerator);
                _RedSpawnBoxes = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointRed);
                _BlueSpawnBoxes = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointBlue);
                _FloorBoxesMap = map;
            }

            var frame = PacBaseSprites.GetFrame(_AnimationTime);
            var ghostSource = GetPacBaseSource(PacBaseSprites.GetGhostCellY(), frame);
            var redSource = GetPacBaseSource(PacBaseSprites.GetCellY(eTeam.Red), frame);
            var blueSource = GetPacBaseSource(PacBaseSprites.GetCellY(eTeam.Blue), frame);

            foreach (var box in _GhostPenBoxes)
                DrawFloor(spriteBatch, viewport, box, _PacBaseTexture, ghostSource, Color.White);

            foreach (var box in _RedSpawnBoxes)
                DrawFloor(spriteBatch, viewport, box, _PacBaseTexture, redSource, Color.White);

            foreach (var box in _BlueSpawnBoxes)
                DrawFloor(spriteBatch, viewport, box, _PacBaseTexture, blueSource, Color.White);
        }

        /// <summary>
        /// The base sheet cell in the row at cellY (a team's spawn area or the ghost pen) at the current blink frame.
        /// </summary>
        private static Rectangle GetPacBaseSource(int cellY, int frame)
        {
            return new Rectangle(PacBaseSprites.GetCellX(frame), cellY, PacBaseSprites.CELL_PIXELS, PacBaseSprites.CELL_PIXELS);
        }

        private void DrawFloor(SpriteBatch spriteBatch, Viewport viewport, CellBox box, Texture2D texture, Rectangle? source, Color tint)
        {
            var screen = _Camera.ToScreenPosition(CellToWorld(box.X, box.Y), viewport);
            var bounds = new Rectangle((int)screen.X, (int)screen.Y, box.Width * CELL_PIXELS, box.Height * CELL_PIXELS);

            if (bounds.Right < 0 || bounds.Bottom < 0 || bounds.Left > viewport.Width || bounds.Top > viewport.Height)
                return;

            spriteBatch.Draw(texture, bounds, source, tint);
        }

        /// <summary>
        /// Draws a wall cell as four quarters cut from the corner tile, each turned about the cell centre to join the walls around it.
        /// On a rainbow level each quarter is tinted from the rainbow gradient (Rainbow.GetWallColor) instead of the wall color.
        /// Unless the fill color is transparent, each quarter's inside is first filled with it: the matching quarter of the fill mask,
        /// or the whole quarter inside a solid block.
        /// </summary>
        private void DrawWall(SpriteBatch spriteBatch, eGridDef[,] map, int x, int y, Rectangle bounds, Color wallColor, Color wallFillColor, bool isRainbow)
        {
            var half = CELL_PIXELS / 2;
            var center = new Vector2(bounds.X + half, bounds.Y + half);
            var isFilled = wallFillColor.A > 0;

            for (var quarter = 0; quarter < 4; quarter++)
            {
                if (!WallTiles.GetQuarter(map, x, y, quarter, out var sourceQuarter, out var quarterTurns))
                {
                    if (isFilled)
                    {
                        // quarters run clockwise from the top left
                        var quarterX = quarter == 1 || quarter == 2 ? half : 0;
                        var quarterY = quarter >= 2 ? half : 0;
                        spriteBatch.Draw(_Pixel, new Rectangle(bounds.X + quarterX, bounds.Y + quarterY, half, half), wallFillColor);
                    }

                    continue;
                }

                // quarters run clockwise from the top left
                var sourceX = sourceQuarter == 1 || sourceQuarter == 2 ? half : 0;
                var sourceY = sourceQuarter >= 2 ? half : 0;
                var source = new Rectangle(sourceX, sourceY, half, half);
                var origin = new Vector2(half - sourceX, half - sourceY);
                var rotation = quarterTurns * MathHelper.PiOver2;

                if (isFilled)
                    spriteBatch.Draw(_WallFillTexture, center, source, wallFillColor, rotation, origin, 1f, SpriteEffects.None, 0f);

                var tint = isRainbow ? ClientColors.ToColor(Rainbow.GetWallColor(x, y, quarter)) : wallColor;
                spriteBatch.Draw(_WallTexture, center, source, tint, rotation, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// Every piece of smashed wall in flight, over everything else on the board: its quarter of the wall as it looked
        /// (fill, then tube, as DrawWall), turned by its quarter turns plus its spin about its own centre. A solid quarter is
        /// just the fill color, so it isn't drawn when the walls are hollow.
        /// </summary>
        private void DrawDebris(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_Debris.Count == 0)
                return;

            var half = CELL_PIXELS / 2;
            var quarterOrigin = new Vector2(half * 0.5f, half * 0.5f);
            var wallColor = ClientColors.ToColor(_State.WallColor);
            var wallFillColor = ClientColors.ToColor(_State.WallFillColor);
            var isFilled = wallFillColor.A > 0;
            var isRainbow = _State.LevelInfo != null && _State.LevelInfo.IsRainbow;

            foreach (var piece in _Debris)
            {
                var center = _Camera.ToScreenPosition(CellToWorld(WallDebris.GetX(piece), WallDebris.GetY(piece)), viewport);

                if (center.X < -CELL_PIXELS || center.Y < -CELL_PIXELS || center.X > viewport.Width + CELL_PIXELS || center.Y > viewport.Height + CELL_PIXELS)
                    continue;

                var spin = WallDebris.GetRotation(piece);

                if (piece.IsSolid)
                {
                    if (isFilled)
                        spriteBatch.Draw(_Pixel, center, null, wallFillColor, spin, new Vector2(0.5f, 0.5f), half, SpriteEffects.None, 0f);

                    continue;
                }

                // quarters run clockwise from the top left
                var sourceX = piece.SourceQuarter == 1 || piece.SourceQuarter == 2 ? half : 0;
                var sourceY = piece.SourceQuarter >= 2 ? half : 0;
                var source = new Rectangle(sourceX, sourceY, half, half);
                var rotation = piece.QuarterTurns * MathHelper.PiOver2 + spin;

                if (isFilled)
                    spriteBatch.Draw(_WallFillTexture, center, source, wallFillColor, rotation, quarterOrigin, 1f, SpriteEffects.None, 0f);

                var tint = isRainbow ? ClientColors.ToColor(Rainbow.GetWallColor(piece.CellX, piece.CellY, piece.Quarter)) : wallColor;
                spriteBatch.Draw(_WallTexture, center, source, tint, rotation, quarterOrigin, 1f, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// A flag's tint right now: it flickers between yellow and its team's color.
        /// </summary>
        private Color GetFlagColor(eTeam team)
        {
            return ClientColors.GetFlagColor(team, _AnimationTime);
        }

        /// <summary>
        /// Flags nobody is carrying, one cell wide, in their team's color, at base or wherever they were dropped. A flag
        /// sealed by a Force Field (triforce) has a pulsing gold bubble over it, ForceField.FIELD_CELLS wide.
        /// </summary>
        private void DrawLooseFlags(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_State.Snapshot == null)
                return;

            _ForceFieldTexture ??= CreateForceFieldTexture(spriteBatch.GraphicsDevice);
            var fieldPixels = (int)(CELL_PIXELS * ForceField.FIELD_CELLS);
            var fieldColor = ClientColors.COLOR_FORCE_FIELD * ForceField.GetPulse(_AnimationTime);

            foreach (var flag in _State.Snapshot.Flags)
            {
                if (flag.IsCarried)
                    continue;

                var center = _Camera.ToScreenPosition(CellToWorld(flag.X, flag.Y), viewport);
                var bounds = new Rectangle((int)(center.X - CELL_PIXELS * 0.5f), (int)(center.Y - CELL_PIXELS * 0.5f), CELL_PIXELS, CELL_PIXELS);
                spriteBatch.Draw(_FlagTexture, bounds, GetFlagColor(flag.Team));

                if (flag.IsSealed)
                {
                    var field = new Rectangle((int)(center.X - fieldPixels * 0.5f), (int)(center.Y - fieldPixels * 0.5f), fieldPixels, fieldPixels);
                    spriteBatch.Draw(_ForceFieldTexture, field, fieldColor);
                }
            }
        }

        /// <summary>
        /// The force field bubble: white, with each pixel's opacity from ForceField.GetAlpha (premultiplied).
        /// </summary>
        private static Texture2D CreateForceFieldTexture(GraphicsDevice graphicsDevice)
        {
            const int SIZE = FORCE_FIELD_TEXTURE_PIXELS;
            var data = new Color[SIZE * SIZE];

            for (var y = 0; y < SIZE; y++)
                for (var x = 0; x < SIZE; x++)
                    data[y * SIZE + x] = Color.White * ForceField.GetAlpha(x, y, SIZE);

            var texture = new Texture2D(graphicsDevice, SIZE, SIZE);
            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// For every flag, on screen or not and carried or not, a pointer in the flag's team color on a circle around
        /// the player (the camera's centre), on the line from the player to the flag and pointing at it. A flag right
        /// on the player (one they carry) has no direction and gets none. The circle's radius reaches the mini-map's
        /// bottom left corner (miniMap is in screen pixels, not board pixels).
        /// </summary>
        private void DrawFlagPointers(SpriteBatch spriteBatch, Viewport viewport, Rectangle miniMap)
        {
            if (_State.Snapshot == null)
                return;

            var centre = _Camera.ToScreenPosition(_Camera.Position, viewport);
            var centreX = centre.X;
            var centreY = centre.Y;
            var radius = FlagPointerMath.GetDistance(centreX, centreY, miniMap.Left, miniMap.Bottom - GetHudBarHeight());
            var origin = new Vector2(_PointerTexture.Width * 0.5f, _PointerTexture.Height * 0.5f);
            var scale = new Vector2((float)FLAG_POINTER_PIXELS / _PointerTexture.Width, (float)FLAG_POINTER_PIXELS / _PointerTexture.Height);

            foreach (var flag in _State.Snapshot.Flags)
            {
                var flagScreen = _Camera.ToScreenPosition(CellToWorld(flag.X, flag.Y), viewport);
                if (FlagPointerMath.GetDistance(centreX, centreY, flagScreen.X, flagScreen.Y) == 0f)
                    continue;

                var (x, y) = FlagPointerMath.GetCirclePosition(centreX, centreY, flagScreen.X, flagScreen.Y, radius);
                var rotation = FlagPointerMath.GetAngle(centreX, centreY, flagScreen.X, flagScreen.Y);
                spriteBatch.Draw(_PointerTexture, new Vector2(x, y), null, ClientColors.GetTeamColor(flag.Team), rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// The carried-flag sprite in the flag's team color, drawn on the same spot of the carrier whichever way it
        /// faces: up and behind on the right-facing sprite, turned and mirrored with the pac along with the flag
        /// itself, so the flag always faces the way the pac is going.
        /// </summary>
        private void DrawCarriedFlag(SpriteBatch spriteBatch, PacState pac, Vector2 pacScreen, float pacWidth)
        {
            foreach (var flag in _State.Snapshot.Flags)
            {
                if (flag.CarrierId != pac.PlayerId)
                    continue;

                var size = pacWidth * CARRIED_FLAG_SCALE;
                var offset = pacWidth * 0.5f * CARRIED_FLAG_OFFSET;
                var (rotation, effects) = GetFacingTransform(pac.Facing);

                // a stunned pac spins in place (and the flag it carries with it)
                if (pac.Stunned)
                    rotation += PowerUpEffects.GetStunSpinAngle(_AnimationTime);

                // the mount point on the right-facing sprite, then mirrored and turned the same way as the pac
                var mount = new Vector2(-offset, -offset);
                if (effects == SpriteEffects.FlipHorizontally)
                    mount.X = -mount.X;

                var centre = pacScreen + Vector2.Transform(mount, Matrix.CreateRotationZ(rotation));
                var origin = new Vector2(_FlagTexture.Width * 0.5f, _FlagTexture.Height * 0.5f);
                var scale = new Vector2(size / _FlagTexture.Width, size / _FlagTexture.Height);
                spriteBatch.Draw(_FlagTexture, centre, null, GetFlagColor(flag.Team), rotation, origin, scale, effects, 0f);
            }
        }

        /// <summary>
        /// Rotation and mirroring that turn a sprite drawn facing right to face the given way. Left is a mirror rather
        /// than a half turn so the sprite isn't upside down.
        /// </summary>
        private static (float Rotation, SpriteEffects Effects) GetFacingTransform(eFacing facing)
        {
            return facing switch
            {
                eFacing.Up => (-MathHelper.PiOver2, SpriteEffects.None),
                eFacing.Down => (MathHelper.PiOver2, SpriteEffects.None),
                eFacing.Left => (0f, SpriteEffects.FlipHorizontally),
                _ => (0f, SpriteEffects.None),
            };
        }

        /// <summary>
        /// Draws every living pac in its team color, facing the way it's going, mouth chomping, with any flag it carries
        /// and, while a power pill is working, a fading speed trail behind it.
        /// </summary>
        private void DrawPacs(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_State.Snapshot == null)
                return;

            var frameWidth = _PacSheet.Width / PAC_FRAME_COUNT;
            var frame = GetChompFrame(_AnimationTime);
            var source = new Rectangle(frame * frameWidth, 0, frameWidth, _PacSheet.Height);
            var origin = new Vector2(frameWidth * 0.5f, _PacSheet.Height * 0.5f);

            foreach (var pac in _State.Snapshot.Pacs)
            {
                if (!pac.Alive)
                    continue;

                var screen = _Camera.ToScreenPosition(CellToWorld(pac.X, pac.Y), viewport);
                var pacWidth = CELL_PIXELS * PacRules.GetDiameter(pac.Size);
                var scale = pacWidth / frameWidth;
                var isOwn = pac.PlayerId == _State.PlayerId;
                var color = ClientColors.GetPacColor(pac.Team, isOwn);

                // power pulse, star blink, white flash and spawn charge blink, trail included (the mini map uses the same)
                var tint = ClientColors.GetPacTint(pac, isOwn, _AnimationTime);

                var (rotation, effects) = GetFacingTransform(pac.Facing);

                // a stunned pac spins in place (and the flag it carries with it)
                if (pac.Stunned)
                    rotation += PowerUpEffects.GetStunSpinAngle(_AnimationTime);

                // a Morph Ball pac rolls as it moves
                if (pac.PowerUpEffect == ePowerUpEffect.MorphBall)
                    rotation += PowerUpEffects.GetRollAngle(pac.X, pac.Y);

                // a Screw Attack pac spins very fast as it moves
                if (pac.PowerUpEffect == ePowerUpEffect.ScrewAttack)
                    rotation += PowerUpEffects.GetScrewAngle(pac.X, pac.Y);

                // speed trail behind a pac with Sugar Rush, oldest copy first so the newer ones sit on top
                var trail = _Trails.GetTrail(pac.PlayerId);
                for (var i = trail.Count - 1; i > -1; i--)
                {
                    var trailScreen = _Camera.ToScreenPosition(CellToWorld(trail[i].X, trail[i].Y), viewport);
                    spriteBatch.Draw(_PacSheet, trailScreen, source, tint * PacTrails.GetAlpha(i), rotation, origin, scale, effects, 0f);
                }

                spriteBatch.Draw(_PacSheet, screen, source, tint, rotation, origin, scale, effects, 0f);

                if (pac.HasFlag)
                    DrawCarriedFlag(spriteBatch, pac, screen, pacWidth);

                DrawPacName(spriteBatch, pac, screen, pacWidth, color);
            }
        }

        /// <summary>
        /// Draws the pac's player name centred below it, with a dark shadow so it reads over the maze.
        /// </summary>
        private void DrawPacName(SpriteBatch spriteBatch, PacState pac, Vector2 pacScreen, float pacWidth, Color color)
        {
            DrawName(spriteBatch, _State.GetPlayerName(pac.PlayerId), pacScreen, pacWidth, color);
        }

        /// <summary>
        /// Draws a name centred below something width pixels wide centred on screen, with a dark shadow so it reads over the maze.
        /// </summary>
        private void DrawName(SpriteBatch spriteBatch, string name, Vector2 screen, float width, Color color)
        {
            if (string.IsNullOrEmpty(name))
                return;

            var size = _NameFont.MeasureString(name);
            var position = new Vector2(
                (float)Math.Round(screen.X - size.X * 0.5f),
                (float)Math.Round(screen.Y + width * 0.5f + NAME_OFFSET_PIXELS));

            spriteBatch.DrawString(_NameFont, name, position + Vector2.One, Color.Black);
            spriteBatch.DrawString(_NameFont, name, position, color);
        }

        /// <summary>
        /// Draws each power up one cell wide from the power up sheet, bobbing up and down (PowerUpRules.GetHoverOffset)
        /// over an oval shadow that stays at its true position, so the bob shows whichever way it moves.
        /// </summary>
        private void DrawPowerUps(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_State.Snapshot == null)
                return;

            _PowerUpShadowTexture ??= CreatePowerUpShadowTexture(spriteBatch.GraphicsDevice);

            var halfSize = CELL_PIXELS * 0.5f;
            var shadowWidth = (int)(PowerUpRules.SHADOW_WIDTH_CELLS * CELL_PIXELS);
            var shadowHeight = (int)(PowerUpRules.SHADOW_HEIGHT_CELLS * CELL_PIXELS);
            var shadowOffset = PowerUpRules.SHADOW_OFFSET_CELLS * CELL_PIXELS;

            foreach (var powerUp in _State.Snapshot.PowerUps)
            {
                var source = new Rectangle(PowerUpSprites.GetCellX(powerUp.Item), PowerUpSprites.GetCellY(powerUp.Item),
                    PowerUpSprites.CELL_PIXELS, PowerUpSprites.CELL_PIXELS);
                var hover = PowerUpRules.GetHoverOffset(_AnimationTime, powerUp.PowerUpId) * CELL_PIXELS;
                var center = _Camera.ToScreenPosition(CellToWorld(powerUp.X, powerUp.Y), viewport);
                var shadow = new Rectangle((int)(center.X - shadowWidth * 0.5f), (int)(center.Y + shadowOffset - shadowHeight * 0.5f),
                    shadowWidth, shadowHeight);
                var bounds = new Rectangle((int)(center.X - halfSize), (int)(center.Y - halfSize + hover), CELL_PIXELS, CELL_PIXELS);

                spriteBatch.Draw(_PowerUpShadowTexture, shadow, Color.White);
                spriteBatch.Draw(_PowerUpSheet, bounds, source, Color.White);
            }
        }

        /// <summary>
        /// Builds the power up shadow texture: a black oval whose opacity comes from PowerUpRules.GetShadowAlpha.
        /// </summary>
        private static Texture2D CreatePowerUpShadowTexture(GraphicsDevice graphicsDevice)
        {
            const int WIDTH = POWER_UP_SHADOW_TEXTURE_WIDTH;
            const int HEIGHT = POWER_UP_SHADOW_TEXTURE_HEIGHT;
            var data = new Color[WIDTH * HEIGHT];

            for (var y = 0; y < HEIGHT; y++)
                for (var x = 0; x < WIDTH; x++)
                    data[y * WIDTH + x] = Color.Black * PowerUpRules.GetShadowAlpha(x, y, WIDTH, HEIGHT);

            var texture = new Texture2D(graphicsDevice, WIDTH, HEIGHT);
            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Draws every weapon effect one cell wide from the weapon effects sheet in its own colors, flipping between
        /// its two frames (WeaponEffectSprites.GetFrame, offset by id so they don't all flip together). Things lying in
        /// the maze (holy flames, banana peels, Tetromino blocks) go first so the rest sit on top; a thrown sword is
        /// turned to point the way it flies, and a hammer facing left is mirrored.
        /// </summary>
        private void DrawWeaponEffects(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_State.Snapshot == null)
                return;

            for (var pass = 0; pass < 2; pass++)
            {
                foreach (var effect in _State.Snapshot.WeaponEffects)
                {
                    if (WeaponEffectSprites.IsOnFloor(effect.Kind) != (pass == 0))
                        continue;

                    var frame = WeaponEffectSprites.GetFrame(_AnimationTime + effect.Id * WeaponEffectSprites.FRAME_SECONDS);
                    var source = new Rectangle(WeaponEffectSprites.GetCellX(frame), WeaponEffectSprites.GetCellY(effect.Kind),
                        WeaponEffectSprites.CELL_PIXELS, WeaponEffectSprites.CELL_PIXELS);
                    var center = _Camera.ToScreenPosition(CellToWorld(effect.X, effect.Y), viewport);
                    var origin = new Vector2(WeaponEffectSprites.CELL_PIXELS * 0.5f);
                    var scale = (float)CELL_PIXELS / WeaponEffectSprites.CELL_PIXELS;
                    var rotation = effect.Kind == eWeaponEffect.Sword ? WeaponEffectSprites.GetRotation(effect.Facing) : 0f;
                    var effects = effect.Kind == eWeaponEffect.Hammer && effect.Facing == eFacing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                    spriteBatch.Draw(_WeaponEffectSheet, center, source, Color.White, rotation, origin, scale, effects, 0f);
                }
            }
        }

        /// <summary>
        /// Draws every living ghost one cell wide, looking the way it's going, with its name below it. Each ghost uses
        /// the sheet recolored in LoadContent for its personality (ClientGameState.GetGhostPersonality), and its name is
        /// in the same color. On a rainbow level every ghost is drawn PacRules.RAINBOW_GHOST_SCALE times as wide, from the
        /// rainbow sheet for where it is (Rainbow.GetScrollPhase), with its name in white.
        /// </summary>
        private void DrawGhosts(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_State.Snapshot == null)
                return;

            var isRainbow = _State.LevelInfo != null && _State.LevelInfo.IsRainbow;
            var size = (int)(PacRules.GetGhostRadius(isRainbow) * 2f * CELL_PIXELS);
            var halfSize = size * 0.5f;

            foreach (var ghost in _State.Snapshot.Ghosts)
            {
                if (!ghost.Alive)
                    continue;

                var personality = _State.GetGhostPersonality(ghost.GhostId);
                var sheet = isRainbow
                    ? _RainbowGhostSheets[Rainbow.GetScrollPhase(ghost.X, ghost.Y, RAINBOW_GHOST_PHASES)]
                    : _GhostSheets[personality.Id];
                var frameWidth = sheet.Width / GHOST_FRAME_COUNT;
                var source = new Rectangle(GetGhostFrame(ghost.Facing) * frameWidth, 0, frameWidth, sheet.Height);
                var center = _Camera.ToScreenPosition(CellToWorld(ghost.X, ghost.Y), viewport);

                if (ghost.Stunned)
                {
                    // a stunned ghost spins in place
                    var origin = new Vector2(frameWidth * 0.5f, sheet.Height * 0.5f);
                    var scale = new Vector2((float)size / frameWidth, (float)size / sheet.Height);
                    spriteBatch.Draw(sheet, center, source, Color.White, PowerUpEffects.GetStunSpinAngle(_AnimationTime), origin, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    var bounds = new Rectangle((int)(center.X - halfSize), (int)(center.Y - halfSize), size, size);
                    spriteBatch.Draw(sheet, bounds, source, Color.White);
                }

                DrawName(spriteBatch, personality.Name, center, size, isRainbow ? Color.White : ClientColors.ToColor(personality.Color));
            }
        }

        /// <summary>
        /// Draws the eyes of every dead ghost (ClientGameState.Eyes) as big as a ghost, looking the way they move, with no name.
        /// Only eyes on the board are drawn.
        /// </summary>
        private void DrawGhostEyes(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_State.Eyes.Count == 0)
                return;

            var isRainbow = _State.LevelInfo != null && _State.LevelInfo.IsRainbow;
            var size = (int)(PacRules.GetGhostRadius(isRainbow) * 2f * CELL_PIXELS);
            var halfSize = size * 0.5f;
            var frameWidth = _GhostEyesSheet.Width / GHOST_FRAME_COUNT;
            var onBoard = new Rectangle(0, 0, viewport.Width, viewport.Height);

            foreach (var eyes in _State.Eyes)
            {
                var center = _Camera.ToScreenPosition(CellToWorld(eyes.X, eyes.Y), viewport);
                var bounds = new Rectangle((int)(center.X - halfSize), (int)(center.Y - halfSize), size, size);

                if (!onBoard.Intersects(bounds))
                    continue;

                var source = new Rectangle(GetGhostFrame(eyes.Facing) * frameWidth, 0, frameWidth, _GhostEyesSheet.Height);
                spriteBatch.Draw(_GhostEyesSheet, bounds, source, Color.White);
            }
        }

        /// <summary>
        /// Frame in a ghost sheet for a facing: the frames look right, down, left and up.
        /// </summary>
        public static int GetGhostFrame(eFacing facing)
        {
            return facing switch
            {
                eFacing.Right => 0,
                eFacing.Down => 1,
                eFacing.Left => 2,
                _ => 3,
            };
        }

        /// <summary>
        /// Mouth frame for a chomp that opens and closes: 0, 1, 2, 3, 2, 1, 0, ...
        /// </summary>
        public static int GetChompFrame(float time)
        {
            var cycle = PAC_FRAME_COUNT * 2 - 2;
            var step = (int)(time / PAC_FRAME_SECONDS) % cycle;
            return step < PAC_FRAME_COUNT ? step : cycle - step;
        }
    }
}
