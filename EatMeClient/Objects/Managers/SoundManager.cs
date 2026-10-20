using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;

namespace EatMeClient
{
    public static class SoundManager
    {
        private const float DEFAULT_VOLUME = 0.2f;

        // Not SoundEffect.MasterVolume, which would scale the music too (MusicManager streams through a SoundEffectInstance)
        private static float _Volume;

        public static void Initialize(float defaultVolume = DEFAULT_VOLUME)
        {
            SetVolume(defaultVolume);
        }

        public static float GetVolume()
        {
            return _Volume;
        }

        public static void SetVolume(float newVolume)
        {
            _Volume = MathHelper.Clamp(newVolume, 0f, 1f);
        }

        /// <summary>
        /// Plays a one-shot, non-positional SoundEffect (UI sounds) at full volume.
        /// </summary>
        public static void PlaySound(string sfxName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sfxName);

            AssetCache.GetSfx(sfxName).Play(_Volume, 0f, 0f);
        }

        /// <summary>
        /// Plays a one-shot SoundEffect at a volume from 0 to 1 (in-world sounds, see SoundMath.GetEventVolume);
        /// nothing plays at 0.
        /// </summary>
        public static void PlaySound(string sfxName, float volume)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sfxName);

            if (volume > 0f)
                AssetCache.GetSfx(sfxName).Play(MathHelper.Clamp(volume, 0f, 1f) * _Volume, 0f, 0f);
        }
    }
}
