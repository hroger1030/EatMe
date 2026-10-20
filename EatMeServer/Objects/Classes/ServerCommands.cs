using System;
using System.Collections.Generic;
using System.Text;

namespace EatMeServer
{
    /// <summary>
    /// Commands typed into the server's console. Execute runs one line against the game and returns the text to
    /// print. Like GameServer.HandleEvent it must be called on the main loop's thread. Nothing a command changes is
    /// written back to server.cfg.
    /// </summary>
    public static class ServerCommands
    {
        public const string HELP = "help";
        public const string STATUS = "status";
        public const string PLAYERS = "players";
        public const string NEXT_LEVEL = "next level";
        public const string LEVEL = "level";
        public const string RESTART = "restart";
        public const string NEW_GAME = "new game";
        public const string KICK = "kick";
        public const string ADD_BOT = "add bot";
        public const string REMOVE_BOT = "remove bot";
        public const string BOTS = "bots";
        public const string AUTO_BALANCE = "autobalance";
        public const string MOTD = "motd";
        public const string SETTINGS = "settings";
        public const string QUIT = "quit";

        /// <summary>
        /// Every command with its usage and what it does, in the order help lists them.
        /// </summary>
        public static readonly string[,] COMMANDS =
        {
            { HELP, "help", "List the commands" },
            { STATUS, "status", "Level, scores and player counts" },
            { PLAYERS, "players", "Every player with their id, team, scores and lag" },
            { NEXT_LEVEL, "next level", "Start the next level now" },
            { LEVEL, "level {n}", "Jump to level n" },
            { RESTART, "restart", "Restart the current level" },
            { NEW_GAME, "new game", "Go back to the starting level" },
            { KICK, "kick {name or id}", "Remove a player from the server" },
            { ADD_BOT, "add bot [n]", "Keep n more players (default 1; per team with autobalance on)" },
            { REMOVE_BOT, "remove bot [n]", "Keep n fewer players (default 1; per team with autobalance on)" },
            { BOTS, "bots [n]", "Show or set MinPlayers, the players (humans plus bots) the server keeps" },
            { AUTO_BALANCE, "autobalance [on|off]", "Show or set whether bots keep the teams the same size" },
            { MOTD, "motd [text]", "Show or set the message of the day for players who join from now on" },
            { SETTINGS, "settings", "Every server.cfg setting with its command and value" },
            { QUIT, "quit", "Stop the server" },
        };

        /// <summary>
        /// Other spellings, each with the command it stands for.
        /// </summary>
        public static readonly Dictionary<string, string> ALIASES = new()
        {
            { "?", HELP },
            { "next", NEXT_LEVEL },
            { "nextlevel", NEXT_LEVEL },
            { "newgame", NEW_GAME },
            { "addbot", ADD_BOT },
            { "removebot", REMOVE_BOT },
            { "minplayers", BOTS },
            { "autobalanceteams", AUTO_BALANCE },
            { "messageoftheday", MOTD },
            { "stop", QUIT },
            { "exit", QUIT },
        };

        public const string UNKNOWN = "Unknown command, type help for a list";

        /// <summary>
        /// Splits a line into its command (the canonical name, lower case, from COMMANDS or ALIASES; a two word
        /// command wins over a one word one), or a setting command from SettingCommands, and the rest of the line, trimmed and with its case kept. Null for a
        /// blank line or one that starts with no known command.
        /// </summary>
        public static string Parse(string line, out string argument)
        {
            argument = string.Empty;
            var text = line?.Trim() ?? string.Empty;
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return null;

            if (words.Length > 1)
            {
                var twoWords = FindCommand(words[0] + " " + words[1]);

                if (twoWords != null)
                {
                    argument = SkipWords(text, 2);
                    return twoWords;
                }
            }

            var oneWord = FindCommand(words[0]);

            if (oneWord != null)
                argument = SkipWords(text, 1);

            return oneWord;
        }

        /// <summary>
        /// Runs one typed line and returns what to print. quit is true when the server should stop.
        /// </summary>
        public static string Execute(GameServer game, string line, out bool quit)
        {
            quit = false;
            var command = Parse(line, out var argument);

            switch (command)
            {
                case null:
                    return string.IsNullOrWhiteSpace(line) ? string.Empty : UNKNOWN;

                case HELP:
                    return GetHelp();

                case STATUS:
                    return GetStatus(game);

                case PLAYERS:
                    return GetPlayers(game);

                case NEXT_LEVEL:
                    game.StartLevel(game.CurrentLevel.Level + 1);
                    return string.Empty;

                case LEVEL:
                    if (!int.TryParse(argument, out var level) || level < 1)
                        return "Usage: level {n}, n 1 or more";

                    game.StartLevel(level);
                    return string.Empty;

                case RESTART:
                    game.StartLevel(game.CurrentLevel.Level);
                    return string.Empty;

                case NEW_GAME:
                    game.StartLevel(ConfigManager.ClampStartingLevel(game.Settings.StartingLevel));
                    return string.Empty;

                case KICK:
                    return Kick(game, argument);

                case ADD_BOT:
                case REMOVE_BOT:
                    return ChangeBots(game, argument, command == ADD_BOT);

                case BOTS:
                    if (argument.Length == 0)
                        return GetBotsText(game);

                    if (!int.TryParse(argument, out var minPlayers) || minPlayers < 0)
                        return "Usage: bots {n}, n 0 or more";

                    game.SetMinPlayers(minPlayers);
                    return GetBotsText(game);

                case AUTO_BALANCE:
                    if (argument.Length > 0)
                    {
                        if (!TryParseOnOff(argument, out var on))
                            return "Usage: autobalance on|off";

                        game.SetAutoBalanceTeams(on);
                    }

                    return $"Autobalance {(game.Settings.AutoBalanceTeams ? "on" : "off")}. {GetBotsText(game)}";

                case MOTD:
                    if (argument.Length > 0)
                        game.Settings.MessageOfTheDay = argument;

                    return $"MOTD: {ConfigManager.GetMessageOfTheDay(game.Settings.MessageOfTheDay)}";

                case SETTINGS:
                    return FormatTable(SettingCommands.GetSettingRows(game.Settings), [false, false, false]);

                case QUIT:
                    quit = true;
                    return "Stopping";

                default:
                    return SettingCommands.Execute(game, command, argument) ?? UNKNOWN;
            }
        }

        /// <summary>
        /// True with the value for on/off, true/false, yes/no or 1/0, ignoring case.
        /// </summary>
        public static bool TryParseOnOff(string text, out bool value)
        {
            switch (text?.Trim().ToLowerInvariant())
            {
                case "on":
                case "true":
                case "yes":
                case "1":
                    value = true;
                    return true;

                case "off":
                case "false":
                case "no":
                case "0":
                    value = false;
                    return true;

                default:
                    value = false;
                    return false;
            }
        }

        public static string GetHelp()
        {
            var width = 0;

            for (var i = 0; i < COMMANDS.GetLength(0); i++)
                width = Math.Max(width, COMMANDS[i, 1].Length);

            for (var i = 0; i < SettingCommands.COMMANDS.GetLength(0); i++)
                width = Math.Max(width, SettingCommands.COMMANDS[i, 1].Length);

            var text = new StringBuilder("Commands:");

            for (var i = 0; i < COMMANDS.GetLength(0); i++)
                text.Append($"\n  {COMMANDS[i, 1].PadRight(width)}  {COMMANDS[i, 2]}");

            text.Append("\nSettings (alone to show, with a value to change; not saved to server.cfg):");

            for (var i = 0; i < SettingCommands.COMMANDS.GetLength(0); i++)
                text.Append($"\n  {SettingCommands.COMMANDS[i, 1].PadRight(width)}  {SettingCommands.COMMANDS[i, 2]}");

            return text.ToString();
        }

        public static string GetStatus(GameServer game)
        {
            var level = game.CurrentLevel;
            var state = game.IsPaused ? "paused" : game.IsIntermission ? "intermission" : "playing";

            // blue on the left, red on the right, as everywhere else
            return $"Level {level.Level} {level.Name} ({state}), Blue {game.BlueScore}, Red {game.RedScore} of {game.Settings.GameConfig.MaxScore}\n"
                + $"{game.PlayerCount} players, {game.BotCount} bots. {GetBotsText(game)}";
        }

        public static string GetPlayers(GameServer game)
        {
            if (game.Players.Count == 0)
                return "No players";

            var rows = new List<string[]> { new[] { "Id", "Name", "Team", "Type", "Score", "Total", "Lag" } };

            foreach (var serverPlayer in game.Players)
            {
                var player = serverPlayer.Player;
                var lagMs = serverPlayer.Lag.SmoothedMs;
                var lag = serverPlayer.IsBot || lagMs < 0 ? "--" : $"{lagMs}ms";
                rows.Add(new[] { player.Id.ToString(), player.Name, player.Team.ToString(), serverPlayer.IsBot ? "Bot" : "Human",
                    player.Score.ToString(), player.TotalScore.ToString(), lag });
            }

            // id, name, team and type on the left; the numbers on the right
            return FormatTable(rows, [false, false, false, false, true, true, true]);
        }

        /// <summary>
        /// Lines up rows of cells in columns as wide as their widest cell, two spaces apart, each cell padded on
        /// the left where rightAligned says so and on the right otherwise. One line per row, no trailing spaces.
        /// </summary>
        public static string FormatTable(List<string[]> rows, bool[] rightAligned)
        {
            var widths = new int[rightAligned.Length];

            foreach (var row in rows)
                for (var column = 0; column < row.Length; column++)
                    widths[column] = Math.Max(widths[column], row[column].Length);

            var text = new StringBuilder();

            foreach (var row in rows)
            {
                var line = new StringBuilder();

                for (var column = 0; column < row.Length; column++)
                {
                    if (column > 0)
                        line.Append("  ");

                    line.Append(rightAligned[column] ? row[column].PadLeft(widths[column]) : row[column].PadRight(widths[column]));
                }

                if (text.Length > 0)
                    text.Append('\n');

                text.Append(line.ToString().TrimEnd());
            }

            return text.ToString();
        }

        private static string Kick(GameServer game, string nameOrId)
        {
            if (nameOrId.Length == 0)
                return "Usage: kick {name or id}";

            var found = game.FindPlayers(nameOrId);
            found.RemoveAll(serverPlayer => serverPlayer.IsBot);

            if (found.Count == 0)
                return game.FindPlayers(nameOrId).Count > 0 ? $"{nameOrId} is a bot; use remove bot or bots" : $"No player {nameOrId}";

            if (found.Count > 1)
            {
                var ids = new List<string>();
                foreach (var serverPlayer in found)
                    ids.Add(serverPlayer.Player.Id.ToString());

                return $"{found.Count} players are called {nameOrId}; kick by id: {string.Join(", ", ids)}";
            }

            var name = found[0].Player.Name;
            return game.Kick(found[0]) ? $"Kicked {name}" : $"Couldn't kick {name}";
        }

        /// <summary>
        /// add bot and remove bot change MinPlayers rather than the bots themselves, since every join and leave
        /// rebalances the bots to it; with autobalance on each team keeps half of MinPlayers, so the step is two.
        /// </summary>
        private static string ChangeBots(GameServer game, string argument, bool add)
        {
            var count = 1;

            if (argument.Length > 0 && (!int.TryParse(argument, out count) || count < 1))
                return $"Usage: {(add ? ADD_BOT : REMOVE_BOT)} [n], n 1 or more";

            var step = game.Settings.AutoBalanceTeams ? 2 : 1;
            var config = game.Settings.GameConfig;

            // start from what the teams hold now, so a MinPlayers already below the human count still changes something
            var current = Math.Max(config.MinPlayers, game.PlayerCount + game.BotCount);
            var change = count * step;

            game.SetMinPlayers(add ? current + change : current - change);
            return GetBotsText(game);
        }

        private static string GetBotsText(GameServer game)
        {
            var config = game.Settings.GameConfig;
            return $"MinPlayers {config.MinPlayers} (max {config.MaxPlayers}), {game.BotCount} bots";
        }

        private static string FindCommand(string word)
        {
            var key = word.ToLowerInvariant();

            for (var i = 0; i < COMMANDS.GetLength(0); i++)
                if (COMMANDS[i, 0] == key)
                    return key;

            if (ALIASES.TryGetValue(key, out var command))
                return command;

            return SettingCommands.Find(key);
        }

        /// <summary>
        /// The text after the first count words, trimmed, with its spacing kept.
        /// </summary>
        private static string SkipWords(string text, int count)
        {
            var index = 0;

            for (var i = 0; i < count; i++)
            {
                while (index < text.Length && text[index] == ' ')
                    index++;

                while (index < text.Length && text[index] != ' ')
                    index++;
            }

            return text[index..].Trim();
        }
    }
}
