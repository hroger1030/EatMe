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
        /// Whether pixel (x, y) of a square image size pixels wide is part of an arch (an upside down U) thickness pixels
        /// wide: a half ring across the top half of the image and two legs down its sides to the bottom row. Pixels are
        /// tested at their centres.
        /// </summary>
        public static bool IsOnArch(int x, int y, int size, float thickness)
        {
            var radius = size * 0.5f;
            var px = x + 0.5f;
            var py = y + 0.5f;

            if (py >= radius)
                return px <= thickness || px >= size - thickness;

            var dx = px - radius;
            var dy = py - radius;
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            return distance <= radius && distance >= radius - thickness;
        }

        /// <summary>
        /// Whether pixel (x, y) of a square image size pixels wide is part of an X: the two diagonals, corner to corner,
        /// thickness pixels wide. Pixels are tested at their centres.
        /// </summary>
        public static bool IsOnCross(int x, int y, int size, float thickness)
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            var limit = thickness * 0.5f * MathF.Sqrt(2f);

            return MathF.Abs(px - py) <= limit || MathF.Abs(px + py - size) <= limit;
        }

        /// <summary>
        /// Whether pixel (x, y) of a square image diameter pixels wide is part of a ring that thickness pixels wide
        /// touching the image's edges. Pixels are tested at their centres.
        /// </summary>
        public static bool IsOnRing(int x, int y, int diameter, float thickness)
        {
            var radius = diameter * 0.5f;
            var dx = x + 0.5f - radius;
            var dy = y + 0.5f - radius;
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            return distance <= radius && distance >= radius - thickness;
        }

        /// <summary>
        /// Whether pixel (x, y) of a square image size pixels wide is inside a triangle pointing up, its base the full
        /// width of the bottom row and its tip in the middle of the top row. Each row is as wide as the triangle at the
        /// row's bottom edge, so the top row isn't empty.
        /// </summary>
        public static bool IsOnTriangle(int x, int y, int size)
        {
            var halfWidth = (y + 1) * 0.5f;

            return MathF.Abs(x + 0.5f - size * 0.5f) <= halfWidth;
        }

        /// <summary>
        /// Inner corners of a star as a fraction of its outer points' radius. Fat enough to read at a few pixels across.
        /// </summary>
        public const float STAR_INNER_RATIO = 0.5f;

        /// <summary>
        /// How much taller than size the star's outline is drawn, so the pixel centres in its points are inside it.
        /// </summary>
        public const float STAR_GROW = 1.11f;

        /// <summary>
        /// How far up the star is moved, as a fraction of size.
        /// </summary>
        public const float STAR_LIFT = 0.1f;

        /// <summary>
        /// Whether pixel (x, y) of a square image size pixels wide is inside a five-pointed star pointing up, filling the
        /// image top to bottom. Pixels are tested at their centres, so the star is drawn a little over size (STAR_GROW)
        /// and nudged up (STAR_LIFT) for its top point to reach the top row.
        /// </summary>
        public static bool IsOnStar(int x, int y, int size)
        {
            var radius = size * STAR_GROW / (1f + MathF.Cos(MathF.PI / 5f));
            var centreX = size * 0.5f;
            var centreY = radius - (size * STAR_LIFT);
            var px = x + 0.5f;
            var py = y + 0.5f;
            var inside = false;

            // ten corners, outer and inner in turn, starting at the top point; even-odd test against each edge
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                var (ax, ay) = GetStarCorner(i, radius, centreX, centreY);
                var (bx, by) = GetStarCorner(j, radius, centreX, centreY);

                if ((ay > py) != (by > py) && px < ax + ((py - ay) * (bx - ax) / (by - ay)))
                    inside = !inside;
            }

            return inside;
        }

        private static (float X, float Y) GetStarCorner(int index, float radius, float centreX, float centreY)
        {
            var angle = (-MathF.PI * 0.5f) + (index * MathF.PI / 5f);
            var r = index % 2 == 0 ? radius : radius * STAR_INNER_RATIO;

            return (centreX + (r * MathF.Cos(angle)), centreY + (r * MathF.Sin(angle)));
        }
    }
}
