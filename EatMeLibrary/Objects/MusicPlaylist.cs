using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EatMeLibrary
{
    /// <summary>
    /// The level music: whatever Track songs the client has, played one per level in a loop.
    /// </summary>
    public static class MusicPlaylist
    {
        /// <summary>
        /// Content folder the tracks are in.
        /// </summary>
        public const string TRACK_FOLDER = "Music";

        private static readonly Regex TRACK_NAME = new(@"^Track(\d+)(b?)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Picks the tracks out of a list of file names (paths and extensions are ignored) and returns their content
        /// names in play order: TrackN by number, then every TrackNb by number. Anything else is left out.
        /// </summary>
        public static List<string> GetTrackNames(IEnumerable<string> fileNames)
        {
            ArgumentNullException.ThrowIfNull(fileNames);

            return fileNames
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new { Name = name, Match = TRACK_NAME.Match(name) })
                .Where(track => track.Match.Success)
                .OrderBy(track => track.Match.Groups[2].Length)
                .ThenBy(track => int.Parse(track.Match.Groups[1].Value))
                .Select(track => TRACK_FOLDER + "/" + track.Name)
                .ToList();
        }

        /// <summary>
        /// Index into a list of trackCount tracks of the one a level plays, starting over once every track has played:
        /// level 1 plays the first track and level trackCount + 1 the first again.
        /// </summary>
        public static int GetTrackForLevel(int level, int trackCount)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), level, "Levels start at 1");

            if (trackCount < 1)
                throw new ArgumentOutOfRangeException(nameof(trackCount), trackCount, "There must be at least one track");

            return (level - 1) % trackCount;
        }

        /// <summary>
        /// What the level music does when the game is unpaused. No music starts while paused, so a level that started
        /// in the meantime starts its track now; the results between levels have none.
        /// </summary>
        public static eResumeMusic GetResumeMusic(bool levelStartedWhilePaused, bool isIntermission)
        {
            if (isIntermission)
                return eResumeMusic.StaySilent;

            return levelStartedWhilePaused ? eResumeMusic.PlayLevelTrack : eResumeMusic.Resume;
        }
    }
}
