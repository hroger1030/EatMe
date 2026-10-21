using EatMeLibrary;

namespace EatMeTests
{
    public class PacBaseSpritesTests
    {
        [Test]
        public void GetCell_BlueOnTop_RedBelow_FramesLeftToRight()
        {
            Assert.That(PacBaseSprites.GetCellY(eTeam.Red), Is.EqualTo(0));
            Assert.That(PacBaseSprites.GetCellY(eTeam.Blue), Is.EqualTo(48));
            Assert.That(PacBaseSprites.GetGhostCellY(), Is.EqualTo(96));
            Assert.That(PacBaseSprites.GetCellX(0), Is.EqualTo(0));
            Assert.That(PacBaseSprites.GetCellX(1), Is.EqualTo(48));
            Assert.Throws<ArgumentOutOfRangeException>(() => PacBaseSprites.GetCellX(PacBaseSprites.FRAME_COUNT));
            Assert.Throws<ArgumentOutOfRangeException>(() => PacBaseSprites.GetCellX(-1));
        }

        [Test]
        public void GetFrame_AlternatesEveryHalfSecond()
        {
            Assert.That(PacBaseSprites.GetFrame(0f), Is.EqualTo(0));
            Assert.That(PacBaseSprites.GetFrame(0.4f), Is.EqualTo(0));
            Assert.That(PacBaseSprites.GetFrame(0.6f), Is.EqualTo(1));
            Assert.That(PacBaseSprites.GetFrame(1.1f), Is.EqualTo(0));
            Assert.That(PacBaseSprites.GetFrame(-1f), Is.EqualTo(0));
        }
    }
}
