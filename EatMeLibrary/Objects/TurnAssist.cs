using FastG;
using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Makes turning easier for human pacs. A direction pressed and let go early is remembered for a while (the
    /// turn buffer), and a turn pressed just after the pac passed an open junction still happens: the pac is put
    /// back on that junction and turned (a late turn). The late turn window grows with the player's lag.
    /// </summary>
    public static class TurnAssist
    {
        /// <summary>
        /// The late turn window is never wider than this, so it can only reach the last cell centre passed.
        /// </summary>
        public const float MAX_GRACE_CELLS = 0.9f;

        private const int MS_PER_SECOND = 1000;
        private const float CELL_CENTER = 0.5f;

        /// <summary>
        /// How far past a junction, in cells, a late turn is still allowed: graceCells plus the distance covered at
        /// speed during the lag (lag capped at maxLagMs), up to MAX_GRACE_CELLS. 0 when graceCells is 0 or less
        /// (late turns off). A negative lag (none measured yet) counts as none.
        /// </summary>
        public static float GetGraceDistance(float graceCells, float speed, int lagMs, int maxLagMs)
        {
            if (graceCells <= 0f)
                return 0f;

            var lag = Math.Clamp(lagMs, 0, Math.Max(0, maxLagMs));
            return Math.Min(graceCells + speed * lag / (float)MS_PER_SECOND, MAX_GRACE_CELLS);
        }

        /// <summary>
        /// The way a pac should try to turn: the held direction, or with nothing held the buffered one while it lasts.
        /// </summary>
        public static eFacing? GetWantedTurn(eFacing? held, eFacing? buffered, int bufferMsLeft)
        {
            return held ?? (bufferMsLeft > 0 ? buffered : null);
        }

        /// <summary>
        /// Turns a pac that has just passed a junction: if wanted is to one side, the last cell centre behind the pac
        /// is no more than maxDistance back, and the way wanted is open there, the pac is put on that centre facing
        /// wanted. Returns the distance it was put back (0 if it wasn't), for the caller to move it on the new way.
        /// </summary>
        public static float TryLateTurn(eGridDef[,] map, GameEntity entity, eFacing wanted, float maxDistance, bool canWarp = true, eGridDef blocked = eGridDef.Wall)
        {
            var facing = entity.Facing;

            if (maxDistance <= 0f || wanted == facing || wanted == MazeMovement.GetOpposite(facing))
                return 0f;

            var behind = MazeMovement.GetOpposite(facing);
            var back = MazeMovement.GetDistanceToCentre(entity.Location, behind);

            // on a centre already: Move turns it the normal way
            if (back <= 0f || back > maxDistance)
                return 0f;

            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var (x, y) = MazeMovement.GetCell(MazeMovement.Advance(entity.Location, behind, back, width, height));
            x = Math.Clamp(x, 0, width - 1);
            y = Math.Clamp(y, 0, height - 1);

            if (!MazeMovement.CanMove(map, x, y, wanted, canWarp, blocked))
                return 0f;

            entity.Location = new Point2(x + CELL_CENTER, y + CELL_CENTER);
            entity.Facing = wanted;
            return back;
        }
    }
}
