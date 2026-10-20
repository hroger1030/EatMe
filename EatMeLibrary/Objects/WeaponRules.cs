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
        /// A fireball moves this many times PacSpeed, much faster than a pac.
        /// </summary>
        public const float FIREBALL_SPEED_SCALE = 3.75f;

        /// <summary>
        /// After the action button does something it does nothing again for this long, whatever the effect, so it
        /// can't be spammed.
        /// </summary>
        public const int ACTION_COOLDOWN_MS = 500;

        /// <summary>
        /// A spread shot moves this many times PacSpeed.
        /// </summary>
        public const float SPREAD_SHOT_SPEED_SCALE = 1.5f;

        /// <summary>
        /// A pac with Spread Shot fires four shots (up, down, left, right) this often.
        /// </summary>
        public const int SPREAD_SHOT_INTERVAL_MS = 500;

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
                eWeaponEffect.SpreadShot => SPREAD_SHOT_SPEED_SCALE,
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
        /// True if (x, y) is on the outer wall's ring, which can never be broken.
        /// </summary>
        public static bool IsOuterCell(int x, int y, int width, int height)
        {
            return x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1;
        }

        /// <summary>
        /// True if the Hammer or Super Size can smash the cell: a wall, not a Tetromino block and not the outer wall.
        /// </summary>
        public static bool CanSmash(eGridDef[,] map, int x, int y)
        {
            var cell = map[x, y];
            return cell.HasFlag(eGridDef.Wall) && !cell.HasFlag(eGridDef.TetrisBlock) && !IsOuterCell(x, y, map.GetLength(0), map.GetLength(1));
        }

        /// <summary>
        /// Fills cells with every map cell a circle of radius (in cells) at centre overlaps, so a big pac's reach matches
        /// its size. Cells off the map are left out; a cell only touched at its edge doesn't count.
        /// </summary>
        public static void GetTouchedCells(Point2 centre, float radius, int width, int height, List<(int X, int Y)> cells)
        {
            cells.Clear();
            var minX = Math.Max(0, (int)MathF.Floor(centre.X - radius));
            var maxX = Math.Min(width - 1, (int)MathF.Floor(centre.X + radius));
            var minY = Math.Max(0, (int)MathF.Floor(centre.Y - radius));
            var maxY = Math.Min(height - 1, (int)MathF.Floor(centre.Y + radius));

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    // the nearest point of the cell's square to the centre
                    var dx = centre.X - Math.Clamp(centre.X, x, x + 1f);
                    var dy = centre.Y - Math.Clamp(centre.Y, y, y + 1f);

                    if (dx * dx + dy * dy < radius * radius)
                        cells.Add((x, y));
                }
            }
        }

        /// <summary>
        /// What a smashed wall cell becomes: open, with a dot.
        /// </summary>
        public static eGridDef GetSmashedCell(eGridDef cell)
        {
            return (cell & ~eGridDef.Wall) | eGridDef.Food;
        }

        /// <summary>
        /// A random cell with a dot that isn't in occupied (cells with a pac or ghost in them), for Warp (pear); null if
        /// there is none.
        /// </summary>
        public static (int X, int Y)? PickWarpCell(eGridDef[,] map, ICollection<(int X, int Y)> occupied, Random random)
        {
            var cells = new List<(int X, int Y)>();

            for (var x = 0; x < map.GetLength(0); x++)
                for (var y = 0; y < map.GetLength(1); y++)
                    if (map[x, y].HasFlag(eGridDef.Food) && !map[x, y].HasFlag(eGridDef.Wall) && !occupied.Contains((x, y)))
                        cells.Add((x, y));

            if (cells.Count == 0)
                return null;

            return cells[random.Next(cells.Count)];
        }

        /// <summary>
        /// Where a pac at location's hammer is: one cell further along facing, wrapped round the map so it stays on it.
        /// </summary>
        public static Point2 GetHammerLocation(Point2 location, eFacing facing, int width, int height)
        {
            var (dx, dy) = facing switch
            {
                eFacing.Up => (0f, -1f),
                eFacing.Down => (0f, 1f),
                eFacing.Left => (-1f, 0f),
                _ => (1f, 0f),
            };

            var x = (location.X + dx + width) % width;
            var y = (location.Y + dy + height) % height;
            return new Point2(x, y);
        }

        /// <summary>
        /// The cell a Tetromino block goes in for a pac in (x, y) facing facing: the one behind it (wrapping round the map).
        /// </summary>
        public static (int X, int Y) GetBehindCell(int x, int y, eFacing facing, int width, int height)
        {
            return MazeMovement.GetNeighbor(x, y, MazeMovement.GetOpposite(facing), width, height);
        }

        /// <summary>
        /// True if a Tetromino block can go in the cell: open, not a spawn tile, flag base or ghost generator, and with
        /// no block in it already.
        /// </summary>
        public static bool CanDropBlock(eGridDef cell)
        {
            const eGridDef SPECIAL = eGridDef.Wall | eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue | eGridDef.FlagBaseRed
                | eGridDef.FlagBaseBlue | eGridDef.GhostGenerator;
            return (cell & SPECIAL) == 0;
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
