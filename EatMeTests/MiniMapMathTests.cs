using EatMeLibrary;

namespace EatMeTests
{
    public class MiniMapMathTests
    {
        [TestCase(0f, 0f)]
        [TestCase(50f, 100f)]
        [TestCase(100f, 200f)]
        [TestCase(25.5f, 51f)]
        public void CellToMiniMap_ScalesWholeMapToSide(float cell, float expected)
        {
            Assert.That(MiniMapMath.CellToMiniMap(cell, 100, 200f), Is.EqualTo(expected).Within(0.001f));
        }

        [TestCase(-5f, 0f)]
        [TestCase(150f, 200f)]
        public void CellToMiniMap_OffMapIsClampedToEdge(float cell, float expected)
        {
            Assert.That(MiniMapMath.CellToMiniMap(cell, 100, 200f), Is.EqualTo(expected));
        }

        [Test]
        public void CellToMiniMap_ZeroMapSizeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MiniMapMath.CellToMiniMap(1f, 0, 200f));
        }

        [Test]
        public void GetFlagBases_FindsEachBaseAtItsCellCentre()
        {
            var map = new eGridDef[5, 5];
            map[1, 2] = eGridDef.FlagBaseBlue;
            map[3, 4] = eGridDef.FlagBaseRed;

            var bases = MiniMapMath.GetFlagBases(map);

            Assert.That(bases, Has.Count.EqualTo(2));

            var blue = bases.Single(b => b.Team == eTeam.Blue);
            Assert.That(blue.BaseX, Is.EqualTo(1.5f));
            Assert.That(blue.BaseY, Is.EqualTo(2.5f));

            var red = bases.Single(b => b.Team == eTeam.Red);
            Assert.That(red.BaseX, Is.EqualTo(3.5f));
            Assert.That(red.BaseY, Is.EqualTo(4.5f));
        }

        [Test]
        public void GetFlagBases_NoBasesGivesEmptyList()
        {
            Assert.That(MiniMapMath.GetFlagBases(new eGridDef[5, 5]), Is.Empty);
        }

        [Test]
        public void GetWarpCells_FindsOnlyOpenEdgeCellsAtTheirCentres()
        {
            var map = new eGridDef[5, 5];

            for (var x = 0; x < 5; x++)
                for (var y = 0; y < 5; y++)
                    map[x, y] = eGridDef.Wall;

            map[0, 2] = eGridDef.Food;
            map[4, 2] = eGridDef.Food;
            map[2, 2] = eGridDef.Food;

            var cells = MiniMapMath.GetWarpCells(map);

            Assert.That(cells, Has.Count.EqualTo(2));
            Assert.That(cells.Any(c => c.X == 0.5f && c.Y == 2.5f), Is.True);
            Assert.That(cells.Any(c => c.X == 4.5f && c.Y == 2.5f), Is.True);
        }

        [TestCase(0, 9, true)]
        [TestCase(9, 9, true)]
        [TestCase(5, 0, true)]
        [TestCase(5, 9, false)]
        [TestCase(5, 5, false)]
        [TestCase(5, 3, false)]
        public void IsOnArch_TopHalfRingAndSideLegs(int x, int y, bool expected)
        {
            Assert.That(MiniMapMath.IsOnArch(x, y, 10, 2f), Is.EqualTo(expected));
        }

        [TestCase(0, 0, true)]
        [TestCase(9, 0, true)]
        [TestCase(0, 9, true)]
        [TestCase(9, 9, true)]
        [TestCase(4, 4, true)]
        [TestCase(5, 0, false)]
        [TestCase(0, 5, false)]
        public void IsOnCross_OnlyTheDiagonalsAreSet(int x, int y, bool expected)
        {
            Assert.That(MiniMapMath.IsOnCross(x, y, 10, 2f), Is.EqualTo(expected));
        }

        [TestCase(0, 5, true)]
        [TestCase(9, 5, true)]
        [TestCase(5, 0, true)]
        [TestCase(5, 5, false)]
        [TestCase(0, 0, false)]
        public void IsOnRing_OnlyEdgeBandIsSet(int x, int y, bool expected)
        {
            Assert.That(MiniMapMath.IsOnRing(x, y, 10, 2f), Is.EqualTo(expected));
        }

        [TestCase(3, 0, true)]
        [TestCase(0, 0, false)]
        [TestCase(0, 7, true)]
        [TestCase(7, 7, true)]
        [TestCase(4, 4, true)]
        [TestCase(0, 3, false)]
        public void IsOnTriangle_PointsUpWithFullWidthBase(int x, int y, bool expected)
        {
            Assert.That(MiniMapMath.IsOnTriangle(x, y, 8), Is.EqualTo(expected));
        }

        [TestCase(5, 5, true)]
        [TestCase(5, 0, true)]
        [TestCase(0, 0, false)]
        [TestCase(9, 0, false)]
        [TestCase(5, 9, false)]
        [TestCase(0, 9, false)]
        [TestCase(9, 9, false)]
        public void IsOnStar_FilledCentreAndTopPoint_EmptyCorners(int x, int y, bool expected)
        {
            Assert.That(MiniMapMath.IsOnStar(x, y, 10), Is.EqualTo(expected));
        }

        [Test]
        public void IsOnStar_IsSymmetricLeftToRight()
        {
            for (var y = 0; y < 10; y++)
                for (var x = 0; x < 10; x++)
                    Assert.That(MiniMapMath.IsOnStar(x, y, 10), Is.EqualTo(MiniMapMath.IsOnStar(9 - x, y, 10)), $"({x}, {y})");
        }
    }
}
