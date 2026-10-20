using EatMeLibrary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace EatMeTests
{
    public class ContentPackTests
    {
        private static ContentPack BuildPack(params (string Path, string Text)[] files)
        {
            var memory = new MemoryStream();

            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (path, text) in files)
                {
                    using var writer = new StreamWriter(archive.CreateEntry(path).Open());
                    writer.Write(text);
                }
            }

            memory.Position = 0;
            return new ContentPack(memory);
        }

        private static string ReadAll(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        [Test]
        public void GetAssetPath_AddsTheBuiltExtension()
        {
            Assert.That(ContentPack.GetAssetPath(Const.IMAGE_TITLE_BACKGROUND), Is.EqualTo("Images/title.xnb"));
        }

        [Test]
        public void NormalizePath_UsesForwardSlashesWithNoLeadingSlash()
        {
            Assert.That(ContentPack.NormalizePath(@"\Images\title.xnb"), Is.EqualTo("Images/title.xnb"));
        }

        [Test]
        public void OpenAsset_ReadsTheBuiltFile()
        {
            using var pack = BuildPack(("Images/title.xnb", "title"), ("Audio/SoundFX_Tic1.xnb", "tick"));

            using var stream = pack.OpenAsset(Const.SFX_TICK);

            Assert.That(stream.CanSeek, Is.True);
            Assert.That(ReadAll(stream), Is.EqualTo("tick"));
        }

        [Test]
        public void OpenFile_IgnoresCaseAndSlashDirection()
        {
            using var pack = BuildPack((@"Text\story.txt", "story"));

            using var stream = pack.OpenFile("TEXT/Story.txt");

            Assert.That(ReadAll(stream), Is.EqualTo("story"));
        }

        [Test]
        public void OpenFile_IsNullForAFileNotInThePack()
        {
            using var pack = BuildPack(("Images/title.xnb", "title"));

            Assert.That(pack.OpenAsset("Music/Track1"), Is.Null);
            Assert.That(pack.Contains("Images/title.xnb"), Is.True);
            Assert.That(pack.Contains("Images/logo.xnb"), Is.False);
        }

        [Test]
        public void OpenFile_CanReadTheSameFileTwice()
        {
            using var pack = BuildPack(("Images/title.xnb", "title"));

            using (var first = pack.OpenFile("Images/title.xnb"))
                Assert.That(ReadAll(first), Is.EqualTo("title"));

            using var second = pack.OpenFile("Images/title.xnb");
            Assert.That(ReadAll(second), Is.EqualTo("title"));
        }

        [Test]
        public void Count_LeavesOutFolderEntries()
        {
            var memory = new MemoryStream();

            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
            {
                archive.CreateEntry("Images/");
                archive.CreateEntry("Images/title.xnb");
            }

            memory.Position = 0;
            using var pack = new ContentPack(memory);

            Assert.That(pack.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryOpen_IsNullWhenThereIsNoPack()
        {
            Assert.That(ContentPack.TryOpen(Path.Combine(Path.GetTempPath(), "EatMeNoSuchPack", Const.CONTENT_PACK_FILE)), Is.Null);
        }

        [Test]
        public void GetFiles_ListsOnlyTheFolder()
        {
            using var pack = BuildPack(("Music/Track1.ogg", "a"), ("Music/Track1.xnb", "b"), ("Images/title.xnb", "c"), ("MusicNotes.txt", "d"));

            Assert.That(pack.GetFiles("Music"), Is.EquivalentTo(new[] { "Music/Track1.ogg", "Music/Track1.xnb" }));
        }

        [Test]
        public void GetFiles_IgnoresCaseAndSlashDirection()
        {
            using var pack = BuildPack(("Music/Track1.ogg", "a"));

            Assert.That(pack.GetFiles(@"\music\"), Is.EquivalentTo(new[] { "Music/Track1.ogg" }));
        }

        [Test]
        public void GetFiles_IsEmptyForAFolderNotInThePack()
        {
            using var pack = BuildPack(("Images/title.xnb", "a"));

            Assert.That(pack.GetFiles("Music"), Is.Empty);
        }
    }
}
