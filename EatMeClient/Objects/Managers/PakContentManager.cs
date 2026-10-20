using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;

namespace EatMeClient
{
    /// <summary>
    /// Loads content from Content.pak when the client has one (published builds), and from the loose built files
    /// otherwise, or when the pack doesn't have a file (the music, which is played from files on disk).
    /// </summary>
    public class PakContentManager : ContentManager
    {
        private readonly ContentPack _Pack;

        public PakContentManager(IServiceProvider serviceProvider, string rootDirectory) : base(serviceProvider, rootDirectory)
        {
            _Pack = ContentPack.TryOpen(Path.Combine(AppContext.BaseDirectory, rootDirectory, Const.CONTENT_PACK_FILE));
        }

        /// <summary>
        /// True when content is coming from Content.pak.
        /// </summary>
        public bool IsPacked => _Pack != null;

        protected override Stream OpenStream(string assetName)
        {
            return _Pack?.OpenAsset(assetName) ?? base.OpenStream(assetName);
        }

        /// <summary>
        /// Opens a file copied into the content folder as is (mgcb /copy entries), by its path in the folder.
        /// </summary>
        public Stream OpenFile(string path)
        {
            return _Pack?.OpenFile(path) ?? TitleContainer.OpenStream(Path.Combine(RootDirectory, path));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _Pack?.Dispose();

            base.Dispose(disposing);
        }
    }
}
