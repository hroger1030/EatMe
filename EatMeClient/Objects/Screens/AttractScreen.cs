using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EatMeClient
{
    /// <summary>
    /// Shown after the title menu has sat idle (AttractMath.IDLE_MS): on black, the story, every ghost and every
    /// power up (alphabetically: sprite and name side by side, centred, and a short description under them across the
    /// centre third of the screen), and the two teams' pacs, scrolling up slowly
    /// until the bottom is on screen, then back to the title after AttractMath.HOLD_MS. Any key, gamepad button
    /// or mouse button goes back at once.
    /// </summary>
    public class AttractScreen
    {
        /// <summary>
        /// Width of the story text column, as a share of the screen.
        /// </summary>
        private const float STORY_WIDTH_RATIO = 0.5f;

        /// <summary>
        /// Space above the first title and below the pacs, and between sections, in title font lines.
        /// </summary>
        private const float SECTION_GAP_LINES = 0.75f;

        /// <summary>
        /// The ghost and power up descriptions span the middle of the screen, from LIST_LEFT_RATIO to LIST_RIGHT_RATIO
        /// of its width (the centre third).
        /// </summary>
        private const float LIST_LEFT_RATIO = 1f / 3f;
        private const float LIST_RIGHT_RATIO = 2f / 3f;

        /// <summary>
        /// Size of a ghost or power up sprite in the lists, in pixels.
        /// </summary>
        private const int ICON_PIXELS = 72;

        /// <summary>
        /// Space between a list entry's sprite and its name, in pixels.
        /// </summary>
        private const int ICON_GAP = 24;

        /// <summary>
        /// Space after each entry's description, before the next entry, in description font lines.
        /// </summary>
        private const float ENTRY_GAP_LINES = 1f;

        /// <summary>
        /// Size of each starring pac and the gap between the two, in pixels.
        /// </summary>
        private const int STAR_PAC_PIXELS = 192;
        private const int STAR_PAC_GAP = 128;

        private readonly Action _OnDone;
        private SpriteFont _TitleFont;
        private SpriteFont _StoryFont;
        private SpriteFont _NameFont;
        private SpriteFont _BlurbFont;
        private Texture2D _PacSheet;
        private Texture2D _PowerUpSheet;
        private Texture2D[] _GhostSheets;
        private List<GhostPersonality> _Ghosts;
        private List<ePowerUp> _PowerUps;
        private List<string> _StoryLines;
        private float _StoryWidth = -1f;
        private readonly Dictionary<string, List<string>> _BlurbLines = [];
        private float _BlurbWidth = -1f;
        private TimeSpan _Time;

        public AttractScreen(Action onDone)
        {
            _OnDone = onDone;
        }

        public void LoadContent()
        {
            _TitleFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC96);
            _StoryFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC36);
            _NameFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC48);
            _BlurbFont = AssetCache.GetFont(Const.FONT_JLSSPACEGOTHIC30);
            _PacSheet = AssetCache.GetTexture(Const.IMAGE_PAC_SHEET);
            _PowerUpSheet = AssetCache.GetTexture(Const.IMAGE_POWER_UP_SHEET);

            var whiteGhost = AssetCache.GetTexture(Const.IMAGE_GHOST);
            _GhostSheets = new Texture2D[GhostPersonalities.ALL.Count];
            foreach (var personality in GhostPersonalities.ALL)
                _GhostSheets[personality.Id] = ImageFactory.GenerateGhostSheet(whiteGhost, ClientColors.ToColor(personality.Color));

            _Ghosts = AttractMath.GetGhostOrder();
            _PowerUps = AttractMath.GetPowerUpOrder();
        }

        /// <summary>
        /// Starts again from the top.
        /// </summary>
        public void Activate()
        {
            _Time = TimeSpan.Zero;
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            if (InputManager.AnyButtonPressed())
            {
                _OnDone();
                return;
            }

            _Time += gameTime.ElapsedGameTime;

            if (AttractMath.IsFinished(_Time, Layout(null, viewport, 0f), viewport.Height))
                _OnDone();
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            var offset = AttractMath.GetScrollOffset(_Time, Layout(null, viewport, 0f), viewport.Height);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Layout(spriteBatch, viewport, -offset);
            spriteBatch.End();
        }

        /// <summary>
        /// Lays the content out from top down and draws it when spriteBatch isn't null (only what is on screen).
        /// Returns the content's height.
        /// </summary>
        private float Layout(SpriteBatch spriteBatch, Viewport viewport, float top)
        {
            var gap = _TitleFont.LineSpacing * SECTION_GAP_LINES;
            var y = top + gap;

            // the story
            y = LayoutTitle(spriteBatch, viewport, Const.STRING_STORY, y);
            var storyWidth = viewport.Width * STORY_WIDTH_RATIO;
            var storyLeft = (viewport.Width - storyWidth) / 2f;

            foreach (var line in GetStoryLines(storyWidth))
            {
                if (spriteBatch != null && IsOnScreen(viewport, y, _StoryFont.LineSpacing))
                    TextHelper.DrawShadowedString(spriteBatch, _StoryFont, line, new Vector2(storyLeft, y), ClientColors.COLOR_DARK_MENU_ITEM);

                y += _StoryFont.LineSpacing;
            }

            // the ghosts
            y = LayoutTitle(spriteBatch, viewport, Const.STRING_ENEMIES, y + gap);

            foreach (var ghost in _Ghosts)
            {
                var sheet = _GhostSheets[ghost.Id];
                var frameWidth = sheet.Width / MultiPlayerGameScreen.GHOST_FRAME_COUNT;
                var source = new Rectangle(MultiPlayerGameScreen.GetGhostFrame(eFacing.Right) * frameWidth, 0, frameWidth, sheet.Height);
                y = LayoutEntry(spriteBatch, viewport, sheet, source, ghost.Name, ClientColors.ToColor(ghost.Color), AttractBlurbs.GetGhostBlurb(ghost.Behavior), y);
            }

            // the power ups
            y = LayoutTitle(spriteBatch, viewport, Const.STRING_ATTRACT_POWER_UPS, y + gap);

            foreach (var item in _PowerUps)
            {
                var source = new Rectangle(PowerUpSprites.GetCellX(item), PowerUpSprites.GetCellY(item), PowerUpSprites.CELL_PIXELS, PowerUpSprites.CELL_PIXELS);
                y = LayoutEntry(spriteBatch, viewport, _PowerUpSheet, source, PowerUpNames.GetName(item), ClientColors.COLOR_DARK_MENU_ITEM, AttractBlurbs.GetPowerUpBlurb(item), y);
            }

            // the stars: blue on the left facing right, red on the right facing left
            y = LayoutTitle(spriteBatch, viewport, Const.STRING_STARRING, y + gap);

            if (spriteBatch != null && IsOnScreen(viewport, y, STAR_PAC_PIXELS))
            {
                var source = MultiPlayerGameScreen.GetPacSource(_PacSheet, PacShapes.DEFAULT, MultiPlayerGameScreen.GetChompFrame(_Time));
                var left = (viewport.Width - STAR_PAC_PIXELS * 2 - STAR_PAC_GAP) / 2;

                spriteBatch.Draw(_PacSheet, new Rectangle(left, (int)y, STAR_PAC_PIXELS, STAR_PAC_PIXELS), source, ClientColors.COLOR_PAC_BLUE);
                spriteBatch.Draw(_PacSheet, new Rectangle(left + STAR_PAC_PIXELS + STAR_PAC_GAP, (int)y, STAR_PAC_PIXELS, STAR_PAC_PIXELS), source,
                    ClientColors.COLOR_PAC_RED, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
            }

            y += STAR_PAC_PIXELS + gap;
            return y - top;
        }

        /// <summary>
        /// A section title, centred in the screen title color with its white shadow. Returns the y below it.
        /// </summary>
        private float LayoutTitle(SpriteBatch spriteBatch, Viewport viewport, string title, float y)
        {
            if (spriteBatch != null && IsOnScreen(viewport, y, _TitleFont.LineSpacing))
            {
                var size = _TitleFont.MeasureString(title);
                TextHelper.DrawShadowedString(spriteBatch, _TitleFont, title, new Vector2((viewport.Width - size.X) / 2f, y), ClientColors.COLOR_DARK_TITLE_ITEM, ClientColors.COLOR_DARK_TITLE_SHADOW);
            }

            return y + _TitleFont.LineSpacing;
        }

        /// <summary>
        /// One list entry: the sprite with the name ICON_GAP to its right, the pair centred on the screen on one row,
        /// then the description under them in the small font, wrapped to the centre third of the screen and centred.
        /// Returns the y below it.
        /// </summary>
        private float LayoutEntry(SpriteBatch spriteBatch, Viewport viewport, Texture2D sheet, Rectangle source, string name, Color nameColor, string blurb, float y)
        {
            var left = viewport.Width * LIST_LEFT_RATIO;
            var right = viewport.Width * LIST_RIGHT_RATIO;
            var rowHeight = Math.Max(ICON_PIXELS, _NameFont.LineSpacing);

            if (spriteBatch != null && IsOnScreen(viewport, y, rowHeight))
            {
                var nameWidth = _NameFont.MeasureString(name).X;
                var iconX = (viewport.Width - ICON_PIXELS - ICON_GAP - nameWidth) / 2f;
                var iconY = y + (rowHeight - ICON_PIXELS) / 2f;
                spriteBatch.Draw(sheet, new Rectangle((int)iconX, (int)iconY, ICON_PIXELS, ICON_PIXELS), source, Color.White);

                var nameX = iconX + ICON_PIXELS + ICON_GAP;
                var nameY = y + (rowHeight - _NameFont.LineSpacing) / 2f;
                TextHelper.DrawShadowedString(spriteBatch, _NameFont, name, new Vector2(nameX, nameY), nameColor);
            }

            y += rowHeight;

            foreach (var line in GetBlurbLines(blurb, right - left))
            {
                if (spriteBatch != null && IsOnScreen(viewport, y, _BlurbFont.LineSpacing))
                {
                    var lineX = left + (right - left - _BlurbFont.MeasureString(line).X) / 2f;
                    TextHelper.DrawShadowedString(spriteBatch, _BlurbFont, line, new Vector2(lineX, y), ClientColors.COLOR_PAC_LIGHT_GREY);
                }

                y += _BlurbFont.LineSpacing;
            }

            return y + _BlurbFont.LineSpacing * ENTRY_GAP_LINES;
        }

        /// <summary>
        /// A description wrapped to width, kept until the width changes.
        /// </summary>
        private List<string> GetBlurbLines(string blurb, float width)
        {
            if (Math.Abs(_BlurbWidth - width) > 0.5f)
            {
                _BlurbLines.Clear();
                _BlurbWidth = width;
            }

            if (!_BlurbLines.TryGetValue(blurb, out var lines))
            {
                lines = TextWrap.Wrap(blurb, width, line => _BlurbFont.MeasureString(line).X);
                _BlurbLines[blurb] = lines;
            }

            return lines;
        }

        /// <summary>
        /// The story wrapped to width, kept until the width changes.
        /// </summary>
        private List<string> GetStoryLines(float width)
        {
            if (_StoryLines == null || Math.Abs(_StoryWidth - width) > 0.5f)
            {
                _StoryLines = TextWrap.Wrap(AssetCache.GetText(Const.TEXT_STORY), width, line => _StoryFont.MeasureString(line).X);
                _StoryWidth = width;
            }

            return _StoryLines;
        }

        private static bool IsOnScreen(Viewport viewport, float y, float height)
        {
            return y + height >= 0f && y <= viewport.Height;
        }
    }
}
