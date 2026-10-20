using FastG;
using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Moves pacs and ghosts through the maze. Everything travels along the lines through cell centres and can
    /// only turn on a cell centre, into an open cell. Going off one edge of the map (through a warp tunnel)
    /// comes back in on the opposite edge. Positions are in map cells.
    /// </summary>
    public static class MazeMovement
    {
        /// <summary>
        /// How close to a cell centre, in cells, counts as being on it.
        /// </summary>
        public const float CENTRE_TOLERANCE = 0.0001f;

        private const float CELL_CENTER = 0.5f;

        /// <summary>
        /// The facing a held direction asks for, or null when nothing is held.
        /// </summary>
        public static eFacing? ToFacing(eDirection direction)
        {
            return direction switch
            {
                eDirection.Up => eFacing.Up,
                eDirection.Down => eFacing.Down,
                eDirection.Left => eFacing.Left,
                eDirection.Right => eFacing.Right,
                _ => null,
            };
        }

        /// <summary>
        /// The held direction that asks for a facing, or None for null. The reverse of ToFacing.
        /// </summary>
        public static eDirection ToDirection(eFacing? facing)
        {
            return facing switch
            {
                eFacing.Up => eDirection.Up,
                eFacing.Down => eDirection.Down,
                eFacing.Left => eDirection.Left,
                eFacing.Right => eDirection.Right,
                _ => eDirection.None,
            };
        }

        public static eFacing GetOpposite(eFacing facing)
        {
            return facing switch
            {
                eFacing.Up => eFacing.Down,
                eFacing.Down => eFacing.Up,
                eFacing.Left => eFacing.Right,
                _ => eFacing.Left,
            };
        }

        public static bool IsHorizontal(eFacing facing) => facing is eFacing.Left or eFacing.Right;

        /// <summary>
        /// The cell a position is in.
        /// </summary>
        public static (int X, int Y) GetCell(Point2 position)
        {
            return ((int)MathF.Floor(position.X), (int)MathF.Floor(position.Y));
        }

        /// <summary>
        /// The cell next to (x, y) in the given direction, wrapping round the edges of the map.
        /// </summary>
        public static (int X, int Y) GetNeighbor(int x, int y, eFacing facing, int width, int height)
        {
            var (nx, ny) = MapMaker.Maze.Step(x, y, facing);
            return ((nx + width) % width, (ny + height) % height);
        }

        /// <summary>
        /// What ghosts can't enter: walls and every spawn room.
        /// </summary>
        public const eGridDef GHOST_BLOCKED = eGridDef.Wall | eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue;

        /// <summary>
        /// What a pac on this team can't enter: walls and the other team's spawn room.
        /// </summary>
        public static eGridDef GetPacBlocked(eTeam team)
        {
            return team switch
            {
                eTeam.Red => eGridDef.Wall | eGridDef.SpawnPointBlue,
                eTeam.Blue => eGridDef.Wall | eGridDef.SpawnPointRed,
                _ => eGridDef.Wall,
            };
        }

        /// <summary>
        /// True if the cell next to (x, y) in the given direction is open.
        /// </summary>
        public static bool CanMove(eGridDef[,] map, int x, int y, eFacing facing)
        {
            return CanMove(map, x, y, facing, true);
        }

        /// <summary>
        /// True if the cell next to (x, y) in the given direction has none of the blocked flags (walls by default).
        /// Without canWarp, warp tunnel cells count as walls (ghosts can't use the tunnels).
        /// </summary>
        public static bool CanMove(eGridDef[,] map, int x, int y, eFacing facing, bool canWarp, eGridDef blocked = eGridDef.Wall)
        {
            var (nx, ny) = GetNeighbor(x, y, facing, map.GetLength(0), map.GetLength(1));

            if ((map[nx, ny] & blocked) != 0)
                return false;

            return canWarp || !IsWarpCell(map, nx, ny);
        }

        /// <summary>
        /// True if (x, y) is a warp tunnel cell: an open cell on the outer edge of the map.
        /// </summary>
        public static bool IsWarpCell(eGridDef[,] map, int x, int y)
        {
            var lastX = map.GetLength(0) - 1;
            var lastY = map.GetLength(1) - 1;

            if (map[x, y].HasFlag(eGridDef.Wall))
                return false;

            return x == 0 || y == 0 || x == lastX || y == lastY;
        }

        /// <summary>
        /// Distance along facing to the next cell centre ahead, or 0 when already on one.
        /// </summary>
        public static float GetDistanceToCentre(Point2 position, eFacing facing)
        {
            var offset = (IsHorizontal(facing) ? position.X : position.Y) - CELL_CENTER;

            if (MathF.Abs(offset - MathF.Round(offset)) < CENTRE_TOLERANCE)
                return 0f;

            return facing is eFacing.Right or eFacing.Down ? MathF.Ceiling(offset) - offset : offset - MathF.Floor(offset);
        }

        /// <summary>
        /// The cell whose centre a mover reaches next going this way: the one it is on if it's on a centre.
        /// </summary>
        public static (int X, int Y) GetNextCentreCell(Point2 position, eFacing facing, int width, int height)
        {
            var (x, y) = GetCell(Advance(position, facing, GetDistanceToCentre(position, facing), width, height));
            return (Math.Clamp(x, 0, width - 1), Math.Clamp(y, 0, height - 1));
        }

        /// <summary>
        /// Moves a position in a straight line, wrapping round the edges of the map.
        /// </summary>
        public static Point2 Advance(Point2 position, eFacing facing, float distance, int width, int height)
        {
            var x = position.X;
            var y = position.Y;

            switch (facing)
            {
                case eFacing.Up: y -= distance; break;
                case eFacing.Down: y += distance; break;
                case eFacing.Left: x -= distance; break;
                default: x += distance; break;
            }

            return new Point2(Wrap(x, width), Wrap(y, height));
        }

        private static float Wrap(float value, int size)
        {
            // A value just below 0 plus size can round up to exactly size, one past the last cell.
            if (value < 0f)
            {
                value += size;
                return value >= size ? 0f : value;
            }

            if (value >= size)
                return value - size;

            return value;
        }

        /// <summary>
        /// The nearest cell to (x, y), counting steps through anything (walls too) and wrapping round the edges, that
        /// has none of the blocked flags: (x, y) itself when it is open. Null if the whole map is blocked.
        /// </summary>
        public static (int X, int Y)? FindNearestOpenCell(eGridDef[,] map, int x, int y, eGridDef blocked = eGridDef.Wall)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var seen = new bool[width, height];
            var queue = new System.Collections.Generic.Queue<(int X, int Y)>();

            queue.Enqueue((x, y));
            seen[x, y] = true;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();

                if ((map[cell.X, cell.Y] & blocked) == 0)
                    return cell;

                foreach (var facing in Enum.GetValues<eFacing>())
                {
                    var (nx, ny) = GetNeighbor(cell.X, cell.Y, facing, width, height);

                    if (!seen[nx, ny])
                    {
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// True if something at from, going this way, is getting closer to to.
        /// </summary>
        public static bool IsHeadingToward(Point2 from, eFacing facing, Point2 to)
        {
            return facing switch
            {
                eFacing.Up => to.Y < from.Y,
                eFacing.Down => to.Y > from.Y,
                eFacing.Left => to.X < from.X,
                _ => to.X > from.X,
            };
        }

        /// <summary>
        /// Moves a pac or ghost up to distance cells along its facing. Turning back happens at once; any other
        /// turn into wanted waits for a cell centre with an open cell that way. With nothing wanted it keeps
        /// going, and it stops on the centre of a cell with a wall ahead. Returns false if a wall stopped it.
        /// Without canWarp, warp tunnel cells count as walls, and cells with any of the blocked flags always do.
        /// </summary>
        public static bool Move(eGridDef[,] map, GameEntity entity, eFacing? wanted, float distance, bool canWarp = true, eGridDef blocked = eGridDef.Wall)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var position = entity.Location;
            var facing = entity.Facing;

            if (wanted == GetOpposite(facing))
                facing = wanted.Value;

            var moved = true;

            while (distance > 0f)
            {
                var toCentre = GetDistanceToCentre(position, facing);

                if (toCentre == 0f)
                {
                    var (x, y) = GetCell(new Point2(Wrap(position.X + CENTRE_TOLERANCE, width), Wrap(position.Y + CENTRE_TOLERANCE, height)));
                    position = new Point2(x + CELL_CENTER, y + CELL_CENTER);

                    if (wanted.HasValue && CanMove(map, x, y, wanted.Value, canWarp, blocked))
                        facing = wanted.Value;

                    if (!CanMove(map, x, y, facing, canWarp, blocked))
                    {
                        moved = false;
                        break;
                    }

                    toCentre = 1f;
                }

                var step = MathF.Min(distance, toCentre);
                position = Advance(position, facing, step, width, height);
                distance -= step;
            }

            entity.Location = position;
            entity.Facing = facing;
            return moved;
        }
    }
}
