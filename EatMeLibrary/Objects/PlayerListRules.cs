using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Rules for the player list the client shows while TAB (or a gamepad shoulder button) is held.
    /// </summary>
    public static class PlayerListRules
    {
        /// <summary>
        /// The players on one team in alphabetical order, ignoring case (then by id, so equal names keep a fixed order).
        /// </summary>
        public static List<PlayerListEntry> SortTeam(IEnumerable<PlayerListEntry> entries, eTeam team)
        {
            var sorted = new List<PlayerListEntry>();

            foreach (var entry in entries)
                if (entry.Team == team)
                    sorted.Add(entry);

            sorted.Sort((a, b) =>
            {
                var byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.PlayerId.CompareTo(b.PlayerId);
            });
            return sorted;
        }

        /// <summary>
        /// What the list shows in the ping column: "&lt;bot&gt;" for a bot, "--" before the first measurement, else "n ms".
        /// </summary>
        public static string GetPingText(PlayerListEntry entry)
        {
            if (entry.IsBot)
                return Const.STRING_PLAYER_LIST_BOT;

            return entry.PingMs < 0 ? Const.STRING_PING_UNKNOWN : $"{entry.PingMs} {Const.STRING_MS}";
        }

        /// <summary>
        /// How many columns a team of count players needs when only maxRows lines fit in one: one column if they fit (or
        /// the team is empty), else as many as it takes.
        /// </summary>
        public static int GetColumnCount(int count, int maxRows)
        {
            if (count <= 0)
                return 1;

            maxRows = Math.Max(maxRows, 1);
            return (count + maxRows - 1) / maxRows;
        }

        /// <summary>
        /// Lines in each of columns columns holding count players, filled evenly top to bottom then left to right.
        /// </summary>
        public static int GetRowCount(int count, int columns)
        {
            if (count <= 0)
                return 0;

            columns = Math.Max(columns, 1);
            return (count + columns - 1) / columns;
        }

        /// <summary>
        /// A team's heading with its player count, e.g. "Blue (4)".
        /// </summary>
        public static string GetHeading(eTeam team, int count)
        {
            return $"{(team == eTeam.Blue ? Const.STRING_TEAM_BLUE : Const.STRING_TEAM_RED)} ({count})";
        }
    }
}
