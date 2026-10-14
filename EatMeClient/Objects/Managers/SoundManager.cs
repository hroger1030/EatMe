using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;

namespace EatMeClient
{
    public static class SoundManager
    {
        private const float DEFAULT_VOLUME = 0.2f;

        public static void Initialize(float defaultVolume = DEFAULT_VOLUME)
        {
            SetVolume(defaultVolume);
        }

        public static float GetVolume()
        {
            return SoundEffect.MasterVolume;
        }

        public static void SetVolume(float newVolume)
        {
            SoundEffect.MasterVolume = MathHelper.Clamp(newVolume, 0f, 1f);
        }

        /// <summary>
        /// Plays a one-shot, non-positional SoundEffect (UI sounds) at full volume.
        /// </summary>
        public static void PlaySound(string sfxName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sfxName);

            AssetCache.GetSfx(sfxName).Play();
        }

        /// <summary>
        /// Plays a one-shot SoundEffect at a volume from 0 to 1 (in-world sounds, see SoundMath.GetEventVolume);
        /// nothing plays at 0.
        /// </summary>
        public static void PlaySound(string sfxName, float volume)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sfxName);

            if (volume > 0f)
                AssetCache.GetSfx(sfxName).Play(MathHelper.Clamp(volume, 0f, 1f), 0f, 0f);
        }
    }
}
