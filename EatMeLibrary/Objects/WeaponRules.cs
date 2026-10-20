using FastG;
using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Rules for weapon effects (WeaponEffect) shared by the server and the bots: speeds, limits, what they hit and
    /// who the seekers go after. Nothing goes through walls: shots stop at them and seekers follow the maze.
    /// </summary>
    public static class WeaponRules
    {
        /// <summary>
        /// A thrown sword moves this many times PacSpeed.
        /// </summary>
        public const float SWORD_SPEED_SCALE = 2f;

        /// <summary>
        /// Swords one pac can have in flight at a time.
        /// </summary>
        public const int MAX_SWORDS = 2;

        /// <summary>
        /// A fireball moves this many times PacSpeed, a little faster than a pac.
        /// </summary>
        public const float FIREBALL_SPEED_SCALE = 1.25f;

        /// <summary>
        /// A pac with Fireball can fire one this often.
        /// </summary>
        public const int FIREBALL_COOLDOWN_MS = 500;

        /// <summary>
        /// The blue shell and poke ball move this many times PacSpeed.
        /// </summary>
        public const float SEEKER_SPEED_SCALE = 3f;

        /// <summary>
        /// A weapon effect counts as a circle of this radius in cells, for touching pacs and ghosts.
        /// </summary>
        public const float HIT_RADIUS = 0.4f;

        /// <summary>
        /// Bots fire (sword or fireball) at an enemy or ghost straight ahead within this many cells.
        /// </summary>
        public const int FIRE_DISTANCE = 10;

        public static float GetSpeedScale(eWeaponEffect kind)
        {
            return kind switch
            {
                eWeaponEffect.Sword => SWORD_SPEED_SCALE,
                eWeaponEffect.Fireball => FIREBALL_SPEED_SCALE,
                eWeaponEffect.BlueShell or eWeaponEffect.PokeBall => SEEKER_SPEED_SCALE,
                _ => 0f,
            };
        }

        /// <summary>
        /// True if a weapon effect at position touches a pac of the given size at pacPosition.
        /// </summary>
        public static bool IsTouchingPac(Point2 position, Point2 pacPosition, float pacSize)
        {
            return PacRules.AreTouching(position, HIT_RADIUS, pacPosition, PacRules.GetRadius(pacSize));
        }

        /// <summary>
        /// True if a weapon effect at position touches a ghost of the given radius at ghostPosition.
        /// </summary>
        public static bool IsTouchingGhost(Point2 position, Point2 ghostPosition, float ghostRadius)
        {
            return PacRules.AreTouching(position, HIT_RADIUS, ghostPosition, ghostRadius);
        }

        /// <summary>
        /// True if a pac with this many swords in flight can throw another.
        /// </summary>
        public static bool CanThrowSword(int swordsInFlight)
        {
            return swordsInFlight < MAX_SWORDS;
        }

        /// <summary>
        /// Who a blue shell fired by a pac on ownerTeam goes after: the living enemy carrying a flag, otherwise the
        /// living enemy with the highest level score (the higher total score, then the lower id, on a tie). Null if
        /// no enemy is alive.
        /// </summary>
        public static Player PickBlueShellTarget(eTeam ownerTeam, IEnumerable<Player> pacs)
        {
            Player carrier = null;
            Player best = null;

            foreach (var pac in pacs)
            {
                if (!pac.Alive || pac.Team == ownerTeam)
                    continue;

                if (pac.HasFlag && (carrier == null || pac.Id < carrier.Id))
                    carrier = pac;

                if (best == null || pac.Score > best.Score || (pac.Score == best.Score
                    && (pac.TotalScore > best.TotalScore || (pac.TotalScore == best.TotalScore && pac.Id < best.Id))))
                    best = pac;
            }

            return carrier ?? best;
        }

        /// <summary>
        /// The living ghost nearest by path on paths (filled from the poke ball's cell), or null if no living ghost
        /// can be reached.
        /// </summary>
        public static Ghost FindNearestGhost(IEnumerable<Ghost> ghosts, PathMap paths)
        {
            Ghost nearest = null;
            var nearestDistance = int.MaxValue;

            foreach (var ghost in ghosts)
            {
                if (!ghost.Alive)
                    continue;

                var (x, y) = MazeMovement.GetCell(ghost.Location);
                var distance = paths.GetDistance(x, y);

                if (distance != PathMap.UNREACHED && distance < nearestDistance)
                {
                    nearest = ghost;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Cells from (x, y) to the first wall going facing (wrapping round the map), at most maxCells; 0 if the next
        /// cell is a wall.
        /// </summary>
        public static int GetClearCells(eGridDef[,] map, int x, int y, eFacing facing, int maxCells)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var cells = 0;

            while (cells < maxCells)
            {
                (x, y) = MazeMovement.GetNeighbor(x, y, facing, width, height);

                if (map[x, y].HasFlag(eGridDef.Wall))
                    break;

                cells++;
            }

            return cells;
        }

        /// <summary>
        /// True if position is in line with (x, y) along facing (the same row or column, ahead) within cells cells.
        /// </summary>
        public static bool IsInLineAhead(int x, int y, eFacing facing, int cells, Point2 position, int width, int height)
        {
            var (px, py) = MazeMovement.GetCell(position);

            for (var i = 1; i <= cells; i++)
            {
                (x, y) = MazeMovement.GetNeighbor(x, y, facing, width, height);

                if (x == px && y == py)
                    return true;
            }

            return false;
        }
    }
}
