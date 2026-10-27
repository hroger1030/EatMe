using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        /// <summary>
        /// Extension of the audio file the content pipeline builds next to each song's .xnb; this is what is played.
        /// </summary>
        public const string MUSIC_FILE_EXTENSION = ".ogg";

        private const string TRACK_PREFIX = "Track";

        /// <summary>
        /// Picks the tracks out of a list of file names (paths and extensions are ignored) and returns their content
        /// names in play order: TrackN by number, then every TrackNb by number. Anything else is left out.
        /// </summary>
        public static List<string> GetTrackNames(IEnumerable<string> fileNames)
        {
            ArgumentNullException.ThrowIfNull(fileNames);

            var tracks = new List<(string Name, int Number, bool IsB)>();

            foreach (string name in fileNames.Select(Path.GetFileNameWithoutExtension))
            {
                if (!name.StartsWith(TRACK_PREFIX, StringComparison.OrdinalIgnoreCase))
                    continue;

                string rest = name.Substring(TRACK_PREFIX.Length);
                bool isB = rest.EndsWith("b", StringComparison.OrdinalIgnoreCase);

                if (isB)
                    rest = rest.Substring(0, rest.Length - 1);

                if (rest.Length > 0 && rest.All(char.IsAsciiDigit))
                    tracks.Add((name, int.Parse(rest), isB));
            }

            return tracks
                .OrderBy(track => track.IsB)
                .ThenBy(track => track.Number)
                .Select(track => TRACK_FOLDER + "/" + track.Name)
                .ToList();
        }

        /// <summary>
        /// Path in the content folder of a song's audio file from its content name, e.g. "Music/Track1" is "Music/Track1.ogg".
        /// </summary>
        public static string GetMusicFile(string songName)
        {
            ArgumentNullException.ThrowIfNull(songName);

            return songName + MUSIC_FILE_EXTENSION;
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
