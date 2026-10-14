using EatMeLibrary;
using Newtonsoft.Json;
using System;
using System.IO;

namespace EatMeClient
{
    /// <summary>
    /// Static object to hold GameSettings instance and load and save it.
    /// </summary>
    public static class ConfigManager
    {
        private const string SETTINGS_FILE_NAME = "settings.ini";

        public static GameSettings Settings { get; set; } = new();

        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, SETTINGS_FILE_NAME);

            if (File.Exists(path))
                Settings = JsonConvert.DeserializeObject<GameSettings>(File.ReadAllText(path)) ?? new GameSettings();

            // a hand edited file could hold anything, so pull brightness and bloom back into range
            Settings.Brightness = DisplayAdjust.ClampBrightness(Settings.Brightness);
            Settings.Bloom = DisplayAdjust.ClampBloom(Settings.Bloom);

            // todo: this is kinda a hack? It works for now.
            MusicManager.SetVolume(Settings.MusicVolume / 100f);
            SoundManager.SetVolume(Settings.SoundVolume / 100f);
        }

        public static void Save()
        {
            var path = Path.Combine(AppContext.BaseDirectory, SETTINGS_FILE_NAME); ;
            File.WriteAllText(path, JsonConvert.SerializeObject(Settings));
        }
    }
}
