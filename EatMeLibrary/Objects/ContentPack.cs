using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace EatMeLibrary
{
    /// <summary>
    /// The client's packed content: a zip of the built content files, looked up by their path in the Content folder.
    /// </summary>
    public sealed class ContentPack : IDisposable
    {
        /// <summary>
        /// Extension the content pipeline gives every built asset.
        /// </summary>
        public const string ASSET_EXTENSION = ".xnb";

        private readonly ZipArchive _Archive;
        private readonly Dictionary<string, ZipArchiveEntry> _Entries = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _Lock = new();

        public ContentPack(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _Archive = new ZipArchive(stream, ZipArchiveMode.Read);

            foreach (var entry in _Archive.Entries)
            {
                // Folders are entries too, with no name
                if (!string.IsNullOrEmpty(entry.Name))
                    _Entries[NormalizePath(entry.FullName)] = entry;
            }
        }

        /// <summary>
        /// Number of files in the pack.
        /// </summary>
        public int Count => _Entries.Count;

        /// <summary>
        /// Opens the pack at path, or returns null if there isn't one.
        /// </summary>
        public static ContentPack TryOpen(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return File.Exists(path) ? new ContentPack(File.OpenRead(path)) : null;
        }

        /// <summary>
        /// A path as the pack stores it: forward slashes, no leading slash.
        /// </summary>
        public static string NormalizePath(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return path.Replace('\\', '/').TrimStart('/');
        }

        /// <summary>
        /// The pack path of a built asset from its content name, e.g. "Images/title" is "Images/title.xnb".
        /// </summary>
        public static string GetAssetPath(string assetName)
        {
            ArgumentNullException.ThrowIfNull(assetName);

            return NormalizePath(assetName) + ASSET_EXTENSION;
        }

        public bool Contains(string path) => _Entries.ContainsKey(NormalizePath(path));

        /// <summary>
        /// Reads a file out of the pack into a seekable stream, or returns null if the pack doesn't have it.
        /// </summary>
        public Stream OpenFile(string path)
        {
            if (!_Entries.TryGetValue(NormalizePath(path), out var entry))
                return null;

            // A zip archive can only read one entry at a time
            lock (_Lock)
            {
                var memory = new MemoryStream((int)entry.Length);

                using (var stream = entry.Open())
                    stream.CopyTo(memory);

                memory.Position = 0;
                return memory;
            }
        }

        /// <summary>
        /// Opens a built asset by its content name, or returns null if the pack doesn't have it.
        /// </summary>
        public Stream OpenAsset(string assetName) => OpenFile(GetAssetPath(assetName));

        public void Dispose()
        {
            _Archive.Dispose();
        }
    }
}
