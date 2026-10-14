namespace EatMeLibrary
{
    /// <summary>
    /// Picks how to draw each quarter of a wall cell so it joins the wall cells around it.
    /// Every quarter comes from one of the four quarters of the corner tile (Const.IMAGE_WALL, joined below and right),
    /// turned clockwise into place. Quarters are numbered clockwise from the top left: 0 top left, 1 top right, 2 bottom right, 3 bottom left.
    /// </summary>
    public static class WallTiles
    {
        // corner tile quarters: what each one draws, given its two sides (first and second, clockwise) and the diagonal between them
        public const int QUARTER_OUTER_CORNER = 0;   // top left: neither side is a wall
        public const int QUARTER_SECOND_SIDE = 1;    // top right: only the second side is a wall
        public const int QUARTER_INNER_CORNER = 2;   // bottom right: both sides are walls, the diagonal is open
        public const int QUARTER_FIRST_SIDE = 3;     // bottom left: only the first side is a wall

        // clockwise from north
        private static readonly int[] DX = [0, 1, 0, -1];
        private static readonly int[] DY = [-1, 0, 1, 0];

        /// <summary>
        /// How to draw quarter <paramref name="quarter"/> of wall cell (x, y): which corner tile quarter to use
        /// and how many clockwise quarter turns to draw it with. Returns false when nothing is drawn,
        /// because both sides and the diagonal between them are walls (inside a solid block).
        /// Cells off the map count as open.
        /// </summary>
        public static bool GetQuarter(eGridDef[,] map, int x, int y, int quarter, out int sourceQuarter, out int quarterTurns)
        {
            // a quarter's sides, in clockwise order: top left is west then north
            var first = (quarter + 3) % 4;
            var second = quarter;

            var firstWall = IsWall(map, x + DX[first], y + DY[first]);
            var secondWall = IsWall(map, x + DX[second], y + DY[second]);

            if (firstWall && secondWall)
            {
                if (IsWall(map, x + DX[first] + DX[second], y + DY[first] + DY[second]))
                {
                    sourceQuarter = 0;
                    quarterTurns = 0;
                    return false;
                }

                sourceQuarter = QUARTER_INNER_CORNER;
            }
            else if (firstWall)
            {
                sourceQuarter = QUARTER_FIRST_SIDE;
            }
            else if (secondWall)
            {
                sourceQuarter = QUARTER_SECOND_SIDE;
            }
            else
            {
                sourceQuarter = QUARTER_OUTER_CORNER;
            }

            quarterTurns = (quarter - sourceQuarter + 4) % 4;
            return true;
        }

        private static bool IsWall(eGridDef[,] map, int x, int y)
        {
            return x >= 0 && y >= 0 && x < map.GetLength(0) && y < map.GetLength(1) && map[x, y].HasFlag(eGridDef.Wall);
        }
    }
}
