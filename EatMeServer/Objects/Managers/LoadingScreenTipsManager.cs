using System;
using System.Collections.Generic;
using System.IO;

namespace EatMeServer
{
    /// <summary>
    /// Holds the loading screen tip lines read from LoadingScreenTips.txt.
    /// </summary>
    public static class LoadingScreenTipsManager
    {
        private const string TIPS_FILE_NAME = "LoadingScreenTips.txt";

        public static List<string> Tips { get; private set; } = [];

        /// <summary>
        /// Loads LoadingScreenTips.txt from the executable's directory. A missing file just means no tips.
        /// </summary>
        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, TIPS_FILE_NAME);

            if (!File.Exists(path))
            {
                Console.WriteLine($"No {TIPS_FILE_NAME} found; players get no loading screen tips.");
                Tips = [];
                return;
            }

            Tips = Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// One tip per line, as many as you like. Blank lines are skipped.
        /// </summary>
        public static List<string> Parse(string text)
        {
            var tips = new List<string>();

            foreach (var line in text.Split('\n'))
            {
                var tip = line.Trim();

                if (tip.Length == 0)
                    continue;

                tips.Add(tip);
            }

            return tips;
        }
    }
}
