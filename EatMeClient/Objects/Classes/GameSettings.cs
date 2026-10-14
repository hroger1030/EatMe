using EatMeLibrary;
using Newtonsoft.Json;

namespace EatMeClient
{
    public class GameSettings
    {
        private const float DEFAULT_VOLUME = 20f;
        private const float DEFAULT_MUSIC_VOLUME = 10f;

        public float MusicVolume { get; set; } = DEFAULT_MUSIC_VOLUME;

        public float SoundVolume { get; set; } = DEFAULT_VOLUME;

        /// <summary>
        /// Whole screen brightness in percent, applied by the Brightness shader; 100 leaves the picture unchanged.
        /// </summary>
        public float Brightness { get; set; } = DisplayAdjust.BRIGHTNESS_DEFAULT;

        /// <summary>
        /// Whole screen colour bloom in percent, applied by the Bloom shader; 0 is off.
        /// </summary>
        public float Bloom { get; set; } = DisplayAdjust.BLOOM_DEFAULT;

        public eControllerType ControllerType { get; set; } = eControllerType.MouseAndKeyboard;

        public string PlayerName { get; set; } = "Player";

        public eTeamPreference PreferredTeam { get; set; } = eTeamPreference.Either;

        /// <summary>
        /// Last server joined by address, as "host" or "host:port".
        /// </summary>
        public string ServerAddress { get; set; } = "127.0.0.1";

        [JsonIgnore]
        public bool Paused { get; set; }

        public GameSettings() { }
    }
}
