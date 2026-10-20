using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EatMeClient
{
    /// <summary>
    /// Player name and team, then either a server address or a game found on the LAN.
    /// </summary>
    public class JoinGameScreen
    {
        private const float SEARCH_INTERVAL_SECONDS = 3f;
        private const float ITEM_SPACING = 20f;
        private const float LAN_HEADING_GAP = 60f;
        private const float HEADING_Y_RATIO = 0.1f;
        private const int MAX_LAN_GAMES = 5;
        private const int MAX_ADDRESS_LENGTH = 40;

        /// <summary>
        /// Team toggle choices, matching TEAM_OPTIONS by index.
        /// </summary>
        private static readonly eTeamPreference[] TEAM_PREFERENCES = [eTeamPreference.Either, eTeamPreference.Red, eTeamPreference.Blue];
        private static readonly string[] TEAM_OPTIONS = [Const.STRING_TEAM_EITHER, Const.STRING_TEAM_RED, Const.STRING_TEAM_BLUE];

        private enum eItemKind { Name, Team, Address, Join, LanGame, Back }

        private struct MenuItem
        {
            public eItemKind Kind { get; }
            public Rectangle Bounds { get; }
            public int LanIndex { get; }

            public MenuItem(eItemKind kind, Rectangle bounds, int lanIndex = -1)
            {
                Kind = kind;
                Bounds = bounds;
                LanIndex = lanIndex;
            }
        }

        private readonly Action<string, int> _OnJoin;
        private readonly Action _OnBack;
        private readonly ServerBrowser _Browser = new();

        private SpriteFont _Font;
        private SpriteFont _TitleFont;
        private TextInputControl _NameInput;
        private ToggleControl _TeamToggle;
        private TextInputControl _AddressInput;
        private IReadOnlyList<ServerListing> _LanGames = [];
        private int _SelectedIndex;
        private float _SearchTimer;
        private string _Error;

        /// <param name="onJoin">Called with host and port when the player picks a server.</param>
        public JoinGameScreen(Action<string, int> onJoin, Action onBack)
        {
            _OnJoin = onJoin;
            _OnBack = onBack;
        }

        public void LoadContent()
        {
            _Font = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC60);
            _TitleFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC96);
            _NameInput = new TextInputControl(_Font, Const.STRING_PLAYER_NAME, PlayerNameRules.MAX_LENGTH, PlayerNameRules.IsAllowedCharacter);
            _TeamToggle = new ToggleControl(_Font, Const.STRING_TEAM, TEAM_OPTIONS);
            _AddressInput = new TextInputControl(_Font, Const.STRING_SERVER_ADDRESS, MAX_ADDRESS_LENGTH, IsAddressCharacter);

            // name, team and address are one line each, labels in a shared column so the values line up
            var labelWidth = Math.Max(_NameInput.GetLabelColumnWidth(), Math.Max(_TeamToggle.GetLabelColumnWidth(), _AddressInput.GetLabelColumnWidth()));
            _NameInput.LabelWidth = labelWidth;
            _NameInput.ShowBox = true;
            _NameInput.PlayTypingSound = true;
            _TeamToggle.LabelWidth = labelWidth;
            _TeamToggle.IsInline = true;
            _AddressInput.LabelWidth = labelWidth;
        }

        public void Activate()
        {
            var settings = ConfigManager.Settings;
            _NameInput.Value = settings.PlayerName ?? string.Empty;
            _TeamToggle.SelectedIndex = Math.Max(0, Array.IndexOf(TEAM_PREFERENCES, settings.PreferredTeam));
            _AddressInput.Value = settings.ServerAddress ?? string.Empty;
            _SelectedIndex = 0;
            _Error = null;
            _LanGames = [];
            _SearchTimer = 0f;
            _Browser.Start();
        }

        /// <summary>
        /// Letters, digits and the punctuation found in host names, IPv4/IPv6 addresses and ports.
        /// </summary>
        public static bool IsAddressCharacter(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c == '.' || c == ':' || c == '-' || c == '[' || c == ']';
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            _SearchTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_SearchTimer >= SEARCH_INTERVAL_SECONDS)
            {
                _SearchTimer = 0f;
                _Browser.Search();
            }

            _LanGames = _Browser.Servers;

            if (InputManager.KeyPressed(Keys.Escape) || InputManager.GamePadButtonPressed(Buttons.B) || InputManager.GamePadButtonPressed(Buttons.Back))
            {
                ExitScreen();
                return;
            }

            var items = GetItems(viewport);
            _SelectedIndex = Math.Clamp(_SelectedIndex, 0, items.Count - 1);

            // arrows only, since W and S may be part of a name being typed
            if (InputManager.KeyPressed(Keys.Down) || InputManager.GamePadButtonPressed(Buttons.DPadDown) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickDown))
            {
                _SelectedIndex = (_SelectedIndex + 1) % items.Count;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            if (InputManager.KeyPressed(Keys.Up) || InputManager.GamePadButtonPressed(Buttons.DPadUp) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickUp))
            {
                _SelectedIndex = (_SelectedIndex - 1 + items.Count) % items.Count;
                SoundManager.PlaySound(Const.SFX_TICK);
            }

            var activate = InputManager.KeyPressed(Keys.Enter) || InputManager.GamePadButtonPressed(Buttons.A);

            // mouse
            var mousePoint = new Point(InputManager.MouseState.X, InputManager.MouseState.Y);
            var clicked = InputManager.MouseButtonPressed(InputManager.MouseButton.Left);

            for (var i = 0; i < items.Count; i++)
            {
                if (!items[i].Bounds.Contains(mousePoint))
                    continue;

                if (clicked || InputManager.MouseMoved())
                    _SelectedIndex = i;

                if (clicked)
                {
                    activate = true;
                    SoundManager.PlaySound(Const.SFX_TICK);
                }

                break;
            }

            var selected = items[_SelectedIndex];

            _NameInput.Update(gameTime, items[0].Bounds.Location.ToVector2(), selected.Kind == eItemKind.Name);
            _AddressInput.Update(gameTime, items[2].Bounds.Location.ToVector2(), selected.Kind == eItemKind.Address);

            if (selected.Kind == eItemKind.Team)
            {
                if (InputManager.KeyPressed(Keys.Right) || InputManager.GamePadButtonPressed(Buttons.DPadRight) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickRight))
                {
                    _TeamToggle.Next();
                    SoundManager.PlaySound(Const.SFX_TICK);
                }

                if (InputManager.KeyPressed(Keys.Left) || InputManager.GamePadButtonPressed(Buttons.DPadLeft) || InputManager.GamePadButtonPressed(Buttons.LeftThumbstickLeft))
                {
                    _TeamToggle.Previous();
                    SoundManager.PlaySound(Const.SFX_TICK);
                }

                if (activate)
                    _TeamToggle.Next();
            }

            if (!activate)
                return;

            switch (selected.Kind)
            {
                case eItemKind.Join:
                    TryJoin(_AddressInput.Value);
                    break;

                case eItemKind.LanGame:
                    var listing = _LanGames[selected.LanIndex];

                    if (listing.CanJoin())
                        TryJoin(listing.Address);
                    else
                        _Error = Const.STRING_REJECT_VERSION;
                    break;

                case eItemKind.Back:
                    ExitScreen();
                    break;
            }
        }

        private void TryJoin(string address)
        {
            if (!PlayerNameRules.IsValid(_NameInput.Value))
            {
                _Error = Const.STRING_INVALID_NAME;
                return;
            }

            if (!ServerAddress.TryParse(address, out var host, out var port))
            {
                _Error = Const.STRING_INVALID_ADDRESS;
                return;
            }

            SaveSettings();
            _Browser.Stop();
            _OnJoin(host, port);
        }

        private void ExitScreen()
        {
            SaveSettings();
            _Browser.Stop();
            _OnBack();
        }

        private void SaveSettings()
        {
            var settings = ConfigManager.Settings;
            settings.PlayerName = _NameInput.Value;
            settings.PreferredTeam = TEAM_PREFERENCES[_TeamToggle.SelectedIndex];
            settings.ServerAddress = _AddressInput.Value;
            ConfigManager.Save();
        }

        private float GetHeadingBottom(Viewport viewport)
        {
            var headingSize = _TitleFont.MeasureString(Const.STRING_JOIN_GAME);
            return viewport.Height * HEADING_Y_RATIO + headingSize.Y;
        }

        private Rectangle CenteredBounds(Viewport viewport, float y, float width, float height)
        {
            return new Rectangle((int)((viewport.Width - width) / 2f), (int)y, (int)width, (int)height);
        }

        /// <summary>
        /// Every selectable item top to bottom, with where it is drawn.
        /// </summary>
        private List<MenuItem> GetItems(Viewport viewport)
        {
            var items = new List<MenuItem>();
            var y = GetHeadingBottom(viewport) + _Font.LineSpacing * 2f;

            void Add(eItemKind kind, float width, float height, int lanIndex = -1)
            {
                items.Add(new MenuItem(kind, CenteredBounds(viewport, y, width, height), lanIndex));
                y += height + ITEM_SPACING;
            }

            // the form rows share a left edge so their labels and values line up; the name box is the widest
            void AddFormRow(eItemKind kind, float width, float height)
            {
                var left = (viewport.Width - _NameInput.GetWidth()) / 2f;
                items.Add(new MenuItem(kind, new Rectangle((int)left, (int)y, (int)width, (int)height)));
                y += height + ITEM_SPACING;
            }

            AddFormRow(eItemKind.Name, _NameInput.GetWidth(), _NameInput.GetHeight());
            AddFormRow(eItemKind.Team, _TeamToggle.GetWidth(), _TeamToggle.GetHeight());
            AddFormRow(eItemKind.Address, _AddressInput.GetWidth(), _AddressInput.GetHeight());
            Add(eItemKind.Join, _Font.MeasureString(Const.STRING_JOIN).X, _Font.LineSpacing);

            // room for the LAN heading, with a wide gap above it
            y += LAN_HEADING_GAP + _Font.LineSpacing + ITEM_SPACING;

            var lanCount = Math.Min(_LanGames.Count, MAX_LAN_GAMES);
            for (var i = 0; i < lanCount; i++)
                Add(eItemKind.LanGame, _Font.MeasureString(GetLanGameText(_LanGames[i])).X, _Font.LineSpacing, i);

            if (lanCount == 0)
                y += _Font.LineSpacing + ITEM_SPACING;

            y += ITEM_SPACING;
            Add(eItemKind.Back, _Font.MeasureString(Const.STRING_BACK).X, _Font.LineSpacing);

            return items;
        }

        private static string GetLanGameText(ServerListing listing)
        {
            return $"{listing.GetDisplayName()} {listing.Info.PlayerCount}/{listing.Info.MaxPlayers}";
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            spriteBatch.Begin();

            var headingSize = _TitleFont.MeasureString(Const.STRING_JOIN_GAME);
            var headingPosition = new Vector2((viewport.Width - headingSize.X) / 2f, viewport.Height * HEADING_Y_RATIO);
            TextHelper.DrawShadowedString(spriteBatch, _TitleFont, Const.STRING_JOIN_GAME, headingPosition, ClientColors.COLOR_DARK_TITLE_ITEM, ClientColors.COLOR_DARK_TITLE_SHADOW);

            var items = GetItems(viewport);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isSelected = i == _SelectedIndex;
                var position = item.Bounds.Location.ToVector2();
                var color = isSelected ? ClientColors.COLOR_DARK_MENU_SELECTED : ClientColors.COLOR_DARK_MENU_ITEM;

                switch (item.Kind)
                {
                    case eItemKind.Name:
                        _NameInput.Draw(spriteBatch, position, isSelected);
                        break;

                    case eItemKind.Team:
                        _TeamToggle.Draw(spriteBatch, position, isSelected);
                        break;

                    case eItemKind.Address:
                        _AddressInput.Draw(spriteBatch, position, isSelected);
                        break;

                    case eItemKind.Join:
                        TextHelper.DrawShadowedString(spriteBatch, _Font, Const.STRING_JOIN, position, color);
                        DrawLanHeading(spriteBatch, viewport, item.Bounds.Bottom + ITEM_SPACING + LAN_HEADING_GAP);
                        break;

                    case eItemKind.LanGame:
                        var listing = _LanGames[item.LanIndex];

                        // a server on another version is listed but greyed out, since it can't be joined
                        if (!isSelected && !listing.CanJoin())
                            color = ClientColors.COLOR_DARK_MENU_HINT;

                        TextHelper.DrawShadowedString(spriteBatch, _Font, GetLanGameText(listing), position, color);
                        break;

                    case eItemKind.Back:
                        TextHelper.DrawShadowedString(spriteBatch, _Font, Const.STRING_BACK, position, color);

                        if (_Error != null)
                        {
                            var errorBounds = GetErrorBounds(viewport, item.Bounds);
                            spriteBatch.DrawString(_Font, _Error, errorBounds.Location.ToVector2(), ClientColors.COLOR_PAC_RED);
                        }
                        break;
                }
            }

            spriteBatch.End();
        }

        /// <summary>
        /// Where the error line goes, just under Back.
        /// </summary>
        private Rectangle GetErrorBounds(Viewport viewport, Rectangle backBounds)
        {
            var errorSize = _Font.MeasureString(_Error);
            return CenteredBounds(viewport, backBounds.Bottom + ITEM_SPACING * 2f, errorSize.X, errorSize.Y);
        }

        private void DrawLanHeading(SpriteBatch spriteBatch, Viewport viewport, float y)
        {
            var size = _Font.MeasureString(Const.STRING_LAN_GAMES);
            TextHelper.DrawShadowedString(spriteBatch, _Font, Const.STRING_LAN_GAMES, new Vector2((viewport.Width - size.X) / 2f, y), ClientColors.COLOR_DARK_TITLE_ITEM, ClientColors.COLOR_DARK_TITLE_SHADOW);

            if (_LanGames.Count > 0)
                return;

            var searchingSize = _Font.MeasureString(Const.STRING_SEARCHING);
            spriteBatch.DrawString(_Font, Const.STRING_SEARCHING, new Vector2((viewport.Width - searchingSize.X) / 2f, y + _Font.LineSpacing + ITEM_SPACING), ClientColors.COLOR_DARK_MENU_HINT);
        }
    }
}
