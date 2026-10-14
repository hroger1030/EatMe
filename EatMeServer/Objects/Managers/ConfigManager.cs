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

            MapMaker.ValidateMazeSize(Settings.GameConfig.MazeSize);
            Settings.GameConfig.DotRegenPercent = Math.Clamp(Settings.GameConfig.DotRegenPercent, DotRegenQueue.MIN_PERCENT, DotRegenQueue.MAX_PERCENT);
            Settings.GameConfig.TurnBufferTimeMs = Math.Max(0, Settings.GameConfig.TurnBufferTimeMs);
            Settings.GameConfig.TurnGraceDistance = Math.Clamp(Settings.GameConfig.TurnGraceDistance, 0f, TurnAssist.MAX_GRACE_CELLS);
            Settings.GameConfig.LagCompensationMaxMs = Math.Max(0, Settings.GameConfig.LagCompensationMaxMs);
            Settings.MessageOfTheDay = GetMessageOfTheDay(Settings.MessageOfTheDay);
            Settings.StartingLevel = ClampStartingLevel(Settings.StartingLevel);
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
