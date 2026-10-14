using EatMeLibrary;

namespace EatMeTests
{
    public class WallTilesTests
    {
        private const int TOP_LEFT = 0;
        private const int TOP_RIGHT = 1;
        private const int BOTTOM_RIGHT = 2;
        private const int BOTTOM_LEFT = 3;

        private static bool GetQuarter(string[] rows, int x, int y, int quarter, out int sourceQuarter, out int quarterTurns)
        {
            return WallTiles.GetQuarter(MazeMovementTests.MakeMap(rows), x, y, quarter, out sourceQuarter, out quarterTurns);
        }

        [TestCase(TOP_LEFT, 0)]
        [TestCase(TOP_RIGHT, 1)]
        [TestCase(BOTTOM_RIGHT, 2)]
        [TestCase(BOTTOM_LEFT, 3)]
        public void GetQuarter_LoneWall_IsOuterCornerTurnedIntoPlace(int quarter, int expectedTurns)
        {
            Assert.That(GetQuarter([".#."], 1, 0, quarter, out var source, out var turns), Is.True);
            Assert.That(source, Is.EqualTo(WallTiles.QUARTER_OUTER_CORNER));
            Assert.That(turns, Is.EqualTo(expectedTurns));
        }

        [Test]
        public void GetQuarter_OffMapCountsAsOpen()
        {
            Assert.That(GetQuarter(["#"], 0, 0, TOP_LEFT, out var source, out _), Is.True);
            Assert.That(source, Is.EqualTo(WallTiles.QUARTER_OUTER_CORNER));
        }

        // The corner tile itself: joined below and right, with the diagonal open, every quarter is drawn unturned.
        [TestCase(TOP_LEFT, WallTiles.QUARTER_OUTER_CORNER)]
        [TestCase(TOP_RIGHT, WallTiles.QUARTER_SECOND_SIDE)]
        [TestCase(BOTTOM_RIGHT, WallTiles.QUARTER_INNER_CORNER)]
        [TestCase(BOTTOM_LEFT, WallTiles.QUARTER_FIRST_SIDE)]
        public void GetQuarter_CornerShape_MatchesCornerTile(int quarter, int expectedSource)
        {
            string[] rows = ["...", ".##", ".#."];

            Assert.That(GetQuarter(rows, 1, 1, quarter, out var source, out var turns), Is.True);
            Assert.That(source, Is.EqualTo(expectedSource));
            Assert.That(turns, Is.Zero);
        }

        [Test]
        public void GetQuarter_HorizontalWall_TopQuartersAreEdges()
        {
            string[] rows = ["...", "###", "..."];

            // top left: west is a wall (its first side), north is open
            Assert.That(GetQuarter(rows, 1, 1, TOP_LEFT, out var source, out var turns), Is.True);
            Assert.That(source, Is.EqualTo(WallTiles.QUARTER_FIRST_SIDE));
            Assert.That(turns, Is.EqualTo(1));

            // top right: east is a wall (its second side), north is open
            Assert.That(GetQuarter(rows, 1, 1, TOP_RIGHT, out source, out turns), Is.True);
            Assert.That(source, Is.EqualTo(WallTiles.QUARTER_SECOND_SIDE));
            Assert.That(turns, Is.Zero);
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        public void GetQuarter_SolidBlock_DrawsNothingInside(int x, int y)
        {
            string[] rows = ["##", "##"];

            // the quarter facing the middle of the block
            var quarter = y == 0 ? (x == 0 ? BOTTOM_RIGHT : BOTTOM_LEFT) : (x == 0 ? TOP_RIGHT : TOP_LEFT);

            Assert.That(GetQuarter(rows, x, y, quarter, out _, out _), Is.False);
        }

        [Test]
        public void GetQuarter_SolidBlock_CentreCellIsEmpty()
        {
            string[] rows = ["###", "###", "###"];

            for (var quarter = 0; quarter < 4; quarter++)
                Assert.That(GetQuarter(rows, 1, 1, quarter, out _, out _), Is.False);
        }

        [Test]
        public void GetQuarter_InsideOfL_IsInnerCorner()
        {
            // the open diagonal is up and right of the corner cell
            string[] rows = ["#.", "##"];

            Assert.That(GetQuarter(rows, 0, 1, TOP_RIGHT, out var source, out var turns), Is.True);
            Assert.That(source, Is.EqualTo(WallTiles.QUARTER_INNER_CORNER));
            Assert.That(turns, Is.EqualTo(3));
        }
    }
}
