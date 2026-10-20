using System;
using System.Collections.Generic;
using System.IO;

namespace EatMeServer
{
    /// <summary>
    /// Holds the bot names read from bot_names.txt. Bot names don't have to follow the player name rules.
    /// </summary>
    public static class BotNameManager
    {
        private const string BOT_NAMES_FILE_NAME = "bot_names.txt";
        private const string FALLBACK_NAME_PREFIX = "Bot";

        public static List<string> Names { get; private set; } = [];

        /// <summary>
        /// Loads bot_names.txt from the executable's directory. A missing file just means bots are named Bot1, Bot2...
        /// </summary>
        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, BOT_NAMES_FILE_NAME);

            if (!File.Exists(path))
            {
                ServerLog.Write(eLogType.Warning, $"No {BOT_NAMES_FILE_NAME} found; bots are named {FALLBACK_NAME_PREFIX}1, {FALLBACK_NAME_PREFIX}2...");
                Names = [];
                return;
            }

            // same format as LoadingScreenTips.txt: one per line, blank lines skipped
            Names = LoadingScreenTipsManager.Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// A random name nobody is using. When every name is taken (or there are none), the first free Bot1, Bot2...
        /// </summary>
        public static string PickName(List<string> names, HashSet<string> namesInUse, Random random)
        {
            var free = new List<string>();

            foreach (var name in names)
            {
                if (!namesInUse.Contains(name))
                    free.Add(name);
            }

            if (free.Count > 0)
                return free[random.Next(free.Count)];

            for (var i = 1; ; i++)
            {
                var name = $"{FALLBACK_NAME_PREFIX}{i}";

                if (!namesInUse.Contains(name))
                    return name;
            }
        }
    }
}
