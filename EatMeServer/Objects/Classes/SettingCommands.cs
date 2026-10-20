using EatMeLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EatMeServer
{
    /// <summary>
    /// One console command per server.cfg setting, named after the setting in lower case: typed alone it shows the
    /// value, typed with a value it changes it (e.g. pacspeed 7.5). MinPlayers, AutoBalanceTeams and MessageOfTheDay
    /// have their own commands in ServerCommands (bots, autobalance, motd). Port and DiscoveryPort have none, since
    /// they only count at startup. A changed value is checked like a loaded file (ConfigManager.Normalize), game rule
    /// changes are sent to every client, and nothing is written back to server.cfg.
    /// </summary>
    public static class SettingCommands
    {
        public const string SERVER_NAME = "servername";
        public const string TICK_RATE = "tickrate";
        public const string TARGET_FPS = "targetfps";
        public const string STARTING_LEVEL = "startinglevel";
        public const string MAX_PLAYERS = "maxplayers";
        public const string MAX_SCORE = "maxscore";
        public const string FLAG_SCORE = "flagscore";
        public const string FLAG_RESCUE = "flagrescue";
        public const string PLAYER_KILL = "playerkill";
        public const string GHOST_KILL = "ghostkill";
        public const string MAZE_SIZE = "mazesize";
        public const string WARP_TUNNEL_COUNT = "warptunnelcount";
        public const string GHOST_COUNT = "ghostcount";
        public const string POWER_PILL_COUNT = "powerpillcount";
        public const string FLAG_COUNT = "flagcount";
        public const string PLAYER_BASE_COUNT = "playerbasecount";
        public const string GHOST_VISION_DISTANCE = "ghostvisiondistance";
        public const string GHOST_REGEN_TIME_MS = "ghostregentimems";
        public const string DOT_REGEN_PERCENT = "dotregenpercent";
        public const string POWER_PILL_REGEN_TIME_MS = "powerpillregentimems";
        public const string PLAYER_RESPAWN_TIME_MS = "playerrespawntimems";
        public const string PAC_MAX_SIZE = "pacmaxsize";
        public const string PAC_SPEED = "pacspeed";
        public const string GHOST_SPEED = "ghostspeed";
        public const string PAC_SHRINK_PER_SECOND = "pacshrinkpersecond";
        public const string MS_UNTIL_STARVATION = "msuntilstarvation";
        public const string DOT_SIZE_GAIN = "dotsizegain";
        public const string POWER_PILL_SIZE_GAIN = "powerpillsizegain";
        public const string POWER_UP_DURATION_MS = "powerupdurationms";
        public const string INTERMISSION_TIME_MS = "intermissiontimems";
        public const string POWER_UP_SPAWN_TIMER_MS = "powerupspawntimerms";
        public const string TURN_BUFFER_TIME_MS = "turnbuffertimems";
        public const string TURN_GRACE_DISTANCE = "turngracedistance";
        public const string LAG_COMPENSATION_MAX_MS = "lagcompensationmaxms";
        public const string SERVER_SEED = "serverseed";

        private const string NEXT_LEVEL_NOTE = " (from the next level; restart to use it now)";
        private const string NEW_GAME_NOTE = " (on the next new game)";

        /// <summary>
        /// Every setting command with its usage and what the setting does, in server.cfg order, as help lists them.
        /// </summary>
        public static readonly string[,] COMMANDS =
        {
            { SERVER_NAME, "servername [text]", "Server name shown in the LAN game list" },
            { TICK_RATE, "tickrate [n]", "Game updates per second (1 or more)" },
            { TARGET_FPS, "targetfps [n]", "Game state sends to clients per second" },
            { STARTING_LEVEL, "startinglevel [n]", "Level a new game starts on (1 or more)" },
            { MAX_PLAYERS, "maxplayers [n]", "Most players (humans plus bots) the server takes" },
            { MAX_SCORE, "maxscore [n]", "Team score needed to win a level" },
            { FLAG_SCORE, "flagscore [n]", "Points for capturing a flag" },
            { FLAG_RESCUE, "flagrescue [n]", "Points for returning your own flag" },
            { PLAYER_KILL, "playerkill [n]", "Points for eating an enemy pac" },
            { GHOST_KILL, "ghostkill [n]", "Points for killing a ghost" },
            { MAZE_SIZE, "mazesize [n]", "Map width and height in cells (even, 18 or more; next level)" },
            { WARP_TUNNEL_COUNT, "warptunnelcount [n]", "Warp tunnels through the outer wall (next level)" },
            { GHOST_COUNT, "ghostcount [n]", "Ghosts in the maze (next level)" },
            { POWER_PILL_COUNT, "powerpillcount [n]", "Power pills in the maze (next level)" },
            { FLAG_COUNT, "flagcount [n]", "Flags per team (next level)" },
            { PLAYER_BASE_COUNT, "playerbasecount [n]", "Spawn rooms per team (next level)" },
            { GHOST_VISION_DISTANCE, "ghostvisiondistance [n]", "Cells within which a ghost spots a pac" },
            { GHOST_REGEN_TIME_MS, "ghostregentimems [n]", "ms before a dead ghost comes back" },
            { DOT_REGEN_PERCENT, "dotregenpercent [n]", "Percent of dots that stay eaten (0 to 100)" },
            { POWER_PILL_REGEN_TIME_MS, "powerpillregentimems [n]", "ms before an eaten power pill comes back" },
            { PLAYER_RESPAWN_TIME_MS, "playerrespawntimems [n]", "ms before a dead pac respawns" },
            { PAC_MAX_SIZE, "pacmaxsize [x]", "Biggest a pac grows from dots" },
            { PAC_SPEED, "pacspeed [x]", "Pac speed in cells per second" },
            { GHOST_SPEED, "ghostspeed [x]", "Ghost speed in cells per second" },
            { PAC_SHRINK_PER_SECOND, "pacshrinkpersecond [x]", "Size a starving pac loses each second" },
            { MS_UNTIL_STARVATION, "msuntilstarvation [n]", "ms without eating before a pac starves" },
            { DOT_SIZE_GAIN, "dotsizegain [x]", "Size a pac gains per dot" },
            { POWER_PILL_SIZE_GAIN, "powerpillsizegain [x]", "Size a pac gains per power pill" },
            { POWER_UP_DURATION_MS, "powerupdurationms [n]", "ms every power up effect lasts" },
            { INTERMISSION_TIME_MS, "intermissiontimems [n]", "ms the results show between levels" },
            { POWER_UP_SPAWN_TIMER_MS, "powerupspawntimerms [n]", "ms between power ups (0 for none)" },
            { TURN_BUFFER_TIME_MS, "turnbuffertimems [n]", "ms an early turn press is remembered (0 for off)" },
            { TURN_GRACE_DISTANCE, "turngracedistance [x]", "Cells past a junction a late turn still works (0 to 0.9)" },
            { LAG_COMPENSATION_MAX_MS, "lagcompensationmaxms [n]", "Most ms of lag that widen the late turn window" },
            { SERVER_SEED, "serverseed [n]", "Seed the mazes are built from (next level)" },
        };

        /// <summary>
        /// The setting command for a word in any case, or null if it isn't one.
        /// </summary>
        public static string Find(string word)
        {
            var key = word?.ToLowerInvariant();

            for (var i = 0; i < COMMANDS.GetLength(0); i++)
                if (COMMANDS[i, 0] == key)
                    return key;

            return null;
        }

        /// <summary>
        /// Runs a setting command: shows the setting with no argument, otherwise changes it. Returns what to print,
        /// or null if command isn't a setting command.
        /// </summary>
        public static string Execute(GameServer game, string command, string argument)
        {
            var settings = game.Settings;
            var config = settings.GameConfig;

            switch (command)
            {
                case SERVER_NAME:
                    if (argument.Length > 0)
                        settings.ServerName = argument;

                    return Show(nameof(ServerSettings.ServerName), settings.ServerName);

                case TICK_RATE:
                    return SetInt(game, command, argument, nameof(ServerSettings.TickRate), () => settings.TickRate, value => settings.TickRate = value, isGameRule: false);

                case TARGET_FPS:
                    return SetInt(game, command, argument, nameof(ServerSettings.TargetFps), () => settings.TargetFps, value => settings.TargetFps = value, isGameRule: false);

                case STARTING_LEVEL:
                    return SetInt(game, command, argument, nameof(ServerSettings.StartingLevel), () => settings.StartingLevel, value => settings.StartingLevel = value, isGameRule: false, NEW_GAME_NOTE);

                case MAX_PLAYERS:
                {
                    var text = SetInt(game, command, argument, nameof(GameConfiguration.MaxPlayers), () => config.MaxPlayers, value => config.MaxPlayers = value, isGameRule: true);

                    // Normalize may have lowered MinPlayers to fit
                    if (argument.Length > 0)
                        game.BalanceBots();

                    return text;
                }

                case MAX_SCORE:
                    return SetInt(game, command, argument, nameof(GameConfiguration.MaxScore), () => config.MaxScore, value => config.MaxScore = value, isGameRule: true);

                case FLAG_SCORE:
                    return SetInt(game, command, argument, nameof(GameConfiguration.FlagScore), () => config.FlagScore, value => config.FlagScore = value, isGameRule: true);

                case FLAG_RESCUE:
                    return SetInt(game, command, argument, nameof(GameConfiguration.FlagRescue), () => config.FlagRescue, value => config.FlagRescue = value, isGameRule: true);

                case PLAYER_KILL:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PlayerKill), () => config.PlayerKill, value => config.PlayerKill = value, isGameRule: true);

                case GHOST_KILL:
                    return SetInt(game, command, argument, nameof(GameConfiguration.GhostKill), () => config.GhostKill, value => config.GhostKill = value, isGameRule: true);

                case MAZE_SIZE:
                    if (argument.Length > 0 && int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mazeSize))
                    {
                        // refused rather than clamped, so the setting keeps its value
                        try
                        {
                            MapMaker.ValidateMazeSize(mazeSize);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            return ex.Message;
                        }
                    }

                    return SetInt(game, command, argument, nameof(GameConfiguration.MazeSize), () => config.MazeSize, value => config.MazeSize = value, isGameRule: true, NEXT_LEVEL_NOTE);

                case WARP_TUNNEL_COUNT:
                    return SetInt(game, command, argument, nameof(GameConfiguration.WarpTunnelCount), () => config.WarpTunnelCount, value => config.WarpTunnelCount = value, isGameRule: true, NEXT_LEVEL_NOTE);

                case GHOST_COUNT:
                    return SetInt(game, command, argument, nameof(GameConfiguration.GhostCount), () => config.GhostCount, value => config.GhostCount = value, isGameRule: true, NEXT_LEVEL_NOTE);

                case POWER_PILL_COUNT:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PowerPillCount), () => config.PowerPillCount, value => config.PowerPillCount = value, isGameRule: true, NEXT_LEVEL_NOTE);

                case FLAG_COUNT:
                    return SetInt(game, command, argument, nameof(GameConfiguration.FlagCount), () => config.FlagCount, value => config.FlagCount = value, isGameRule: true, NEXT_LEVEL_NOTE);

                case PLAYER_BASE_COUNT:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PlayerBaseCount), () => config.PlayerBaseCount, value => config.PlayerBaseCount = value, isGameRule: true, NEXT_LEVEL_NOTE);

                case GHOST_VISION_DISTANCE:
                    return SetInt(game, command, argument, nameof(GameConfiguration.GhostVisionDistance), () => config.GhostVisionDistance, value => config.GhostVisionDistance = value, isGameRule: true);

                case GHOST_REGEN_TIME_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.GhostRegenTimeMs), () => config.GhostRegenTimeMs, value => config.GhostRegenTimeMs = value, isGameRule: true);

                case DOT_REGEN_PERCENT:
                    return SetInt(game, command, argument, nameof(GameConfiguration.DotRegenPercent), () => config.DotRegenPercent, value => config.DotRegenPercent = value, isGameRule: true);

                case POWER_PILL_REGEN_TIME_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PowerPillRegenTimeMs), () => config.PowerPillRegenTimeMs, value => config.PowerPillRegenTimeMs = value, isGameRule: true);

                case PLAYER_RESPAWN_TIME_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PlayerRespawnTimeMs), () => config.PlayerRespawnTimeMs, value => config.PlayerRespawnTimeMs = value, isGameRule: true);

                case PAC_MAX_SIZE:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.PacMaxSize), () => config.PacMaxSize, value => config.PacMaxSize = value);

                case PAC_SPEED:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.PacSpeed), () => config.PacSpeed, value => config.PacSpeed = value);

                case GHOST_SPEED:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.GhostSpeed), () => config.GhostSpeed, value => config.GhostSpeed = value);

                case PAC_SHRINK_PER_SECOND:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.PacShrinkPerSecond), () => config.PacShrinkPerSecond, value => config.PacShrinkPerSecond = value);

                case MS_UNTIL_STARVATION:
                    return SetInt(game, command, argument, nameof(GameConfiguration.MsUntilStarvation), () => config.MsUntilStarvation, value => config.MsUntilStarvation = value, isGameRule: true);

                case DOT_SIZE_GAIN:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.DotSizeGain), () => config.DotSizeGain, value => config.DotSizeGain = value);

                case POWER_PILL_SIZE_GAIN:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.PowerPillSizeGain), () => config.PowerPillSizeGain, value => config.PowerPillSizeGain = value);

                case POWER_UP_DURATION_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PowerUpDurationMs), () => config.PowerUpDurationMs, value => config.PowerUpDurationMs = value, isGameRule: true);

                case INTERMISSION_TIME_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.IntermissionTimeMs), () => config.IntermissionTimeMs, value => config.IntermissionTimeMs = value, isGameRule: true);

                case POWER_UP_SPAWN_TIMER_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.PowerUpSpawnTimerMs), () => config.PowerUpSpawnTimerMs, value => config.PowerUpSpawnTimerMs = value, isGameRule: true);

                case TURN_BUFFER_TIME_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.TurnBufferTimeMs), () => config.TurnBufferTimeMs, value => config.TurnBufferTimeMs = value, isGameRule: true);

                case TURN_GRACE_DISTANCE:
                    return SetFloat(game, command, argument, nameof(GameConfiguration.TurnGraceDistance), () => config.TurnGraceDistance, value => config.TurnGraceDistance = value);

                case LAG_COMPENSATION_MAX_MS:
                    return SetInt(game, command, argument, nameof(GameConfiguration.LagCompensationMaxMs), () => config.LagCompensationMaxMs, value => config.LagCompensationMaxMs = value, isGameRule: true);

                case SERVER_SEED:
                    return SetInt(game, command, argument, nameof(GameConfiguration.ServerSeed), () => config.ServerSeed, value => config.ServerSeed = value, isGameRule: true, NEXT_LEVEL_NOTE);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Every server.cfg setting the console can show or change (all but the ports), with the command for it and
        /// its value, in server.cfg order.
        /// </summary>
        public static List<string[]> GetSettingRows(ServerSettings settings)
        {
            var config = settings.GameConfig;

            return new List<string[]>
            {
                new[] { "Setting", "Command", "Value" },
                new[] { nameof(ServerSettings.ServerName), SERVER_NAME, settings.ServerName },
                new[] { nameof(ServerSettings.MessageOfTheDay), ServerCommands.MOTD, settings.MessageOfTheDay },
                new[] { nameof(ServerSettings.TickRate), TICK_RATE, Format(settings.TickRate) },
                new[] { nameof(ServerSettings.TargetFps), TARGET_FPS, Format(settings.TargetFps) },
                new[] { nameof(ServerSettings.AutoBalanceTeams), ServerCommands.AUTO_BALANCE, settings.AutoBalanceTeams ? "on" : "off" },
                new[] { nameof(ServerSettings.StartingLevel), STARTING_LEVEL, Format(settings.StartingLevel) },
                new[] { nameof(GameConfiguration.MaxPlayers), MAX_PLAYERS, Format(config.MaxPlayers) },
                new[] { nameof(GameConfiguration.MinPlayers), ServerCommands.BOTS, Format(config.MinPlayers) },
                new[] { nameof(GameConfiguration.MaxScore), MAX_SCORE, Format(config.MaxScore) },
                new[] { nameof(GameConfiguration.FlagScore), FLAG_SCORE, Format(config.FlagScore) },
                new[] { nameof(GameConfiguration.FlagRescue), FLAG_RESCUE, Format(config.FlagRescue) },
                new[] { nameof(GameConfiguration.PlayerKill), PLAYER_KILL, Format(config.PlayerKill) },
                new[] { nameof(GameConfiguration.GhostKill), GHOST_KILL, Format(config.GhostKill) },
                new[] { nameof(GameConfiguration.MazeSize), MAZE_SIZE, Format(config.MazeSize) },
                new[] { nameof(GameConfiguration.WarpTunnelCount), WARP_TUNNEL_COUNT, Format(config.WarpTunnelCount) },
                new[] { nameof(GameConfiguration.GhostCount), GHOST_COUNT, Format(config.GhostCount) },
                new[] { nameof(GameConfiguration.PowerPillCount), POWER_PILL_COUNT, Format(config.PowerPillCount) },
                new[] { nameof(GameConfiguration.FlagCount), FLAG_COUNT, Format(config.FlagCount) },
                new[] { nameof(GameConfiguration.PlayerBaseCount), PLAYER_BASE_COUNT, Format(config.PlayerBaseCount) },
                new[] { nameof(GameConfiguration.GhostVisionDistance), GHOST_VISION_DISTANCE, Format(config.GhostVisionDistance) },
                new[] { nameof(GameConfiguration.GhostRegenTimeMs), GHOST_REGEN_TIME_MS, Format(config.GhostRegenTimeMs) },
                new[] { nameof(GameConfiguration.DotRegenPercent), DOT_REGEN_PERCENT, Format(config.DotRegenPercent) },
                new[] { nameof(GameConfiguration.PowerPillRegenTimeMs), POWER_PILL_REGEN_TIME_MS, Format(config.PowerPillRegenTimeMs) },
                new[] { nameof(GameConfiguration.PlayerRespawnTimeMs), PLAYER_RESPAWN_TIME_MS, Format(config.PlayerRespawnTimeMs) },
                new[] { nameof(GameConfiguration.PacMaxSize), PAC_MAX_SIZE, Format(config.PacMaxSize) },
                new[] { nameof(GameConfiguration.PacSpeed), PAC_SPEED, Format(config.PacSpeed) },
                new[] { nameof(GameConfiguration.GhostSpeed), GHOST_SPEED, Format(config.GhostSpeed) },
                new[] { nameof(GameConfiguration.PacShrinkPerSecond), PAC_SHRINK_PER_SECOND, Format(config.PacShrinkPerSecond) },
                new[] { nameof(GameConfiguration.MsUntilStarvation), MS_UNTIL_STARVATION, Format(config.MsUntilStarvation) },
                new[] { nameof(GameConfiguration.DotSizeGain), DOT_SIZE_GAIN, Format(config.DotSizeGain) },
                new[] { nameof(GameConfiguration.PowerPillSizeGain), POWER_PILL_SIZE_GAIN, Format(config.PowerPillSizeGain) },
                new[] { nameof(GameConfiguration.PowerUpDurationMs), POWER_UP_DURATION_MS, Format(config.PowerUpDurationMs) },
                new[] { nameof(GameConfiguration.IntermissionTimeMs), INTERMISSION_TIME_MS, Format(config.IntermissionTimeMs) },
                new[] { nameof(GameConfiguration.PowerUpSpawnTimerMs), POWER_UP_SPAWN_TIMER_MS, Format(config.PowerUpSpawnTimerMs) },
                new[] { nameof(GameConfiguration.TurnBufferTimeMs), TURN_BUFFER_TIME_MS, Format(config.TurnBufferTimeMs) },
                new[] { nameof(GameConfiguration.TurnGraceDistance), TURN_GRACE_DISTANCE, Format(config.TurnGraceDistance) },
                new[] { nameof(GameConfiguration.LagCompensationMaxMs), LAG_COMPENSATION_MAX_MS, Format(config.LagCompensationMaxMs) },
                new[] { nameof(GameConfiguration.ServerSeed), SERVER_SEED, Format(config.ServerSeed) },
            };
        }

        public static string Format(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Format(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Show(string name, string value)
        {
            return $"{name} = {value}";
        }

        /// <summary>
        /// Shows a whole number setting, or parses, sets and normalizes it; a game rule is then sent to every client.
        /// </summary>
        private static string SetInt(GameServer game, string command, string argument, string name, Func<int> get, Action<int> set, bool isGameRule, string note = "")
        {
            if (argument.Length == 0)
                return Show(name, Format(get()));

            if (!int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return $"Usage: {command} [whole number]";

            set(value);
            ConfigManager.Normalize(game.Settings);

            if (isGameRule)
                game.SendGameConfig();

            return Show(name, Format(get())) + note;
        }

        /// <summary>
        /// Shows a decimal game rule, or parses (with a decimal point), sets and normalizes it and sends it to every client.
        /// </summary>
        private static string SetFloat(GameServer game, string command, string argument, string name, Func<float> get, Action<float> set)
        {
            if (argument.Length == 0)
                return Show(name, Format(get()));

            if (!float.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
                return $"Usage: {command} [number]";

            set(value);
            ConfigManager.Normalize(game.Settings);
            game.SendGameConfig();

            return Show(name, Format(get()));
        }
    }
}
