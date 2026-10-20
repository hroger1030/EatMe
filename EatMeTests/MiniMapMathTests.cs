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

        [Test]
        public void GetCellBoxes_OneBoxPerGroupOfTouchingCells()
        {
            var map = new eGridDef[10, 10];

            for (var x = 1; x <= 3; x++)
                for (var y = 2; y <= 4; y++)
                    map[x, y] = eGridDef.SpawnPointRed;

            map[7, 7] = eGridDef.SpawnPointRed;
            map[7, 8] = eGridDef.SpawnPointRed | eGridDef.Food;
            map[5, 5] = eGridDef.SpawnPointBlue;

            var boxes = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointRed);

            Assert.That(boxes, Has.Count.EqualTo(2));
            Assert.That(boxes.Any(b => b.X == 1 && b.Y == 2 && b.Width == 3 && b.Height == 3), Is.True);
            Assert.That(boxes.Any(b => b.X == 7 && b.Y == 7 && b.Width == 1 && b.Height == 2), Is.True);
        }

        [Test]
        public void GetCellBoxes_DiagonalCellsAreSeparateBoxes()
        {
            var map = new eGridDef[4, 4];
            map[1, 1] = eGridDef.GhostGenerator;
            map[2, 2] = eGridDef.GhostGenerator;

            Assert.That(MiniMapMath.GetCellBoxes(map, eGridDef.GhostGenerator), Has.Count.EqualTo(2));
        }

        [Test]
        public void GetCellBoxes_GeneratedMapHasEverySpawnRoomAndOneGhostGenerator()
        {
            var config = new GameConfiguration();
            var map = MapMaker.Generate(config, 1234);

            var red = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointRed);
            var blue = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointBlue);
            var ghosts = MiniMapMath.GetCellBoxes(map, eGridDef.GhostGenerator);

            Assert.That(red, Has.Count.EqualTo(config.PlayerBaseCount));
            Assert.That(blue, Has.Count.EqualTo(config.PlayerBaseCount));
            Assert.That(red.All(b => b.Width == MapMaker.SPAWN_AREA_SIZE && b.Height == MapMaker.SPAWN_AREA_SIZE), Is.True);
            Assert.That(ghosts, Has.Count.EqualTo(1));
            Assert.That(ghosts[0].Width, Is.EqualTo(MapMaker.GHOST_GENERATOR_SIZE));
            Assert.That(ghosts[0].Height, Is.EqualTo(MapMaker.GHOST_GENERATOR_SIZE));
        }
    }
}
