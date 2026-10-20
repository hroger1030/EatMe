using EatMeLibrary;
using Newtonsoft.Json;
using System;
using System.IO;

namespace EatMeServer
{
    /// <summary>
    /// Static object to hold the ServerSettings instance and load it.
    /// </summary>
    public static class ConfigManager
    {
        private const string SETTINGS_FILE_NAME = "server.cfg";

        public static ServerSettings Settings { get; set; }

        /// <summary>
        /// Loads server.cfg from the executable's directory. Throws if the file is missing or invalid, or if
        /// GameConfig.MazeSize is odd or too small. GameConfig.DotRegenPercent is clamped to 0-100, TurnBufferTimeMs and
        /// LagCompensationMaxMs to 0 or more, and TurnGraceDistance to 0-TurnAssist.MAX_GRACE_CELLS. An empty or missing
        /// MessageOfTheDay becomes Const.MOTD_DEFAULT, and a StartingLevel below 1 becomes 1.
        /// </summary>
        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, SETTINGS_FILE_NAME);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found: {path}", path);

            Settings = JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(path))
                ?? throw new InvalidDataException($"Config file is empty: {path}");

            Normalize(Settings);
        }

        /// <summary>
        /// Checks and tidies settings, as loaded from server.cfg or changed with the console's set command: throws
        /// ArgumentOutOfRangeException if GameConfig.MazeSize is odd or too small, and otherwise clamps what is out of
        /// range (see Load), TickRate to 1 or more and MinPlayers to 0 to MaxPlayers.
        /// </summary>
        public static void Normalize(ServerSettings settings)
        {
            var config = settings.GameConfig;

            MapMaker.ValidateMazeSize(config.MazeSize);
            config.DotRegenPercent = Math.Clamp(config.DotRegenPercent, DotRegenQueue.MIN_PERCENT, DotRegenQueue.MAX_PERCENT);
            config.TurnBufferTimeMs = Math.Max(0, config.TurnBufferTimeMs);
            config.TurnGraceDistance = Math.Clamp(config.TurnGraceDistance, 0f, TurnAssist.MAX_GRACE_CELLS);
            config.LagCompensationMaxMs = Math.Max(0, config.LagCompensationMaxMs);
            config.MaxPlayers = Math.Max(1, config.MaxPlayers);
            config.MinPlayers = Math.Clamp(config.MinPlayers, 0, config.MaxPlayers);
            settings.TickRate = Math.Max(1, settings.TickRate);
            settings.MessageOfTheDay = GetMessageOfTheDay(settings.MessageOfTheDay);
            settings.StartingLevel = ClampStartingLevel(settings.StartingLevel);
        }

        /// <summary>
        /// The level to start on: the configured one, or 1 if it is below 1. There is no upper limit, since past the
        /// last level the list starts over.
        /// </summary>
        public static int ClampStartingLevel(int configured)
        {
            return Math.Max(1, configured);
        }

        /// <summary>
        /// The message of the day to send: the configured one, or Const.MOTD_DEFAULT if it is empty or missing.
        /// </summary>
        public static string GetMessageOfTheDay(string configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
                return Const.MOTD_DEFAULT;

            return configured.Trim();
        }
    }
}
