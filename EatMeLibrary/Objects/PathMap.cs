using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Steps through open cells from one or more start cells to every cell, using the warp tunnels. Holds the
    /// distance to each cell, the first step from the start toward it, and the cells in the order they were reached
    /// (nearest first). A search can stop at maxDistance steps; cells further away count as UNREACHED. Cells with any
    /// of the blocked flags (walls by default) are never entered, and neither are cells marked true in avoid (start cells excepted). Fill it again
    /// to reuse it; the arrays are only made again when the map size changes.
    /// </summary>
    public class PathMap
    {
        public const int UNREACHED = -1;

        private const sbyte NO_STEP = -1;

        private static readonly eFacing[] DIRECTIONS = Enum.GetValues<eFacing>();

        private readonly List<(int X, int Y)> _Order = [];
        private readonly Queue<(int X, int Y)> _Queue = new();
        private int[,] _Distance = new int[0, 0];
        private sbyte[,] _FirstStep = new sbyte[0, 0];

        /// <summary>
        /// Every reachable cell, nearest first.
        /// </summary>
        public IReadOnlyList<(int X, int Y)> Order => _Order;

        /// <summary>
        /// Steps from the nearest start cell to (x, y), or UNREACHED.
        /// </summary>
        public int GetDistance(int x, int y)
        {
            return _Distance[x, y];
        }

        /// <summary>
        /// The way to leave the start cell to get to (x, y) by a shortest path. Null for a start cell or one that
        /// can't be reached. Only meaningful when filled from a single start.
        /// </summary>
        public eFacing? GetFirstStep(int x, int y)
        {
            var step = _FirstStep[x, y];
            return step == NO_STEP ? null : (eFacing)step;
        }

        public void Fill(eGridDef[,] map, int x, int y, int maxDistance = int.MaxValue, eGridDef blocked = eGridDef.Wall, bool[,] avoid = null)
        {
            Fill(map, [(x, y)], maxDistance, blocked, avoid);
        }

        public void Fill(eGridDef[,] map, IEnumerable<(int X, int Y)> starts, int maxDistance = int.MaxValue, eGridDef blocked = eGridDef.Wall, bool[,] avoid = null)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            if (_Distance.GetLength(0) != width || _Distance.GetLength(1) != height)
            {
                _Distance = new int[width, height];
                _FirstStep = new sbyte[width, height];
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    _Distance[x, y] = UNREACHED;
                    _FirstStep[x, y] = NO_STEP;
                }
            }

            _Order.Clear();
            _Queue.Clear();

            foreach (var (x, y) in starts)
            {
                if (_Distance[x, y] != UNREACHED)
                    continue;

                _Distance[x, y] = 0;
                _Order.Add((x, y));
                _Queue.Enqueue((x, y));
            }

            while (_Queue.Count > 0)
            {
                var (cx, cy) = _Queue.Dequeue();

                if (_Distance[cx, cy] >= maxDistance)
                    continue;

                foreach (var direction in DIRECTIONS)
                {
                    var (nx, ny) = MazeMovement.GetNeighbor(cx, cy, direction, width, height);

                    // the same test as MazeMovement.CanMove, without working out the neighbour twice
                    if (_Distance[nx, ny] != UNREACHED || (map[nx, ny] & blocked) != 0 || (avoid != null && avoid[nx, ny]))
                        continue;

                    _Distance[nx, ny] = _Distance[cx, cy] + 1;

                    // cells next to a start take their own direction; the rest inherit the step that led to them
                    _FirstStep[nx, ny] = _Distance[cx, cy] == 0 ? (sbyte)direction : _FirstStep[cx, cy];
                    _Order.Add((nx, ny));
                    _Queue.Enqueue((nx, ny));
                }
            }
        }
    }
}
