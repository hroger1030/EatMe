using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    public class GhostPersonalityTests
    {
        private const float VISION = 10f;
        private const int SERVER_SEED = 1234;
        private static readonly Point2 ORIGIN = new(0f, 0f);

        /// <summary>
        /// A plus sign: one junction at (3, 3) with four dead end arms.
        /// </summary>
        private static readonly string[] CROSS =
        [
            "#######",
            "###.###",
            "###.###",
            "#.....#",
            "###.###",
            "###.###",
            "#######",
        ];

        private static readonly string[] RING =
        [
            "#####",
            "#...#",
            "#.#.#",
            "#...#",
            "#####",
        ];

        private static Player MakePac(int id, float x, float y, float size = 50f, bool hasFlag = false, bool powered = false)
        {
            return new Player { Id = id, Location = new Point2(x, y), Alive = true, HasFlag = hasFlag, Size = size, PowerUpEffect = powered ? ePowerUpEffect.PowerPill : ePowerUpEffect.None, PowerUpMs = powered ? 1000 : 0 };
        }

        private static Ghost MakeGhost(int id, float x, float y, eGhostBehavior behavior = eGhostBehavior.Normal)
        {
            var personality = GhostPersonalities.ALL.First(p => p.Behavior == behavior);
            return new Ghost { Id = id, Location = new Point2(x, y), Alive = true, Personality = personality };
        }

        private static float GetHue(FastColor color)
        {
            var r = color.R / 255f;
            var g = color.G / 255f;
            var b = color.B / 255f;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            if (delta == 0f)
                return 0f;

            var hue = max == r ? 60f * (((g - b) / delta) % 6f) : max == g ? 60f * (((b - r) / delta) + 2f) : 60f * (((r - g) / delta) + 4f);
            return hue < 0f ? hue + 360f : hue;
        }

        private static float GetSaturation(FastColor color)
        {
            var max = Math.Max(color.R, Math.Max(color.G, color.B));
            var min = Math.Min(color.R, Math.Min(color.G, color.B));
            return max == 0 ? 0f : (max - min) / (float)max;
        }

        // ---- the table ----

        [Test]
        public void All_IdsMatchIndexAndNamesAndColorsAreUnique()
        {
            for (var i = 0; i < GhostPersonalities.ALL.Count; i++)
                Assert.That(GhostPersonalities.ALL[i].Id, Is.EqualTo(i));

            Assert.That(GhostPersonalities.ALL.Select(p => p.Name).Distinct().Count(), Is.EqualTo(GhostPersonalities.ALL.Count));
            Assert.That(GhostPersonalities.ALL.Select(p => p.Color).Distinct().Count(), Is.EqualTo(GhostPersonalities.ALL.Count));
            Assert.That(GhostPersonalities.ALL.Select(p => p.Behavior).Distinct().Count(), Is.EqualTo(GhostPersonalities.ALL.Count));
            Assert.That(GhostPersonalities.ALL.Count, Is.EqualTo(Enum.GetValues<eGhostBehavior>().Length));
        }

        [Test]
        public void Norm_IsTheNormalGhost()
        {
            Assert.That(GhostPersonalities.NORM.Name, Is.EqualTo("Norm"));
            Assert.That(GhostPersonalities.NORM.Behavior, Is.EqualTo(eGhostBehavior.Normal));
            Assert.That(GhostPersonalities.NORM.SpeedScale, Is.EqualTo(1f));
            Assert.That(GhostPersonalities.NORM.VisionScale, Is.EqualTo(1f));
        }

        [Test]
        public void Colors_RecolorAndKeepClearOfTeamColors()
        {
            foreach (var personality in GhostPersonalities.ALL)
            {
                var color = personality.Color;

                // the body is found by being pure white, so a white ghost would be left unrecolored and blend with the art
                Assert.That(color, Is.Not.EqualTo(GhostPalette.BODY_COLOR), personality.Name);
                Assert.That(color.A, Is.EqualTo(255), personality.Name);

                if (GetSaturation(color) < 0.3f)
                    continue;

                var hue = GetHue(color);
                Assert.That(hue is > 10f and < 345f, Is.True, $"{personality.Name} looks red ({hue})");
                Assert.That(hue is < 195f or > 245f, Is.True, $"{personality.Name} looks blue ({hue})");
            }
        }

        // ---- the lineup ----

        [Test]
        public void PickGhostLineup_SameSeedSameLineup()
        {
            var config = new GameConfiguration { GhostCount = 6 };

            for (var level = 1; level <= 10; level++)
            {
                var seed = Levels.GetSeed(SERVER_SEED, level);

                var ghostList1 = MapMaker.PickGhostLineup(config, seed, level);
                var ghostList2 = MapMaker.PickGhostLineup(config, seed, level);

                Assert.That(ghostList1, Is.EqualTo(ghostList2));
            }
        }

        [Test]
        public void PickGhostLineup_IsGhostCountLongAndEmptyWithNoGhosts()
        {
            Assert.That(MapMaker.PickGhostLineup(new GameConfiguration { GhostCount = 4 }, 1, 1), Has.Length.EqualTo(4));
            Assert.That(MapMaker.PickGhostLineup(new GameConfiguration { GhostCount = 0 }, 1, 1), Is.Empty);
        }

        [Test]
        public void PickGhostLineup_NoRepeatsUntilEveryGhostIsUsed()
        {
            var unique = GhostPersonalities.ALL.Count;

            for (var count = 1; count <= unique; count++)
            {
                for (var level = 1; level <= 6; level++)
                {
                    var lineup = MapMaker.PickGhostLineup(new GameConfiguration { GhostCount = count }, Levels.GetSeed(SERVER_SEED, level), level);
                    Assert.That(lineup.Distinct().Count(), Is.EqualTo(count), $"{count} ghosts, level {level}");
                }
            }
        }

        [Test]
        public void PickGhostLineup_FillsSlotsPastTheUniqueGhostsWithNorm()
        {
            var unique = GhostPersonalities.ALL.Count;
            var config = new GameConfiguration { GhostCount = unique + 5 };

            for (var level = 1; level <= 4; level++)
            {
                var lineup = MapMaker.PickGhostLineup(config, Levels.GetSeed(SERVER_SEED, level), level);

                Assert.That(lineup.Distinct().Count(), Is.EqualTo(unique));
                Assert.That(lineup.Count(p => p == GhostPersonalities.NORM), Is.EqualTo(6));
                Assert.That(lineup.Where(p => p == GhostPersonalities.NORM).Select(p => p.Name), Is.All.EqualTo("Norm"));
            }
        }

        [Test]
        public void PickGhostLineup_OddLevelsAlwaysHaveNorm()
        {
            for (var count = 1; count <= 8; count++)
            {
                for (var serverSeed = 0; serverSeed < 50; serverSeed++)
                {
                    for (var level = 1; level <= 31; level += 2)
                    {
                        var lineup = MapMaker.PickGhostLineup(new GameConfiguration { GhostCount = count }, Levels.GetSeed(serverSeed, level), level);
                        Assert.That(lineup, Does.Contain(GhostPersonalities.NORM), $"{count} ghosts, server seed {serverSeed}, level {level}");
                    }
                }
            }
        }

        [Test]
        public void PickGhostLineup_EvenLevelsPickNormMoreThanAnyOtherGhost()
        {
            var config = new GameConfiguration { GhostCount = 4 };
            var counts = new int[GhostPersonalities.ALL.Count];

            for (var serverSeed = 0; serverSeed < 500; serverSeed++)
                for (var level = 2; level <= 32; level += 2)
                    foreach (var personality in MapMaker.PickGhostLineup(config, Levels.GetSeed(serverSeed, level), level))
                        counts[personality.Id]++;

            var norm = counts[GhostPersonalities.NORM.Id];

            for (var id = 0; id < counts.Length; id++)
                if (id != GhostPersonalities.NORM.Id)
                    Assert.That(norm, Is.GreaterThan(counts[id] * 2), GhostPersonalities.ALL[id].Name);
        }

        [Test]
        public void PickGhostLineup_DoesNotChangeTheMap()
        {
            var config = new GameConfiguration { MazeSize = 40 };
            var seed = Levels.GetSeed(SERVER_SEED, 3);
            var before = MapMaker.Generate(config, seed);

            MapMaker.PickGhostLineup(config, seed, 3);

            Assert.That(MapMaker.Generate(config, seed), Is.EqualTo(before));
        }

        [Test]
        public void Server_GhostsTakeTheirLevelsLineup()
        {
            var settings = new ServerSettings
            {
                ServerName = "Ghosts",
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = 5, MinPlayers = 0 },
            };
            var server = new GameServer(settings, []);
            var level = server.CurrentLevel.Level;
            var lineup = MapMaker.PickGhostLineup(settings.GameConfig, Levels.GetSeed(settings.GameConfig.ServerSeed, level), level);

            Assert.That(server.Ghosts.Select(g => g.Personality), Is.EqualTo(lineup));
            Assert.That(server.Ghosts.Select(g => g.Name), Is.EqualTo(lineup.Select(p => p.Name)));
        }

        [Test]
        public void ClientState_LevelStart_WorksOutTheSameLineup()
        {
            var config = new GameConfiguration { ServerSeed = 4242, GhostCount = 4 };
            var state = new ClientGameState();
            state.Apply(new WelcomeMessage { PlayerId = 3, Team = eTeam.Blue, ServerName = "Srv", LoadingScreenTips = [], GameConfig = config }, 0);
            state.Apply(new LevelStartMessage { Level = 2 }, 0);

            Assert.That(state.GhostLineup, Is.EqualTo(MapMaker.PickGhostLineup(config, Levels.GetSeed(4242, 2), 2)));
            Assert.That(state.GetGhostPersonality(1), Is.SameAs(state.GhostLineup[1]));
            Assert.That(state.GetGhostPersonality(99), Is.SameAs(GhostPersonalities.NORM));
        }

        // ---- targets ----

        [Test]
        public void PickStubbornTarget_KeepsItsTargetOverACloserPac()
        {
            var current = MakePac(1, 8f, 0f);
            var closer = MakePac(2, 2f, 0f);

            Assert.That(GhostBrain.PickStubbornTarget(ORIGIN, current, [current, closer], VISION), Is.SameAs(current));
        }

        [Test]
        public void PickStubbornTarget_SwapsOnlyForAFlagCarrier()
        {
            var current = MakePac(1, 2f, 0f);
            var carrier = MakePac(2, 8f, 0f, hasFlag: true);

            Assert.That(GhostBrain.PickStubbornTarget(ORIGIN, current, [current, carrier], VISION), Is.SameAs(carrier));
        }

        [Test]
        public void PickStubbornTarget_PicksAgainWhenItsTargetDies()
        {
            var current = MakePac(1, 2f, 0f);
            var other = MakePac(2, 5f, 0f);
            current.Alive = false;

            Assert.That(GhostBrain.PickStubbornTarget(ORIGIN, current, [current, other], VISION), Is.SameAs(other));
        }

        [Test]
        public void PickFlagHoundTarget_SeesCarriersFurtherAndIgnoresOthersUnlessClose()
        {
            var nearby = MakePac(1, 5f, 0f);
            var carrier = MakePac(2, 15f, 0f, hasFlag: true);
            var veryClose = MakePac(3, 2f, 0f);

            Assert.That(GhostBrain.PickFlagHoundTarget(ORIGIN, [nearby], VISION), Is.Null);
            Assert.That(GhostBrain.PickFlagHoundTarget(ORIGIN, [nearby, carrier], VISION), Is.SameAs(carrier));
            Assert.That(GhostBrain.PickFlagHoundTarget(ORIGIN, [nearby, veryClose], VISION), Is.SameAs(veryClose));
            Assert.That(GhostBrain.PickFlagHoundTarget(ORIGIN, [veryClose, carrier], VISION), Is.SameAs(carrier));
        }

        [Test]
        public void PickFlagHoundTarget_IgnoresPoweredCarriers()
        {
            var carrier = MakePac(1, 15f, 0f, hasFlag: true, powered: true);

            Assert.That(GhostBrain.PickFlagHoundTarget(ORIGIN, [carrier], VISION), Is.Null);
        }

        [Test]
        public void PickSmallestAndBiggest_GoBySizeWithinVision()
        {
            var small = MakePac(1, 8f, 0f, size: 10f);
            var big = MakePac(2, 6f, 0f, size: 80f);
            var middle = MakePac(3, 2f, 0f, size: 40f);
            var tinyOutOfSight = MakePac(4, 20f, 0f, size: 1f);
            var hugePowered = MakePac(5, 3f, 0f, size: 200f, powered: true);
            List<Player> pacs = [small, big, middle, tinyOutOfSight, hugePowered];

            Assert.That(GhostBrain.PickSmallest(ORIGIN, pacs, VISION), Is.SameAs(small));
            Assert.That(GhostBrain.PickBiggest(ORIGIN, pacs, VISION), Is.SameAs(big));
        }

        [Test]
        public void PickSmallest_TieGoesToTheNearest()
        {
            var far = MakePac(1, 8f, 0f, size: 10f);
            var near = MakePac(2, 3f, 0f, size: 10f);

            Assert.That(GhostBrain.PickSmallest(ORIGIN, [far, near], VISION), Is.SameAs(near));
        }

        [Test]
        public void PickGrudgeTarget_HuntsItsKillerFromFurther()
        {
            var killer = MakePac(7, 25f, 0f);
            var near = MakePac(2, 2f, 0f);

            Assert.That(GhostBrain.PickGrudgeTarget(ORIGIN, null, [near, killer], VISION, 7), Is.SameAs(killer));
            Assert.That(GhostBrain.PickGrudgeTarget(ORIGIN, null, [near, killer], VISION, Ghost.NO_GRUDGE), Is.SameAs(near));
        }

        [Test]
        public void PickGrudgeTarget_LeavesAPoweredKillerAlone()
        {
            var killer = MakePac(7, 5f, 0f, powered: true);
            var near = MakePac(2, 2f, 0f);

            Assert.That(GhostBrain.PickGrudgeTarget(ORIGIN, null, [near, killer], VISION, 7), Is.SameAs(near));
        }

        [Test]
        public void PickRandomTarget_PicksEveryPacInSightAndNoOther()
        {
            var a = MakePac(1, 2f, 0f);
            var b = MakePac(2, 5f, 0f);
            var outOfSight = MakePac(3, 20f, 0f);
            var powered = MakePac(4, 3f, 0f, powered: true);
            List<Player> pacs = [a, b, outOfSight, powered];
            var random = new Random(5);
            var picked = new HashSet<Player>();

            for (var i = 0; i < 100; i++)
                picked.Add(GhostBrain.PickRandomTarget(ORIGIN, pacs, VISION, random));

            Assert.That(picked, Is.EquivalentTo(new[] { a, b }));
            Assert.That(GhostBrain.PickRandomTarget(ORIGIN, [outOfSight], VISION, random), Is.Null);
        }

        [Test]
        public void FindNearestPac_IncludesPoweredPacs()
        {
            var powered = MakePac(1, 2f, 0f, powered: true);
            var other = MakePac(2, 5f, 0f);

            Assert.That(GhostBrain.FindNearestPac(ORIGIN, [other, powered], VISION), Is.SameAs(powered));
            Assert.That(GhostBrain.FindNearestPac(ORIGIN, [MakePac(3, 20f, 0f)], VISION), Is.Null);
        }

        [Test]
        public void IsChaseable_NeedsALivingUnpoweredPacInTheList()
        {
            var pac = MakePac(1, 0f, 0f);
            var powered = MakePac(2, 0f, 0f, powered: true);

            Assert.That(GhostBrain.IsChaseable(pac, [pac]), Is.True);
            Assert.That(GhostBrain.IsChaseable(pac, []), Is.False);
            Assert.That(GhostBrain.IsChaseable(powered, [powered]), Is.False);
            Assert.That(GhostBrain.IsChaseable(null, [pac]), Is.False);
        }

        [Test]
        public void FindLeader_IsTheNearestOtherLivingGhostThatIsntASidekick()
        {
            var me = MakeGhost(0, 0f, 0f, eGhostBehavior.Sidekick);
            var far = MakeGhost(1, 5f, 0f);
            var dead = MakeGhost(2, 2f, 0f);
            var otherSidekick = MakeGhost(3, 1f, 0f, eGhostBehavior.Sidekick);
            dead.Alive = false;

            Assert.That(GhostBrain.FindLeader(me, [me, far, dead, otherSidekick]), Is.SameAs(far));
            Assert.That(GhostBrain.FindLeader(me, [me]), Is.Null);
        }

        // ---- movement and timing ----

        [Test]
        public void GetAmbushCell_LooksAheadUntilAWall()
        {
            var map = MazeMovementTests.MakeMap(CROSS);

            Assert.That(GhostBrain.GetAmbushCell(map, 1, 3, eFacing.Right, 2), Is.EqualTo((3, 3)));
            Assert.That(GhostBrain.GetAmbushCell(map, 1, 3, eFacing.Right, 10), Is.EqualTo((5, 3)));
            Assert.That(GhostBrain.GetAmbushCell(map, 1, 3, eFacing.Up, 4), Is.EqualTo((1, 3)));
        }

        [Test]
        public void ShouldScatter_StartsCloseAndKeepsGoingUntilHome()
        {
            var close = GhostBrain.SCATTER_DISTANCE * GhostBrain.SCATTER_DISTANCE;

            Assert.That(GhostBrain.ShouldScatter(false, close, false), Is.True);
            Assert.That(GhostBrain.ShouldScatter(false, close + 1f, false), Is.False);
            Assert.That(GhostBrain.ShouldScatter(false, float.MaxValue, false), Is.False);
            Assert.That(GhostBrain.ShouldScatter(true, float.MaxValue, false), Is.True);
            Assert.That(GhostBrain.ShouldScatter(true, 0f, true), Is.False);
        }

        [Test]
        public void FindHomeCorner_IsTheOpenCellNearestEachCorner()
        {
            var map = MazeMovementTests.MakeMap(RING);

            Assert.That(GhostBrain.FindHomeCorner(map, 0), Is.EqualTo((1, 1)));
            Assert.That(GhostBrain.FindHomeCorner(map, 1), Is.EqualTo((3, 1)));
            Assert.That(GhostBrain.FindHomeCorner(map, 2), Is.EqualTo((1, 3)));
            Assert.That(GhostBrain.FindHomeCorner(map, 3), Is.EqualTo((3, 3)));
            Assert.That(GhostBrain.FindHomeCorner(map, 5), Is.EqualTo((3, 1)));
        }

        [Test]
        public void FindHomeCorner_SkipsSpawnTiles()
        {
            var map = MazeMovementTests.MakeMap(RING);
            map[1, 1] |= eGridDef.SpawnPointRed;

            Assert.That(GhostBrain.FindHomeCorner(map, 0), Is.Not.EqualTo((1, 1)));
        }

        [Test]
        public void IsOutsideLeash_IsPastTheLeashInAStraightLine()
        {
            Assert.That(GhostBrain.IsOutsideLeash((12, 0), (0, 0), 12), Is.False);
            Assert.That(GhostBrain.IsOutsideLeash((13, 0), (0, 0), 12), Is.True);
            Assert.That(GhostBrain.IsOutsideLeash((9, 9), (0, 0), 12), Is.True);
        }

        [Test]
        public void UpdateAttention_GivesUpThenRests()
        {
            var ghost = MakeGhost(0, 0f, 0f, eGhostBehavior.Sprinter);
            ghost.CurrentTarget = MakePac(1, 1f, 0f);

            Assert.That(GhostBrain.UpdateAttention(ghost, GhostBrain.ATTENTION_MS - 1), Is.True);
            Assert.That(GhostBrain.UpdateAttention(ghost, 1), Is.False);
            Assert.That(ghost.RestMs, Is.EqualTo(GhostBrain.REST_MS));
            Assert.That(GhostBrain.UpdateAttention(ghost, GhostBrain.REST_MS / 2), Is.False);
            Assert.That(GhostBrain.UpdateAttention(ghost, GhostBrain.REST_MS / 2), Is.False);
            Assert.That(GhostBrain.UpdateAttention(ghost, 1), Is.True);
        }

        [Test]
        public void UpdateAttention_ForgetsTimeSpentWithNoTarget()
        {
            var ghost = MakeGhost(0, 0f, 0f, eGhostBehavior.Sprinter);
            ghost.CurrentTarget = MakePac(1, 1f, 0f);
            GhostBrain.UpdateAttention(ghost, GhostBrain.ATTENTION_MS - 1);

            ghost.CurrentTarget = null;
            GhostBrain.UpdateAttention(ghost, 1);

            Assert.That(ghost.ChaseMs, Is.Zero);
        }

        [Test]
        public void IsJunction_NeedsThreeWays()
        {
            var map = MazeMovementTests.MakeMap(CROSS);

            Assert.That(GhostBrain.IsJunction(map, 3, 3), Is.True);
            Assert.That(GhostBrain.IsJunction(map, 2, 3), Is.False);
            Assert.That(GhostBrain.IsJunction(map, 1, 3), Is.False);
        }

        [Test]
        public void FindLurkJunction_FindsTheNearestJunctionFarEnoughAway()
        {
            var map = MazeMovementTests.MakeMap(CROSS);

            Assert.That(GhostBrain.FindLurkJunction(map, 1, 3, 0), Is.EqualTo((3, 3)));
            Assert.That(GhostBrain.FindLurkJunction(map, 1, 3, 2), Is.EqualTo((3, 3)));
            Assert.That(GhostBrain.FindLurkJunction(map, 1, 3, 3), Is.Null);
        }

        [Test]
        public void FindLurkJunction_SkipsTheGhostGenerator()
        {
            var map = MazeMovementTests.MakeMap(CROSS);
            map[3, 3] |= eGridDef.GhostGenerator;

            Assert.That(GhostBrain.FindLurkJunction(map, 1, 3, 0), Is.Null);
        }

        [Test]
        public void GetFrenzySpeedScale_RisesWithTheLeadingScore()
        {
            Assert.That(GhostBrain.GetFrenzySpeedScale(0, 100), Is.EqualTo(GhostBrain.FRENZY_MIN_SPEED_SCALE).Within(0.0001f));
            Assert.That(GhostBrain.GetFrenzySpeedScale(50, 100), Is.EqualTo((GhostBrain.FRENZY_MIN_SPEED_SCALE + GhostBrain.FRENZY_MAX_SPEED_SCALE) * 0.5f).Within(0.0001f));
            Assert.That(GhostBrain.GetFrenzySpeedScale(100, 100), Is.EqualTo(GhostBrain.FRENZY_MAX_SPEED_SCALE).Within(0.0001f));
            Assert.That(GhostBrain.GetFrenzySpeedScale(500, 100), Is.EqualTo(GhostBrain.FRENZY_MAX_SPEED_SCALE).Within(0.0001f));
        }

        [Test]
        public void RecordTargetCell_SkipsTheSameCellTwice()
        {
            var history = new Queue<(int Ms, int X, int Y)>();

            GhostBrain.RecordTargetCell(history, 0, (1, 1));
            GhostBrain.RecordTargetCell(history, 100, (1, 1));
            GhostBrain.RecordTargetCell(history, 200, (2, 1));

            Assert.That(history, Is.EqualTo(new[] { (0, 1, 1), (200, 2, 1) }));
        }

        [Test]
        public void GetLaggedCell_IsWhereTheTargetWasLagMsAgo()
        {
            var history = new Queue<(int Ms, int X, int Y)>([(0, 1, 1), (1000, 2, 1), (2000, 3, 1), (4000, 4, 1)]);

            Assert.That(GhostBrain.GetLaggedCell(history, 4500, 3000), Is.EqualTo((2, 1)));
            Assert.That(history, Has.Count.EqualTo(3), "older cells are dropped");
            Assert.That(GhostBrain.GetLaggedCell(history, 7500, 3000), Is.EqualTo((4, 1)));
        }

        [Test]
        public void GetLaggedCell_UsesTheOldestWhenNothingIsOldEnough()
        {
            var history = new Queue<(int Ms, int X, int Y)>([(4000, 4, 1), (4200, 5, 1)]);

            Assert.That(GhostBrain.GetLaggedCell(history, 4500, 3000), Is.EqualTo((4, 1)));
            Assert.That(GhostBrain.GetLaggedCell(new Queue<(int Ms, int X, int Y)>(), 0, 3000), Is.Null);
        }

        [Test]
        public void ChooseStraightLineDirection_PointsAtTheTargetIgnoringWalls()
        {
            var map = MazeMovementTests.MakeMap(CROSS);
            var random = new Random(1);

            Assert.That(GhostBrain.ChooseStraightLineDirection(map, 3, 3, eFacing.Right, (4, 5), random), Is.EqualTo(eFacing.Down));
            Assert.That(GhostBrain.ChooseStraightLineDirection(map, 3, 3, eFacing.Right, (3, 3), random), Is.Null);
        }

        [Test]
        public void ChooseStraightLineDirection_TurnsBackOnlyWhenItMust()
        {
            var map = MazeMovementTests.MakeMap(CROSS);

            // the target is behind, but turning back isn't allowed while there's another way
            Assert.That(GhostBrain.ChooseStraightLineDirection(map, 3, 3, eFacing.Right, (1, 3), new Random(1)), Is.Not.EqualTo(eFacing.Left));
            Assert.That(GhostBrain.ChooseStraightLineDirection(map, 5, 3, eFacing.Right, (1, 3), new Random(1)), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void IsFacedBy_SeesDownAClearStraightLine()
        {
            var map = MazeMovementTests.MakeMap(CROSS);

            Assert.That(GhostBrain.IsFacedBy(map, 1, 3, eFacing.Right, 4, 3, 10), Is.True);
            Assert.That(GhostBrain.IsFacedBy(map, 1, 3, eFacing.Right, 4, 3, 2), Is.False, "too far");
            Assert.That(GhostBrain.IsFacedBy(map, 1, 3, eFacing.Left, 4, 3, 10), Is.False, "facing away");
            Assert.That(GhostBrain.IsFacedBy(map, 1, 3, eFacing.Right, 3, 1, 10), Is.False, "round a corner");
            Assert.That(GhostBrain.IsFacedBy(map, 3, 3, eFacing.Up, 3, 3, 10), Is.True, "same cell");
        }

        [Test]
        public void GetMimicDirection_CopiesThePacWhenItCan()
        {
            var map = MazeMovementTests.MakeMap(CROSS);
            var random = new Random(1);

            Assert.That(GhostBrain.GetMimicDirection(map, 3, 3, eFacing.Right, eFacing.Up, random), Is.EqualTo(eFacing.Up));
            Assert.That(GhostBrain.GetMimicDirection(map, 1, 3, eFacing.Left, eFacing.Up, random), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void IsNapping_NapsAtTheEndOfEachCycle()
        {
            var awake = GhostBrain.NAP_CYCLE_MS - GhostBrain.NAP_MS;

            Assert.That(GhostBrain.IsNapping(0), Is.False);
            Assert.That(GhostBrain.IsNapping(awake - 1), Is.False);
            Assert.That(GhostBrain.IsNapping(awake), Is.True);
            Assert.That(GhostBrain.IsNapping(GhostBrain.NAP_CYCLE_MS - 1), Is.True);
            Assert.That(GhostBrain.IsNapping(GhostBrain.NAP_CYCLE_MS), Is.False);
            Assert.That(GhostBrain.IsNapping(GhostBrain.NAP_CYCLE_MS + awake), Is.True);
        }

        // ---- on the server ----

        private static GameServer MakeServer(int ghosts)
        {
            var settings = new ServerSettings
            {
                ServerName = "Ghosts",
                TargetFps = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = ghosts, MinPlayers = 0 },
            };

            return new GameServer(settings, []);
        }

        private static Player Join(GameServer server, int id)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = eTeamPreference.Red });
            return server.Players.Single(p => p.Connection == connection).Player;
        }

        [Test]
        public void KillingAGhost_GivesItAGrudgeThatEndsWhenItGetsTheKiller()
        {
            var config = new GameConfiguration();
            var server = MakeServer(1);
            var red = Join(server, 1);
            var ghost = server.Ghosts[0];
            red.Location = ghost.Location;
            red.Size = config.PacMaxSize;
            red.PowerUpEffect = ePowerUpEffect.PowerPill;
            red.PowerUpMs = config.PowerUpDurationMs;

            server.UpdateGhosts(0f, 0);
            Assert.That(ghost.GrudgeId, Is.EqualTo(red.Id));

            server.UpdateGhosts(0f, config.GhostRegenTimeMs);
            red.PowerUpEffect = ePowerUpEffect.None;
            red.PowerUpMs = 0;
            red.Size = PacRules.START_SIZE;
            red.Location = ghost.Location;
            server.UpdateGhosts(0f, 0);

            Assert.That(red.Alive, Is.False);
            Assert.That(ghost.GrudgeId, Is.EqualTo(Ghost.NO_GRUDGE));
        }

        [Test]
        public void EveryPersonality_StaysOnOpenCellsInALongGame()
        {
            var server = MakeServer(GhostPersonalities.ALL.Count);
            for (var id = 1; id <= 4; id++)
                Join(server, id);

            for (var tick = 0; tick < 2000; tick++)
            {
                server.Update(33);

                foreach (var ghost in server.Ghosts.Where(g => g.Alive))
                {
                    var (x, y) = MazeMovement.GetCell(ghost.Location);
                    Assert.That(server.Map[x, y] & MazeMovement.GHOST_BLOCKED, Is.EqualTo((eGridDef)0), $"{ghost.Name} tick {tick}");
                }
            }
        }
    }
}
