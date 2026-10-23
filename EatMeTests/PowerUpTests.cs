using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    public class PowerUpTests
    {
        /// <summary>
        /// Power ups move at PowerUpSpeed; the default.
        /// </summary>
        private static readonly float SPEED = new GameConfiguration().PowerUpSpeed;

        /// <summary>
        /// A corridor whose open left end, (0, 1), is a warp exit, with a team spawn tile in the way at (3, 1).
        /// </summary>
        private static readonly string[] CORRIDOR =
        [
            "#######",
            ".....##",
            "#######",
        ];

        /// <summary>
        /// The straight way left from (4, 1) is walled at (3, 1), so a power up must go round by row 3: ten cells to (0, 1).
        /// </summary>
        private static readonly string[] DETOUR =
        [
            "#######",
            "...#..#",
            "#.###.#",
            "#.....#",
            "#######",
        ];

        private static GameServer MakeServer(int spawnTimer = int.MaxValue)
        {
            var settings = new ServerSettings
            {
                ServerName = "Power ups",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { PowerUpSpawnTimerMs = spawnTimer },
            };

            return ServerTestHelper.SkipCountdown(new GameServer(settings, []));
        }

        private static (Player Player, FakeConnection Connection) Join(GameServer server, int id)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = eTeamPreference.Red });
            return (server.Players.Single(p => p.Connection == connection).Player, connection);
        }

        private static PowerUp MakePowerUp(int x, int y, (int X, int Y) exit)
        {
            return new PowerUp { Item = ePowerUp.Cherry, Exit = exit, Location = new Point2(x + 0.5f, y + 0.5f), Facing = eFacing.Up, Alive = true };
        }

        /// <summary>
        /// Moves a power up a tick at a time until it reaches its exit, checking it never enters a wall. Returns the ticks taken.
        /// </summary>
        private static int MoveToExit(eGridDef[,] map, PowerUp powerUp, int maxTicks = 10_000)
        {
            var paths = new PathMap();
            paths.Fill(map, powerUp.Exit.X, powerUp.Exit.Y, blocked: PowerUpRules.BLOCKED);

            for (var tick = 1; tick <= maxTicks; tick++)
            {
                if (PowerUpRules.Move(map, powerUp, paths, SPEED / 60f))
                    return tick;

                var (x, y) = MazeMovement.GetCell(powerUp.Location);
                Assert.That(map[x, y].HasFlag(eGridDef.Wall), Is.False, $"in a wall at ({x}, {y})");
            }

            Assert.Fail("never reached its exit");
            return maxTicks;
        }

        [Test]
        public void GetHoverOffset_IsASineWaveWithinTheHoverHeight()
        {
            Assert.That(PowerUpRules.GetHoverOffset(TimeSpan.Zero, 0), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(PowerUpRules.GetHoverOffset(TimeSpan.FromMilliseconds(PowerUpRules.HOVER_PERIOD_MS * 0.25), 0), Is.EqualTo(PowerUpRules.HOVER_CELLS).Within(0.0001f));
            Assert.That(PowerUpRules.GetHoverOffset(TimeSpan.FromMilliseconds(PowerUpRules.HOVER_PERIOD_MS * 0.75), 0), Is.EqualTo(-PowerUpRules.HOVER_CELLS).Within(0.0001f));

            for (var t = TimeSpan.Zero; t < TimeSpan.FromSeconds(5); t += TimeSpan.FromMilliseconds(10))
                Assert.That(Math.Abs(PowerUpRules.GetHoverOffset(t, 3)), Is.LessThanOrEqualTo(PowerUpRules.HOVER_CELLS + 0.0001f));
        }

        [Test]
        public void GetShadowAlpha_DarkestAtCentre_ClearOutsideTheOval()
        {
            Assert.That(PowerUpRules.GetShadowAlpha(31, 11, 64, 24), Is.EqualTo(PowerUpRules.SHADOW_ALPHA).Within(0.01f));
            Assert.That(PowerUpRules.GetShadowAlpha(0, 0, 64, 24), Is.EqualTo(0f));
            Assert.That(PowerUpRules.GetShadowAlpha(63, 23, 64, 24), Is.EqualTo(0f));
            Assert.That(PowerUpRules.GetShadowAlpha(48, 11, 64, 24), Is.LessThan(PowerUpRules.GetShadowAlpha(36, 11, 64, 24)));
        }

        [Test]
        public void GetHoverOffset_DifferentIds_BobOutOfStep()
        {
            Assert.That(PowerUpRules.GetHoverOffset(TimeSpan.FromSeconds(1), 1), Is.Not.EqualTo(PowerUpRules.GetHoverOffset(TimeSpan.FromSeconds(1), 2)).Within(0.001f));
        }

        [Test]
        public void PickItem_PicksOnlyFromTheSet_AndEveryItemInIt()
        {
            var items = new[] { ePowerUp.Cherry, ePowerUp.Triangleforce, ePowerUp.MonsterBall };
            var random = new Random(5);
            var seen = new HashSet<ePowerUp>();

            for (var i = 0; i < 200; i++)
            {
                var item = PowerUpRules.PickItem(items, random);
                Assert.That(items, Does.Contain(item), item.ToString());
                seen.Add(item);
            }

            Assert.That(seen, Is.EquivalentTo(items));
            Assert.That(PowerUpRules.PickItem([], random), Is.EqualTo(ePowerUp.None));
        }

        [TestCase(0, 29999, 30000, 0)]
        [TestCase(29999, 30000, 30000, 1)]
        [TestCase(0, 90000, 30000, 3)]
        [TestCase(30000, 59999, 30000, 0)]
        [TestCase(0, 90000, 0, 0)]
        [TestCase(0, 90000, -5, 0)]
        public void GetSpawnsDue_OnePerMultipleOfTheInterval(int before, int after, int interval, int expected)
        {
            Assert.That(PowerUpRules.GetSpawnsDue(before, after, interval), Is.EqualTo(expected));
        }

        [Test]
        public void IsTouching_PacOverlappingPowerUp()
        {
            Assert.That(PowerUpRules.IsTouching(new Point2(5.5f, 5.5f), PacRules.START_SIZE, new Point2(6.2f, 5.5f)), Is.True);
            Assert.That(PowerUpRules.IsTouching(new Point2(5.5f, 5.5f), PacRules.START_SIZE, new Point2(8.5f, 5.5f)), Is.False);
        }

        [Test]
        public void Move_ReachesItsExit_AcrossSpawnTiles()
        {
            var map = MazeMovementTests.MakeMap(CORRIDOR);
            map[3, 1] |= eGridDef.SpawnPointRed;
            map[2, 1] |= eGridDef.SpawnPointBlue;
            var powerUp = MakePowerUp(4, 1, (0, 1));

            var ticks = MoveToExit(map, powerUp);

            // four cells at power up speed, 60 ticks a second
            Assert.That(ticks, Is.EqualTo(4f / SPEED * 60f).Within(2));
        }

        [Test]
        public void Move_GoesRoundWalls()
        {
            var map = MazeMovementTests.MakeMap(DETOUR);
            var powerUp = MakePowerUp(4, 1, (0, 1));

            var ticks = MoveToExit(map, powerUp);

            // ten cells at power up speed, 60 ticks a second
            Assert.That(ticks, Is.EqualTo(10f / SPEED * 60f).Within(2));
        }

        [Test]
        public void FindExits_FindsTheWarpCells()
        {
            var map = MazeMovementTests.MakeMap(CORRIDOR);

            Assert.That(PowerUpRules.FindExits(map), Is.EqualTo(new List<(int X, int Y)> { (0, 1) }));
        }

        [Test]
        public void Server_FindsFourSpawnPoints()
        {
            var server = MakeServer();

            Assert.That(server.PowerUpSpawns, Has.Count.EqualTo(4));
        }

        [Test]
        public void SpawnPowerUp_OnASpawnPoint_HeadingForAnExit_WithAnyItem()
        {
            var server = MakeServer();

            for (var i = 0; i < 20; i++)
            {
                var powerUp = server.SpawnPowerUp();
                var cell = MazeMovement.GetCell(powerUp.Location);

                Assert.That(server.PowerUpSpawns, Does.Contain(cell));
                Assert.That(MazeMovement.IsWarpCell(server.Map, powerUp.Exit.X, powerUp.Exit.Y), Is.True);
                Assert.That(PowerUpRules.SPAWNABLE, Does.Contain(powerUp.Item));
            }

            Assert.That(server.PowerUps.Select(p => p.Id), Is.Unique);
        }

        [Test]
        public void UpdatePowerUps_SpawnsOneEachTimerInterval()
        {
            var server = MakeServer(spawnTimer: 30000);

            server.UpdatePowerUps(TimeSpan.FromMilliseconds(29999));
            Assert.That(server.PowerUps, Is.Empty);

            server.UpdatePowerUps(TimeSpan.FromMilliseconds(1));
            Assert.That(server.PowerUps, Has.Count.EqualTo(1));

            server.UpdatePowerUps(TimeSpan.FromMilliseconds(30000));
            Assert.That(server.PowerUps, Has.Count.EqualTo(2));
        }

        [Test]
        public void UpdatePowerUps_TimerZero_NeverSpawns()
        {
            var server = MakeServer(spawnTimer: 0);

            server.UpdatePowerUps(TimeSpan.FromMilliseconds(1_000_000));

            Assert.That(server.PowerUps, Is.Empty);
        }

        [Test]
        public void StartLevel_ClearsPowerUpsAndRestartsTheTimer()
        {
            var server = MakeServer(spawnTimer: 30000);
            server.UpdatePowerUps(TimeSpan.FromMilliseconds(45000));

            server.StartLevel(2);

            Assert.That(server.PowerUps, Is.Empty);
            Assert.That(server.PowerUpTimerMs, Is.EqualTo(0));
        }

        [Test]
        public void UpdatePowerUps_ReachingItsExit_LeavesTheLevel()
        {
            var server = MakeServer();
            var powerUp = server.SpawnPowerUp();

            for (var tick = 0; tick < 100_000 && server.PowerUps.Count > 0; tick++)
            {
                server.UpdatePowerUps(TimeSpan.FromMilliseconds(16));

                if (server.PowerUps.Count > 0)
                {
                    var (x, y) = MazeMovement.GetCell(powerUp.Location);
                    Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.False);
                }
            }

            Assert.That(server.PowerUps, Is.Empty);
        }

        [Test]
        public void UpdatePowerUps_PacTouchingIt_EatsIt()
        {
            var server = MakeServer();
            var (player, connection) = Join(server, 1);
            var powerUp = server.SpawnPowerUp();
            player.Location = powerUp.Location;

            server.UpdatePowerUps(TimeSpan.Zero);

            Assert.That(server.PowerUps, Is.Empty);
            var eaten = connection.Received.OfType<GameEventMessage>().Single(e => e.EventType == eGameEvent.PowerUpEaten);
            Assert.That(eaten.ActorId, Is.EqualTo(player.Id));
            Assert.That(eaten.TargetId, Is.EqualTo(powerUp.Id));
        }

        [Test]
        public void UpdatePowerUps_EatingOne_CountsItOnThePlayer()
        {
            var server = MakeServer();
            var (player, _) = Join(server, 1);
            var powerUp = server.SpawnPowerUp();
            player.Location = powerUp.Location;

            server.UpdatePowerUps(TimeSpan.Zero);

            Assert.That(player.PowerUpsEaten, Is.EqualTo(1));
        }

        [Test]
        public void UpdatePowerUps_DeadPac_DoesNotEatIt()
        {
            var server = MakeServer();
            var (player, _) = Join(server, 1);
            var powerUp = server.SpawnPowerUp();
            player.Location = powerUp.Location;
            player.Alive = false;

            server.UpdatePowerUps(TimeSpan.Zero);

            Assert.That(server.PowerUps, Has.Count.EqualTo(1));
        }

        [Test]
        public void Ghosts_IgnorePowerUps()
        {
            var server = MakeServer();
            var powerUp = server.SpawnPowerUp();
            var ghost = server.Ghosts[0];
            ghost.Location = powerUp.Location;

            server.UpdateGhosts(TimeSpan.Zero);
            server.UpdatePowerUps(TimeSpan.Zero);

            Assert.That(server.PowerUps, Has.Count.EqualTo(1));
            Assert.That(ghost.Alive, Is.True);
        }

        [Test]
        public void Snapshot_IncludesPowerUps()
        {
            var server = MakeServer();
            var powerUp = server.SpawnPowerUp();

            var snapshot = server.BuildSnapshot();

            Assert.That(snapshot.PowerUps, Is.EqualTo(new List<PowerUpState> { powerUp.ToState() }));
        }
    }
}
