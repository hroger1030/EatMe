using EatMeLibrary;
using FastG;

namespace EatMeTests
{
    public class PacBrainTests
    {
        private const float MAX_SIZE = 100f;

        /// <summary>
        /// One corridor, cells (1, 1) to (7, 1).
        /// </summary>
        private static readonly string[] CORRIDOR =
        [
            "#########",
            "#.......#",
            "#########",
        ];

        /// <summary>
        /// Builds a map like MazeMovementTests.MakeMap, with 'o' for a dot and 'O' for a power pill.
        /// </summary>
        private static eGridDef[,] MakeMap(string[] rows)
        {
            var map = MazeMovementTests.MakeMap(rows);

            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == 'o')
                        map[x, y] = eGridDef.Food;
                    else if (rows[y][x] == 'O')
                        map[x, y] = eGridDef.PowerPill;
                }
            }

            return map;
        }

        private static Player MakePac(int id, eTeam team, int x, int y, float size = PacRules.START_SIZE, eFacing facing = eFacing.Right)
        {
            return new Player { Id = id, Team = team, Location = new Point2(x + 0.5f, y + 0.5f), Size = size, Facing = facing, Alive = true };
        }

        private static Ghost MakeGhost(int x, int y)
        {
            return new Ghost { Location = new Point2(x + 0.5f, y + 0.5f), Alive = true };
        }

        private static BotWorld MakeWorld(eGridDef[,] map, List<Player> pacs, List<Ghost> ghosts = null, List<GameFlag> flags = null)
        {
            var basePaths = new Dictionary<(int X, int Y), PathMap>();

            foreach (var flag in flags ?? [])
            {
                var cell = MazeMovement.GetCell(new Point2(flag.BaseX, flag.BaseY));
                var paths = new PathMap();
                paths.Fill(map, cell.X, cell.Y);
                basePaths[cell] = paths;
            }

            return new BotWorld { Map = map, MaxSize = MAX_SIZE, Pacs = pacs, Ghosts = ghosts ?? [], Flags = flags ?? [], FlagBasePaths = basePaths };
        }

        /// <summary>
        /// ChooseDirection with no random turns, so the bot always does what its priorities say.
        /// </summary>
        private static eFacing? Choose(BotWorld world, Player me, Random random = null, int randomTurnPercent = 0)
        {
            var (x, y) = MazeMovement.GetCell(me.Location);
            return PacBrain.ChooseDirection(world, me, x, y, new PathMap(), new PathMap(), random ?? new Random(1), randomTurnPercent: randomTurnPercent, reactionMs: 0);
        }

        [Test]
        public void ChooseDirection_GoesForANearbyPowerUp_OverDots()
        {
            var map = MakeMap(["#########", "#......o#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1);
            var world = MakeWorld(map, [me]);

            Assert.That(Choose(world, me), Is.EqualTo(eFacing.Right));

            world.PowerUps = [new PowerUp { Item = ePowerUp.Cherry, Location = new Point2(1.5f, 1.5f), Alive = true }];

            Assert.That(Choose(world, me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void PathMap_DistancesFirstStepsAndOrder()
        {
            var paths = new PathMap();

            paths.Fill(MakeMap(CORRIDOR), 3, 1);

            Assert.That(paths.GetDistance(3, 1), Is.Zero);
            Assert.That(paths.GetDistance(7, 1), Is.EqualTo(4));
            Assert.That(paths.GetDistance(1, 1), Is.EqualTo(2));
            Assert.That(paths.GetDistance(0, 0), Is.EqualTo(PathMap.UNREACHED));
            Assert.That(paths.GetFirstStep(7, 1), Is.EqualTo(eFacing.Right));
            Assert.That(paths.GetFirstStep(1, 1), Is.EqualTo(eFacing.Left));
            Assert.That(paths.GetFirstStep(3, 1), Is.Null);
            Assert.That(paths.Order[0], Is.EqualTo((3, 1)));
            Assert.That(paths.Order, Has.Count.EqualTo(7));
        }

        [Test]
        public void PathMap_UsesWarpTunnels()
        {
            var paths = new PathMap();

            paths.Fill(MakeMap(["#####", ".....", "#####"]), 0, 1);

            Assert.That(paths.GetDistance(4, 1), Is.EqualTo(1));
            Assert.That(paths.GetFirstStep(4, 1), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void PathMap_SeveralStarts_DistanceToNearest()
        {
            var paths = new PathMap();

            paths.Fill(MakeMap(CORRIDOR), [(1, 1), (7, 1)]);

            Assert.That(paths.GetDistance(4, 1), Is.EqualTo(3));
            Assert.That(paths.GetDistance(6, 1), Is.EqualTo(1));
        }

        [Test]
        public void GoesForNearestDot()
        {
            var map = MakeMap(["#########", "#o.....o#", "#########"]);
            var me = MakePac(1, eTeam.Red, 3, 1);

            Assert.That(Choose(MakeWorld(map, [me]), me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void Full_StopsEatingDots_ButStillTakesPowerPill()
        {
            var dots = MakeMap(["#########", "#o.....o#", "#########"]);
            var me = MakePac(1, eTeam.Red, 3, 1, MAX_SIZE);

            Assert.That(PacBrain.PickTarget(MakeWorld(dots, [me]), me, 3, 1, Fill(dots, me)), Is.Null);

            var pills = MakeMap(["#########", "#O.....O#", "#########"]);
            Assert.That(PacBrain.PickTarget(MakeWorld(pills, [me]), me, 3, 1, Fill(pills, me)), Is.EqualTo((1, 1)));

            me.PowerUpEffect = ePowerUp.PowerPill;
            me.PowerUpMs = 1000;
            Assert.That(PacBrain.PickTarget(MakeWorld(pills, [me]), me, 3, 1, Fill(pills, me)), Is.Null, "already powered");
        }

        [Test]
        public void RunsFromGhost()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 4, 1);

            Assert.That(Choose(MakeWorld(map, [me], [MakeGhost(6, 1)]), me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void GhostKiller_ChasesGhost()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 4, 1, MAX_SIZE, eFacing.Left);
            me.PowerUpEffect = ePowerUp.PowerPill;
            me.PowerUpMs = 1000;

            Assert.That(Choose(MakeWorld(map, [me], [MakeGhost(6, 1)]), me), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void RunsFromBiggerEnemy_ChasesSmallerOne()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 4, 1, 50f);
            var big = MakePac(2, eTeam.Blue, 6, 1, 80f);
            var small = MakePac(3, eTeam.Blue, 6, 1, 20f);

            Assert.That(Choose(MakeWorld(map, [me, big]), me), Is.EqualTo(eFacing.Left));
            Assert.That(Choose(MakeWorld(map, [me, small]), me), Is.EqualTo(eFacing.Right));
        }

        private static void MakeInvincible(Player pac)
        {
            pac.PowerUpEffect = ePowerUp.Starman;
            pac.PowerUpMs = 1000;
        }

        [Test]
        public void Invincible_ChasesBiggerEnemyAndGhosts()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 4, 1, 10f, eFacing.Left);
            MakeInvincible(me);

            Assert.That(Choose(MakeWorld(map, [me, MakePac(2, eTeam.Blue, 6, 1, 90f)]), me), Is.EqualTo(eFacing.Right), "bigger enemy is prey");
            Assert.That(Choose(MakeWorld(map, [me], [MakeGhost(6, 1)]), me), Is.EqualTo(eFacing.Right), "ghost is prey, not a threat");
        }

        [Test]
        public void Invincible_HuntsOutToSearchDistance()
        {
            var map = MakeMap(["##########################", "#........................#", "##########################"]);
            var me = MakePac(1, eTeam.Red, 1, 1);
            var far = MakePac(2, eTeam.Blue, 1 + PacBrain.HUNT_DISTANCE + 10, 1, 90f);
            var world = MakeWorld(map, [me, far]);

            MakeInvincible(me);
            Assert.That(PacBrain.PickGoal(world, me, 1, 1, Fill(map, me), out var target), Is.EqualTo(eBotGoal.Hunt));
            Assert.That(target, Is.EqualTo(MazeMovement.GetCell(far.Location)));
        }

        [Test]
        public void Invincible_IgnoresPowerPillsAndPowerUps()
        {
            var map = MakeMap(["#########", "#O.....o#", "#########"]);
            var me = MakePac(1, eTeam.Red, 3, 1);
            var world = MakeWorld(map, [me]);
            world.PowerUps = [new PowerUp { Item = ePowerUp.Cherry, Location = new Point2(2.5f, 1.5f), Alive = true }];

            Assert.That(PacBrain.PickGoal(world, me, 3, 1, Fill(map, me), out _), Is.EqualTo(eBotGoal.PowerPill));

            MakeInvincible(me);
            Assert.That(PacBrain.PickGoal(world, me, 3, 1, Fill(map, me), out var target), Is.EqualTo(eBotGoal.Dot));
            Assert.That(target, Is.EqualTo((7, 1)), "the plain dot, not the power pill");
        }

        [Test]
        public void RunsFromInvincibleEnemy_EvenWhenBigger()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 4, 1, 90f);
            var star = MakePac(2, eTeam.Blue, 6, 1, 10f);
            MakeInvincible(star);
            var world = MakeWorld(map, [me, star]);

            Assert.That(PacBrain.IsThreat(world, me, star), Is.True);
            Assert.That(PacBrain.IsPrey(world, me, star), Is.False);
            Assert.That(Choose(world, me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void AnyBot_GoesForADroppedEnemyFlag_BeforePowerPills()
        {
            var map = MakeMap(["#########", "#O......#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1);
            var dropped = new GameFlag(1, eTeam.Blue, 7.5f, 1.5f) { X = 6.5f, Y = 1.5f };
            var world = MakeWorld(map, [me], flags: [dropped]);

            Assert.That(PacBrain.IsAttacker(me), Is.False, "a defender goes for it too");
            Assert.That(PacBrain.PickGoal(world, me, 4, 1, Fill(map, me), out var target), Is.EqualTo(eBotGoal.LooseFlag));
            Assert.That(target, Is.EqualTo((6, 1)));

            // with a power up working it can't pick a flag up, so it doesn't go for it
            me.PowerUpEffect = ePowerUp.Cherry;
            me.PowerUpMs = 1000;
            Assert.That(PacBrain.PickGoal(world, me, 4, 1, Fill(map, me), out _), Is.Not.EqualTo(eBotGoal.LooseFlag));
        }

        [Test]
        public void DroppedEnemyFlag_InterruptsAKeptGoal()
        {
            var map = MakeMap(["#########", "#O......#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1);
            var dropped = new GameFlag(1, eTeam.Blue, 7.5f, 1.5f) { X = 6.5f, Y = 1.5f };
            var world = MakeWorld(map, [me], flags: [dropped]);

            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, Fill(map, me), eBotGoal.Dot), Is.True);
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, Fill(map, me), eBotGoal.LooseFlag), Is.False);

            dropped.X = dropped.BaseX;
            dropped.Y = dropped.BaseY;
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, Fill(map, me), eBotGoal.Dot), Is.False, "a flag at its base isn't loose");
        }

        [Test]
        public void PrefersNearbyPowerPill_OverHunting()
        {
            var map = MakeMap(["#########", "#O......#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1, 50f);
            var world = MakeWorld(map, [me, MakePac(2, eTeam.Blue, 6, 1, 10f)]);

            Assert.That(PacBrain.PickGoal(world, me, 4, 1, Fill(map, me), out var target), Is.EqualTo(eBotGoal.PowerPill));
            Assert.That(target, Is.EqualTo((1, 1)));
        }

        [Test]
        public void IgnoresBiggerTeammate()
        {
            var map = MakeMap(["#########", "#o......#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1);
            var friend = MakePac(3, eTeam.Red, 2, 1, 90f);

            Assert.That(Choose(MakeWorld(map, [me, friend]), me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void CarryingFlag_GoesHome()
        {
            var map = MakeMap(["#########", "#......o#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1);
            me.HasFlag = true;
            var flags = new List<GameFlag> { new(1, eTeam.Red, 1.5f, 1.5f) };

            Assert.That(Choose(MakeWorld(map, [me], flags: flags), me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void OwnFlagDropped_GoesToRescueIt()
        {
            var map = MakeMap(["#########", "#......o#", "#########"]);
            var me = MakePac(1, eTeam.Red, 4, 1);
            var flag = new GameFlag(1, eTeam.Red, 7.5f, 1.5f) { X = 1.5f, Y = 1.5f };

            Assert.That(Choose(MakeWorld(map, [me], flags: [flag]), me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void AttackerGoesForEnemyFlag_DefenderEats()
        {
            var map = MakeMap(["#########", "#......o#", "#########"]);
            var flags = new List<GameFlag> { new(1, eTeam.Blue, 1.5f, 1.5f) };
            var attacker = MakePac(2, eTeam.Red, 4, 1);
            var defender = MakePac(3, eTeam.Red, 4, 1);

            Assert.That(PacBrain.IsAttacker(attacker), Is.True);
            Assert.That(PacBrain.IsAttacker(defender), Is.False);
            Assert.That(Choose(MakeWorld(map, [attacker], flags: flags), attacker), Is.EqualTo(eFacing.Left));
            Assert.That(Choose(MakeWorld(map, [defender], flags: flags), defender), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void ChasesSmallerEnemyCarryingItsFlag()
        {
            var map = MakeMap(["##########################", "#o.......................#", "##########################"]);
            var me = MakePac(1, eTeam.Red, 3, 1, 80f);
            var carrier = MakePac(4, eTeam.Blue, 20, 1, 20f);
            carrier.HasFlag = true;

            Assert.That(Choose(MakeWorld(map, [me, carrier]), me), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void PathMap_StopsAtMaxDistance()
        {
            var paths = new PathMap();

            paths.Fill(MakeMap(CORRIDOR), 1, 1, 2);

            Assert.That(paths.GetDistance(3, 1), Is.EqualTo(2));
            Assert.That(paths.GetDistance(4, 1), Is.EqualTo(PathMap.UNREACHED));
            Assert.That(paths.Order, Has.Count.EqualTo(3));
        }

        [Test]
        public void CarryingFlag_HomeBeyondSearch_FollowsBaseMap()
        {
            var row = "#" + new string('.', PacBrain.SEARCH_DISTANCE + 20) + "#";
            var wall = new string('#', row.Length);
            var map = MakeMap([wall, row, wall]);
            var me = MakePac(1, eTeam.Red, 2, 1, facing: eFacing.Left);
            me.HasFlag = true;
            var flags = new List<GameFlag> { new(1, eTeam.Red, row.Length - 1.5f, 1.5f) };

            Assert.That(Choose(MakeWorld(map, [me], flags: flags), me), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Attacker_EnemyBaseBeyondSearch_HeadsThere()
        {
            var row = "#" + new string('.', PacBrain.SEARCH_DISTANCE + 20) + "#";
            var wall = new string('#', row.Length);
            var map = MakeMap([wall, row, wall]);
            var me = MakePac(2, eTeam.Red, row.Length - 3, 1);
            var flags = new List<GameFlag> { new(1, eTeam.Blue, 1.5f, 1.5f) };

            Assert.That(Choose(MakeWorld(map, [me], flags: flags), me), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void ChooseDownhill_StepsTowardStart()
        {
            var map = MakeMap(CORRIDOR);
            var paths = new PathMap();
            paths.Fill(map, 7, 1);

            Assert.That(PacBrain.ChooseDownhill(map, 2, 1, paths), Is.EqualTo(eFacing.Right));
            Assert.That(PacBrain.ChooseDownhill(map, 7, 1, paths), Is.Null);
        }

        [Test]
        public void ShouldTurnBack_OnlyForThreatAheadInLine()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 3, 1);
            me.Location = new Point2(3.7f, 1.5f);

            Assert.That(PacBrain.ShouldTurnBack(MakeWorld(map, [me], [MakeGhost(6, 1)]), me), Is.True);
            Assert.That(PacBrain.ShouldTurnBack(MakeWorld(map, [me], [MakeGhost(1, 1)]), me), Is.False);
            Assert.That(PacBrain.ShouldTurnBack(MakeWorld(map, [me], [MakeGhost(6, 3)]), me), Is.False);

            me.Size = MAX_SIZE;
            me.PowerUpEffect = ePowerUp.PowerPill;
            me.PowerUpMs = 1000;
            Assert.That(PacBrain.ShouldTurnBack(MakeWorld(map, [me], [MakeGhost(6, 1)]), me), Is.False);
        }

        [Test]
        public void ShouldTurnBack_NotForAThreatBehindAWall()
        {
            var map = MakeMap(["#########", "#...#...#", "#########"]);
            var me = MakePac(1, eTeam.Red, 2, 1);
            me.Location = new Point2(2.7f, 1.5f);

            Assert.That(PacBrain.ShouldTurnBack(MakeWorld(map, [me], [MakeGhost(6, 1)]), me), Is.False);
            Assert.That(PacBrain.ShouldTurnBack(MakeWorld(map, [me], [MakeGhost(3, 1)]), me), Is.True);
        }

        /// <summary>
        /// A loop of cells round a block, (1, 1) to (5, 3).
        /// </summary>
        private static readonly string[] LOOP =
        [
            "#######",
            "#.....#",
            "#.###.#",
            "#.....#",
            "#######",
        ];

        [Test]
        public void ChooseFleeDirection_KeepsGoingWhenBothWaysAreAsGood()
        {
            var map = MakeMap(LOOP);

            // the threat is straight across the loop, as far one way round as the other
            Assert.That(PacBrain.ChooseFleeDirection(map, 3, 1, eFacing.Left, [(3, 3)], new PathMap()), Is.EqualTo(eFacing.Left));
            Assert.That(PacBrain.ChooseFleeDirection(map, 3, 1, eFacing.Right, [(3, 3)], new PathMap()), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void ChooseFleeDirection_TurnsBackWhenThatIsFarEnoughAway()
        {
            var map = MakeMap(LOOP);

            // ahead is 4 steps from the threat, behind 6
            Assert.That(PacBrain.ChooseFleeDirection(map, 3, 1, eFacing.Left, [(2, 3)], new PathMap()), Is.EqualTo(eFacing.Right));
            Assert.That(PacBrain.ChooseFleeDirection(map, 3, 1, eFacing.Right, [(2, 3)], new PathMap()), Is.EqualTo(eFacing.Right));
        }

        /// <summary>
        /// Moves me a cell at a time, picking at each centre with memory (a fresh one per step when null, so it
        /// remembers nothing), msPerStep apart. Returns how many times it turned back.
        /// </summary>
        private static int CountTurnBacks(BotWorld world, Player me, BotMemory memory, int steps, int msPerStep = 200)
        {
            var paths = new PathMap();
            var threatPaths = new PathMap();
            var random = new Random(1);
            var width = world.Map.GetLength(0);
            var height = world.Map.GetLength(1);
            var turnBacks = 0;

            for (var i = 0; i < steps; i++)
            {
                var (x, y) = MazeMovement.GetCell(me.Location);
                var choice = PacBrain.ChooseDirection(world, me, x, y, paths, threatPaths, random, memory, 0, 0) ?? me.Facing;

                if (!MazeMovement.CanMove(world.Map, x, y, choice, true, PacBrain.GetBlocked(me)))
                    continue;

                if (choice == MazeMovement.GetOpposite(me.Facing))
                    turnBacks++;

                var (nx, ny) = MazeMovement.GetNeighbor(x, y, choice, width, height);
                me.Location = new Point2(nx + 0.5f, ny + 0.5f);
                me.Facing = choice;
                memory?.Update(msPerStep);
            }

            return turnBacks;
        }

        private static eFacing? ChooseWithMemory(BotWorld world, Player me, BotMemory memory)
        {
            var (x, y) = MazeMovement.GetCell(me.Location);
            return PacBrain.ChooseDirection(world, me, x, y, new PathMap(), new PathMap(), new Random(1), memory, 0, 0);
        }

        [Test]
        public void ChooseDirection_TakesReactionMsToStartFleeing()
        {
            var map = MakeMap(["#################", "#...............#", "#################"]);
            var memory = new BotMemory();
            var me = MakePac(1, eTeam.Red, 6, 1, facing: eFacing.Left);
            var world = MakeWorld(map, [me], [MakeGhost(2, 1)]);
            var (x, y) = MazeMovement.GetCell(me.Location);

            eFacing? Pick() => PacBrain.ChooseDirection(world, me, x, y, new PathMap(), new PathMap(), new Random(1), memory, 0);

            Assert.That(Pick(), Is.EqualTo(eFacing.Left), "carries on toward the ghost it has only just seen");
            Assert.That(memory.IsFleeing, Is.False);

            memory.Update(BotMemory.REACTION_MS - 1);
            Assert.That(Pick(), Is.EqualTo(eFacing.Left));

            memory.Update(1);
            Assert.That(Pick(), Is.EqualTo(eFacing.Right), "runs once its reaction time is up");
            Assert.That(memory.IsFleeing, Is.True);
        }

        [Test]
        public void BotMemory_ForgettingAThreatRestartsTheReactionTime()
        {
            var memory = new BotMemory();

            memory.NoticeThreat();
            memory.Update(BotMemory.REACTION_MS);
            Assert.That(memory.HasReacted, Is.True);

            memory.ForgetThreat();
            Assert.That(memory.HasReacted, Is.False);

            memory.NoticeThreat();
            Assert.That(memory.HasReacted, Is.False);
            memory.NoticeThreat();
            memory.Update(BotMemory.REACTION_MS);
            Assert.That(memory.HasReacted, Is.True, "seeing it again doesn't restart the time");
        }

        [Test]
        public void StillGhostByADot_DoesNotPinTheBot()
        {
            var map = MakeMap(["#################", "#.o.............#", "#################"]);
            var ghosts = new List<Ghost> { MakeGhost(1, 1) };

            var forgetful = MakePac(1, eTeam.Red, 9, 1, facing: eFacing.Left);
            var forgetfulTurns = CountTurnBacks(MakeWorld(map, [forgetful], ghosts), forgetful, null, 30);

            var me = MakePac(1, eTeam.Red, 9, 1, facing: eFacing.Left);
            var turns = CountTurnBacks(MakeWorld(map, [me], ghosts), me, new BotMemory(), 30);

            Assert.That(forgetfulTurns, Is.GreaterThan(10), "a bot that remembers nothing flip-flops at the edge of DANGER_DISTANCE");
            Assert.That(turns, Is.LessThanOrEqualTo(4));
        }

        [Test]
        public void ChooseDirection_KeepsFleeingUntilCalmDistance()
        {
            var map = MakeMap(["#################", "#...............#", "#################"]);
            var memory = new BotMemory();
            var me = MakePac(1, eTeam.Red, 6, 1, facing: eFacing.Left);
            var world = MakeWorld(map, [me], [MakeGhost(1, 1)]);

            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Right));
            Assert.That(memory.IsFleeing, Is.True);

            // past FLEE_MIN_MS and past DANGER_DISTANCE, but still within CALM_DISTANCE
            memory.Update(BotMemory.FLEE_MIN_MS);
            me.Location = new Point2(8.5f, 1.5f);
            me.Facing = eFacing.Right;
            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Right));
            Assert.That(memory.IsFleeing, Is.True);

            me.Location = new Point2(10.5f, 1.5f);
            ChooseWithMemory(world, me, memory);
            Assert.That(memory.IsFleeing, Is.False);
            Assert.That(memory.IsAvoiding, Is.True);
            Assert.That(memory.AvoidMask[1, 1], Is.True);
            Assert.That(memory.AvoidMask[1 + BotMemory.AVOID_RADIUS + 1, 1], Is.False);
        }

        [Test]
        public void ChooseDirection_KeepsFleeingForFleeMinMs()
        {
            var map = MakeMap(["#################", "#...............#", "#################"]);
            var memory = new BotMemory();
            var me = MakePac(1, eTeam.Red, 6, 1, facing: eFacing.Left);
            var ghost = MakeGhost(1, 1);
            var world = MakeWorld(map, [me], [ghost]);

            ChooseWithMemory(world, me, memory);

            // the ghost is gone, but the bot keeps running from where it was until FLEE_MIN_MS is up
            ghost.Alive = false;
            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Right));
            Assert.That(memory.IsFleeing, Is.True);

            memory.Update(BotMemory.FLEE_MIN_MS);
            ChooseWithMemory(world, me, memory);
            Assert.That(memory.IsFleeing, Is.False);
        }

        [Test]
        public void AfterFleeing_PathsAroundWhereTheThreatWas()
        {
            // two ways round a block to the dot at (9, 5), the top one shorter
            var map = MakeMap(["###########", "#.........#", "#.#######.#", "#.#######.#", "#.#######.#", "#........o#", "###########"]);
            var memory = new BotMemory();
            var me = MakePac(1, eTeam.Red, 2, 1, facing: eFacing.Up);
            var world = MakeWorld(map, [me]);

            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Right), "shortest way, nothing to avoid");

            // a threat stood on the top way
            memory.Flee([(6, 1)]);
            memory.StopFleeing(map.GetLength(0), map.GetLength(1));
            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Left), "goes the other way round");

            memory.Update(BotMemory.AVOID_MS);
            memory.ClearGoal();
            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Right), "back to the short way once avoiding is over");
        }

        [Test]
        public void ChooseDirection_KeepsItsGoalUntilItsTimeIsUp()
        {
            var map = MakeMap(["###########", "#o........#", "###########"]);
            var memory = new BotMemory();
            var me = MakePac(1, eTeam.Red, 4, 1, size: 50f, facing: eFacing.Left);
            var world = MakeWorld(map, [me]);

            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Left));
            Assert.That(memory.Goal, Is.EqualTo(eBotGoal.Dot));

            // smaller prey turns up behind; the bot sticks to the dot for now
            world.Pacs = [me, MakePac(2, eTeam.Blue, 7, 1)];
            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Left));

            memory.Update(BotMemory.GOAL_COMMIT_MAX_MS);
            Assert.That(ChooseWithMemory(world, me, memory), Is.EqualTo(eFacing.Right));
            Assert.That(memory.Goal, Is.EqualTo(eBotGoal.Hunt));
        }

        [Test]
        public void ChooseDirection_DropsAGoalThatIsDone()
        {
            var map = MakeMap(["###########", "#o........#", "###########"]);
            var memory = new BotMemory();
            var me = MakePac(1, eTeam.Red, 4, 1, size: 50f, facing: eFacing.Left);
            var prey = MakePac(2, eTeam.Blue, 2, 1);
            var world = MakeWorld(map, [me, prey]);

            ChooseWithMemory(world, me, memory);
            Assert.That(memory.Goal, Is.EqualTo(eBotGoal.Hunt));

            prey.Alive = false;
            ChooseWithMemory(world, me, memory);
            Assert.That(memory.Goal, Is.EqualTo(eBotGoal.Dot));
        }

        [Test]
        public void InterruptsGoal_ForAFlagOrARescue()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 4, 1);
            var flags = new List<GameFlag> { new(1, eTeam.Red, 7.5f, 1.5f) };
            var world = MakeWorld(map, [me], flags: flags);
            var paths = Fill(map, me);

            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, paths, eBotGoal.Dot), Is.False);

            me.HasFlag = true;
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, paths, eBotGoal.Dot), Is.True, "picked up a flag");
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, paths, eBotGoal.CarryHome), Is.False);

            me.HasFlag = false;
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, paths, eBotGoal.CarryHome), Is.True, "lost the flag");

            flags[0].X = 1.5f;
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, paths, eBotGoal.Dot), Is.True, "own flag dropped");
            Assert.That(PacBrain.InterruptsGoal(world, me, 4, 1, paths, eBotGoal.Rescue), Is.False);
        }

        [Test]
        public void IsMovingTarget_OnlyForTargetsThatMove()
        {
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.Hunt), Is.True);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.HuntCarrier), Is.True);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.HuntGhost), Is.True);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.PowerUp), Is.True);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.CarryHome), Is.True);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.Dot), Is.False);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.PowerPill), Is.False);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.Rescue), Is.False);
            Assert.That(PacBrain.IsMovingTarget(eBotGoal.EnemyFlag), Is.False);
        }

        [Test]
        public void BotMemory_GoalTimeIsBetweenMinAndMax()
        {
            var memory = new BotMemory();
            var random = new Random(3);

            for (var i = 0; i < 100; i++)
            {
                memory.SetGoal(eBotGoal.Dot, random);
                Assert.That(memory.GoalMsLeft, Is.InRange(BotMemory.GOAL_COMMIT_MIN_MS, BotMemory.GOAL_COMMIT_MAX_MS));
            }

            Assert.That(memory.IsCommitted, Is.True);
            memory.Update(BotMemory.GOAL_COMMIT_MAX_MS);
            Assert.That(memory.IsCommitted, Is.False);
            Assert.That(memory.GoalMsLeft, Is.Zero);
        }

        [Test]
        public void BotMemory_TurnBackCooldown()
        {
            var memory = new BotMemory();

            Assert.That(memory.CanTurnBack, Is.True);
            memory.TurnedBack();
            Assert.That(memory.CanTurnBack, Is.False);
            memory.Update(BotMemory.TURN_BACK_COOLDOWN_MS);
            Assert.That(memory.CanTurnBack, Is.True);
        }

        [Test]
        public void BotMemory_ResetForgetsEverything()
        {
            var memory = new BotMemory();
            memory.SetGoal(eBotGoal.Hunt, new Random(1));
            memory.Flee([(2, 2)]);
            memory.StopFleeing(10, 10);
            memory.TurnedBack();

            memory.Reset();

            Assert.That(memory.Goal, Is.EqualTo(eBotGoal.None));
            Assert.That(memory.IsFleeing, Is.False);
            Assert.That(memory.IsAvoiding, Is.False);
            Assert.That(memory.ThreatCells, Is.Empty);
            Assert.That(memory.CanTurnBack, Is.True);
        }

        [Test]
        public void PathMap_NeverEntersAvoidedCells()
        {
            var map = MakeMap(CORRIDOR);
            var avoid = new bool[map.GetLength(0), map.GetLength(1)];
            avoid[5, 1] = true;
            avoid[3, 1] = true;

            var paths = new PathMap();
            paths.Fill(map, 3, 1, avoid: avoid);

            Assert.That(paths.GetDistance(3, 1), Is.Zero, "the start is never avoided");
            Assert.That(paths.GetDistance(4, 1), Is.EqualTo(1));
            Assert.That(paths.GetDistance(5, 1), Is.EqualTo(PathMap.UNREACHED));
            Assert.That(paths.GetDistance(6, 1), Is.EqualTo(PathMap.UNREACHED));
        }

        [Test]
        public void ChooseWanderDirection_AvoidFood_PicksEmptyCell()
        {
            var map = MakeMap(["#####", "#.o.#", "#.#.#", "#...#", "#####"]);

            for (var seed = 0; seed < 20; seed++)
                Assert.That(PacBrain.ChooseWanderDirection(map, 1, 1, eFacing.Down, new Random(seed), true), Is.EqualTo(eFacing.Down));
        }

        [Test]
        public void GetPerceptionError_InRange_SameForSamePair_DiffersBetweenPairs()
        {
            var errors = new HashSet<float>();
            var below = 0;

            for (var other = 0; other < 200; other++)
            {
                var error = PacBrain.GetPerceptionError(1234, 7, other);

                Assert.That(error, Is.InRange(-1f, 1f));
                Assert.That(PacBrain.GetPerceptionError(1234, 7, other), Is.EqualTo(error));
                errors.Add(error);

                if (error < 0f)
                    below++;
            }

            Assert.That(errors, Has.Count.GreaterThan(190));
            Assert.That(below, Is.InRange(60, 140), "errors fall both ways");
            Assert.That(PacBrain.GetPerceptionError(1234, 7, 8), Is.Not.EqualTo(PacBrain.GetPerceptionError(1234, 8, 7)));
            Assert.That(PacBrain.GetPerceptionError(1234, 7, 8), Is.Not.EqualTo(PacBrain.GetPerceptionError(1235, 7, 8)));
        }

        [Test]
        public void GetPerceivedSize_WithinErrorOfRealSize()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 1, 1);
            var limit = PacBrain.SIZE_PERCEPTION_ERROR * MAX_SIZE;

            for (var seed = 0; seed < 50; seed++)
            {
                var other = MakePac(2, eTeam.Blue, 3, 1, 40f);
                var world = MakeWorld(map, [me, other]);
                world.PerceptionSeed = seed;

                Assert.That(PacBrain.GetPerceivedSize(world, me, other), Is.InRange(40f - limit, 40f + limit));
            }
        }

        [Test]
        public void EqualSizedEnemies_SomeLookBiggerSomeSmaller()
        {
            var map = MakeMap(CORRIDOR);
            var me = MakePac(1, eTeam.Red, 1, 1, 50f);
            var threats = 0;
            var prey = 0;

            for (var id = 2; id < 102; id++)
            {
                var other = MakePac(id, eTeam.Blue, 3, 1, 50f);
                var world = MakeWorld(map, [me, other]);

                if (PacBrain.IsThreat(world, me, other))
                    threats++;

                if (PacBrain.IsPrey(world, me, other))
                    prey++;
            }

            Assert.That(threats, Is.GreaterThan(0));
            Assert.That(prey, Is.GreaterThan(0));
        }

        [Test]
        public void RandomTurns_SometimesWander_NeverWhenFleeingOrCarrying()
        {
            var map = MakeMap(["#####", "#o..#", "#.#.#", "#...#", "#####"]);
            var me = MakePac(1, eTeam.Red, 1, 3, facing: eFacing.Down);
            var world = MakeWorld(map, [me]);
            var random = new Random(5);
            var wandered = 0;

            // the dot is up, but up is turning back, so wandering always goes right
            for (var i = 0; i < 200; i++)
                if (Choose(world, me, random, 50) == eFacing.Right)
                    wandered++;

            Assert.That(wandered, Is.InRange(60, 140));

            for (var i = 0; i < 50; i++)
                Assert.That(Choose(world, me, random, 0), Is.EqualTo(eFacing.Up));

            var corridor = MakeMap(CORRIDOR);
            var fleeing = MakePac(1, eTeam.Red, 4, 1, facing: eFacing.Right);

            for (var i = 0; i < 50; i++)
                Assert.That(Choose(MakeWorld(corridor, [fleeing], [MakeGhost(6, 1)]), fleeing, random, 100), Is.EqualTo(eFacing.Left));

            var carrier = MakePac(1, eTeam.Red, 4, 1, facing: eFacing.Right);
            carrier.HasFlag = true;
            var flags = new List<GameFlag> { new(1, eTeam.Red, 1.5f, 1.5f) };

            for (var i = 0; i < 50; i++)
                Assert.That(Choose(MakeWorld(corridor, [carrier], flags: flags), carrier, random, 100), Is.EqualTo(eFacing.Left));
        }

        private static PathMap Fill(eGridDef[,] map, Player me)
        {
            var paths = new PathMap();
            var (x, y) = MazeMovement.GetCell(me.Location);
            paths.Fill(map, x, y);
            return paths;
        }
    }
}
