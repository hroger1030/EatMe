using System;
using FastG;

namespace EatMeLibrary
{
    /// <summary>
    /// Rules for Melon Roll (melon): the action button sends the pac rolling in a straight line, out of its player's
    /// control, smashing walls and stunning anyone it touches until it reaches something it can't smash or the effect ends.
    /// </summary>
    public static class MelonRoll
    {
        /// <summary>
        /// A rolling pac moves this many times as fast.
        /// </summary>
        public const float SPEED_SCALE = 3f;

        private const float CELL_CENTER = 0.5f;

        /// <summary>
        /// Full turns a second a rolling pac spins on the client.
        /// </summary>
        public const float SPIN_TURNS_PER_SECOND = 4f;

        /// <summary>
        /// True if a rolling pac can roll into (x, y): any cell its team can enter (blocked, from
        /// MazeMovement.GetPacBlocked), or a wall it can smash (WeaponRules.CanSmash: not the outer wall or a Tetromino block).
        /// </summary>
        public static bool CanRollInto(eGridDef[,] map, int x, int y, eGridDef blocked)
        {
            var cell = map[x, y];

            if ((cell & blocked & ~eGridDef.Wall) != 0)
                return false;

            return !cell.HasFlag(eGridDef.Wall) || WeaponRules.CanSmash(map, x, y);
        }

        /// <summary>
        /// Which way a roll starts: the way the player holds (held) if the pac can go that way now (along the line it is
        /// on, or any open way from a cell centre), otherwise the way it faces. Null if it can't roll at all, which only
        /// happens on a cell centre with nothing to roll into ahead.
        /// </summary>
        public static eFacing? GetStartFacing(eGridDef[,] map, GameEntity pac, eFacing? held, eGridDef blocked)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            if (MazeMovement.GetDistanceToCentre(pac.Location, pac.Facing) > 0f)
            {
                // between centres only straight on or straight back
                return held == MazeMovement.GetOpposite(pac.Facing) ? held : pac.Facing;
            }

            var (x, y) = MazeMovement.GetNextCentreCell(pac.Location, pac.Facing, width, height);

            if (held.HasValue && CanRollAhead(map, x, y, held.Value, blocked))
                return held;

            return CanRollAhead(map, x, y, pac.Facing, blocked) ? pac.Facing : null;
        }

        /// <summary>
        /// Rolls the pac distance cells straight on the way it faces, through walls it can smash and warp tunnels,
        /// stopping on the centre of the last cell before one it can't roll into (CanRollInto). Returns false if it stopped.
        /// </summary>
        public static bool Move(eGridDef[,] map, GameEntity pac, float distance, eGridDef blocked)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            while (distance > 0f)
            {
                var toCentre = MazeMovement.GetDistanceToCentre(pac.Location, pac.Facing);

                if (toCentre == 0f)
                {
                    var (x, y) = MazeMovement.GetNextCentreCell(pac.Location, pac.Facing, width, height);

                    if (!CanRollAhead(map, x, y, pac.Facing, blocked))
                    {
                        pac.Location = new Point2(x + CELL_CENTER, y + CELL_CENTER);
                        return false;
                    }

                    toCentre = 1f;
                }

                // the cell ahead has been checked, so nothing on the way to its centre blocks
                var step = MathF.Min(distance, toCentre);
                MazeMovement.Move(map, pac, null, step, canWarp: true, blocked: 0);
                distance -= step;
            }

            return true;
        }

        /// <summary>
        /// The angle (radians, 0 to 2 pi) a rolling pac is turned to at a time, spinning SPIN_TURNS_PER_SECOND clockwise.
        /// </summary>
        public static float GetSpinAngle(TimeSpan time)
        {
            var turns = (float)time.TotalSeconds * SPIN_TURNS_PER_SECOND;
            return (turns - MathF.Floor(turns)) * MathF.PI * 2f;
        }

        private static bool CanRollAhead(eGridDef[,] map, int x, int y, eFacing facing, eGridDef blocked)
        {
            var (nx, ny) = MazeMovement.GetNeighbor(x, y, facing, map.GetLength(0), map.GetLength(1));
            return CanRollInto(map, nx, ny, blocked);
        }
    }
}
