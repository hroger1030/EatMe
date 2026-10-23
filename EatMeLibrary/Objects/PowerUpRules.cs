using FastG;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EatMeLibrary
{
    /// <summary>
    /// How power ups move, are eaten and hover. Shared by the server (movement, eating) and the client (hover).
    /// </summary>
    public static class PowerUpRules
    {
        /// <summary>
        /// Radius, in cells, a pac must touch to eat a power up.
        /// </summary>
        public const float RADIUS = 0.5f;

        /// <summary>
        /// Only walls stop a power up; it crosses spawn tiles.
        /// </summary>
        public const eGridDef BLOCKED = eGridDef.Wall;

        /// <summary>
        /// Every item, in enum order.
        /// </summary>
        public static readonly IReadOnlyList<ePowerUp> ALL = Enum.GetValues<ePowerUp>().Where(item => item != ePowerUp.None).ToList().AsReadOnly();

        /// <summary>
        /// The items that can appear on the board: every item but the Power Pill, since every maze has power pills.
        /// </summary>
        public static readonly IReadOnlyList<ePowerUp> SPAWNABLE = ALL.Where(item => item != ePowerUp.PowerPill).ToList().AsReadOnly();

        /// <summary>
        /// How far, in cells, a power up's sprite bobs above and below its position.
        /// </summary>
        public const float HOVER_CELLS = 0.1f;

        /// <summary>
        /// How long one bob up and down takes.
        /// </summary>
        public const int HOVER_PERIOD_MS = 1200;

        /// <summary>
        /// Fraction of a bob each power up id is shifted by, so power ups side by side don't bob in step.
        /// </summary>
        public const float HOVER_PHASE_PER_ID = 0.37f;

        /// <summary>
        /// Size, in cells, of the oval shadow drawn under a power up at its true position, so its bob shows whichever
        /// way it moves.
        /// </summary>
        public const float SHADOW_WIDTH_CELLS = 0.7f;
        public const float SHADOW_HEIGHT_CELLS = 0.25f;

        /// <summary>
        /// How far, in cells, the shadow's centre sits below the power up's position.
        /// </summary>
        public const float SHADOW_OFFSET_CELLS = 0.45f;

        /// <summary>
        /// Opacity of the shadow at its centre; it fades to nothing at its edge.
        /// </summary>
        public const float SHADOW_ALPHA = 0.6f;

        /// <summary>
        /// Opacity (0 to 1) of pixel (x, y) of a shadow texture width by height pixels: SHADOW_ALPHA at the oval's
        /// centre, fading to 0 at its edge, and 0 outside it.
        /// </summary>
        public static float GetShadowAlpha(int x, int y, int width, int height)
        {
            var dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
            var dy = (y + 0.5f - height * 0.5f) / (height * 0.5f);
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance >= 1f)
                return 0f;

            return SHADOW_ALPHA * (1f - distance * distance);
        }

        /// <summary>
        /// Vertical offset, in cells, of a power up's sprite at the given time: a sine wave between -HOVER_CELLS and HOVER_CELLS.
        /// </summary>
        public static float GetHoverOffset(TimeSpan time, int powerUpId)
        {
            var phase = (time.TotalMilliseconds / HOVER_PERIOD_MS) + (powerUpId * HOVER_PHASE_PER_ID);
            return HOVER_CELLS * (float)Math.Sin(phase * 2.0 * Math.PI);
        }

        /// <summary>
        /// True if a pac of the given size at pacPosition is touching a power up at powerUpPosition.
        /// </summary>
        public static bool IsTouching(Point2 pacPosition, float size, Point2 powerUpPosition)
        {
            return PacRules.AreTouching(pacPosition, PacRules.GetRadius(size), powerUpPosition, RADIUS);
        }

        /// <summary>
        /// One item picked at random from a list of items (ALL for every item), or None if the list is empty.
        /// </summary>
        public static ePowerUp PickItem(IReadOnlyList<ePowerUp> items, Random random)
        {
            return items.Count == 0 ? ePowerUp.None : items[random.Next(items.Count)];
        }

        /// <summary>
        /// How many power ups are due when a level's timer moves from beforeMs to afterMs: one each time it passes a
        /// whole multiple of intervalMs. None if intervalMs is 0 or less.
        /// </summary>
        public static int GetSpawnsDue(int beforeMs, int afterMs, int intervalMs)
        {
            if (intervalMs <= 0)
                return 0;

            return afterMs / intervalMs - beforeMs / intervalMs;
        }

        /// <summary>
        /// Every warp tunnel cell on the map (MazeMovement.IsWarpCell): where power ups head for.
        /// </summary>
        public static List<(int X, int Y)> FindExits(eGridDef[,] map)
        {
            var exits = new List<(int X, int Y)>();

            for (var x = 0; x < map.GetLength(0); x++)
                for (var y = 0; y < map.GetLength(1); y++)
                    if (MazeMovement.IsWarpCell(map, x, y))
                        exits.Add((x, y));

            return exits;
        }

        /// <summary>
        /// Every power up spawn point on the map.
        /// </summary>
        public static List<(int X, int Y)> FindSpawns(eGridDef[,] map)
        {
            var spawns = new List<(int X, int Y)>();

            for (var x = 0; x < map.GetLength(0); x++)
                for (var y = 0; y < map.GetLength(1); y++)
                    if (map[x, y].HasFlag(eGridDef.PowerUpSpawn))
                        spawns.Add((x, y));

            return spawns;
        }

        /// <summary>
        /// Moves a power up distance cells toward its exit, turning downhill on exitPaths (a PathMap filled from the exit
        /// with BLOCKED) at each cell centre. Returns true when it reaches the exit cell's centre, so it should leave the level.
        /// distance must be under a cell, so at most one centre is reached.
        /// </summary>
        public static bool Move(eGridDef[,] map, PowerUp powerUp, PathMap exitPaths, float distance)
        {
            eFacing? wanted = null;

            if (MazeMovement.GetDistanceToCentre(powerUp.Location, powerUp.Facing) <= distance)
            {
                var (cx, cy) = MazeMovement.GetNextCentreCell(powerUp.Location, powerUp.Facing, map.GetLength(0), map.GetLength(1));

                if ((cx, cy) == powerUp.Exit)
                    return true;

                wanted = PacBrain.ChooseDownhill(map, cx, cy, exitPaths, BLOCKED);
            }

            MazeMovement.Move(map, powerUp, wanted, distance, true, BLOCKED);
            return false;
        }
    }
}
