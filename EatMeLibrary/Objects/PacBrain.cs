using FastG;
using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// How bot pacs pick which way to go. In order: run from anything that can kill them, take a carried flag home,
    /// rescue their own flag, chase an enemy carrying their flag, grab a dropped enemy flag, grab a nearby power pill
    /// or power up, hunt smaller enemies (and ghosts once big enough), go for the enemy flag (attackers only), eat the
    /// nearest dot, and otherwise wander. Bots press the action button whenever their buff has a use (ShouldUseAction).
    /// A bot keeps the goal it picks for a while (BotMemory) instead of picking again at every cell centre, and once
    /// it has fled from something it keeps fleeing a little longer and then keeps away from where it was.
    /// Now and then a bot wanders instead (RANDOM_TURN_PERCENT), and it sees other pacs' sizes only roughly
    /// (GetPerceivedSize).
    /// </summary>
    public static class PacBrain
    {
        /// <summary>
        /// Steps away at which a bot runs from a ghost or bigger enemy.
        /// </summary>
        public const int DANGER_DISTANCE = 5;

        /// <summary>
        /// Steps away at which a bot chases a smaller enemy, or a ghost it can kill.
        /// </summary>
        public const int HUNT_DISTANCE = 8;

        /// <summary>
        /// Steps away at which a bot chases an enemy carrying its flag.
        /// </summary>
        public const int CARRIER_HUNT_DISTANCE = 20;

        /// <summary>
        /// Steps away at which a bot goes to return its own dropped flag.
        /// </summary>
        public const int RESCUE_DISTANCE = 30;

        /// <summary>
        /// How far a bot looks, in steps. Anything further is only reached through BotWorld.FlagBasePaths.
        /// </summary>
        public const int SEARCH_DISTANCE = 30;

        /// <summary>
        /// Steps away at which a bot without a power pill working goes for one.
        /// </summary>
        public const int POWER_PILL_DISTANCE = 15;

        /// <summary>
        /// Steps away a power up can be for a bot to go after it.
        /// </summary>
        public const int POWER_UP_DISTANCE = 10;

        /// <summary>
        /// A bot only hunts enemies this much smaller than itself, so the chase isn't lost to a few dots.
        /// </summary>
        public const float PREY_SIZE_MARGIN = 1.01f;

        /// <summary>
        /// Percent chance, at each cell centre, that a bot with nothing to run from and no flag to take home wanders
        /// instead of heading for its target, so bots that start together don't all do the same thing.
        /// </summary>
        public const int RANDOM_TURN_PERCENT = 15;

        /// <summary>
        /// How far a bot can misjudge another pac's size, as a share of BotWorld.MaxSize either way.
        /// </summary>
        public const float SIZE_PERCEPTION_ERROR = 0.1f;

        /// <summary>
        /// Once fleeing, a bot keeps fleeing from threats this many steps away (DANGER_DISTANCE starts it), so it
        /// doesn't flip between fleeing and coming back at the edge of DANGER_DISTANCE.
        /// </summary>
        public const int CALM_DISTANCE = DANGER_DISTANCE + 3;

        /// <summary>
        /// A fleeing bot only turns back when that takes it at least this many steps further from its threats than the
        /// best way on, so it doesn't flip back and forth as a chasing ghost re-picks its path.
        /// </summary>
        public const int FLEE_TURN_BACK_MARGIN = 2;

        /// <summary>
        /// The order PickGoal tries goals in, after CarryHome.
        /// </summary>
        private static readonly eBotGoal[] GOAL_ORDER =
        [
            eBotGoal.Rescue,
            eBotGoal.HuntCarrier,
            eBotGoal.LooseFlag,
            eBotGoal.PowerPill,
            eBotGoal.PowerUp,
            eBotGoal.Hunt,
            eBotGoal.HuntGhost,
            eBotGoal.EnemyFlag,
            eBotGoal.Dot,
        ];

        /// <summary>
        /// The order PickGoal tries goals in, after CarryHome, while the bot is invincible (Player.IsInvincible): chase
        /// mode, hunting everything in reach before anything else. It skips power pills and power ups, which would
        /// replace the effect.
        /// </summary>
        private static readonly eBotGoal[] INVINCIBLE_GOAL_ORDER =
        [
            eBotGoal.HuntCarrier,
            eBotGoal.Hunt,
            eBotGoal.HuntGhost,
            eBotGoal.Rescue,
            eBotGoal.LooseFlag,
            eBotGoal.EnemyFlag,
            eBotGoal.Dot,
        ];

        /// <summary>
        /// Which way a bot about to be on the centre of cell (x, y) goes next. paths and threatPaths are scratch
        /// space, reused between calls. memory is what the bot remembers between calls (null for a bot that
        /// remembers nothing): it keeps a goal for a while (BotMemory.IsCommitted) unless the goal is done or a more
        /// urgent one comes up (InterruptsGoal), keeps fleeing until threats are CALM_DISTANCE away and
        /// BotMemory.FLEE_MIN_MS has passed, then keeps out of where they were for a while (BotMemory.AvoidMask).
        /// Outside fleeing it only turns back when its goal changed or its target moves (IsMovingTarget).
        /// randomTurnPercent is the chance it wanders instead of heading for a target (never while fleeing or
        /// carrying a flag). A bot that isn't fleeing takes reactionMs (BotMemory.NoticeThreat) to start fleeing from
        /// a threat it has just seen, carrying on as if it weren't there until then. Null when it has nowhere better
        /// to go than straight on.
        /// </summary>
        public static eFacing? ChooseDirection(BotWorld world, Player me, int x, int y, PathMap paths, PathMap threatPaths, Random random,
            BotMemory memory = null, int randomTurnPercent = RANDOM_TURN_PERCENT, int reactionMs = BotMemory.REACTION_MS)
        {
            memory ??= new BotMemory();

            var blocked = GetBlocked(me);
            var full = IsFull(me.Size, world.MaxSize);
            paths.Fill(world.Map, x, y, SEARCH_DISTANCE, blocked);

            var threats = FindThreats(world, me, paths, memory.IsFleeing ? CALM_DISTANCE : DANGER_DISTANCE);
            if (threats.Count == 0 && memory.IsFleeing && memory.FleeMsLeft > 0f)
                threats.AddRange(memory.ThreatCells);

            if (threats.Count == 0)
                memory.ForgetThreat();
            else if (!memory.IsFleeing)
            {
                memory.NoticeThreat(reactionMs);

                if (!memory.HasReacted)
                    threats.Clear();
            }

            if (threats.Count > 0)
            {
                memory.Flee(threats);
                return ChooseFleeDirection(world.Map, x, y, me.Facing, threats, threatPaths, blocked);
            }

            if (memory.IsFleeing)
                memory.StopFleeing(world.Map.GetLength(0), world.Map.GetLength(1));

            if (!me.HasFlag && random.Next(100) < randomTurnPercent)
                return ChooseWanderDirection(world.Map, x, y, me.Facing, random, full, blocked);

            // paths for finding things to go to keep out of where the threats were; spotting threats never does
            if (memory.IsAvoiding)
                paths.Fill(world.Map, x, y, SEARCH_DISTANCE, blocked, memory.AvoidMask);

            // only a kept goal's target can be flip-flopped between; a fresh pick may turn back
            var goalChanged = false;
            var target = memory.IsCommitted && !InterruptsGoal(world, me, x, y, paths, memory.Goal)
                ? FindGoalTarget(world, me, x, y, paths, memory.Goal)
                : null;

            if (!target.HasValue)
            {
                var goal = PickGoal(world, me, x, y, paths, out target);
                goalChanged = true;

                if (goal == eBotGoal.None)
                    memory.ClearGoal();
                else
                    memory.SetGoal(goal, random);
            }

            if (target.HasValue)
            {
                var step = GetStepToward(world, x, y, paths, target.Value, blocked);

                // a fixed target doesn't jump behind the bot, so turning back toward it means flip-flopping
                if (step.HasValue && step == MazeMovement.GetOpposite(me.Facing) && !goalChanged && !IsMovingTarget(memory.Goal))
                    return ChooseWanderDirection(world.Map, x, y, me.Facing, random, full, blocked);

                if (step.HasValue)
                    return step;
            }

            memory.ClearGoal();
            return ChooseWanderDirection(world.Map, x, y, me.Facing, random, full, blocked);
        }

        /// <summary>
        /// The goal a bot on cell (x, y) should go for now, highest priority first (eBotGoal), with its target cell.
        /// A bot carrying a flag only ever takes it home. None (and a null target) if there's nothing worth going
        /// to. paths must be filled from (x, y).
        /// </summary>
        public static eBotGoal PickGoal(BotWorld world, Player me, int x, int y, PathMap paths, out (int X, int Y)? target)
        {
            if (me.HasFlag)
            {
                target = FindGoalTarget(world, me, x, y, paths, eBotGoal.CarryHome);
                return target.HasValue ? eBotGoal.CarryHome : eBotGoal.None;
            }

            foreach (var goal in me.IsInvincible ? INVINCIBLE_GOAL_ORDER : GOAL_ORDER)
            {
                target = FindGoalTarget(world, me, x, y, paths, goal);

                if (target.HasValue)
                    return goal;
            }

            target = null;
            return eBotGoal.None;
        }

        /// <summary>
        /// The cell a bot on cell (x, y) heads for to work on goal, or null if the goal is done or can't be had
        /// right now. paths must be filled from (x, y). Targets beyond SEARCH_DISTANCE are always flag base cells.
        /// </summary>
        public static (int X, int Y)? FindGoalTarget(BotWorld world, Player me, int x, int y, PathMap paths, eBotGoal goal)
        {
            var ghostKiller = CanKillGhosts(world, me);

            // in chase mode (invincible) it hunts out to everything it can see
            var huntDistance = me.IsInvincible ? SEARCH_DISTANCE : HUNT_DISTANCE;
            var carrierHuntDistance = me.IsInvincible ? SEARCH_DISTANCE : CARRIER_HUNT_DISTANCE;

            switch (goal)
            {
                case eBotGoal.CarryHome:
                    if (!me.HasFlag)
                        return null;

                    // a capture needs our flag at base; if it isn't, get it back when it's close, otherwise just go home
                    var ownAway = !HasFlagAtBase(world, me.Team);
                    var near = ownAway ? FindNearestFlag(world, paths, f => f.Team == me.Team, SEARCH_DISTANCE) : null;
                    return near ?? FindNearestBase(world, x, y, f => f.Team == me.Team && (ownAway || f.IsAtBase));

                case eBotGoal.Rescue:
                    return FindNearestFlag(world, paths, f => f.Team == me.Team && !f.IsAtBase && !f.IsCarried, RESCUE_DISTANCE);

                case eBotGoal.HuntCarrier:
                    return FindNearestPac(world, me, paths, carrierHuntDistance, true);

                case eBotGoal.LooseFlag:
                    // any bot grabs a dropped enemy flag it can see, if it is free to pick one up
                    return PowerUpEffects.CanPickUpFlag(me.PowerUpEffect)
                        ? FindNearestFlag(world, paths, f => f.CanBePickedUpBy(me.Team) && !f.IsAtBase, SEARCH_DISTANCE)
                        : null;

                case eBotGoal.PowerUp:
                    return !me.IsInvincible ? FindNearestPowerUp(world, paths, POWER_UP_DISTANCE) : null;

                case eBotGoal.Hunt:
                    return FindNearestPac(world, me, paths, huntDistance, false);

                case eBotGoal.HuntGhost:
                    return ghostKiller ? FindNearestGhost(world, paths, huntDistance) : null;

                case eBotGoal.PowerPill:
                    return !ghostKiller && !me.IsPowered ? FindNearestCell(world.Map, paths, eGridDef.PowerPill, POWER_PILL_DISTANCE) : null;

                case eBotGoal.EnemyFlag:
                    if (!IsAttacker(me))
                        return null;

                    // a loose enemy flag nearby, otherwise the enemy base, where the flag usually is; a flag sealed in a
                    // force field (triforce) can't be taken, so it is left alone
                    return FindNearestFlag(world, paths, f => f.CanBePickedUpBy(me.Team), SEARCH_DISTANCE)
                        ?? FindNearestBase(world, x, y, f => f.Team != me.Team && f.IsAtBase && !f.IsSealed);

                case eBotGoal.Dot:
                    // an invincible bot leaves power pills alone, since one would replace its effect
                    var food = me.IsInvincible ? eGridDef.Food : eGridDef.Food | eGridDef.PowerPill;
                    return !IsFull(me.Size, world.MaxSize) ? FindNearestCell(world.Map, paths, food, SEARCH_DISTANCE) : null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// True if something more urgent than goal has come up, so a bot drops it at once rather than waiting for its
        /// time to run out: it picked up a flag (or lost the one it was taking home), its own flag needs rescuing, or an
        /// enemy flag it can pick up has been dropped nearby.
        /// </summary>
        public static bool InterruptsGoal(BotWorld world, Player me, int x, int y, PathMap paths, eBotGoal goal)
        {
            if (me.HasFlag != (goal == eBotGoal.CarryHome))
                return true;

            // in chase mode a rescue waits its turn (INVINCIBLE_GOAL_ORDER)
            if (me.IsInvincible)
                return false;

            if (goal is eBotGoal.CarryHome or eBotGoal.Rescue)
                return false;

            if (FindGoalTarget(world, me, x, y, paths, eBotGoal.Rescue).HasValue)
                return true;

            return goal != eBotGoal.LooseFlag && goal != eBotGoal.HuntCarrier && FindGoalTarget(world, me, x, y, paths, eBotGoal.LooseFlag).HasValue;
        }

        /// <summary>
        /// True for goals whose target can move behind the bot (pacs, ghosts, power ups, a carried flag), where
        /// turning back is the right thing to do.
        /// </summary>
        public static bool IsMovingTarget(eBotGoal goal)
        {
            return goal is eBotGoal.CarryHome or eBotGoal.HuntCarrier or eBotGoal.PowerUp or eBotGoal.Hunt or eBotGoal.HuntGhost;
        }

        /// <summary>
        /// The cells a bot keeps out of: walls and the enemy spawn room (MazeMovement.GetPacBlocked), and its own spawn
        /// room too while carrying a flag, since taking a flag onto a spawn tile drops it.
        /// </summary>
        public static eGridDef GetBlocked(Player me)
        {
            var blocked = MazeMovement.GetPacBlocked(me.Team);
            return me.HasFlag ? blocked | eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue : blocked;
        }

        /// <summary>
        /// The cell a bot on cell (x, y) heads for, or null if there's nothing worth going to. paths must be filled
        /// from (x, y). Targets beyond SEARCH_DISTANCE are always flag base cells.
        /// </summary>
        public static (int X, int Y)? PickTarget(BotWorld world, Player me, int x, int y, PathMap paths)
        {
            PickGoal(world, me, x, y, paths, out var target);
            return target;
        }

        /// <summary>
        /// First step from (x, y) toward target: along paths when the search reached it, otherwise downhill on the
        /// target's FlagBasePaths map. Null if neither reaches it.
        /// </summary>
        public static eFacing? GetStepToward(BotWorld world, int x, int y, PathMap paths, (int X, int Y) target, eGridDef blocked = eGridDef.Wall)
        {
            if (paths.GetDistance(target.X, target.Y) != PathMap.UNREACHED)
                return paths.GetFirstStep(target.X, target.Y);

            if (world.FlagBasePaths.TryGetValue(target, out var basePaths))
                return ChooseDownhill(world.Map, x, y, basePaths, blocked);

            return null;
        }

        /// <summary>
        /// The way from (x, y) to the open neighbour nearest the start of a full PathMap. Null if none is nearer.
        /// </summary>
        public static eFacing? ChooseDownhill(eGridDef[,] map, int x, int y, PathMap paths, eGridDef blocked = eGridDef.Wall)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            eFacing? best = null;
            var bestDistance = paths.GetDistance(x, y);

            foreach (var direction in Enum.GetValues<eFacing>())
            {
                if (!MazeMovement.CanMove(map, x, y, direction, true, blocked))
                    continue;

                var (nx, ny) = MazeMovement.GetNeighbor(x, y, direction, width, height);
                var distance = paths.GetDistance(nx, ny);

                if (distance != PathMap.UNREACHED && (bestDistance == PathMap.UNREACHED || distance < bestDistance))
                {
                    best = direction;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// True if other is an enemy that can kill me: it is invincible, or looks big enough to eat me
        /// (GetPerceivedSize). Nothing is a threat to an invincible bot, and a stunned pac is no threat to anyone.
        /// </summary>
        public static bool IsThreat(BotWorld world, Player me, Player other)
        {
            // pacs passing through each other (Raccoon Tail) can't fight
            if (!other.Alive || other.IsStunned || other.Team == me.Team || me.IsInvincible || me.PassesThrough || other.PassesThrough)
                return false;

            return other.IsInvincible || GetPerceivedSize(world, me, other) > me.Size;
        }

        /// <summary>
        /// True if other is an enemy I can kill and should hunt: any enemy that isn't invincible when I am, otherwise
        /// one that looks small enough (GetPerceivedSize). An invincible enemy is never prey.
        /// </summary>
        public static bool IsPrey(BotWorld world, Player me, Player other)
        {
            if (!other.Alive || other.Team == me.Team || other.IsInvincible || me.PassesThrough || other.PassesThrough)
                return false;

            return me.IsInvincible || me.Size > GetPerceivedSize(world, me, other) * PREY_SIZE_MARGIN;
        }

        /// <summary>
        /// True if touching a ghost kills the ghost rather than me: a power pill at full size (PacRules.CanKillGhost)
        /// or Invincible.
        /// </summary>
        public static bool CanKillGhosts(BotWorld world, Player me)
        {
            return me.IsInvincible || PacRules.CanKillGhost(me.Size, me.IsPowered, world.MaxSize);
        }

        /// <summary>
        /// How big other looks to me: its size give or take up to SIZE_PERCEPTION_ERROR of world.MaxSize. The error
        /// is added rather than scaled so it still counts at size 0, so it can come out below 0. Each bot misjudges
        /// each pac by the same amount for as long as world.PerceptionSeed stays the same (one level).
        /// </summary>
        public static float GetPerceivedSize(BotWorld world, Player me, Player other)
        {
            return other.Size + GetPerceptionError(world.PerceptionSeed, me.Id, other.Id) * SIZE_PERCEPTION_ERROR * world.MaxSize;
        }

        /// <summary>
        /// A number from -1 to 1, always the same for the same seed and pair of player ids, different between pairs.
        /// </summary>
        public static float GetPerceptionError(int seed, int observerId, int otherId)
        {
            var hash = (uint)seed * 0x9E3779B1u ^ (uint)observerId * 0x85EBCA77u ^ (uint)otherId * 0xC2B2AE3Du;

            // murmur3's finalizer, so neighbouring ids give unrelated errors
            hash ^= hash >> 16;
            hash *= 0x85EBCA6Bu;
            hash ^= hash >> 13;
            hash *= 0xC2B2AE35u;
            hash ^= hash >> 16;

            return (float)(hash / (double)uint.MaxValue * 2.0 - 1.0);
        }

        /// <summary>
        /// Bots with even ids go after the enemy flag; the rest stay to eat and hunt.
        /// </summary>
        public static bool IsAttacker(Player me)
        {
            return me.Id % 2 == 0;
        }

        /// <summary>
        /// True when dots can't make a pac any bigger, so it stops going after them.
        /// </summary>
        public static bool IsFull(float size, float maxSize)
        {
            return size >= maxSize;
        }

        /// <summary>
        /// Cells of the ghosts and bigger enemies within maxDistance steps; stunned ones can't hurt it, so they don't
        /// count. paths must be filled from the bot.
        /// </summary>
        public static List<(int X, int Y)> FindThreats(BotWorld world, Player me, PathMap paths, int maxDistance = DANGER_DISTANCE)
        {
            var threats = new List<(int X, int Y)>();

            // a bot passing through ghosts (Raccoon Tail) has nothing to fear from them
            if (!CanKillGhosts(world, me) && !me.PassesThrough)
                foreach (var ghost in world.Ghosts)
                    if (ghost.Alive && !ghost.IsStunned)
                        AddIfClose(threats, paths, MazeMovement.GetCell(ghost.Location), maxDistance);

            foreach (var pac in world.Pacs)
                if (IsThreat(world, me, pac))
                    AddIfClose(threats, paths, MazeMovement.GetCell(pac.Location), maxDistance);

            // other pacs' holy flames kill anything that isn't invincible; they don't move, so only the next steps matter
            if (!me.IsInvincible)
                foreach (var effect in world.WeaponEffects)
                    if (effect.Kind == eWeaponEffect.HolyFlame && effect.OwnerId != me.Id)
                        AddIfClose(threats, paths, MazeMovement.GetCell(effect.Location), Math.Min(maxDistance, FLAME_DANGER_DISTANCE));

            return threats;
        }

        /// <summary>
        /// Steps away at which a bot steers clear of a holy flame.
        /// </summary>
        public const int FLAME_DANGER_DISTANCE = 2;

        /// <summary>
        /// True if a bot should press the action button now for its buff:
        /// - Sword Swing, Fire Flower and BFG: the nearest pac or ghost in line ahead of it, within
        ///   WeaponRules.FIRE_DISTANCE and before any wall, is an enemy pac that isn't invincible or a ghost (a
        ///   teammate in the way holds its fire). It fires the BFG even when the burst will catch it too.
        /// - Gotcha: whenever any ghost is alive, since poke balls find ghosts by themselves.
        /// - POW: a ghost or an enemy pac that isn't invincible, neither stunned already, is within the standard effect
        ///   radius (PowerUpEffects.EFFECT_RADIUS).
        /// - Bomb: its bomb isn't dropped yet and a ghost or enemy pac that isn't stunned is within the standard radius.
        /// - Banana Peel and Block Drop: a ghost or enemy pac that isn't stunned is within DANGER_DISTANCE cells.
        /// - Warp: something that can kill it (a ghost, or an enemy that IsThreat) is within DANGER_DISTANCE cells.
        /// </summary>
        public static bool ShouldUseAction(BotWorld world, Player me)
        {
            switch (me.PowerUpEffect)
            {
                case ePowerUpEffect.Gotcha:
                    foreach (var ghost in world.Ghosts)
                        if (ghost.Alive)
                            return true;

                    return false;

                case ePowerUpEffect.SwordSwing:
                case ePowerUpEffect.FireFlower:
                case ePowerUpEffect.Bfg:
                    return HasTargetAhead(world, me);

                case ePowerUpEffect.Pow:
                    return HasTargetNear(world, me, PowerUpEffects.EFFECT_RADIUS, true);

                case ePowerUpEffect.Bomb:
                    return !me.BombDropped && HasTargetNear(world, me, PowerUpEffects.EFFECT_RADIUS, false);

                case ePowerUpEffect.BananaPeel:
                case ePowerUpEffect.BlockDrop:
                    return HasTargetNear(world, me, DANGER_DISTANCE, false);

                case ePowerUpEffect.Warp:
                    return HasThreatNear(world, me, DANGER_DISTANCE);

                default:
                    return false;
            }
        }

        /// <summary>
        /// True if the nearest pac or ghost in line ahead of me, within WeaponRules.FIRE_DISTANCE and before any wall,
        /// is an enemy pac that isn't invincible or a ghost.
        /// </summary>
        private static bool HasTargetAhead(BotWorld world, Player me)
        {
            var width = world.Map.GetLength(0);
            var height = world.Map.GetLength(1);
            var (x, y) = MazeMovement.GetCell(me.Location);
            var clear = WeaponRules.GetClearCells(world.Map, x, y, me.Facing, WeaponRules.FIRE_DISTANCE);

            for (var cells = 1; cells <= clear; cells++)
            {
                foreach (var pac in world.Pacs)
                    if (pac != me && pac.Alive && WeaponRules.IsInLineAhead(x, y, me.Facing, cells, pac.Location, width, height)
                        && !WeaponRules.IsInLineAhead(x, y, me.Facing, cells - 1, pac.Location, width, height))
                        return pac.Team != me.Team && !pac.IsInvincible;

                foreach (var ghost in world.Ghosts)
                    if (ghost.Alive && WeaponRules.IsInLineAhead(x, y, me.Facing, cells, ghost.Location, width, height)
                        && !WeaponRules.IsInLineAhead(x, y, me.Facing, cells - 1, ghost.Location, width, height))
                        return true;
            }

            return false;
        }

        /// <summary>
        /// True if a living ghost or enemy pac that isn't stunned is no more than cells away from me horizontally and
        /// vertically. With skipInvincible, invincible enemies don't count.
        /// </summary>
        private static bool HasTargetNear(BotWorld world, Player me, float cells, bool skipInvincible)
        {
            foreach (var ghost in world.Ghosts)
                if (ghost.Alive && !ghost.IsStunned && IsWithin(me.Location, ghost.Location, cells))
                    return true;

            foreach (var pac in world.Pacs)
                if (pac.Alive && pac.Team != me.Team && !pac.IsStunned && !(skipInvincible && pac.IsInvincible) && IsWithin(me.Location, pac.Location, cells))
                    return true;

            return false;
        }

        /// <summary>
        /// True if something that can kill me (a ghost I can't kill or pass through, or an enemy that IsThreat) is no
        /// more than cells away horizontally and vertically.
        /// </summary>
        private static bool HasThreatNear(BotWorld world, Player me, float cells)
        {
            if (!CanKillGhosts(world, me) && !me.PassesThrough)
                foreach (var ghost in world.Ghosts)
                    if (ghost.Alive && !ghost.IsStunned && IsWithin(me.Location, ghost.Location, cells))
                        return true;

            foreach (var pac in world.Pacs)
                if (IsThreat(world, me, pac) && IsWithin(me.Location, pac.Location, cells))
                    return true;

            return false;
        }

        /// <summary>
        /// True if to is no more than cells away from from, horizontally and vertically.
        /// </summary>
        public static bool IsWithin(Point2 from, Point2 to, float cells)
        {
            return MathF.Abs(to.X - from.X) <= cells && MathF.Abs(to.Y - from.Y) <= cells;
        }

        /// <summary>
        /// Which way a bot on the centre of cell (x, y), heading facing, runs from threats in the given cells: toward
        /// the open neighbour furthest from all of them by path, straight on when that ties. It turns back only when
        /// there is no other way or turning back is FLEE_TURN_BACK_MARGIN steps further away than the best way on.
        /// Null if it is boxed in.
        /// </summary>
        public static eFacing? ChooseFleeDirection(eGridDef[,] map, int x, int y, eFacing facing, List<(int X, int Y)> threats, PathMap threatPaths, eGridDef blocked = eGridDef.Wall)
        {
            // the bot's neighbours are at most CALM_DISTANCE + 1 from a threat; anything further is far enough
            threatPaths.Fill(map, threats, CALM_DISTANCE + 2);

            var back = MazeMovement.GetOpposite(facing);
            eFacing? best = null;
            var bestDistance = -1;

            foreach (var direction in Enum.GetValues<eFacing>())
            {
                if (direction == back)
                    continue;

                var distance = GetFleeDistance(map, x, y, direction, threatPaths, blocked);

                if (distance > bestDistance || (distance == bestDistance && direction == facing))
                {
                    best = direction;
                    bestDistance = distance;
                }
            }

            var backDistance = GetFleeDistance(map, x, y, back, threatPaths, blocked);

            if (backDistance >= 0 && (!best.HasValue || backDistance >= bestDistance + FLEE_TURN_BACK_MARGIN))
                return back;

            return best;
        }

        /// <summary>
        /// Steps from the threats to the neighbour of (x, y) the given way: CALM_DISTANCE + 2 for one no threat
        /// reaches (as far away as it gets), -1 if the bot can't go that way.
        /// </summary>
        private static int GetFleeDistance(eGridDef[,] map, int x, int y, eFacing direction, PathMap threatPaths, eGridDef blocked)
        {
            if (!MazeMovement.CanMove(map, x, y, direction, true, blocked))
                return -1;

            var (nx, ny) = MazeMovement.GetNeighbor(x, y, direction, map.GetLength(0), map.GetLength(1));
            var distance = threatPaths.GetDistance(nx, ny);

            return distance == PathMap.UNREACHED ? CALM_DISTANCE + 2 : distance;
        }

        /// <summary>
        /// True if a bot between cell centres is heading straight at a threat within DANGER_DISTANCE along its row or
        /// column with no wall between them, so it should turn back now rather than wait for the next centre.
        /// </summary>
        public static bool ShouldTurnBack(BotWorld world, Player me)
        {
            var (x, y) = MazeMovement.GetCell(me.Location);
            var clear = WeaponRules.GetClearCells(world.Map, x, y, me.Facing, DANGER_DISTANCE);

            if (!CanKillGhosts(world, me) && !me.PassesThrough)
                foreach (var ghost in world.Ghosts)
                    if (ghost.Alive && !ghost.IsStunned && IsAhead(world, me, x, y, clear, ghost.Location))
                        return true;

            foreach (var pac in world.Pacs)
                if (IsThreat(world, me, pac) && IsAhead(world, me, x, y, clear, pac.Location))
                    return true;

            return false;
        }

        /// <summary>
        /// Which way a bot on the centre of cell (x, y), heading facing, goes next: a random open way, never turning
        /// back unless it has to. With avoidFood it picks an empty cell over a dot when it can. Bots can use the warp
        /// tunnels. Null when it is boxed in.
        /// </summary>
        public static eFacing? ChooseWanderDirection(eGridDef[,] map, int x, int y, eFacing facing, Random random, bool avoidFood = false, eGridDef blocked = eGridDef.Wall)
        {
            var options = new List<eFacing>(4);
            var empty = new List<eFacing>(4);
            var back = MazeMovement.GetOpposite(facing);
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            foreach (var direction in Enum.GetValues<eFacing>())
            {
                if (direction == back || !MazeMovement.CanMove(map, x, y, direction, true, blocked))
                    continue;

                options.Add(direction);

                var (nx, ny) = MazeMovement.GetNeighbor(x, y, direction, width, height);
                if ((map[nx, ny] & (eGridDef.Food | eGridDef.PowerPill)) == 0)
                    empty.Add(direction);
            }

            if (avoidFood && empty.Count > 0)
                return empty[random.Next(empty.Count)];

            if (options.Count > 0)
                return options[random.Next(options.Count)];

            return MazeMovement.CanMove(map, x, y, back, true, blocked) ? back : null;
        }

        /// <summary>
        /// True if other is in me's own cell ahead of it, or in one of the clear cells ahead of cell (x, y).
        /// </summary>
        private static bool IsAhead(BotWorld world, Player me, int x, int y, int clear, Point2 other)
        {
            if (MazeMovement.GetCell(other) == (x, y))
                return MazeMovement.IsHeadingToward(me.Location, me.Facing, other);

            return WeaponRules.IsInLineAhead(x, y, me.Facing, clear, other, world.Map.GetLength(0), world.Map.GetLength(1));
        }

        private static void AddIfClose(List<(int X, int Y)> cells, PathMap paths, (int X, int Y) cell, int maxDistance)
        {
            var distance = paths.GetDistance(cell.X, cell.Y);

            if (distance != PathMap.UNREACHED && distance <= maxDistance)
                cells.Add(cell);
        }

        /// <summary>
        /// Nearest flag the filter wants within maxDistance steps. A carried flag is where its carrier is.
        /// </summary>
        private static (int X, int Y)? FindNearestFlag(BotWorld world, PathMap paths, Func<GameFlag, bool> wanted, int maxDistance)
        {
            (int X, int Y)? best = null;
            var bestDistance = int.MaxValue;

            foreach (var flag in world.Flags)
            {
                if (!wanted(flag))
                    continue;

                var cell = MazeMovement.GetCell(new Point2(flag.X, flag.Y));
                var distance = paths.GetDistance(cell.X, cell.Y);

                if (distance != PathMap.UNREACHED && distance <= maxDistance && distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Base cell of the wanted flag whose base is nearest (x, y), measured on FlagBasePaths. Null if none.
        /// </summary>
        private static (int X, int Y)? FindNearestBase(BotWorld world, int x, int y, Func<GameFlag, bool> wanted)
        {
            (int X, int Y)? best = null;
            var bestDistance = int.MaxValue;

            foreach (var flag in world.Flags)
            {
                if (!wanted(flag))
                    continue;

                var cell = MazeMovement.GetCell(new Point2(flag.BaseX, flag.BaseY));

                if (!world.FlagBasePaths.TryGetValue(cell, out var basePaths))
                    continue;

                var distance = basePaths.GetDistance(x, y);

                if (distance != PathMap.UNREACHED && distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool HasFlagAtBase(BotWorld world, eTeam team)
        {
            foreach (var flag in world.Flags)
                if (flag.Team == team && flag.IsAtBase)
                    return true;

            return false;
        }

        /// <summary>
        /// Nearest prey within maxDistance steps; with carriersOnly, only enemies carrying a flag.
        /// </summary>
        private static (int X, int Y)? FindNearestPac(BotWorld world, Player me, PathMap paths, int maxDistance, bool carriersOnly)
        {
            (int X, int Y)? best = null;
            var bestDistance = int.MaxValue;

            foreach (var pac in world.Pacs)
            {
                if (!IsPrey(world, me, pac) || (carriersOnly && !pac.HasFlag))
                    continue;

                var cell = MazeMovement.GetCell(pac.Location);
                var distance = paths.GetDistance(cell.X, cell.Y);

                if (distance != PathMap.UNREACHED && distance <= maxDistance && distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static (int X, int Y)? FindNearestGhost(BotWorld world, PathMap paths, int maxDistance)
        {
            (int X, int Y)? best = null;
            var bestDistance = int.MaxValue;

            foreach (var ghost in world.Ghosts)
            {
                if (!ghost.Alive)
                    continue;

                var cell = MazeMovement.GetCell(ghost.Location);
                var distance = paths.GetDistance(cell.X, cell.Y);

                if (distance != PathMap.UNREACHED && distance <= maxDistance && distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static (int X, int Y)? FindNearestPowerUp(BotWorld world, PathMap paths, int maxDistance)
        {
            (int X, int Y)? best = null;
            var bestDistance = int.MaxValue;

            foreach (var powerUp in world.PowerUps)
            {
                var cell = MazeMovement.GetCell(powerUp.Location);
                var distance = paths.GetDistance(cell.X, cell.Y);

                if (distance != PathMap.UNREACHED && distance <= maxDistance && distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Nearest cell, other than the bot's own, holding any of the given items, within maxDistance steps.
        /// </summary>
        private static (int X, int Y)? FindNearestCell(eGridDef[,] map, PathMap paths, eGridDef items, int maxDistance)
        {
            foreach (var (x, y) in paths.Order)
            {
                var distance = paths.GetDistance(x, y);

                if (distance > maxDistance)
                    break;

                if (distance > 0 && (map[x, y] & items) != 0)
                    return (x, y);
            }

            return null;
        }
    }
}
