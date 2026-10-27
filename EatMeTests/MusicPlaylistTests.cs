using EatMeLibrary;

namespace EatMeTests
{
    public class MusicPlaylistTests
    {
        [Test]
        public void GetTrackNames_PlaysNormalTracksThenBTracks()
        {
            var names = MusicPlaylist.GetTrackNames(["Track2b.ogg", "Track10.ogg", "Track1b.ogg", "Track2.ogg", "Track1.ogg"]);

            Assert.That(names, Is.EqualTo(new[] { "Music/Track1", "Music/Track2", "Music/Track10", "Music/Track1b", "Music/Track2b" }));
        }

        [Test]
        public void GetTrackNames_LeavesOutOtherFiles()
        {
            var names = MusicPlaylist.GetTrackNames(["TitleTheme.ogg", "SoundFX_Death.ogg", "Track.ogg", "Track1c.ogg", "MyTrack1.ogg", "Track3.ogg"]);

            Assert.That(names, Is.EqualTo(new[] { "Music/Track3" }));
        }

        [Test]
        public void GetTrackNames_IgnoresPaths()
        {
            var names = MusicPlaylist.GetTrackNames([@"C:\Game\Content\Music\Track1.ogg"]);

            Assert.That(names, Is.EqualTo(new[] { "Music/Track1" }));
        }

        [Test]
        public void GetTrackNames_NoTracks_ReturnsEmpty()
        {
            Assert.That(MusicPlaylist.GetTrackNames([]), Is.Empty);
        }

        [TestCase(1, 8, 0)]
        [TestCase(8, 8, 7)]
        [TestCase(9, 8, 0)]
        [TestCase(9, 9, 8)]
        [TestCase(17, 8, 0)]
        [TestCase(5, 1, 0)]
        public void GetTrackForLevel_CyclesThroughTracks(int level, int trackCount, int expected)
        {
            Assert.That(MusicPlaylist.GetTrackForLevel(level, trackCount), Is.EqualTo(expected));
        }

        [TestCase(0, 8)]
        [TestCase(1, 0)]
        public void GetTrackForLevel_BadArguments_Throw(int level, int trackCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MusicPlaylist.GetTrackForLevel(level, trackCount));
        }

        [TestCase(false, false, eResumeMusic.Resume)]
        [TestCase(true, false, eResumeMusic.PlayLevelTrack)]
        [TestCase(false, true, eResumeMusic.StaySilent)]
        [TestCase(true, true, eResumeMusic.StaySilent)]
        public void GetResumeMusic_PicksWhatToPlay(bool levelStartedWhilePaused, bool isIntermission, eResumeMusic expected)
        {
            Assert.That(MusicPlaylist.GetResumeMusic(levelStartedWhilePaused, isIntermission), Is.EqualTo(expected));
        }

        [Test]
        public void GetMusicFile_AddsTheOggExtension()
        {
            Assert.That(MusicPlaylist.GetMusicFile(Const.SONG_TITLE_THEME), Is.EqualTo("Music/TitleTheme.ogg"));
        }
    }
}
