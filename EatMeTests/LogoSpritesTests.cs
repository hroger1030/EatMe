using EatMeLibrary;

namespace EatMeTests
{
    public class LogoSpritesTests
    {
        [Test]
        public void GetFrame_StepsEveryFrameSecondsAndLoops()
        {
            Assert.That(LogoSprites.GetFrame(TimeSpan.FromSeconds(-1)), Is.EqualTo(0));
            Assert.That(LogoSprites.GetFrame(TimeSpan.FromMilliseconds(90)), Is.EqualTo(0));
            Assert.That(LogoSprites.GetFrame(TimeSpan.FromMilliseconds(110)), Is.EqualTo(1));
            Assert.That(LogoSprites.GetFrame(TimeSpan.FromMilliseconds(LogoSprites.FRAME_MS * (LogoSprites.FRAME_COUNT + 2.5))), Is.EqualTo(2));
        }

        [Test]
        public void GetCell_FramesStackedTopToBottom()
        {
            Assert.That(LogoSprites.GetCellX(0), Is.EqualTo(0));
            Assert.That(LogoSprites.GetCellY(0), Is.EqualTo(0));
            Assert.That(LogoSprites.GetCellX(1), Is.EqualTo(0));
            Assert.That(LogoSprites.GetCellY(1), Is.EqualTo(200));
            Assert.That(LogoSprites.GetCellX(3), Is.EqualTo(0));
            Assert.That(LogoSprites.GetCellY(3), Is.EqualTo(600));
        }

        [Test]
        public void GetCell_ThrowsOutsideTheSheet()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LogoSprites.GetCellX(LogoSprites.FRAME_COUNT));
            Assert.Throws<ArgumentOutOfRangeException>(() => LogoSprites.GetCellY(-1));
        }
    }
}
