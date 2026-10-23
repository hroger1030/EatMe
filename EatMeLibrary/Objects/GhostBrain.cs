using FastG;
using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// How ghosts pick who to chase and which way to go.
    /// </summary>
    public static class GhostBrain
    {
        private const sbyte NOT_VISITED = -1;
        private const int UNREACHED = -1;

        /// <summary>
        /// Cells ahead of its target the Ambusher aims for.
        /// </summary>
        public const int AMBUSH_STEPS = 4;

        /// <summary>
        /// Cells from its target at which the Ditherer loses its nerve and heads home.
        /// </summary>
        public const float SCATTER_DISTANCE = 6f;

        /// <summary>
        /// The FlagHound sees flag carriers this many times as far as other ghosts.
        /// </summary>
        public const float FLAG_HOUND_VISION_SCALE = 2f;

        /// <summary>
        /// Cells within which the FlagHound goes for a pac that isn't carrying a flag.
        /// </summary>
        public const float FLAG_HOUND_CLOSE_DISTANCE = 3f;

        /// <summary>
        /// Cells from its spawn cell the Guard may go.
        /// </summary>
        public const int LEASH_CELLS = 12;

        /// <summary>
        /// ms the Sprinter chases one target before giving up.
        /// </summary>
        public const int ATTENTION_MS = 4000;

        /// <summary>
        /// ms the Sprinter wanders after giving up before it picks a new target.
        /// </summary>
        public const int REST_MS = 2000;

        /// <summary>
        /// The Lurker's speed scale while it chases.
        /// </summary>
        public const float LURK_CHARGE_SCALE = 1.15f;

        /// <summary>
        /// Fewest steps from where the Lurker is to the junction it waits on next, so it leaves the pen and moves on
        /// after a chase.
        /// </summary>
        public const int LURK_MIN_STEPS = 6;

        /// <summary>
        /// The Grudge sees the pac that last killed it this many times as far as other ghosts.
        /// </summary>
        public const float GRUDGE_VISION_SCALE = 3f;

        /// <summary>
        /// The Elroy's speed scale at the start of a level and when the leading team reaches MaxScore.
        /// </summary>
        public const float FRENZY_MIN_SPEED_SCALE = 0.9f;
        public const float FRENZY_MAX_SPEED_SCALE = 1.3f;

        /// <summary>
        /// ms behind its target the SlowThinker is.
        /// </summary>
        public const int LAG_MS = 3000;

        /// <summary>
        /// The Sleeper naps for the last NAP_MS of every NAP_CYCLE_MS.
        /// </summary>
        public const int NAP_CYCLE_MS = 10000;
        public const int NAP_MS = 3000;

        /// <summary>
        /// Cells from its leader within which the Sidekick stops closing in.
        /// </summary>
        public const float FOLLOW_DISTANCE = 3f;

        /// <summary>
        /// Cells within which the Roamer stops wandering and goes for a pac.
        /// </summary>
        public const float DIZZY_ATTACK_DISTANCE = 5f;

        /// <summary>
        /// Picks the pac a ghost chases. A ghost keeps its target until the target dies, leaves or eats a power pill,
        /// or a better pac comes within vision: a flag carrier beats any pac that isn't carrying, otherwise the closer
        /// pac wins. Ghosts never chase pacs that repel them (Player.RepelsGhosts: a power pill or Doctor Away).
        /// Returns null when there is no target and nobody it can chase is within vision.
        /// </summary>
        public static Player PickTarget(Point2 ghostPosition, Player current, IEnumerable<Player> pacs, float vision)
        {
            Player bestTarget = null;
            var bestDistance = float.MaxValue;
            var currentPresent = false;
            var visionSquared = vision * vision;

            foreach (var pac in pacs)
            {
                if (!pac.Alive || pac.RepelsGhosts)
                    continue;

                if (pac == current)
                    currentPresent = true;

                var distance = GetDistanceSquared(ghostPosition, pac.Location);

                if (distance > visionSquared)
                    continue;

                if (bestTarget == null || IsBetterTarget(pac, distance, bestTarget, bestDistance))
                {
                    bestTarget = pac;
                    bestDistance = distance;
                }
            }

            if (current == null || !currentPresent)
                return bestTarget;

            if (bestTarget != null && IsBetterTarget(bestTarget, bestDistance, current, GetDistanceSquared(ghostPosition, current.Location)))
                return bestTarget;

            return current;
        }

        /// <summary>
        /// The nearest living pac within vision that repels ghosts (Player.RepelsGhosts), which the ghost runs from.
        /// Null if there is none.
        /// </summary>
        public static Player FindThreat(Point2 ghostPosition, IEnumerable<Player> pacs, float vision)
        {
            Player threat = null;
            var threatDistance = vision * vision;

            foreach (var pac in pacs)
            {
                if (!pac.Alive || !pac.RepelsGhosts)
                    continue;

                var distance = GetDistanceSquared(ghostPosition, pac.Location);

                if (distance <= threatDistance)
                {
                    threat = pac;
                    threatDistance = distance;
                }
            }

            return threat;
        }

        /// <summary>
        /// Which way a ghost on the centre of cell (x, y) runs from a threat in cell (threatX, threatY): toward the
        /// open neighbour furthest from the threat by path through the maze, turning back if that is best.
        /// The threat's paths use the warp tunnels but the ghost never steps into one. Null if the ghost is boxed in.
        /// </summary>
        public static eFacing? ChooseFleeDirection(eGridDef[,] map, int x, int y, int threatX, int threatY)
        {
            var distances = GetPathDistances(map, threatX, threatY);
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            eFacing? best = null;
            var bestDistance = -1;

            foreach (var direction in Enum.GetValues<eFacing>())
            {
                if (!MazeMovement.CanMove(map, x, y, direction, false, MazeMovement.GHOST_BLOCKED))
                    continue;

                var (nx, ny) = MazeMovement.GetNeighbor(x, y, direction, width, height);

                // a cell the threat can't reach is as far away as it gets
                var distance = distances[nx, ny] == UNREACHED ? int.MaxValue : distances[nx, ny];

                if (distance > bestDistance)
                {
                    best = direction;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Steps through open cells from (fromX, fromY) to every cell, using the warp tunnels. UNREACHED where no path.
        /// </summary>
        public static int[,] GetPathDistances(eGridDef[,] map, int fromX, int fromY)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var distances = new int[width, height];

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    distances[x, y] = UNREACHED;

            var queue = new Queue<(int X, int Y)>();
            distances[fromX, fromY] = 0;
            queue.Enqueue((fromX, fromY));

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                foreach (var direction in Enum.GetValues<eFacing>())
                {
                    if (!MazeMovement.CanMove(map, cx, cy, direction))
                        continue;

                    var (nx, ny) = MazeMovement.GetNeighbor(cx, cy, direction, width, height);

                    if (distances[nx, ny] != UNREACHED)
                        continue;

                    distances[nx, ny] = distances[cx, cy] + 1;
                    queue.Enqueue((nx, ny));
                }
            }

            return distances;
        }

        /// <summary>
        /// True if pac a (squared distance aDistance away) is a better target than pac b. Flag carriers come first.
        /// </summary>
        public static bool IsBetterTarget(Player a, float aDistance, Player b, float bDistance)
        {
            if (a.HasFlag != b.HasFlag)
                return a.HasFlag;

            return aDistance < bDistance;
        }

        public static float GetDistanceSquared(Point2 a, Point2 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Which way a ghost on the centre of cell (x, y), heading facing, goes next. With a target cell it takes
        /// the shortest path there. Without one, or if the target can't be reached, it picks a random open way,
        /// never turning back unless it has to. Ghosts never use the warp tunnels or enter a spawn room. Returns null when it is on the
        /// target cell or boxed in.
        /// </summary>
        public static eFacing? ChooseDirection(eGridDef[,] map, int x, int y, eFacing facing, (int X, int Y)? target, Random random)
        {
            if (target.HasValue)
            {
                if (target.Value == (x, y))
                    return null;

                var step = FindFirstStep(map, x, y, target.Value.X, target.Value.Y);
                if (step.HasValue)
                    return step;
            }

            var options = new List<eFacing>(4);
            var back = MazeMovement.GetOpposite(facing);

            foreach (var direction in Enum.GetValues<eFacing>())
                if (direction != back && MazeMovement.CanMove(map, x, y, direction, false, MazeMovement.GHOST_BLOCKED))
                    options.Add(direction);

            if (options.Count > 0)
                return options[random.Next(options.Count)];

            return MazeMovement.CanMove(map, x, y, back, false, MazeMovement.GHOST_BLOCKED) ? back : null;
        }

        /// <summary>
        /// First step a ghost takes on a shortest path through open cells from (fromX, fromY) to (toX, toY),
        /// keeping out of the warp tunnels and spawn rooms. Null if they are the same cell or there is no path.
        /// </summary>
        public static eFacing? FindFirstStep(eGridDef[,] map, int fromX, int fromY, int toX, int toY)
        {
            if (fromX == toX && fromY == toY)
                return null;

            var width = map.GetLength(0);
            var height = map.GetLength(1);

            // the first step taken to reach each cell
            var firstStep = new sbyte[width, height];
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    firstStep[x, y] = NOT_VISITED;

            var queue = new Queue<(int X, int Y)>();

            foreach (var direction in Enum.GetValues<eFacing>())
            {
                if (!MazeMovement.CanMove(map, fromX, fromY, direction, false, MazeMovement.GHOST_BLOCKED))
                    continue;

                var (nx, ny) = MazeMovement.GetNeighbor(fromX, fromY, direction, width, height);

                if (firstStep[nx, ny] != NOT_VISITED || (nx == fromX && ny == fromY))
                    continue;

                firstStep[nx, ny] = (sbyte)direction;
                queue.Enqueue((nx, ny));
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                if (cx == toX && cy == toY)
                    return (eFacing)firstStep[cx, cy];

                foreach (var direction in Enum.GetValues<eFacing>())
                {
                    if (!MazeMovement.CanMove(map, cx, cy, direction, false, MazeMovement.GHOST_BLOCKED))
                        continue;

                    var (nx, ny) = MazeMovement.GetNeighbor(cx, cy, direction, width, height);

                    if (firstStep[nx, ny] != NOT_VISITED || (nx == fromX && ny == fromY))
                        continue;

                    firstStep[nx, ny] = firstStep[cx, cy];
                    queue.Enqueue((nx, ny));
                }
            }

            return null;
        }

        /// <summary>
        /// True if a ghost may go after this pac: alive, not repelling ghosts (Player.RepelsGhosts), and among the living pacs.
        /// </summary>
        public static bool IsChaseable(Player pac, IEnumerable<Player> pacs)
        {
            if (pac == null || !pac.Alive || pac.RepelsGhosts)
                return false;

            foreach (var other in pacs)
                if (other == pac)
                    return true;

            return false;
        }

        /// <summary>
        /// The Hunter's target: like PickTarget, but it never swaps its target for a closer pac, only for a flag carrier
        /// when its target isn't carrying one.
        /// </summary>
        public static Player PickStubbornTarget(Point2 ghostPosition, Player current, IEnumerable<Player> pacs, float vision)
        {
            var best = PickTarget(ghostPosition, null, pacs, vision);

            if (!IsChaseable(current, pacs))
                return best;

            return best != null && best.HasFlag && !current.HasFlag ? best : current;
        }

        /// <summary>
        /// The FlagHound's target: the nearest flag carrier within vision times FLAG_HOUND_VISION_SCALE, otherwise the
        /// nearest pac within FLAG_HOUND_CLOSE_DISTANCE. Null if neither. Never a pac with a power pill working.
        /// </summary>
        public static Player PickFlagHoundTarget(Point2 ghostPosition, IEnumerable<Player> pacs, float vision)
        {
            Player carrier = null;
            Player close = null;
            var carrierDistance = vision * FLAG_HOUND_VISION_SCALE * vision * FLAG_HOUND_VISION_SCALE;
            var closeDistance = FLAG_HOUND_CLOSE_DISTANCE * FLAG_HOUND_CLOSE_DISTANCE;

            foreach (var pac in pacs)
            {
                if (!pac.Alive || pac.RepelsGhosts)
                    continue;

                var distance = GetDistanceSquared(ghostPosition, pac.Location);

                if (pac.HasFlag && distance <= carrierDistance)
                {
                    carrier = pac;
                    carrierDistance = distance;
                }

                if (distance <= closeDistance)
                {
                    close = pac;
                    closeDistance = distance;
                }
            }

            return carrier ?? close;
        }

        /// <summary>
        /// The smallest pac within vision that has no power pill working, the nearest of them on a tie. Null if none.
        /// </summary>
        public static Player PickSmallest(Point2 ghostPosition, IEnumerable<Player> pacs, float vision)
        {
            return PickBySize(ghostPosition, pacs, vision, true);
        }

        /// <summary>
        /// The biggest pac within vision that has no power pill working, the nearest of them on a tie. Null if none.
        /// </summary>
        public static Player PickBiggest(Point2 ghostPosition, IEnumerable<Player> pacs, float vision)
        {
            return PickBySize(ghostPosition, pacs, vision, false);
        }

        private static Player PickBySize(Point2 ghostPosition, IEnumerable<Player> pacs, float vision, bool smallest)
        {
            Player best = null;
            var bestDistance = float.MaxValue;
            var visionSquared = vision * vision;

            foreach (var pac in pacs)
            {
                if (!pac.Alive || pac.RepelsGhosts)
                    continue;

                var distance = GetDistanceSquared(ghostPosition, pac.Location);

                if (distance > visionSquared)
                    continue;

                var better = best == null
                    || (smallest ? pac.Size < best.Size : pac.Size > best.Size)
                    || (pac.Size == best.Size && distance < bestDistance);

                if (better)
                {
                    best = pac;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// The Grudge's target: the pac with grudgeId if it is chaseable and within vision times GRUDGE_VISION_SCALE,
        /// otherwise PickTarget's choice.
        /// </summary>
        public static Player PickGrudgeTarget(Point2 ghostPosition, Player current, IEnumerable<Player> pacs, float vision, int grudgeId)
        {
            if (grudgeId != Ghost.NO_GRUDGE)
            {
                var grudgeVision = vision * GRUDGE_VISION_SCALE;

                foreach (var pac in pacs)
                    if (pac.Id == grudgeId && pac.Alive && !pac.RepelsGhosts && GetDistanceSquared(ghostPosition, pac.Location) <= grudgeVision * grudgeVision)
                        return pac;
            }

            return PickTarget(ghostPosition, current, pacs, vision);
        }

        /// <summary>
        /// A random pac within vision with no power pill working, for the Confused. Null if none.
        /// </summary>
        public static Player PickRandomTarget(Point2 ghostPosition, IReadOnlyList<Player> pacs, float vision, Random random)
        {
            var visionSquared = vision * vision;
            var count = 0;

            foreach (var pac in pacs)
                if (pac.Alive && !pac.RepelsGhosts && GetDistanceSquared(ghostPosition, pac.Location) <= visionSquared)
                    count++;

            if (count == 0)
                return null;

            var pick = random.Next(count);

            foreach (var pac in pacs)
                if (pac.Alive && !pac.RepelsGhosts && GetDistanceSquared(ghostPosition, pac.Location) <= visionSquared && pick-- == 0)
                    return pac;

            return null;
        }

        /// <summary>
        /// A random cell a ghost can stand in, for the Roamer to wander to: not a wall, spawn tile or warp cell.
        /// Null if the map has none.
        /// </summary>
        public static (int X, int Y)? PickWanderCell(eGridDef[,] map, Random random)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var count = 0;

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    if (IsWanderCell(map, x, y))
                        count++;

            if (count == 0)
                return null;

            var pick = random.Next(count);

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    if (IsWanderCell(map, x, y) && pick-- == 0)
                        return (x, y);

            return null;
        }

        private static bool IsWanderCell(eGridDef[,] map, int x, int y)
        {
            return (map[x, y] & MazeMovement.GHOST_BLOCKED) == 0 && !MazeMovement.IsWarpCell(map, x, y);
        }

        /// <summary>
        /// The Ambusher's goal: the cell up to steps cells on from (x, y) going facing, stopping before anything a ghost
        /// can't enter. (x, y) itself if the way is shut.
        /// </summary>
        public static (int X, int Y) GetAmbushCell(eGridDef[,] map, int x, int y, eFacing facing, int steps)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            for (var i = 0; i < steps && MazeMovement.CanMove(map, x, y, facing, false, MazeMovement.GHOST_BLOCKED); i++)
                (x, y) = MazeMovement.GetNeighbor(x, y, facing, width, height);

            return (x, y);
        }

        /// <summary>
        /// Whether the Ditherer should be heading home: once scattering it keeps going until it is home; otherwise it
        /// starts when its target is within SCATTER_DISTANCE (targetDistanceSquared is squared; pass float.MaxValue with
        /// no target).
        /// </summary>
        public static bool ShouldScatter(bool scattering, float targetDistanceSquared, bool atHome)
        {
            if (scattering)
                return !atHome;

            return targetDistanceSquared <= SCATTER_DISTANCE * SCATTER_DISTANCE;
        }

        /// <summary>
        /// The cell a ghost can stand on that is nearest corner (0 top left, 1 top right, 2 bottom left, 3 bottom right,
        /// taken modulo 4): not a wall, a spawn tile or a warp tunnel.
        /// </summary>
        public static (int X, int Y) FindHomeCorner(eGridDef[,] map, int corner)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            corner = ((corner % 4) + 4) % 4;
            var cornerX = corner % 2 == 0 ? 0 : width - 1;
            var cornerY = corner < 2 ? 0 : height - 1;
            var best = (X: width / 2, Y: height / 2);
            var bestDistance = int.MaxValue;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if ((map[x, y] & MazeMovement.GHOST_BLOCKED) != 0 || MazeMovement.IsWarpCell(map, x, y))
                        continue;

                    var distance = ((x - cornerX) * (x - cornerX)) + ((y - cornerY) * (y - cornerY));

                    if (distance < bestDistance)
                    {
                        best = (x, y);
                        bestDistance = distance;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// True if cell is more than leash cells (straight line) from home.
        /// </summary>
        public static bool IsOutsideLeash((int X, int Y) cell, (int X, int Y) home, int leash)
        {
            var dx = cell.X - home.X;
            var dy = cell.Y - home.Y;
            return (dx * dx) + (dy * dy) > leash * leash;
        }

        /// <summary>
        /// Runs the Sprinter's attention for a tick and says whether it may keep its target: after ATTENTION_MS on one
        /// target it gives up and rests for REST_MS. Clears ChaseMs whenever it has no target.
        /// </summary>
        public static bool UpdateAttention(Ghost ghost, TimeSpan elapsed)
        {
            var elapsedMs = (int)elapsed.TotalMilliseconds;

            if (ghost.RestMs > 0)
            {
                ghost.RestMs -= elapsedMs;
                ghost.ChaseMs = 0;
                return false;
            }

            if (ghost.CurrentTarget == null)
            {
                ghost.ChaseMs = 0;
                return true;
            }

            ghost.ChaseMs += elapsedMs;

            if (ghost.ChaseMs < ATTENTION_MS)
                return true;

            ghost.ChaseMs = 0;
            ghost.RestMs = REST_MS;
            return false;
        }

        /// <summary>
        /// True if a ghost on (x, y) has three or more ways to go.
        /// </summary>
        public static bool IsJunction(eGridDef[,] map, int x, int y)
        {
            var ways = 0;

            foreach (var direction in Enum.GetValues<eFacing>())
                if (MazeMovement.CanMove(map, x, y, direction, false, MazeMovement.GHOST_BLOCKED))
                    ways++;

            return ways >= 3;
        }

        /// <summary>
        /// The Lurker's next waiting place: the nearest junction (by path) at least minSteps from (x, y) that isn't part
        /// of the ghost generator. Null if there is none.
        /// </summary>
        public static (int X, int Y)? FindLurkJunction(eGridDef[,] map, int x, int y, int minSteps)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var distances = new int[width, height];

            for (var cx = 0; cx < width; cx++)
                for (var cy = 0; cy < height; cy++)
                    distances[cx, cy] = UNREACHED;

            var queue = new Queue<(int X, int Y)>();
            distances[x, y] = 0;
            queue.Enqueue((x, y));

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                if (distances[cx, cy] >= minSteps && !map[cx, cy].HasFlag(eGridDef.GhostGenerator) && IsJunction(map, cx, cy))
                    return (cx, cy);

                foreach (var direction in Enum.GetValues<eFacing>())
                {
                    if (!MazeMovement.CanMove(map, cx, cy, direction, false, MazeMovement.GHOST_BLOCKED))
                        continue;

                    var (nx, ny) = MazeMovement.GetNeighbor(cx, cy, direction, width, height);

                    if (distances[nx, ny] != UNREACHED)
                        continue;

                    distances[nx, ny] = distances[cx, cy] + 1;
                    queue.Enqueue((nx, ny));
                }
            }

            return null;
        }

        /// <summary>
        /// The Elroy's speed scale: FRENZY_MIN_SPEED_SCALE to FRENZY_MAX_SPEED_SCALE in a straight line as the leading
        /// team's score goes from 0 to maxScore.
        /// </summary>
        public static float GetFrenzySpeedScale(int leadingScore, int maxScore)
        {
            var progress = maxScore <= 0 ? 1f : Math.Clamp((float)leadingScore / maxScore, 0f, 1f);
            return FRENZY_MIN_SPEED_SCALE + ((FRENZY_MAX_SPEED_SCALE - FRENZY_MIN_SPEED_SCALE) * progress);
        }

        /// <summary>
        /// Adds the SlowThinker's target's cell to its history, stamped nowMs, unless it is the cell already newest there.
        /// </summary>
        public static void RecordTargetCell(Queue<(int Ms, int X, int Y)> history, int nowMs, (int X, int Y) cell)
        {
            var newest = (X: -1, Y: -1);

            foreach (var entry in history)
                newest = (entry.X, entry.Y);

            if (newest != cell)
                history.Enqueue((nowMs, cell.X, cell.Y));
        }

        /// <summary>
        /// The SlowThinker's goal: the newest cell in history (oldest first, stamped in ms) at least lagMs older than
        /// nowMs, or the oldest if none is that old. Drops entries older than that from the front. Null if history is empty.
        /// </summary>
        public static (int X, int Y)? GetLaggedCell(Queue<(int Ms, int X, int Y)> history, int nowMs, int lagMs)
        {
            if (history.Count == 0)
                return null;

            var cutoff = nowMs - lagMs;

            // keep the front entry while the one after it is still old enough to aim at
            while (history.Count > 1)
            {
                using var entries = history.GetEnumerator();
                entries.MoveNext();
                entries.MoveNext();

                if (entries.Current.Ms > cutoff)
                    break;

                history.Dequeue();
            }

            var oldest = history.Peek();
            return (oldest.X, oldest.Y);
        }

        /// <summary>
        /// The StraightLiner's way out of (x, y): no path finding, just the open way (not back, unless it must) whose
        /// next cell is nearest target in a straight line. Wanders like ChooseDirection without a target. Null when on
        /// the target or boxed in.
        /// </summary>
        public static eFacing? ChooseStraightLineDirection(eGridDef[,] map, int x, int y, eFacing facing, (int X, int Y)? target, Random random)
        {
            if (!target.HasValue)
                return ChooseDirection(map, x, y, facing, null, random);

            if (target.Value == (x, y))
                return null;

            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var back = MazeMovement.GetOpposite(facing);
            eFacing? best = null;
            var bestDistance = int.MaxValue;

            foreach (var direction in Enum.GetValues<eFacing>())
            {
                if (direction == back || !MazeMovement.CanMove(map, x, y, direction, false, MazeMovement.GHOST_BLOCKED))
                    continue;

                var (nx, ny) = MazeMovement.GetNeighbor(x, y, direction, width, height);
                var distance = ((nx - target.Value.X) * (nx - target.Value.X)) + ((ny - target.Value.Y) * (ny - target.Value.Y));

                if (distance < bestDistance)
                {
                    best = direction;
                    bestDistance = distance;
                }
            }

            if (best.HasValue)
                return best;

            return MazeMovement.CanMove(map, x, y, back, false, MazeMovement.GHOST_BLOCKED) ? back : null;
        }

        /// <summary>
        /// True if a pac on (pacX, pacY) facing pacFacing looks straight at (ghostX, ghostY), with nothing but open cells
        /// between, within maxSteps cells. The Shy freezes while this is so.
        /// </summary>
        public static bool IsFacedBy(eGridDef[,] map, int pacX, int pacY, eFacing pacFacing, int ghostX, int ghostY, int maxSteps)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var (x, y) = (pacX, pacY);

            for (var i = 0; i <= maxSteps; i++)
            {
                if (x == ghostX && y == ghostY)
                    return true;

                if (!MazeMovement.CanMove(map, x, y, pacFacing, false, eGridDef.Wall))
                    return false;

                (x, y) = MazeMovement.GetNeighbor(x, y, pacFacing, width, height);
            }

            return false;
        }

        /// <summary>
        /// The Copycat's way out of (x, y): the way the pac it copies is facing if it is open, otherwise a wander as in
        /// ChooseDirection.
        /// </summary>
        public static eFacing? GetMimicDirection(eGridDef[,] map, int x, int y, eFacing facing, eFacing pacFacing, Random random)
        {
            if (MazeMovement.CanMove(map, x, y, pacFacing, false, MazeMovement.GHOST_BLOCKED))
                return pacFacing;

            return ChooseDirection(map, x, y, facing, null, random);
        }

        /// <summary>
        /// True while the Sleeper naps: the last NAP_MS of every NAP_CYCLE_MS since it spawned.
        /// </summary>
        public static bool IsNapping(int lifeMs)
        {
            return lifeMs % NAP_CYCLE_MS >= NAP_CYCLE_MS - NAP_MS;
        }

        /// <summary>
        /// The ghost the Sidekick follows: the nearest other living ghost that isn't a Sidekick itself. Null if none.
        /// </summary>
        public static Ghost FindLeader(Ghost me, IEnumerable<Ghost> ghosts)
        {
            Ghost leader = null;
            var leaderDistance = float.MaxValue;

            foreach (var ghost in ghosts)
            {
                if (ghost == me || !ghost.Alive || ghost.Personality.Behavior == eGhostBehavior.Sidekick)
                    continue;

                var distance = GetDistanceSquared(me.Location, ghost.Location);

                if (distance < leaderDistance)
                {
                    leader = ghost;
                    leaderDistance = distance;
                }
            }

            return leader;
        }
    }
}
