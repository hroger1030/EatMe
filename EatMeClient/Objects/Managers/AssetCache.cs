using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace EatMeClient
{
    public static class AssetCache
    {
        private static ContentManager _ContentManager;
        private static readonly Dictionary<string, Texture2D> _Textures = new();
        private static readonly Dictionary<string, SoundEffect> _Sounds = new();
        private static readonly Dictionary<string, Song> _Songs = new();
        private static readonly Dictionary<string, SpriteFont> _Fonts = new();
        private static readonly Dictionary<string, string> _Texts = new();
        private static readonly Dictionary<string, Effect> _Effects = new();

        // Sprite fonts are built with printable ASCII only (32-126), so swap typographic characters for plain ones
        private static readonly (string From, string To)[] ASCII_REPLACEMENTS =
        [
            ("‘", "'"), ("’", "'"), ("“", "\""), ("”", "\""),
            ("–", "-"), ("—", "-"), ("…", "..."), (" ", " "),
        ];


        public static void Initialize(ContentManager contentManager)
        {
            _ContentManager = contentManager;
        }

        public static Texture2D GetTexture(string imagePath) => _Textures[imagePath];

        public static SoundEffect GetSfx(string sfxPath) => _Sounds[sfxPath];

        public static Song GetSong(string songPath) => _Songs[songPath];

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

        public static void LoadSongs(IEnumerable<string> newSongs)
        {
            ArgumentNullException.ThrowIfNull(newSongs);

            foreach (var name in newSongs)
            {
                if (!_Songs.ContainsKey(name))
                    _Songs[name] = _ContentManager.Load<Song>(name);
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
        /// Loads plain text files copied into the content folder (mgcb /copy entries).
        /// </summary>
        public static void LoadTexts(IEnumerable<string> newTexts)
        {
            ArgumentNullException.ThrowIfNull(newTexts);

            foreach (var name in newTexts)
            {
                if (_Texts.ContainsKey(name))
                    continue;

                using var stream = TitleContainer.OpenStream(Path.Combine(_ContentManager.RootDirectory, name));
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
