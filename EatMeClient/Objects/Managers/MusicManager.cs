using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;

namespace EatMeClient
{
    public static class MusicManager
    {
        private const float DEFAULT_VOLUME = 0.5f;

        /// <summary>
        /// Content names of the level tracks found in the content folder, in play order (MusicPlaylist.GetTrackNames).
        /// </summary>
        public static IReadOnlyList<string> LevelTracks { get; set; } = [];

        public static void Initialize(float defaultVolume = DEFAULT_VOLUME)
        {
            MediaPlayer.IsRepeating = true;
            SetVolume(defaultVolume);
        }

        public static void Play(Song song)
        {
            if (MediaPlayer.State == MediaState.Playing && MediaPlayer.Queue.ActiveSong == song)
                return;

            MediaPlayer.Play(song);
        }

        /// <summary>
        /// Plays the level's track, one track per level round the playlist. Does nothing when there are no tracks.
        /// </summary>
        public static void PlayLevelTrack(int level)
        {
            if (LevelTracks.Count == 0)
                return;

            Play(AssetCache.GetSong(LevelTracks[MusicPlaylist.GetTrackForLevel(level, LevelTracks.Count)]));
        }

        public static void Stop()
        {
            MediaPlayer.Stop();
        }

        public static void Pause()
        {
            MediaPlayer.Pause();
        }

        public static void Resume()
        {
            MediaPlayer.Resume();
        }

        public static float GetVolume()
        {
            return MediaPlayer.Volume;
        }

        public static void SetVolume(float newVolume)
        {
            MediaPlayer.Volume = MathHelper.Clamp(newVolume, 0f, 1f);
        }
    }
}
