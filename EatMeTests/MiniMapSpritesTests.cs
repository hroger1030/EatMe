using EatMeLibrary;

namespace EatMeTests
{
    public class MiniMapSpritesTests
    {
        [TestCase(eMiniMapIcon.Dot, 0)]
        [TestCase(eMiniMapIcon.OwnPacUp, 16)]
        [TestCase(eMiniMapIcon.FlagBase, 144)]
        public void GetCellX_StepsOneCellPerIcon(eMiniMapIcon icon, int expected)
        {
            Assert.That(MiniMapSprites.GetCellX(icon), Is.EqualTo(expected));
        }

        [Test]
        public void GetCellY_SheetIsOneRow()
        {
            foreach (var icon in Enum.GetValues<eMiniMapIcon>())
                Assert.That(MiniMapSprites.GetCellY(icon), Is.EqualTo(0));
        }

        [Test]
        public void Count_MatchesTheEnum()
        {
            Assert.That(Enum.GetValues<eMiniMapIcon>(), Has.Length.EqualTo(MiniMapSprites.COUNT));
        }

        [TestCase(eFacing.Up, eMiniMapIcon.OwnPacUp)]
        [TestCase(eFacing.Down, eMiniMapIcon.OwnPacDown)]
        [TestCase(eFacing.Left, eMiniMapIcon.OwnPacLeft)]
        [TestCase(eFacing.Right, eMiniMapIcon.OwnPacRight)]
        public void GetOwnPacIcon_MatchesFacing(eFacing facing, eMiniMapIcon expected)
        {
            Assert.That(MiniMapSprites.GetOwnPacIcon(facing), Is.EqualTo(expected));
        }
    }
}
