using EatMeLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace EatMeServer
{
    /// <summary>
    /// Holds the high scores read from highscores.txt, highest first. The list rules are in HighScoreRules.
    /// </summary>
    public static class HighScoreManager
    {
        private const string HIGH_SCORES_FILE_NAME = "highscores.txt";
        private const string DATE_FORMAT = "yyyy-MM-dd";

        public static List<HighScoreEntry> Scores { get; private set; } = [];

        /// <summary>
        /// Loads highscores.txt from the executable's directory. A missing file just means no high scores.
        /// Lines that can't be read are reported and skipped.
        /// </summary>
        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HIGH_SCORES_FILE_NAME);

            if (!File.Exists(path))
            {
                ServerLog.Write(eLogType.Warning, $"No {HIGH_SCORES_FILE_NAME} found; there are no high scores.");
                Scores = [];
                return;
            }

            var badLines = new List<string>();
            Scores = HighScoreRules.Parse(File.ReadAllText(path), badLines);

            foreach (var line in badLines)
                ServerLog.Write(eLogType.Warning, $"Skipped bad line in {HIGH_SCORES_FILE_NAME}: {line}");
        }

        /// <summary>
        /// Today's date as high score entries store it.
        /// </summary>
        public static string GetDate(DateTime date)
        {
            return date.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes the list to highscores.txt in the executable's directory. A failed write is reported, not fatal.
        /// </summary>
        public static void Save(List<HighScoreEntry> scores)
        {
            var path = Path.Combine(AppContext.BaseDirectory, HIGH_SCORES_FILE_NAME);

            try
            {
                File.WriteAllText(path, HighScoreRules.Format(scores));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ServerLog.Write(eLogType.Error, $"Couldn't save {HIGH_SCORES_FILE_NAME}: {ex.Message}");
            }
        }
    }
}
