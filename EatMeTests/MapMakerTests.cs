using EatMeLibrary;

namespace EatMeTests
{
    public class MapMakerTests
    {
        private static readonly (int X, int Y)[] NEIGHBOURS = [(1, 0), (-1, 0), (0, 1), (0, -1)];

        /// <summary>
        /// Maze sizes and seeds to check: sizes whose coarse maze is odd and even, and the real levels at the default size.
        /// </summary>
        private static IEnumerable<TestCaseData> Maps()
        {
            foreach (int size in new[] { 22, 24, 42 })
                foreach (int seed in new[] { 1, 2, 3 })
                    yield return new TestCaseData(size, seed).SetName($"Size {size}, seed {seed}");

            for (int level = 1; level <= Levels.Count; level++)
                yield return new TestCaseData(new GameConfiguration().MazeSize, Levels.GetSeed(new GameConfiguration().ServerSeed, level)).SetName($"Level {level}");
        }

        private static eGridDef[,] Generate(int size, int seed)
        {
            return MapMaker.Generate(new GameConfiguration { MazeSize = size }, seed);
        }

        private static bool IsOpen(eGridDef[,] map, int x, int y) => !map[x, y].HasFlag(eGridDef.Wall);

        private static int CountOpenNeighbours(eGridDef[,] map, int x, int y)
        {
            int count = 0;
            foreach (var (dx, dy) in NEIGHBOURS)
                if (IsOpen(map, x + dx, y + dy))
                    count++;
            return count;
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_HasFourPowerUpSpawnsOnPlainDotCells(int size, int seed)
        {
            var map = Generate(size, seed);
            var spawns = PowerUpRules.FindSpawns(map);

            Assert.That(spawns, Has.Count.EqualTo(4));

            foreach (var (x, y) in spawns)
                Assert.That(map[x, y], Is.EqualTo(eGridDef.Food | eGridDef.PowerUpSpawn), $"({x}, {y})");
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_PowerUpSpawnsAreSymmetricAndNearTheGhostPen(int size, int seed)
        {
            var map = Generate(size, seed);
            var spawns = PowerUpRules.FindSpawns(map);
            var mirrored = spawns.All(s => spawns.Contains(MapMaker.GetCopy(s.X, s.Y, size, false)));
            var rotated = spawns.All(s => spawns.Contains(MapMaker.GetCopy(s.X, s.Y, size, true)));

            Assert.That(mirrored || rotated, Is.True);
            Assert.That(spawns.Count(s => s.X < size / 2), Is.EqualTo(2));

            foreach (var (x, y) in spawns)
                Assert.That(Math.Max(Math.Abs(x + 0.5f - size / 2), Math.Abs(y + 0.5f - size / 2)), Is.LessThanOrEqualTo(8f), $"({x}, {y})");
        }

        [Test]
        public void Generate_SameSeed_GivesSameMap()
        {
            var map1 = Generate(42, 7);
            var map2 = Generate(42, 7);
            Assert.That(map1, Is.EqualTo(map2));
        }

        [TestCase(MapMaker.MIN_MAZE_SIZE + 1)]
        [TestCase(41)]
        [TestCase(101)]
        public void Generate_OddSize_Throws(int size)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Generate(size, 1));
        }

        [TestCase(MapMaker.MIN_MAZE_SIZE - 2)]
        [TestCase(0)]
        [TestCase(-4)]
        public void Generate_TooSmall_Throws(int size)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Generate(size, 1));
        }

        [TestCase(MapMaker.MIN_MAZE_SIZE)]
        [TestCase(100)]
        public void ValidateMazeSize_EvenAndBigEnough_DoesNotThrow(int size)
        {
            Assert.DoesNotThrow(() => MapMaker.ValidateMazeSize(size));
        }

        [Test]
        public void MinMazeSize_IsEven()
        {
            Assert.That(MapMaker.MIN_MAZE_SIZE % 2, Is.Zero);
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_RightHalfIsCopyOfLeft(int size, int seed)
        {
            var map = Generate(size, seed);
            bool mirrored = true;
            bool rotated = true;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    var (mx, my) = MapMaker.GetCopy(x, y, size, false);
                    var (rx, ry) = MapMaker.GetCopy(x, y, size, true);

                    mirrored &= IsOpen(map, x, y) == IsOpen(map, mx, my);
                    rotated &= IsOpen(map, x, y) == IsOpen(map, rx, ry);
                }

            Assert.That(mirrored || rotated, Is.True);
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_HasNoDeadEnds(int size, int seed)
        {
            var map = Generate(size, seed);

            for (int x = 1; x < size - 1; x++)
                for (int y = 1; y < size - 1; y++)
                    if (IsOpen(map, x, y))
                        Assert.That(CountOpenNeighbours(map, x, y), Is.GreaterThanOrEqualTo(2), $"dead end at ({x}, {y})");
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_EveryOpenCellIsReachable(int size, int seed)
        {
            Assert.That(IsAllReachable(Generate(size, seed)), Is.True);
        }

        private static bool IsAllReachable(eGridDef[,] map)
        {
            int size = map.GetLength(0);
            var seen = new bool[size, size];
            var queue = new Queue<(int X, int Y)>();
            int open = 0;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (IsOpen(map, x, y))
                    {
                        open++;
                        if (queue.Count == 0)
                        {
                            seen[x, y] = true;
                            queue.Enqueue((x, y));
                        }
                    }

            int reached = 0;
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                reached++;

                foreach (var (dx, dy) in NEIGHBOURS)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || ny < 0 || nx >= size || ny >= size || seen[nx, ny] || !IsOpen(map, nx, ny))
                        continue;

                    seen[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }

            return reached == open;
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_HasPlentyOfJunctions(int size, int seed)
        {
            var map = Generate(size, seed);
            int open = 0;
            int junctions = 0;

            for (int x = 1; x < size - 1; x++)
                for (int y = 1; y < size - 1; y++)
                    if (IsOpen(map, x, y))
                    {
                        open++;
                        if (CountOpenNeighbours(map, x, y) >= 3)
                            junctions++;
                    }

            Assert.That(junctions * 100f / open, Is.GreaterThanOrEqualTo(MapMaker.MAZE_JUNCTION_PERCENT - 2));
        }

        [TestCaseSource(nameof(Maps))]
        [TestCase(MapMaker.MIN_MAZE_SIZE, 1)]
        [TestCase(MapMaker.MIN_MAZE_SIZE + 2, 2)]
        public void Generate_FlagsSitInOpenRooms(int size, int seed)
        {
            AssertFlagsInRooms(Generate(size, seed), 1);
        }

        [TestCase(42, 1)]
        [TestCase(100, 2)]
        public void Generate_SeveralFlags_EachSitInOpenRooms(int size, int seed)
        {
            AssertFlagsInRooms(MapMaker.Generate(new GameConfiguration { MazeSize = size, FlagCount = 3 }, seed), 3);
        }

        /// <summary>
        /// Every flag base must be the middle of a fully open 3x3 square, with the expected number per team.
        /// </summary>
        private static void AssertFlagsInRooms(eGridDef[,] map, int flagsPerTeam)
        {
            int size = map.GetLength(0);
            int red = 0;
            int blue = 0;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    bool isRed = map[x, y].HasFlag(eGridDef.FlagBaseRed);
                    bool isBlue = map[x, y].HasFlag(eGridDef.FlagBaseBlue);

                    if (!isRed && !isBlue)
                        continue;

                    if (isRed) red++;
                    if (isBlue) blue++;

                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            Assert.That(IsOpen(map, x + dx, y + dy), Is.True, $"flag at ({x}, {y}) has a wall at ({x + dx}, {y + dy})");
                }

            Assert.That(red, Is.EqualTo(flagsPerTeam));
            Assert.That(blue, Is.EqualTo(flagsPerTeam));
        }

        [TestCaseSource(nameof(Maps))]
        [TestCase(MapMaker.MIN_MAZE_SIZE, 1)]
        [TestCase(MapMaker.MIN_MAZE_SIZE + 2, 2)]
        public void Generate_GhostGeneratorsSitInOpenPen(int size, int seed)
        {
            var map = Generate(size, seed);

            // The generators fill an open GHOST_GENERATOR_SIZE square block in the middle of the map.
            const int SIDE = MapMaker.GHOST_GENERATOR_SIZE;
            int start = size / 2 - SIDE / 2;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    bool inBlock = x >= start && x < start + SIDE && y >= start && y < start + SIDE;

                    Assert.That(map[x, y].HasFlag(eGridDef.GhostGenerator), Is.EqualTo(inBlock), $"ghost generator wrong at ({x}, {y})");

                    if (inBlock)
                        Assert.That(IsOpen(map, x, y), Is.True, $"ghost generator at ({x}, {y}) is a wall");
                }
        }

        /// <summary>
        /// A size x size map of walls with one open corridor running round just inside the edge, so it is
        /// both mirrored and rotated.
        /// </summary>
        private static eGridDef[,] WallsWithCorridor(int size)
        {
            var map = new eGridDef[size, size];

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    bool inside = x > 0 && y > 0 && x < size - 1 && y < size - 1;
                    bool ring = x == 1 || y == 1 || x == size - 2 || y == size - 2;
                    map[x, y] = inside && ring ? (eGridDef)0 : eGridDef.Wall;
                }

            return map;
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        [TestCase(true, 3)]
        public void AddRandomRooms_RoomsAreOpenInRangeAndCopied(bool rotate, int seed)
        {
            const int SIZE = 22;
            var maze = new MapMaker.Maze(10, rotate);
            maze.AddWalls(new Random(seed), MapMaker.MAZE_JUNCTION_PERCENT);
            var map = MapMaker.Expand(maze.ToCells());

            var rooms = MapMaker.AddRandomRooms(map, rotate, new Random(seed), MapMaker.MAX_RANDOM_ROOMS);

            Assert.That(rooms, Has.Count.InRange(1, MapMaker.MAX_RANDOM_ROOMS));
            foreach (var (rx, ry, width, height) in rooms)
            {
                Assert.That(width, Is.InRange(MapMaker.MIN_ROOM_SIZE, MapMaker.MAX_ROOM_SIZE));
                Assert.That(height, Is.InRange(MapMaker.MIN_ROOM_SIZE, MapMaker.MAX_ROOM_SIZE));
                Assert.That(rx, Is.GreaterThanOrEqualTo(1));
                Assert.That(ry, Is.GreaterThanOrEqualTo(1));
                Assert.That(rx + width - 1, Is.LessThanOrEqualTo(SIZE / 2 - 1));
                Assert.That(ry + height - 1, Is.LessThanOrEqualTo(SIZE - 2));

                for (int x = rx; x < rx + width; x++)
                    for (int y = ry; y < ry + height; y++)
                    {
                        var copy = MapMaker.GetCopy(x, y, SIZE, rotate);
                        Assert.That(IsOpen(map, x, y), Is.True, $"({x}, {y})");
                        Assert.That(IsOpen(map, copy.X, copy.Y), Is.True, $"copy of ({x}, {y})");
                    }
            }

            Assert.That(IsAllReachable(map), Is.True);
        }

        [Test]
        public void AddRandomRooms_NothingOpenToTouch_AddsNone()
        {
            var map = new eGridDef[21, 21];
            for (int x = 0; x < 21; x++)
                for (int y = 0; y < 21; y++)
                    map[x, y] = eGridDef.Wall;

            Assert.That(MapMaker.AddRandomRooms(map, false, new Random(1), MapMaker.MAX_RANDOM_ROOMS), Is.Empty);
        }

        [Test]
        public void AddOuterWall_WallsEveryEdgeCell()
        {
            var map = new eGridDef[9, 9];

            MapMaker.AddOuterWall(map);

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    bool edge = x == 0 || y == 0 || x == 8 || y == 8;
                    Assert.That(IsOpen(map, x, y), Is.EqualTo(!edge), $"({x}, {y})");
                }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AddRoom_InSolidWall_IsJoinedToTheMaze(bool rotate)
        {
            var map = WallsWithCorridor(15);

            MapMaker.AddRoom(map, rotate, 4, 6, 2, 3);

            for (int x = 4; x < 6; x++)
                for (int y = 6; y < 9; y++)
                    Assert.That(IsOpen(map, x, y), Is.True, $"({x}, {y})");

            Assert.That(IsAllReachable(map), Is.True);
        }

        [Test]
        public void IsClear_KeepsTheGap()
        {
            var reserved = new List<(int X, int Y, int Width, int Height)> { (5, 5, 3, 3) };

            Assert.That(MapMaker.IsClear(reserved, 8, 5, 2, 2, 0), Is.True);
            Assert.That(MapMaker.IsClear(reserved, 8, 5, 2, 2, 1), Is.False);
            Assert.That(MapMaker.IsClear(reserved, 9, 5, 2, 2, 1), Is.True);
            Assert.That(MapMaker.IsClear(reserved, 6, 6, 1, 1, 0), Is.False);
        }

        [Test]
        public void RemoveSmallWalls_OpensPillarsAndStubs_KeepsLongerWalls()
        {
            var map = new eGridDef[11, 11];
            MapMaker.AddOuterWall(map);
            map[3, 3] = eGridDef.Wall;                                                  // pillar
            map[6, 3] = eGridDef.Wall; map[7, 3] = eGridDef.Wall;                       // stub
            map[3, 6] = eGridDef.Wall; map[4, 6] = eGridDef.Wall; map[5, 6] = eGridDef.Wall;   // wall
            map[8, 8] = eGridDef.Wall; map[9, 8] = eGridDef.Wall;                       // joined to the outer wall

            MapMaker.RemoveSmallWalls(map);

            Assert.That(IsOpen(map, 3, 3), Is.True);
            Assert.That(IsOpen(map, 6, 3) && IsOpen(map, 7, 3), Is.True);
            Assert.That(IsOpen(map, 3, 6) || IsOpen(map, 4, 6) || IsOpen(map, 5, 6), Is.False);
            Assert.That(IsOpen(map, 8, 8), Is.False);
        }

        [TestCase(10, false)]
        [TestCase(10, true)]
        [TestCase(11, true)]
        public void AddWalls_StopsAtTargetAndStaysValid(int rooms, bool rotate)
        {
            var maze = new MapMaker.Maze(rooms, rotate);

            maze.AddWalls(new Random(5), MapMaker.MAZE_JUNCTION_PERCENT);

            Assert.That(maze.IsValid(), Is.True);
            Assert.That(maze.GetJunctionPercent(), Is.LessThanOrEqualTo(MapMaker.MAZE_JUNCTION_PERCENT));
            Assert.That(maze.CountCenterCrossings(), Is.GreaterThanOrEqualTo(MapMaker.Maze.MIN_CENTER_CROSSINGS));
        }

        private static List<(int X, int Y)> FindCells(eGridDef[,] map, eGridDef item)
        {
            var cells = new List<(int X, int Y)>();

            for (int x = 0; x < map.GetLength(0); x++)
                for (int y = 0; y < map.GetLength(1); y++)
                    if (map[x, y].HasFlag(item))
                        cells.Add((x, y));

            return cells;
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_EachSpawnIsAnOpen3x3Room(int size, int seed)
        {
            var map = Generate(size, seed);

            foreach (var spawn in new[] { eGridDef.SpawnPointRed, eGridDef.SpawnPointBlue })
            {
                var cells = FindCells(map, spawn);

                Assert.That(cells, Has.Count.EqualTo(9), spawn.ToString());
                Assert.That(cells.Max(c => c.X) - cells.Min(c => c.X), Is.EqualTo(2), spawn.ToString());
                Assert.That(cells.Max(c => c.Y) - cells.Min(c => c.Y), Is.EqualTo(2), spawn.ToString());

                foreach (var (x, y) in cells)
                {
                    Assert.That(IsOpen(map, x, y), Is.True, $"{spawn} at ({x}, {y})");
                    Assert.That(map[x, y] & (eGridDef.Food | eGridDef.PowerPill | eGridDef.FlagBaseRed | eGridDef.FlagBaseBlue), Is.EqualTo((eGridDef)0), $"{spawn} at ({x}, {y})");
                }
            }
        }

        [TestCaseSource(nameof(Maps))]
        public void Generate_NoFlagBaseOnOrNearASpawn(int size, int seed)
        {
            var map = Generate(size, seed);
            var spawns = FindCells(map, eGridDef.SpawnPointRed).Concat(FindCells(map, eGridDef.SpawnPointBlue)).ToList();
            var bases = FindCells(map, eGridDef.FlagBaseRed).Concat(FindCells(map, eGridDef.FlagBaseBlue)).ToList();

            foreach (var flagBase in bases)
                foreach (var spawn in spawns)
                    Assert.That(Math.Max(Math.Abs(flagBase.X - spawn.X), Math.Abs(flagBase.Y - spawn.Y)), Is.GreaterThanOrEqualTo(3),
                                $"flag base ({flagBase.X}, {flagBase.Y}) by spawn ({spawn.X}, {spawn.Y})");
        }

        [Test]
        public void Generate_SmallestMaps_FitSpawnRooms([Values(MapMaker.MIN_MAZE_SIZE, MapMaker.MIN_MAZE_SIZE + 2)] int size)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var map = Generate(size, seed);
                Assert.That(FindCells(map, eGridDef.SpawnPointRed), Has.Count.EqualTo(MapMaker.SPAWN_AREA_SIZE * MapMaker.SPAWN_AREA_SIZE), $"seed {seed}");
            }
        }

        [TestCaseSource(nameof(Maps))]
        [TestCase(MapMaker.MIN_MAZE_SIZE, 1)]
        [TestCase(MapMaker.MIN_MAZE_SIZE + 2, 2)]
        public void Generate_SpawnAreasHaveAnOpenRingRoundThem(int size, int seed)
        {
            var map = Generate(size, seed);

            foreach (var flag in new[] { eGridDef.SpawnPointRed, eGridDef.SpawnPointBlue })
                foreach (var spawn in FindCells(map, flag))
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int x = spawn.X + dx;
                            int y = spawn.Y + dy;

                            Assert.That(IsOpen(map, x, y), Is.True, $"spawn at ({spawn.X}, {spawn.Y}) has a wall at ({x}, {y})");
                            Assert.That(map[x, y].HasFlag(flag) || (map[x, y] & (eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue)) == 0, Is.True,
                                        $"spawn at ({spawn.X}, {spawn.Y}) has no open ring at ({x}, {y})");
                        }
        }

        [Test]
        public void Expand_AddsOneCell_AndKeepsTheMapSymmetric()
        {
            var maze = new MapMaker.Maze(10, false);
            maze.AddWalls(new Random(3), MapMaker.MAZE_JUNCTION_PERCENT);
            var cells = maze.ToCells();

            var map = MapMaker.Expand(cells);
            int size = map.GetLength(0);

            Assert.That(size, Is.EqualTo(cells.GetLength(0) + 1));
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    var (cx, cy) = MapMaker.GetCopy(x, y, size, false);
                    Assert.That(IsOpen(map, x, y), Is.EqualTo(IsOpen(map, cx, cy)), $"({x}, {y})");
                }
        }

        [Test]
        public void NewMaze_IsFullyOpen()
        {
            var maze = new MapMaker.Maze(5, false);

            Assert.That(maze.GetExits(2, 2), Is.EqualTo(4));
            Assert.That(maze.GetExits(0, 0), Is.EqualTo(2));
            Assert.That(maze.IsValid(), Is.True);
        }

        [Test]
        public void FillRoom_IsCopiedToOtherHalf()
        {
            var maze = new MapMaker.Maze(5, true);

            maze.FillRoom(0, 1);

            Assert.That(maze.IsSolid(0, 1), Is.True);
            Assert.That(maze.IsSolid(4, 3), Is.True);
            Assert.That(maze.GetExits(4, 3), Is.EqualTo(0));
        }
    }
}
