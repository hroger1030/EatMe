using EatMeLibrary;
using System;
using System.Collections.Generic;
using System.IO;

namespace EatMeClient
{
    /// <summary>
    /// The client's own high score list: the top scores from every server it has been sent, kept in highscores.txt
    /// next to the executable (the same Name,Score,Date format the server uses).
    /// </summary>
    public static class HighScoreStore
    {
        private const string HIGH_SCORES_FILE_NAME = "highscores.txt";

        public static List<HighScoreEntry> Scores { get; private set; } = [];

        /// <summary>
        /// Loads highscores.txt. A missing or unreadable file just means no high scores yet; bad lines are skipped.
        /// </summary>
        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HIGH_SCORES_FILE_NAME);

            try
            {
                Scores = File.Exists(path) ? HighScoreRules.Parse(File.ReadAllText(path)) : [];
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Scores = [];
            }

            HighScoreRules.Rank(Scores);
        }

        /// <summary>
        /// Adds a server's list to the client's (HighScoreRules.Merge) and saves the file if anything changed.
        /// </summary>
        public static void Merge(List<HighScoreEntry> incoming)
        {
            if (HighScoreRules.Merge(Scores, incoming))
                Save();
        }

        /// <summary>
        /// Writes highscores.txt. A failed write is ignored; the scores stay in memory for this run.
        /// </summary>
        public static void Save()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HIGH_SCORES_FILE_NAME);

            try
            {
                File.WriteAllText(path, HighScoreRules.Format(Scores));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }
    }
}
