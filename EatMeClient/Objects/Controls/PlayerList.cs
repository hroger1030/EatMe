using System;
using System.Collections.Generic;
using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// The player list shown while TAB (or a gamepad shoulder button) is held: every player and bot, blue on the left and
    /// red on the right, each team under its own heading in alphabetical order with its ping (or "&lt;bot&gt;") right aligned,
    /// in the team's color (the player's own line in its lighter own shade), on a half-transparent panel with rounded corners
    /// centred in bounds. A team too long for one column is split into as many columns as it needs, side by side, filled
    /// top to bottom then left to right.
    /// </summary>
    public class PlayerList
    {
        private const int PADDING_PIXELS = 24;
        private const int TEAM_GAP_PIXELS = 64;
        private const int COLUMN_GAP_PIXELS = 40;
        private const int PING_GAP_PIXELS = 32;
        private const int HEADING_GAP_PIXELS = 8;

        private readonly SpriteFont _font;
        private Texture2D _pixel;

        public PlayerList(SpriteFont font)
        {
            _font = font;
        }

        /// <summary>
        /// Draws the list centred in bounds. ownId is the player's own id, drawn in its own team shade.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, List<PlayerListEntry> blue, List<PlayerListEntry> red, int ownId)
        {
            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            var lineHeight = _font.LineSpacing;
            var headingHeight = lineHeight + HEADING_GAP_PIXELS;
            var maxRows = Math.Max((bounds.Height - PADDING_PIXELS * 2 - headingHeight) / lineHeight, 1);

            var blueColumns = PlayerListRules.GetColumnCount(blue.Count, maxRows);
            var redColumns = PlayerListRules.GetColumnCount(red.Count, maxRows);
            var blueRows = PlayerListRules.GetRowCount(blue.Count, blueColumns);
            var redRows = PlayerListRules.GetRowCount(red.Count, redColumns);

            // every column as wide as the widest line, so the panel is balanced
            var columnWidth = Math.Max(GetColumnWidth(blue, eTeam.Blue), GetColumnWidth(red, eTeam.Red));
            var blueWidth = GetTeamWidth(blueColumns, columnWidth);
            var redWidth = GetTeamWidth(redColumns, columnWidth);

            var width = PADDING_PIXELS * 2 + blueWidth + TEAM_GAP_PIXELS + redWidth;
            var height = PADDING_PIXELS * 2 + headingHeight + Math.Max(blueRows, redRows) * lineHeight;

            var panel = new Rectangle(bounds.X + (bounds.Width - width) / 2, bounds.Y + (bounds.Height - height) / 2, width, height);
            TextureHelper.FillRounded(spriteBatch, _pixel, panel, MiniMap.CORNER_RADIUS, ClientColors.COLOR_TEXT_PANEL);

            var top = panel.Y + PADDING_PIXELS;
            var blueX = panel.X + PADDING_PIXELS;
            DrawTeam(spriteBatch, blue, eTeam.Blue, blueX, top, columnWidth, blueRows, ownId);
            DrawTeam(spriteBatch, red, eTeam.Red, blueX + blueWidth + TEAM_GAP_PIXELS, top, columnWidth, redRows, ownId);
        }

        /// <summary>
        /// Draws one team's heading and its players, rows to a column, columns left to right from x.
        /// </summary>
        private void DrawTeam(SpriteBatch spriteBatch, List<PlayerListEntry> entries, eTeam team, int x, int y, int columnWidth, int rows, int ownId)
        {
            TextHelper.DrawShadowedString(spriteBatch, _font, PlayerListRules.GetHeading(team, entries.Count), new Vector2(x, y), ClientColors.GetTeamColor(team));
            var firstRowY = y + _font.LineSpacing + HEADING_GAP_PIXELS;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var columnX = x + i / rows * (columnWidth + COLUMN_GAP_PIXELS);
                var rowY = firstRowY + i % rows * _font.LineSpacing;
                var color = ClientColors.GetPacColor(team, entry.PlayerId == ownId);
                TextHelper.DrawShadowedString(spriteBatch, _font, entry.Name, new Vector2(columnX, rowY), color);

                var ping = PlayerListRules.GetPingText(entry);
                var pingX = columnX + columnWidth - _font.MeasureString(ping).X;
                TextHelper.DrawShadowedString(spriteBatch, _font, ping, new Vector2((float)Math.Round(pingX), rowY), color);
            }
        }

        /// <summary>
        /// Widest name and ping pair in a team; the heading too, which only needs to fit the team's first column.
        /// </summary>
        private int GetColumnWidth(List<PlayerListEntry> entries, eTeam team)
        {
            var width = _font.MeasureString(PlayerListRules.GetHeading(team, entries.Count)).X;

            foreach (var entry in entries)
                width = Math.Max(width, _font.MeasureString(entry.Name).X + PING_GAP_PIXELS + _font.MeasureString(PlayerListRules.GetPingText(entry)).X);

            return (int)Math.Ceiling(width);
        }

        private static int GetTeamWidth(int columns, int columnWidth)
        {
            return columns * columnWidth + (columns - 1) * COLUMN_GAP_PIXELS;
        }
    }
}
