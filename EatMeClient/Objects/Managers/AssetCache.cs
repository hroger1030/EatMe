using EatMeLibrary;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace EatMeClient
{
    public static class AssetCache
    {
        private static PakContentManager _ContentManager;
        private static readonly Dictionary<string, Texture2D> _Textures = new();
        private static readonly Dictionary<string, SoundEffect> _Sounds = new();
        private static readonly Dictionary<string, byte[]> _Music = new();
        private static readonly Dictionary<string, SpriteFont> _Fonts = new();
        private static readonly Dictionary<string, string> _Texts = new();
        private static readonly Dictionary<string, Effect> _Effects = new();

        // Sprite fonts are built with printable ASCII only (32-126), so swap typographic characters for plain ones
        private static readonly (string From, string To)[] ASCII_REPLACEMENTS =
        [
            ("‘", "'"), ("’", "'"), ("“", "\""), ("”", "\""),
            ("–", "-"), ("—", "-"), ("…", "..."), (" ", " "),
        ];


        public static void Initialize(PakContentManager contentManager)
        {
            _ContentManager = contentManager;
        }

        public static Texture2D GetTexture(string imagePath) => _Textures[imagePath];

        public static SoundEffect GetSfx(string sfxPath) => _Sounds[sfxPath];

        /// <summary>
        /// Opens a song's .ogg for MusicManager to decode and stream: from memory if LoadMusic cached it (the title theme),
        /// otherwise straight from the content (Content.pak when there is one), so level tracks are read only when played.
        /// </summary>
        public static Stream OpenMusic(string songPath)
        {
            ArgumentNullException.ThrowIfNull(songPath);

            return _Music.TryGetValue(songPath, out var music)
                ? new MemoryStream(music, false)
                : _ContentManager.OpenFile(MusicPlaylist.GetMusicFile(songPath));
        }

        public static SpriteFont GetFont(string fontPath) => _Fonts[fontPath];

        public static string GetText(string textPath) => _Texts[textPath];

        public static Effect GetEffect(string effectPath) => _Effects[effectPath];

        // Loaders

        public static void LoadTextures(IEnumerable<string> newTextures)
        {
            ArgumentNullException.ThrowIfNull(newTextures);

            foreach (var name in newTextures)
            {
                if (!_Textures.ContainsKey(name))
                    _Textures[name] = _ContentManager.Load<Texture2D>(name);
            }
        }

        public static void LoadSfx(IEnumerable<string> newSfx)
        {
            ArgumentNullException.ThrowIfNull(newSfx);

            foreach (var name in newSfx)
            {
                if (!_Sounds.ContainsKey(name))
                    _Sounds[name] = _ContentManager.Load<SoundEffect>(name);
            }
        }

        /// <summary>
        /// Keeps each song's .ogg (MusicPlaylist.GetMusicFile) in memory, from Content.pak when there is one; for songs played
        /// often (the title theme). Others are read by OpenMusic when they start.
        /// </summary>
        public static void LoadMusic(IEnumerable<string> newSongs)
        {
            ArgumentNullException.ThrowIfNull(newSongs);

            foreach (var name in newSongs)
            {
                if (_Music.ContainsKey(name))
                    continue;

                using var stream = _ContentManager.OpenFile(MusicPlaylist.GetMusicFile(name));
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                _Music[name] = memory.ToArray();
            }
        }

        public static void LoadFonts()
        {
            var fontList = new List<string> { Const.FONT_JLSSPACEGOTHIC30, Const.FONT_JLSSPACEGOTHIC36, Const.FONT_JLSSPACEGOTHIC48, Const.FONT_JLSSPACEGOTHIC60, Const.FONT_JLSSPACEGOTHIC96, Const.FONT_ARIAL20 };

            foreach (var name in fontList)
            {
                if (_Fonts.ContainsKey(name))
                    continue;

                _Fonts[name] = _ContentManager.Load<SpriteFont>(name);
            }
        }

        public static void LoadEffects(IEnumerable<string> newEffects)
        {
            ArgumentNullException.ThrowIfNull(newEffects);

            foreach (var name in newEffects)
            {
                if (!_Effects.ContainsKey(name))
                    _Effects[name] = _ContentManager.Load<Effect>(name);
            }
        }

        /// <summary>
        /// Loads plain text files copied into the content folder (mgcb /copy entries), from Content.pak when there is one.
        /// </summary>
        public static void LoadTexts(IEnumerable<string> newTexts)
        {
            ArgumentNullException.ThrowIfNull(newTexts);

            foreach (var name in newTexts)
            {
                if (_Texts.ContainsKey(name))
                    continue;

                using var stream = _ContentManager.OpenFile(name);
                using var reader = new StreamReader(stream);
                _Texts[name] = ToAscii(reader.ReadToEnd());
            }
        }

        public static string ToAscii(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            foreach (var (from, to) in ASCII_REPLACEMENTS)
                text = text.Replace(from, to);

            return text;
        }
    }
}
