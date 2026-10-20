using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using System.Collections.Generic;

namespace EatMeClient
{
    /// <summary>
    /// Plays one looping song at a time, decoding its .ogg (AssetCache.OpenMusic, so it can come from Resource.pac)
    /// with NVorbis and streaming it through a DynamicSoundEffectInstance.
    /// </summary>
    public static class MusicManager
    {
        private const float DEFAULT_VOLUME = 0.5f;

        /// <summary>
        /// Length of each streamed buffer; BUFFER_COUNT of them are kept queued so a slow frame doesn't run dry.
        /// </summary>
        private const int BUFFER_MS = 250;
        private const int BUFFER_COUNT = 3;

        private static DynamicSoundEffectInstance _Instance;
        private static VorbisReader _Reader;
        private static string _SongName;
        private static float[] _Samples;
        private static byte[] _Pcm;
        private static float _Volume;

        /// <summary>
        /// Content names of the level tracks found in the content folder, in play order (MusicPlaylist.GetTrackNames).
        /// </summary>
        public static IReadOnlyList<string> LevelTracks { get; set; } = [];

        public static void Initialize(float defaultVolume = DEFAULT_VOLUME)
        {
            SetVolume(defaultVolume);
        }

        /// <summary>
        /// Starts a song looping from the beginning, unless it is already playing.
        /// </summary>
        public static void Play(string songName)
        {
            if (_SongName == songName && _Instance?.State == SoundState.Playing)
                return;

            Stop();

            _Reader = new VorbisReader(AssetCache.OpenMusic(songName), true);
            _Instance = new DynamicSoundEffectInstance(_Reader.SampleRate, _Reader.Channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo);
            _Instance.Volume = _Volume;
            _Instance.BufferNeeded += (_, _) => FillBuffers();

            var sampleCount = _Reader.SampleRate * _Reader.Channels * BUFFER_MS / 1000;
            _Samples = new float[sampleCount];
            _Pcm = new byte[sampleCount * PcmConverter.BYTES_PER_SAMPLE];
            _SongName = songName;

            FillBuffers();
            _Instance.Play();
        }

        /// <summary>
        /// Plays the level's track, one track per level round the playlist. Does nothing when there are no tracks.
        /// </summary>
        public static void PlayLevelTrack(int level)
        {
            if (LevelTracks.Count == 0)
                return;

            Play(LevelTracks[MusicPlaylist.GetTrackForLevel(level, LevelTracks.Count)]);
        }

        public static void Stop()
        {
            _Instance?.Stop();
            _Instance?.Dispose();
            _Reader?.Dispose();
            _Instance = null;
            _Reader = null;
            _SongName = null;
        }

        public static void Pause()
        {
            if (_Instance?.State == SoundState.Playing)
                _Instance.Pause();
        }

        public static void Resume()
        {
            if (_Instance?.State == SoundState.Paused)
                _Instance.Resume();
        }

        public static float GetVolume()
        {
            return _Volume;
        }

        public static void SetVolume(float newVolume)
        {
            _Volume = MathHelper.Clamp(newVolume, 0f, 1f);

            if (_Instance != null)
                _Instance.Volume = _Volume;
        }

        /// <summary>
        /// Keeps BUFFER_COUNT buffers queued, decoding the next stretch of the song into each.
        /// </summary>
        private static void FillBuffers()
        {
            while (_Instance != null && _Instance.PendingBufferCount < BUFFER_COUNT)
            {
                var count = ReadLooped();

                if (count == 0)
                    return;

                _Instance.SubmitBuffer(_Pcm, 0, PcmConverter.ToPcm16(_Samples, count, _Pcm));
            }
        }

        /// <summary>
        /// Fills the sample buffer, going back to the start of the song when it ends, and returns how many samples
        /// it holds (0 only for a song with no audio).
        /// </summary>
        private static int ReadLooped()
        {
            var filled = 0;
            var restarted = false;

            while (filled < _Samples.Length)
            {
                var read = _Reader.ReadSamples(_Samples, filled, _Samples.Length - filled);

                if (read == 0)
                {
                    // Nothing right after a restart means the song is empty
                    if (restarted)
                        break;

                    _Reader.SamplePosition = 0;
                    restarted = true;
                    continue;
                }

                filled += read;
                restarted = false;
            }

            return filled;
        }
    }
}
