using FastG;
using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Maps positions on the map to the mini-map, which shows the whole map squeezed into a square.
    /// </summary>
    public static class MiniMapMath
    {
        /// <summary>
        /// How far into the mini-map, in pixels, a map position (in cells, along one axis) lands. A map of mapSize cells
        /// fills sidePixels; positions off the map are clamped to its edges.
        /// </summary>
        public static float CellToMiniMap(float cell, int mapSize, float sidePixels)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(mapSize, 0);

            return Math.Clamp(cell / mapSize, 0f, 1f) * sidePixels;
        }

        /// <summary>
        /// A flag sitting on its base for every flag base cell in the map, so BaseX and BaseY give the cell centre.
        /// </summary>
        public static List<GameFlag> GetFlagBases(eGridDef[,] map)
        {
            const float CELL_CENTER = 0.5f;

            var bases = new List<GameFlag>();

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y].HasFlag(eGridDef.FlagBaseRed))
                        bases.Add(new GameFlag(bases.Count, eTeam.Red, x + CELL_CENTER, y + CELL_CENTER));

                    if (map[x, y].HasFlag(eGridDef.FlagBaseBlue))
                        bases.Add(new GameFlag(bases.Count, eTeam.Blue, x + CELL_CENTER, y + CELL_CENTER));
                }
            }

            return bases;
        }

        /// <summary>
        /// The centre of every warp door: each open cell on the map's edge (MazeMovement.IsWarpCell).
        /// </summary>
        public static List<Point2> GetWarpCells(eGridDef[,] map)
        {
            const float CELL_CENTER = 0.5f;

            var cells = new List<Point2>();

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (MazeMovement.IsWarpCell(map, x, y))
                        cells.Add(new Point2(x + CELL_CENTER, y + CELL_CENTER));
                }
            }

            return cells;
        }

        /// <summary>
        /// The bounding box of every group of touching cells (side by side, not diagonally) that have flag, e.g. one box
        /// per spawn room for SpawnPointRed, or one for the ghost generator. Boxes are listed in the order their first
        /// cell is found, column by column.
        /// </summary>
        public static List<CellBox> GetCellBoxes(eGridDef[,] map, eGridDef flag)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var seen = new bool[width, height];
            var boxes = new List<CellBox>();
            var stack = new Stack<(int X, int Y)>();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (seen[x, y] || !map[x, y].HasFlag(flag))
                        continue;

                    var left = x;
                    var right = x;
                    var top = y;
                    var bottom = y;

                    seen[x, y] = true;
                    stack.Push((x, y));

                    while (stack.Count > 0)
                    {
                        var (cx, cy) = stack.Pop();

                        left = Math.Min(left, cx);
                        right = Math.Max(right, cx);
                        top = Math.Min(top, cy);
                        bottom = Math.Max(bottom, cy);

                        PushIfMatch(map, flag, seen, stack, cx - 1, cy);
                        PushIfMatch(map, flag, seen, stack, cx + 1, cy);
                        PushIfMatch(map, flag, seen, stack, cx, cy - 1);
                        PushIfMatch(map, flag, seen, stack, cx, cy + 1);
                    }

                    boxes.Add(new CellBox(left, top, right - left + 1, bottom - top + 1));
                }
            }

            return boxes;
        }

        private static void PushIfMatch(eGridDef[,] map, eGridDef flag, bool[,] seen, Stack<(int X, int Y)> stack, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
                return;

            if (seen[x, y] || !map[x, y].HasFlag(flag))
                return;

            seen[x, y] = true;
            stack.Push((x, y));
        }
    }
}
