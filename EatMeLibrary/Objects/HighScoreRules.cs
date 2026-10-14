using System;
using System.Collections.Generic;
using System.Linq;

namespace EatMeLibrary
{
    /// <summary>
    /// High score list rules shared by the server (its highscores.txt) and the client (its own highscores.txt, every
    /// score it has been sent by any server).
    /// </summary>
    public static class HighScoreRules
    {
        /// <summary>
        /// How many entries a list keeps.
        /// </summary>
        public const int MAX_HIGH_SCORES = 100;

        /// <summary>
        /// One entry per line as Name,Score,Date (the name may itself hold commas). Blank lines are skipped; lines
        /// without three fields, a name, or a whole score of 0 or more are added to badLines (if given) and skipped.
        /// Returns the entries highest score first, keeping file order for ties.
        /// </summary>
        public static List<HighScoreEntry> Parse(string text, List<string> badLines = null)
        {
            var entries = new List<HighScoreEntry>();

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim();

                if (line.Length == 0)
                    continue;

                var dateComma = line.LastIndexOf(',');
                var scoreComma = dateComma > 0 ? line.LastIndexOf(',', dateComma - 1) : -1;

                if (scoreComma < 0)
                {
                    badLines?.Add(line);
                    continue;
                }

                var name = line.Substring(0, scoreComma).Trim();
                var scoreText = line.Substring(scoreComma + 1, dateComma - scoreComma - 1).Trim();
                var date = line.Substring(dateComma + 1).Trim();

                if (name.Length == 0 || !int.TryParse(scoreText, out var score) || score < 0)
                {
                    badLines?.Add(line);
                    continue;
                }

                entries.Add(new HighScoreEntry { Name = name, Score = score, Date = date });
            }

            return entries.OrderByDescending(entry => entry.Score).ToList();
        }

        /// <summary>
        /// The list as a highscores.txt holds it: one Name,Score,Date per line, the same format Parse reads.
        /// </summary>
        public static string Format(List<HighScoreEntry> scores)
        {
            return string.Concat(scores.Select(score => $"{score.Name},{score.Score},{score.Date}{Environment.NewLine}"));
        }

        /// <summary>
        /// Re-ranks scores in place highest first, keeping list order for ties, and cuts it to MAX_HIGH_SCORES.
        /// </summary>
        public static void Rank(List<HighScoreEntry> scores)
        {
            var ranked = scores.OrderByDescending(score => score.Score).Take(MAX_HIGH_SCORES).ToList();
            scores.Clear();
            scores.AddRange(ranked);
        }

        /// <summary>
        /// Puts entry on the list (if it isn't already there), re-ranks it and cuts it to MAX_HIGH_SCORES. An entry
        /// already on the list is just re-ranked, so a player whose score has gone up moves up rather than appearing
        /// twice. Returns true if entry is on the list afterwards.
        /// </summary>
        public static bool Submit(List<HighScoreEntry> scores, HighScoreEntry entry)
        {
            if (!scores.Contains(entry))
                scores.Add(entry);

            Rank(scores);

            return scores.Contains(entry);
        }

        /// <summary>
        /// Adds a server's list to the client's own, keeping MAX_HIGH_SCORES. The client keeps one entry per name per
        /// date: a server updates a player's entry as their score grows, so an incoming entry with the same name and
        /// date as a stored one only raises that entry's score (never lowers it); any other is added as a copy.
        /// Scores already kept are only pushed off by higher ones. Returns true if the stored list changed.
        /// </summary>
        public static bool Merge(List<HighScoreEntry> stored, List<HighScoreEntry> incoming)
        {
            if (incoming == null || incoming.Count == 0)
                return false;

            var before = Format(stored);

            foreach (var entry in incoming)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name) || entry.Score < 0)
                    continue;

                var existing = stored.Find(score => score.Name == entry.Name && score.Date == entry.Date);

                if (existing == null)
                    stored.Add(new HighScoreEntry { Name = entry.Name, Score = entry.Score, Date = entry.Date });
                else if (entry.Score > existing.Score)
                    existing.Score = entry.Score;
            }

            Rank(stored);

            return Format(stored) != before;
        }

        /// <summary>
        /// How a rank is shown in the table, e.g. "1)" for the top score (index 0).
        /// </summary>
        public static string GetRankText(int index)
        {
            return $"{index + 1})";
        }

        /// <summary>
        /// How many table rows count entries need when laid out columns to a row.
        /// </summary>
        public static int GetRowCount(int count, int columns)
        {
            return columns <= 0 || count <= 0 ? 0 : (count + columns - 1) / columns;
        }

        /// <summary>
        /// The entry index shown at row, column: entries run left to right then down, so scrolling down always
        /// reaches lower scores.
        /// </summary>
        public static int GetEntryIndex(int row, int column, int columns)
        {
            return row * columns + column;
        }
    }
}
