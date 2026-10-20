using System;
using System.Collections.Generic;
using System.Text;

namespace EatMeLibrary
{
    /// <summary>
    /// Builds a random maze of wall and open cells. Blue owns the left half, red owns the right half.
    /// Only the left half is generated; the right half is a copy of it, either mirrored across the
    /// vertical center line or rotated 180 degrees about the center (picked at random per map).
    /// </summary>
    public static class MapMaker
    {
        /// <summary>
        /// Smallest map with room for the ghost pen and the flag rooms beside it. Map sizes must be even.
        /// </summary>
        public const int MIN_MAZE_SIZE = 20;

        /// <summary>
        /// Walls are added until this share of the maze's open cells are junctions (3 or 4 ways out).
        /// Higher means more choices for the player. A fully open grid is about 33%.
        /// This is a constant, not a setting, because clients must build the same map as the server.
        /// </summary>
        public const int MAZE_JUNCTION_PERCENT = 28;

        /// <summary>
        /// Smallest and largest side of a random room, in cells. Rooms can be any rectangle in this range.
        /// </summary>
        public const int MIN_ROOM_SIZE = 2;
        public const int MAX_ROOM_SIZE = 5;

        /// <summary>
        /// Most random rooms per half. Each map gets 0 to this many.
        /// </summary>
        public const int MAX_RANDOM_ROOMS = 4;

        /// <summary>
        /// Smallest and largest flag room side, in cells. The base sits in the middle with open cells all round.
        /// </summary>
        public const int MIN_FLAG_ROOM_SIZE = 3;
        public const int MAX_FLAG_ROOM_SIZE = 5;

        /// <summary>
        /// Spawn room side, in cells: the spawn tiles with an open ring one cell wide all round them.
        /// </summary>
        public const int SPAWN_ROOM_SIZE = 5;

        /// <summary>
        /// Side of the square of spawn tiles in the middle of each spawn room, in cells.
        /// </summary>
        public const int SPAWN_AREA_SIZE = 3;

        /// <summary>
        /// Side of the ghost generator block in the middle of the map, in cells.
        /// </summary>
        public const int GHOST_GENERATOR_SIZE = 4;

        /// <summary>
        /// Side of the ghost pen: the generator with one open cell all round it.
        /// </summary>
        public const int GHOST_PEN_SIZE = GHOST_GENERATOR_SIZE + 2;

        /// <summary>
        /// Walls inside the map with fewer cells than this are removed, so there are no lone pillars or stubs.
        /// </summary>
        public const int MIN_WALL_CELLS = 3;

        private static readonly (int X, int Y)[] NEIGHBOURS = [(1, 0), (-1, 0), (0, 1), (0, -1)];

        // mixed into the level seed for PickGhostLineup, so its Random doesn't run in step with the maze's
        private const int GHOST_LINEUP_SALT = 0x6A05;

        /// <summary>
        /// Generates a map. The same config and seed always produce the same map. The steps are: the maze,
        /// random rooms, the outer wall, the warp tunnels, the special rooms (ghost pen, flags, spawns), the items, then the
        /// power up spawn points.
        /// </summary>
        public static eGridDef[,] Generate(GameConfiguration config, int seed)
        {
            ArgumentNullException.ThrowIfNull(config);
            ValidateMazeSize(config.MazeSize);

            var random = new Random(seed);
            bool rotate = random.Next(2) == 0;

            // 1. The maze is built on a coarse grid of rooms, starting fully open and adding walls, then
            // expanded so that rooms and the passages between them become cells, with a wall cell
            // wherever a passage is closed or a room is solid.
            var maze = new Maze((int)(config.MazeSize * 0.5f) - 1, rotate);
            maze.AddWalls(random, MAZE_JUNCTION_PERCENT);
            var output = Expand(maze.ToCells());

            // 2-4.
            AddRandomRooms(output, rotate, random, random.Next(MAX_RANDOM_ROOMS + 1));
            AddOuterWall(output);
            OpenWarpTunnels(output, config.WarpTunnelCount, rotate, random);

            // 5. Special rooms, each kept clear of the ones before it.
            var ghostPen = AddGhostPen(output, rotate);
            var reserved = new List<(int X, int Y, int Width, int Height)> { ghostPen };
            var flagCells = AddFlagRooms(output, rotate, random, config.FlagCount, reserved);
            var spawnRooms = AddSpawnRooms(output, rotate, random, config.PlayerBaseCount, reserved);

            RemoveSmallWalls(output);
            PlaceItems(output, config, rotate, random, flagCells, spawnRooms);
            AddPowerUpSpawns(output, rotate, ghostPen);

            return output;
        }

        /// <summary>
        /// Picks a personality for every ghost in a level (index = ghost id), GameConfiguration.GhostCount of them. The same
        /// config, seed and level always give the same lineup, so clients work it out without being told. Its Random is
        /// separate from Generate's, so picking ghosts doesn't change the map. No personality appears twice except Norm,
        /// which fills every slot past the last unique one. Odd levels always have Norm; on even levels Norm is drawn
        /// GhostPersonalities.NORM_WEIGHT times as often as any other ghost.
        /// </summary>
        public static GhostPersonality[] PickGhostLineup(GameConfiguration config, int seed, int level)
        {
            ArgumentNullException.ThrowIfNull(config);

            var count = Math.Max(0, config.GhostCount);
            var lineup = new GhostPersonality[count];

            if (count == 0)
                return lineup;

            var random = new Random(unchecked((seed * 397) ^ GHOST_LINEUP_SALT));
            var pool = new List<GhostPersonality>(GhostPersonalities.ALL.Count);

            foreach (var personality in GhostPersonalities.ALL)
                if (personality != GhostPersonalities.NORM)
                    pool.Add(personality);

            var picked = 0;
            var normInPool = true;

            if (level % 2 == 1)
            {
                lineup[picked++] = GhostPersonalities.NORM;
                normInPool = false;
            }

            while (picked < count && (pool.Count > 0 || normInPool))
            {
                var roll = random.Next(pool.Count + (normInPool ? GhostPersonalities.NORM_WEIGHT : 0));

                if (normInPool)
                {
                    if (roll < GhostPersonalities.NORM_WEIGHT)
                    {
                        lineup[picked++] = GhostPersonalities.NORM;
                        normInPool = false;
                        continue;
                    }

                    roll -= GhostPersonalities.NORM_WEIGHT;
                }

                lineup[picked++] = pool[roll];
                pool.RemoveAt(roll);
            }

            while (picked < count)
                lineup[picked++] = GhostPersonalities.NORM;

            // shuffled, so a guaranteed Norm isn't always ghost 0 on the same spawn cell
            for (var i = count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (lineup[i], lineup[j]) = (lineup[j], lineup[i]);
            }

            return lineup;
        }

        /// <summary>
        /// Throws if size can't be a map: it must be even, so the two halves meet on a line between two
        /// columns with no center column, and at least MIN_MAZE_SIZE.
        /// </summary>
        public static void ValidateMazeSize(int size)
        {
            if (size < MIN_MAZE_SIZE)
                throw new ArgumentOutOfRangeException(nameof(size), size, $"MazeSize must be at least {MIN_MAZE_SIZE}.");

            if (size % 2 != 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "MazeSize must be an even number.");
        }

        /// <summary>
        /// Carves count random rooms into the left half (and their copies), each MIN_ROOM_SIZE to MAX_ROOM_SIZE
        /// cells on a side. Rooms stay off the outer ring and must touch an open cell, so they join the maze.
        /// A room with no spot is skipped. Returns the rooms added.
        /// </summary>
        public static List<(int X, int Y, int Width, int Height)> AddRandomRooms(eGridDef[,] grid, bool rotate, Random random, int count)
        {
            int size = grid.GetLength(0);
            int lastX = (int)(size * 0.5f) - 1;
            var rooms = new List<(int X, int Y, int Width, int Height)>(count);
            var spots = new List<(int X, int Y)>();

            for (int i = 0; i < count; i++)
            {
                int width = random.Next(MIN_ROOM_SIZE, MAX_ROOM_SIZE + 1);
                int height = random.Next(MIN_ROOM_SIZE, MAX_ROOM_SIZE + 1);

                spots.Clear();
                for (int x = 1; x + width - 1 <= lastX; x++)
                    for (int y = 1; y + height - 1 <= size - 2; y++)
                        if (TouchesOpen(grid, x, y, width, height))
                            spots.Add((x, y));

                if (spots.Count == 0)
                    continue;

                var (rx, ry) = spots[random.Next(spots.Count)];
                CarveRoom(grid, rotate, rx, ry, width, height);
                rooms.Add((rx, ry, width, height));
            }

            return rooms;
        }

        /// <summary>
        /// Makes every cell on the edge of the map a wall.
        /// </summary>
        public static void AddOuterWall(eGridDef[,] grid)
        {
            int last = grid.GetLength(0) - 1;

            for (int i = 0; i <= last; i++)
            {
                grid[i, 0] |= eGridDef.Wall;
                grid[i, last] |= eGridDef.Wall;
                grid[0, i] |= eGridDef.Wall;
                grid[last, i] |= eGridDef.Wall;
            }
        }

        /// <summary>
        /// Carves the ghost pen in the center of the map: the GHOST_GENERATOR_SIZE square ghost generator block with one
        /// open cell all round it. Returns the pen.
        /// </summary>
        public static (int X, int Y, int Width, int Height) AddGhostPen(eGridDef[,] grid, bool rotate)
        {
            int start = (int)(grid.GetLength(0) * 0.5f) - (int)(GHOST_PEN_SIZE * 0.5f);

            AddRoom(grid, rotate, start, start, GHOST_PEN_SIZE, GHOST_PEN_SIZE);
            return (start, start, GHOST_PEN_SIZE, GHOST_PEN_SIZE);
        }

        /// <summary>
        /// Carves a room for each flag in the back quarter of the left half, at a random height and of random
        /// size, not overlapping any reserved room. Each room is added to reserved. Returns the center cell
        /// of each room, where its flag base goes.
        /// </summary>
        public static List<(int X, int Y)> AddFlagRooms(eGridDef[,] grid, bool rotate, Random random, int count,
                                                        List<(int X, int Y, int Width, int Height)> reserved)
        {
            int size = grid.GetLength(0);
            int back = (int)(size * 0.25f);
            var centers = new List<(int X, int Y)>(count);
            var spots = new List<(int X, int Y)>();

            for (int i = 0; i < count; i++)
            {
                int width = random.Next(MIN_FLAG_ROOM_SIZE, MAX_FLAG_ROOM_SIZE + 1);
                int height = random.Next(MIN_FLAG_ROOM_SIZE, MAX_FLAG_ROOM_SIZE + 1);

                spots.Clear();
                for (int x = 1; x <= Math.Max(1, back - width + 1); x++)
                    for (int y = 1; y + height - 1 <= size - 2; y++)
                        if (IsClear(reserved, x, y, width, height, 0))
                            spots.Add((x, y));

                if (spots.Count == 0)
                    throw new InvalidOperationException("Not enough space for the flag rooms.");

                var (rx, ry) = spots[random.Next(spots.Count)];
                AddRoom(grid, rotate, rx, ry, width, height);
                reserved.Add((rx, ry, width, height));
                centers.Add((rx + (int)(width * 0.5f), ry + (int)(height * 0.5f)));
            }

            return centers;
        }

        /// <summary>
        /// Carves a SPAWN_ROOM_SIZE square room for each team spawn, kept at least one cell clear of every
        /// reserved room so no flag base is on or near it. It goes in the back quarter of the left half if it
        /// can, otherwise anywhere in the left half. Each room is added to reserved. Returns each room's top
        /// left cell and side.
        /// </summary>
        public static List<(int X, int Y, int Side)> AddSpawnRooms(eGridDef[,] grid, bool rotate, Random random, int count,
                                                                   List<(int X, int Y, int Width, int Height)> reserved)
        {
            int size = grid.GetLength(0);
            int back = (int)(size * 0.25f);
            int lastX = (int)(size * 0.5f) - SPAWN_ROOM_SIZE;
            var rooms = new List<(int X, int Y, int Side)>(count);
            var spots = new List<(int X, int Y)>();

            for (int i = 0; i < count; i++)
            {
                spots.Clear();
                for (int pass = 0; pass < 2 && spots.Count == 0; pass++)
                {
                    int maxX = pass == 0 ? Math.Min(lastX, Math.Max(1, back - SPAWN_ROOM_SIZE + 1)) : lastX;

                    for (int x = 1; x <= maxX; x++)
                        for (int y = 1; y + SPAWN_ROOM_SIZE - 1 <= size - 2; y++)
                            if (IsClear(reserved, x, y, SPAWN_ROOM_SIZE, SPAWN_ROOM_SIZE, 1))
                                spots.Add((x, y));
                }

                if (spots.Count == 0)
                    throw new InvalidOperationException("Not enough space for the spawn rooms.");

                var (rx, ry) = spots[random.Next(spots.Count)];
                AddRoom(grid, rotate, rx, ry, SPAWN_ROOM_SIZE, SPAWN_ROOM_SIZE);
                reserved.Add((rx, ry, SPAWN_ROOM_SIZE, SPAWN_ROOM_SIZE));
                rooms.Add((rx, ry, SPAWN_ROOM_SIZE));
            }

            return rooms;
        }

        /// <summary>
        /// True if the room, grown by gap cells on every side, overlaps none of the reserved rooms.
        /// </summary>
        public static bool IsClear(List<(int X, int Y, int Width, int Height)> reserved, int x, int y, int width, int height, int gap)
        {
            foreach (var room in reserved)
                if (x - gap < room.X + room.Width && room.X < x + width + gap &&
                    y - gap < room.Y + room.Height && room.Y < y + height + gap)
                    return false;

            return true;
        }

        /// <summary>
        /// Carves an open room and its copy, and if it touched no open cell, digs a passage from it to the
        /// nearest one so it can be reached.
        /// </summary>
        public static void AddRoom(eGridDef[,] grid, bool rotate, int x, int y, int width, int height)
        {
            bool joined = TouchesOpen(grid, x, y, width, height);

            CarveRoom(grid, rotate, x, y, width, height);

            if (!joined)
                ConnectRoom(grid, rotate, x, y, width, height);
        }

        /// <summary>
        /// Opens every cell of the room and of its copy on the other half.
        /// </summary>
        public static void CarveRoom(eGridDef[,] grid, bool rotate, int x, int y, int width, int height)
        {
            int size = grid.GetLength(0);

            for (int cx = x; cx < x + width; cx++)
                for (int cy = y; cy < y + height; cy++)
                {
                    var copy = GetCopy(cx, cy, size, rotate);
                    grid[cx, cy] &= ~eGridDef.Wall;
                    grid[copy.X, copy.Y] &= ~eGridDef.Wall;
                }
        }

        /// <summary>
        /// True if any cell of the room, or any cell beside one of its sides, is open.
        /// </summary>
        public static bool TouchesOpen(eGridDef[,] grid, int x, int y, int width, int height)
        {
            int size = grid.GetLength(0);

            for (int cx = x - 1; cx <= x + width; cx++)
                for (int cy = y - 1; cy <= y + height; cy++)
                {
                    bool corner = (cx == x - 1 || cx == x + width) && (cy == y - 1 || cy == y + height);

                    if (!corner && cx >= 0 && cy >= 0 && cx < size && cy < size && IsOpen(grid, cx, cy))
                        return true;
                }

            return false;
        }

        /// <summary>
        /// Digs the shortest passage through the walls (inside the outer ring) from an open room to the nearest
        /// open cell outside it, and the copy of that passage.
        /// </summary>
        public static void ConnectRoom(eGridDef[,] grid, bool rotate, int x, int y, int width, int height)
        {
            int size = grid.GetLength(0);
            var from = new Dictionary<(int X, int Y), (int X, int Y)>();
            var queue = new Queue<(int X, int Y)>();

            for (int cx = x; cx < x + width; cx++)
                for (int cy = y; cy < y + height; cy++)
                {
                    from[(cx, cy)] = (cx, cy);
                    queue.Enqueue((cx, cy));
                }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();

                foreach (var (dx, dy) in NEIGHBOURS)
                {
                    var next = (X: cell.X + dx, Y: cell.Y + dy);

                    if (next.X < 1 || next.Y < 1 || next.X > size - 2 || next.Y > size - 2 || from.ContainsKey(next))
                        continue;

                    if (IsOpen(grid, next.X, next.Y))
                    {
                        // Open the walls walked through, back to the room.
                        for (var step = cell; from[step] != step; step = from[step])
                        {
                            var copy = GetCopy(step.X, step.Y, size, rotate);
                            grid[step.X, step.Y] &= ~eGridDef.Wall;
                            grid[copy.X, copy.Y] &= ~eGridDef.Wall;
                        }

                        return;
                    }

                    from[next] = cell;
                    queue.Enqueue(next);
                }
            }

            throw new InvalidOperationException("A room could not be joined to the maze.");
        }

        /// <summary>
        /// Opens every wall inside the map (not joined to the outer wall) made of fewer than MIN_WALL_CELLS
        /// cells. Its cells only have open cells around them, so this makes no dead ends.
        /// </summary>
        public static void RemoveSmallWalls(eGridDef[,] grid)
        {
            int size = grid.GetLength(0);
            var seen = new bool[size, size];
            var wall = new List<(int X, int Y)>();
            var queue = new Queue<(int X, int Y)>();

            for (int x = 1; x < size - 1; x++)
                for (int y = 1; y < size - 1; y++)
                {
                    if (seen[x, y] || IsOpen(grid, x, y))
                        continue;

                    wall.Clear();
                    bool onEdge = false;
                    seen[x, y] = true;
                    queue.Enqueue((x, y));

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        wall.Add((cx, cy));
                        onEdge |= cx == 0 || cy == 0 || cx == size - 1 || cy == size - 1;

                        foreach (var (dx, dy) in NEIGHBOURS)
                        {
                            int nx = cx + dx;
                            int ny = cy + dy;

                            if (nx < 0 || ny < 0 || nx >= size || ny >= size || seen[nx, ny] || IsOpen(grid, nx, ny))
                                continue;

                            seen[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }

                    if (!onEdge && wall.Count < MIN_WALL_CELLS)
                        foreach (var (cx, cy) in wall)
                            grid[cx, cy] &= ~eGridDef.Wall;
                }
        }

        /// <summary>
        /// Renders a map as ASCII art, two characters per cell. Walls are "##". Open cell contents:
        /// G ghost generator, R/B red/blue flag base, r/b red/blue spawn point, O power pill, . food,
        /// space empty.
        /// </summary>
        public static string Print(eGridDef[,] map)
        {
            ArgumentNullException.ThrowIfNull(map);

            int width = map.GetLength(0);
            int height = map.GetLength(1);
            var sb = new StringBuilder();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, y].HasFlag(eGridDef.Wall))
                        sb.Append("##");
                    else
                        sb.Append(GetGlyph(map[x, y])).Append(' ');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static char GetGlyph(eGridDef cell)
        {
            if (cell.HasFlag(eGridDef.GhostGenerator)) return 'G';
            if (cell.HasFlag(eGridDef.FlagBaseRed)) return 'R';
            if (cell.HasFlag(eGridDef.FlagBaseBlue)) return 'B';
            if (cell.HasFlag(eGridDef.SpawnPointRed)) return 'r';
            if (cell.HasFlag(eGridDef.SpawnPointBlue)) return 'b';
            if (cell.HasFlag(eGridDef.PowerPill)) return 'O';
            if (cell.HasFlag(eGridDef.PowerUpSpawn)) return 'P';
            if (cell.HasFlag(eGridDef.Food)) return '.';
            return ' ';
        }

        /// <summary>
        /// Returns the cell that (x, y) is copied to on the other half of the map.
        /// </summary>
        public static (int X, int Y) GetCopy(int x, int y, int size, bool rotate)
        {
            return rotate ? (size - 1 - x, size - 1 - y) : (size - 1 - x, y);
        }

        /// <summary>
        /// Converts the expanded cell grid (always odd sized) to a map one cell bigger, which is even. The
        /// center row and column are duplicated, which keeps the map symmetric.
        /// </summary>
        public static eGridDef[,] Expand(bool[,] open)
        {
            int center = (int)((open.GetLength(0) - 1) * 0.5f);
            int size = open.GetLength(0) + 1;
            var grid = new eGridDef[size, size];

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    int sx = x > center ? x - 1 : x;
                    int sy = y > center ? y - 1 : y;

                    if (!open[sx, sy])
                        grid[x, y] = eGridDef.Wall;
                }

            return grid;
        }

        /// <summary>
        /// Cuts holes in the outer wall. A hole on one side warps to the hole directly opposite it,
        /// so holes are always cut in opposite pairs (a tunnel) where both ends lead into an open cell.
        /// The symmetric copy of each tunnel is cut too, so the count may overshoot by one.
        /// </summary>
        public static void OpenWarpTunnels(eGridDef[,] grid, int count, bool rotate, Random random)
        {
            int size = grid.GetLength(0);
            int last = size - 1;
            var rows = new List<int>();
            var columns = new List<int>();

            for (int i = 1; i < last; i++)
            {
                if (IsOpen(grid, 1, i) && IsOpen(grid, last - 1, i))
                    rows.Add(i);

                if (IsOpen(grid, i, 1) && IsOpen(grid, i, last - 1))
                    columns.Add(i);
            }

            Shuffle(rows, random);
            Shuffle(columns, random);

            var tunnels = new HashSet<(bool Horizontal, int Index)>();

            while (tunnels.Count < count && (rows.Count > 0 || columns.Count > 0))
            {
                bool horizontal = columns.Count == 0 || (rows.Count > 0 && random.Next(2) == 0);
                var list = horizontal ? rows : columns;
                int index = list[^1];
                list.RemoveAt(list.Count - 1);

                var (ax, ay) = horizontal ? (0, index) : (index, 0);
                var (bx, by) = horizontal ? (last, index) : (index, last);

                foreach (var (x, y) in new[] { (ax, ay), (bx, by), GetCopy(ax, ay, size, rotate), GetCopy(bx, by, size, rotate) })
                {
                    grid[x, y] &= ~eGridDef.Wall;
                    tunnels.Add(horizontal ? (true, y) : (false, x));
                }
            }
        }

        /// <summary>
        /// Places the map items. Red flag bases go on flagCells (the centers of the flag rooms) and blue
        /// ones on their copies. The SPAWN_AREA_SIZE square in the middle of each spawn room is blue spawn tiles, and its
        /// copy red ones; the ring round them gets dots.
        /// </summary>
        public static void PlaceItems(eGridDef[,] grid, GameConfiguration config, bool rotate, Random random,
                                      List<(int X, int Y)> flagCells, List<(int X, int Y, int Side)> spawnRooms)
        {
            int size = grid.GetLength(0);
            var used = new HashSet<(int X, int Y)>();

            // The ghost generator is a GHOST_GENERATOR_SIZE square block in the middle of the ghost pen. Its left half
            // is placed and copied, which fills the block whether the map is mirrored or rotated.
            int halfWidth = (int)(size * 0.5f);
            int ghostStart = halfWidth - (int)(GHOST_GENERATOR_SIZE * 0.5f);

            for (int x = ghostStart; x < halfWidth; x++)
                for (int y = ghostStart; y < ghostStart + GHOST_GENERATOR_SIZE; y++)
                {
                    var ghostCopy = GetCopy(x, y, size, rotate);

                    grid[x, y] |= eGridDef.GhostGenerator;
                    grid[ghostCopy.X, ghostCopy.Y] |= eGridDef.GhostGenerator;
                    used.Add((x, y));
                    used.Add(ghostCopy);
                }

            // Team items are placed on the left (blue) and copied to the right (red). Flags and spawns sit in their rooms.

            foreach (var cell in flagCells)
            {
                var copy = GetCopy(cell.X, cell.Y, size, rotate);

                grid[cell.X, cell.Y] |= eGridDef.FlagBaseBlue;
                grid[copy.X, copy.Y] |= eGridDef.FlagBaseRed;
                used.Add(cell);
                used.Add(copy);
            }

            int ring = (int)((SPAWN_ROOM_SIZE - SPAWN_AREA_SIZE) * 0.5f);

            foreach (var room in spawnRooms)
                for (int x = room.X + ring; x < room.X + ring + SPAWN_AREA_SIZE; x++)
                    for (int y = room.Y + ring; y < room.Y + ring + SPAWN_AREA_SIZE; y++)
                    {
                        var copy = GetCopy(x, y, size, rotate);

                        grid[x, y] |= eGridDef.SpawnPointBlue;
                        grid[copy.X, copy.Y] |= eGridDef.SpawnPointRed;
                        used.Add((x, y));
                        used.Add(copy);
                    }

            // Power pills are split evenly between the halves.
            for (int i = 0; i < (int)(config.PowerPillCount * 0.5f); i++)
                PlacePair(grid, used, random, rotate, halfWidth - 1, eGridDef.PowerPill, eGridDef.PowerPill);

            // Every other open cell gets food.
            const eGridDef OCCUPIED = eGridDef.Wall | eGridDef.PowerPill | eGridDef.FlagBaseRed | eGridDef.FlagBaseBlue |
                                      eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue | eGridDef.GhostGenerator;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if ((grid[x, y] & OCCUPIED) == 0)
                        grid[x, y] |= eGridDef.Food;
        }

        /// <summary>
        /// Marks two power up spawn points near the ghost pen, and their copies on the other half.
        /// On the left half, outside the pen, it takes the plain dot cell (only Food) nearest the pen's centre above the
        /// middle row, and the nearest one below it. The cells keep their dots. Needs no randomness.
        /// </summary>
        public static void AddPowerUpSpawns(eGridDef[,] grid, bool rotate, (int X, int Y, int Width, int Height) ghostPen)
        {
            int size = grid.GetLength(0);
            int half = (int)(size * 0.5f);

            foreach (var above in new[] { true, false })
            {
                (int X, int Y)? best = null;
                var bestDistance = float.MaxValue;

                for (int x = 1; x < half; x++)
                    for (int y = 1; y < size - 1; y++)
                    {
                        if (grid[x, y] != eGridDef.Food || (y < half) != above)
                            continue;

                        if (x >= ghostPen.X && x < ghostPen.X + ghostPen.Width && y >= ghostPen.Y && y < ghostPen.Y + ghostPen.Height)
                            continue;

                        float dx = x + 0.5f - half;
                        float dy = y + 0.5f - half;
                        float distance = (dx * dx) + (dy * dy);

                        if (distance < bestDistance)
                        {
                            best = (x, y);
                            bestDistance = distance;
                        }
                    }

                if (!best.HasValue)
                    continue;

                var copy = GetCopy(best.Value.X, best.Value.Y, size, rotate);
                grid[best.Value.X, best.Value.Y] |= eGridDef.PowerUpSpawn;
                grid[copy.X, copy.Y] |= eGridDef.PowerUpSpawn;
            }
        }

        /// <summary>
        /// Picks a random unused open cell in columns 1..depth (not on the outer wall), marks it with
        /// the red flag, and marks its copy on the other half with the blue flag.
        /// </summary>
        public static void PlacePair(eGridDef[,] grid, HashSet<(int X, int Y)> used, Random random, bool rotate,
                                      int depth, eGridDef red, eGridDef blue)
        {
            int size = grid.GetLength(0);
            var candidates = new List<(int X, int Y)>();

            for (int x = 1; x <= depth; x++)
                for (int y = 1; y < size - 1; y++)
                    if (IsOpen(grid, x, y) && !used.Contains((x, y)))
                        candidates.Add((x, y));

            if (candidates.Count == 0)
                throw new InvalidOperationException("Not enough free cells to place map items.");

            var cell = candidates[random.Next(candidates.Count)];
            var copy = GetCopy(cell.X, cell.Y, size, rotate);

            grid[cell.X, cell.Y] |= red;
            grid[copy.X, copy.Y] |= blue;
            used.Add(cell);
            used.Add(copy);
        }

        public static bool IsOpen(eGridDef[,] grid, int x, int y) => !grid[x, y].HasFlag(eGridDef.Wall);

        public static void Shuffle<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Coarse grid of rooms with walls on their edges. VWalls[line, y] is the vertical wall on
        /// line x = line (0..size), HWalls[x, line] is the horizontal wall on line y = line (0..size).
        /// Line 0 and line size are the outer boundary and are always walls. A solid room is filled in,
        /// which merges the walls around it into a thick block. Every change is also applied to its
        /// copy on the other half, so the maze stays symmetric. Rooms are carved into the cell map
        /// afterwards (see Generate), not here.
        /// </summary>
        public sealed class Maze
        {
            /// <summary>
            /// Fewest passages that must stay open between the two halves.
            /// </summary>
            public const int MIN_CENTER_CROSSINGS = 2;

            /// <summary>
            /// Chance that a room in the left half is tried as a solid block.
            /// </summary>
            private const float SOLID_ROOM_CHANCE = 0.3f;

            private static readonly eFacing[] DIRECTIONS = [eFacing.Up, eFacing.Down, eFacing.Left, eFacing.Right];

            private readonly int _Size;
            private readonly int _HalfWidth;
            private readonly bool _Rotate;
            private readonly bool[,] _VWalls;
            private readonly bool[,] _HWalls;
            private readonly bool[,] _Solid;

            // Scratch space for the reachability check, reused so each check needs no clearing.
            private readonly int[,] _Seen;
            private readonly int[,] _Target;
            private readonly List<(int X, int Y)> _Touched = new(12);
            private readonly List<(int X, int Y)> _Reach = new(8);
            private readonly List<(int X, int Y)> _Targets = new(8);

            // Running junction tally for the whole maze, kept up to date by TryChange.
            private (int Rooms, int Junctions, int Exits) _Tally;

            // Every wall or solid flag changed since TryChange started, with its old value.
            private readonly List<(bool[,] Array, int A, int B, bool Was)> _Undo = new(16);
            private int _Stamp;

            /// <summary>
            /// Starts fully open: only the outer boundary is wall, so every room is a 4-way junction.
            /// </summary>
            public Maze(int size, bool rotate)
            {
                _Size = size;
                _Rotate = rotate;
                _HalfWidth = (int)((size + 1) * 0.5f);   // includes the center column for odd sizes
                _VWalls = new bool[size + 1, size];
                _HWalls = new bool[size, size + 1];
                _Solid = new bool[size, size];
                _Seen = new int[size, size];
                _Target = new int[size, size];

                for (int b = 0; b < size; b++)
                {
                    _VWalls[0, b] = true;
                    _VWalls[size, b] = true;
                    _HWalls[b, 0] = true;
                    _HWalls[b, size] = true;
                }
            }

            /// <summary>
            /// Adds walls in two passes. First every wall post (where four rooms meet) gets at least one
            /// wall, so there are no lone one-cell pillars. Then passages are closed and rooms filled, in
            /// random order, until the junction share falls to targetPercent. A change is kept only if
            /// IsValid still holds afterwards.
            /// </summary>
            public void AddWalls(Random random, int targetPercent)
            {
                _Tally = CountJunctions(GetAllRooms());

                // Pass 1: post (px, py) sits where vertical line px meets horizontal line py.
                var posts = new List<(int X, int Y)>();
                for (int px = 1; px <= (int)(_Size * 0.5f); px++)
                    for (int py = 1; py < _Size; py++)
                        posts.Add((px, py));

                Shuffle(posts, random);

                var sides = new List<(int X, int Y, eFacing Dir, int FarX, int FarY)>(4);

                foreach (var (px, py) in posts)
                {
                    if (PostHasWall(px, py))
                        continue;

                    // The four walls that meet at the post, each named by a room beside it, with the
                    // post at the wall's other end. A wall whose other end is also bare covers two
                    // posts at once, so those are tried first, which keeps the walls few.
                    sides.Clear();
                    sides.Add((px - 1, py - 1, eFacing.Right, px, py - 1));
                    sides.Add((px - 1, py, eFacing.Right, px, py + 1));
                    sides.Add((px - 1, py - 1, eFacing.Down, px - 1, py));
                    sides.Add((px, py - 1, eFacing.Down, px + 1, py));
                    Shuffle(sides, random);

                    bool done = false;
                    for (int pass = 0; pass < 2 && !done; pass++)
                        foreach (var (x, y, dir, farX, farY) in sides)
                        {
                            // First pass: walls with a bare far end. Second pass: the rest.
                            if (PostHasWall(farX, farY) != (pass == 1))
                                continue;

                            if (TryChange(x, y, dir))
                            {
                                done = true;
                                break;
                            }
                        }
                }

                // Pass 2: a null direction means fill the room solid.
                var changes = new List<(int X, int Y, eFacing? Dir)>();

                for (int x = 0; x < _HalfWidth; x++)
                    for (int y = 0; y < _Size; y++)
                    {
                        changes.Add((x, y, eFacing.Right));
                        changes.Add((x, y, eFacing.Down));

                        if (random.NextDouble() < SOLID_ROOM_CHANCE)
                            changes.Add((x, y, null));
                    }

                Shuffle(changes, random);

                foreach (var (x, y, dir) in changes)
                {
                    if (GetPercent(_Tally) <= targetPercent)
                        break;

                    TryChange(x, y, dir);
                }
            }

            /// <summary>
            /// Closes one side of a room (or fills the room when dir is null), and undoes it if the maze
            /// is no longer valid. Returns true if kept.
            /// </summary>
            public bool TryChange(int x, int y, eFacing? dir)
            {
                // Only the rooms beside a closed wall can change: they may become dead ends or get cut off,
                // and their part of the junction tally is all that needs working out again.
                _Touched.Clear();

                // The rooms that must still reach each other afterwards. The copy of a change is the
                // mirror image of it, so if this half stays joined up the other half does too, and
                // only the rooms on this side of the change need checking. A filled room next to its
                // own copy is the exception, so the copy's neighbours are added then.
                _Reach.Clear();

                if (dir.HasValue)
                {
                    var next = Step(x, y, dir.Value);
                    AddTouched(x, y);
                    AddTouched(next.X, next.Y);

                    _Reach.Add((x, y));
                    if (InBounds(next.X, next.Y))
                        _Reach.Add(next);
                }
                else
                {
                    var copy = GetRoomCopy(x, y);
                    bool besideCopy = Math.Abs(copy.X - x) + Math.Abs(copy.Y - y) == 1;

                    AddTouched(x, y);
                    foreach (eFacing side in DIRECTIONS)
                    {
                        var next = Step(x, y, side);
                        AddTouched(next.X, next.Y);

                        if (InBounds(next.X, next.Y))
                            _Reach.Add(next);

                        var copyNext = Step(copy.X, copy.Y, side);
                        if (besideCopy && InBounds(copyNext.X, copyNext.Y))
                            _Reach.Add(copyNext);
                    }
                }

                var before = CountJunctions(_Touched);
                _Undo.Clear();

                if (dir.HasValue)
                    SetWall(x, y, dir.Value, true);
                else
                    FillRoom(x, y);

                if (IsValidNear(_Touched, _Reach))
                {
                    var after = CountJunctions(_Touched);
                    _Tally = (_Tally.Rooms + after.Rooms - before.Rooms,
                              _Tally.Junctions + after.Junctions - before.Junctions,
                              _Tally.Exits + after.Exits - before.Exits);
                    return true;
                }

                // Newest first, since a wall and its copy can be the same entry.
                for (int i = _Undo.Count - 1; i >= 0; i--)
                {
                    var (array, a, b, was) = _Undo[i];
                    array[a, b] = was;
                }

                return false;
            }

            /// <summary>
            /// Share of the open cells, once expanded, that are junctions. Only rooms can be junctions;
            /// each open passage between two rooms becomes a cell with two ways out.
            /// </summary>
            public float GetJunctionPercent()
            {
                return GetPercent(CountJunctions(GetAllRooms()));
            }

            public List<(int X, int Y)> GetAllRooms()
            {
                var rooms = new List<(int X, int Y)>(_Size * _Size);
                for (int x = 0; x < _Size; x++)
                    for (int y = 0; y < _Size; y++)
                        rooms.Add((x, y));
                return rooms;
            }

            /// <summary>
            /// Open rooms, junction rooms and ways out, summed over the given rooms.
            /// </summary>
            public (int Rooms, int Junctions, int Exits) CountJunctions(List<(int X, int Y)> rooms)
            {
                int open = 0;
                int junctions = 0;
                int exits = 0;

                foreach (var (x, y) in rooms)
                {
                    if (_Solid[x, y])
                        continue;

                    int roomExits = GetExits(x, y);
                    open++;
                    exits += roomExits;

                    if (roomExits >= 3)
                        junctions++;
                }

                return (open, junctions, exits);
            }

            /// <summary>
            /// Junction share of a tally. Every passage was counted once from each end.
            /// </summary>
            public static float GetPercent((int Rooms, int Junctions, int Exits) tally)
            {
                return tally.Junctions * 100f / (tally.Rooms + tally.Exits * 0.5f);
            }

            /// <summary>
            /// Adds a room and its copy to the touched list, once each, if they are on the map.
            /// </summary>
            private void AddTouched(int x, int y)
            {
                if (!InBounds(x, y))
                    return;

                var room = (x, y);
                var copy = GetRoomCopy(x, y);

                if (!_Touched.Contains(room))
                    _Touched.Add(room);

                if (!_Touched.Contains(copy))
                    _Touched.Add(copy);
            }

            /// <summary>
            /// True when no open room is a dead end, every open room is reachable, and the halves are
            /// joined by at least MIN_CENTER_CROSSINGS passages.
            /// </summary>
            public bool IsValid()
            {
                int open = 0;
                int startX = -1;
                int startY = -1;

                for (int x = 0; x < _Size; x++)
                    for (int y = 0; y < _Size; y++)
                    {
                        if (_Solid[x, y])
                            continue;

                        if (GetExits(x, y) < 2)
                            return false;

                        open++;
                        startX = x;
                        startY = y;
                    }

                if (open == 0 || CountCenterCrossings() < MIN_CENTER_CROSSINGS)
                    return false;

                return CountReachable(startX, startY) == open;
            }

            /// <summary>
            /// Same as IsValid for a maze that was valid before walls were added beside the touched rooms.
            /// Removing passages can only split the maze where a passage was removed, so the maze is
            /// still in one piece if the rooms either side of the change (reach) can still reach each other.
            /// </summary>
            public bool IsValidNear(List<(int X, int Y)> touched, List<(int X, int Y)> reach)
            {
                foreach (var (x, y) in touched)
                    if (!_Solid[x, y] && GetExits(x, y) < 2)
                        return false;

                if (CountCenterCrossings() < MIN_CENTER_CROSSINGS)
                    return false;

                _Targets.Clear();
                foreach (var (x, y) in reach)
                    if (!_Solid[x, y])
                        _Targets.Add((x, y));

                return _Targets.Count == 0 || CanReachAll(_Targets);
            }

            /// <summary>
            /// True if every room in the list can be reached from the first one. Stops as soon as the
            /// last one is found, so a check near a small change stays small.
            /// </summary>
            public bool CanReachAll(List<(int X, int Y)> rooms)
            {
                _Stamp++;

                // Count each room once; the list can repeat rooms.
                int remaining = 0;
                foreach (var (x, y) in rooms)
                    if (_Target[x, y] != _Stamp)
                    {
                        _Target[x, y] = _Stamp;
                        remaining++;
                    }

                var queue = new Queue<(int X, int Y)>();
                var start = rooms[0];
                _Seen[start.X, start.Y] = _Stamp;
                queue.Enqueue(start);
                remaining--;

                while (queue.Count > 0 && remaining > 0)
                {
                    var (cx, cy) = queue.Dequeue();

                    foreach (eFacing dir in DIRECTIONS)
                    {
                        if (HasWall(cx, cy, dir))
                            continue;

                        var (nx, ny) = Step(cx, cy, dir);
                        if (_Seen[nx, ny] == _Stamp)
                            continue;

                        _Seen[nx, ny] = _Stamp;
                        queue.Enqueue((nx, ny));

                        if (_Target[nx, ny] == _Stamp)
                            remaining--;
                    }
                }

                return remaining == 0;
            }

            /// <summary>
            /// Returns the room that (x, y) is copied to on the other half.
            /// </summary>
            public (int X, int Y) GetRoomCopy(int x, int y)
            {
                return (_Size - 1 - x, _Rotate ? _Size - 1 - y : y);
            }

            public bool InBounds(int x, int y) => x >= 0 && x < _Size && y >= 0 && y < _Size;

            /// <summary>
            /// Counts the open passages across the center line. For even sizes that is the seam between
            /// the halves; for odd sizes it is the west side of the center column.
            /// </summary>
            public int CountCenterCrossings()
            {
                int line = (int)(_Size * 0.5f);
                int count = 0;

                for (int y = 0; y < _Size; y++)
                    if (!_VWalls[line, y])
                        count++;

                return count;
            }

            /// <summary>
            /// Number of rooms reachable from (x, y) through open passages, including (x, y).
            /// </summary>
            public int CountReachable(int x, int y)
            {
                var seen = new bool[_Size, _Size];
                var queue = new Queue<(int X, int Y)>();

                seen[x, y] = true;
                queue.Enqueue((x, y));
                int count = 0;

                while (queue.Count > 0)
                {
                    var (cx, cy) = queue.Dequeue();
                    count++;

                    foreach (eFacing dir in DIRECTIONS)
                    {
                        if (HasWall(cx, cy, dir))
                            continue;

                        var (nx, ny) = Step(cx, cy, dir);
                        if (!seen[nx, ny])
                        {
                            seen[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }

                return count;
            }

            /// <summary>
            /// Expands the maze to a (2 * size + 1) square grid of cells, true = open. Room (x, y) becomes
            /// cell (2x + 1, 2y + 1) unless it is solid, and an open edge between two rooms becomes the open
            /// cell between them. Posts are always wall.
            /// </summary>
            public bool[,] ToCells()
            {
                int cells = _Size * 2 + 1;
                var open = new bool[cells, cells];

                for (int x = 0; x < _Size; x++)
                    for (int y = 0; y < _Size; y++)
                    {
                        open[x * 2 + 1, y * 2 + 1] = !_Solid[x, y];
                        open[x * 2, y * 2 + 1] = !_VWalls[x, y];
                        open[x * 2 + 1, y * 2] = !_HWalls[x, y];
                    }

                return open;
            }

            /// <summary>
            /// True if any wall meets the post where vertical line px crosses horizontal line py.
            /// Posts on the outer boundary always have one.
            /// </summary>
            public bool PostHasWall(int px, int py)
            {
                if (px <= 0 || py <= 0 || px >= _Size || py >= _Size)
                    return true;

                return _VWalls[px, py - 1] || _VWalls[px, py] || _HWalls[px - 1, py] || _HWalls[px, py];
            }

            public bool IsSolid(int x, int y) => _Solid[x, y];

            public bool HasWall(int x, int y, eFacing dir)
            {
                return dir switch
                {
                    eFacing.Up => _HWalls[x, y],
                    eFacing.Down => _HWalls[x, y + 1],
                    eFacing.Left => _VWalls[x, y],
                    _ => _VWalls[x + 1, y],
                };
            }

            /// <summary>
            /// Opens or closes the wall on the given side of a room, and its copy on the other half.
            /// Boundary walls are left in place.
            /// </summary>
            public void SetWall(int x, int y, eFacing dir, bool wall)
            {
                switch (dir)
                {
                    case eFacing.Up:
                    case eFacing.Down:
                        int hLine = dir == eFacing.Up ? y : y + 1;
                        if (hLine <= 0 || hLine >= _Size) return;
                        Set(_HWalls, x, hLine, wall);
                        Set(_HWalls, _Size - 1 - x, _Rotate ? _Size - hLine : hLine, wall);
                        break;

                    default:
                        int vLine = dir == eFacing.Left ? x : x + 1;
                        if (vLine <= 0 || vLine >= _Size) return;
                        Set(_VWalls, vLine, y, wall);
                        Set(_VWalls, _Size - vLine, _Rotate ? _Size - 1 - y : y, wall);
                        break;
                }
            }

            /// <summary>
            /// Closes every side of a room and marks it solid, along with its copy on the other half.
            /// </summary>
            public void FillRoom(int x, int y)
            {
                foreach (eFacing dir in DIRECTIONS)
                    SetWall(x, y, dir, true);

                var copy = GetRoomCopy(x, y);
                Set(_Solid, x, y, true);
                Set(_Solid, copy.X, copy.Y, true);
            }

            /// <summary>
            /// Sets one wall or solid flag, noting its old value so TryChange can undo it.
            /// </summary>
            private void Set(bool[,] array, int a, int b, bool value)
            {
                _Undo.Add((array, a, b, array[a, b]));
                array[a, b] = value;
            }

            public int GetExits(int x, int y)
            {
                int count = 0;
                foreach (eFacing dir in DIRECTIONS)
                    if (!HasWall(x, y, dir))
                        count++;
                return count;
            }

            public static (int X, int Y) Step(int x, int y, eFacing dir)
            {
                return dir switch
                {
                    eFacing.Up => (x, y - 1),
                    eFacing.Down => (x, y + 1),
                    eFacing.Left => (x - 1, y),
                    _ => (x + 1, y),
                };
            }
        }
    }
}
