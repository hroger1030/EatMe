using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    public class TurnAssistTests
    {
        /// <summary>
        /// A crossroads at (3, 2), with the corridor running left to right.
        /// </summary>
        private static readonly string[] CROSS =
        [
            "#######",
            "###.###",
            "#.....#",
            "###.###",
            "#######",
        ];

        /// <summary>
        /// A warp tunnel along row 2, with a side passage up from the edge cell (0, 2).
        /// </summary>
        private static readonly string[] TUNNEL_SIDE =
        [
            "#######",
            ".######",
            ".......",
            "#######",
        ];

        private static Player MakePac(float x, float y, eFacing facing)
        {
            return new Player { Location = new Point2(x, y), Facing = facing, Alive = true };
        }

        [Test]
        public void TryLateTurn_JustPastOpenJunction_TurnsOnCentre()
        {
            var pac = MakePac(3.7f, 2.5f, eFacing.Right);

            var back = TurnAssist.TryLateTurn(MazeMovementTests.MakeMap(CROSS), pac, eFacing.Up, 0.3f);

            Assert.That(back, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Up));
            Assert.That(pac.Location.X, Is.EqualTo(3.5f));
            Assert.That(pac.Location.Y, Is.EqualTo(2.5f));
        }

        [Test]
        public void TryLateTurn_PastWindow_DoesNothing()
        {
            var pac = MakePac(3.9f, 2.5f, eFacing.Right);

            var back = TurnAssist.TryLateTurn(MazeMovementTests.MakeMap(CROSS), pac, eFacing.Up, 0.3f);

            Assert.That(back, Is.EqualTo(0f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
            Assert.That(pac.Location.X, Is.EqualTo(3.9f));
        }

        [Test]
        public void TryLateTurn_SideIsWall_DoesNothing()
        {
            var pac = MakePac(2.7f, 2.5f, eFacing.Right);

            var back = TurnAssist.TryLateTurn(MazeMovementTests.MakeMap(CROSS), pac, eFacing.Up, 0.3f);

            Assert.That(back, Is.EqualTo(0f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void TryLateTurn_SideIsBlockedSpawn_DoesNothing()
        {
            var map = MazeMovementTests.MakeMap(CROSS);
            map[3, 1] = eGridDef.SpawnPointBlue;
            var pac = MakePac(3.7f, 2.5f, eFacing.Right);

            var back = TurnAssist.TryLateTurn(map, pac, eFacing.Up, 0.3f, blocked: MazeMovement.GetPacBlocked(eTeam.Red));

            Assert.That(back, Is.EqualTo(0f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
        }

        [TestCase(eFacing.Right)]
        [TestCase(eFacing.Left)]
        public void TryLateTurn_SameWayOrBack_DoesNothing(eFacing wanted)
        {
            var pac = MakePac(3.7f, 2.5f, eFacing.Right);

            var back = TurnAssist.TryLateTurn(MazeMovementTests.MakeMap(CROSS), pac, wanted, 0.3f);

            Assert.That(back, Is.EqualTo(0f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
            Assert.That(pac.Location.X, Is.EqualTo(3.7f));
        }

        [Test]
        public void TryLateTurn_OnCentre_LeavesItToMove()
        {
            var pac = MakePac(3.5f, 2.5f, eFacing.Right);

            var back = TurnAssist.TryLateTurn(MazeMovementTests.MakeMap(CROSS), pac, eFacing.Up, 0.3f);

            Assert.That(back, Is.EqualTo(0f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void TryLateTurn_JustThroughWarp_TurnsOnEdgeCell()
        {
            // went left through (0, 2) and came out on the right edge
            var pac = MakePac(6.8f, 2.5f, eFacing.Left);

            var back = TurnAssist.TryLateTurn(MazeMovementTests.MakeMap(TUNNEL_SIDE), pac, eFacing.Up, 0.9f);

            Assert.That(back, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Up));
            Assert.That(pac.Location.X, Is.EqualTo(0.5f));
            Assert.That(pac.Location.Y, Is.EqualTo(2.5f));
        }

        [Test]
        public void GetGraceDistance_NoLag_IsBaseGrace()
        {
            Assert.That(TurnAssist.GetGraceDistance(0.3f, 6f, 0, 250), Is.EqualTo(0.3f));
        }

        [Test]
        public void GetGraceDistance_NoLagMeasured_IsBaseGrace()
        {
            Assert.That(TurnAssist.GetGraceDistance(0.3f, 6f, -1, 250), Is.EqualTo(0.3f));
        }

        [Test]
        public void GetGraceDistance_Lag_AddsDistanceCovered()
        {
            Assert.That(TurnAssist.GetGraceDistance(0.2f, 6f, 50, 250), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void GetGraceDistance_LagOverCap_UsesCap()
        {
            Assert.That(TurnAssist.GetGraceDistance(0.1f, 4f, 500, 100), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void GetGraceDistance_LagCompensationOff_IsBaseGrace()
        {
            Assert.That(TurnAssist.GetGraceDistance(0.3f, 6f, 200, 0), Is.EqualTo(0.3f));
        }

        [Test]
        public void GetGraceDistance_NeverPastMax()
        {
            Assert.That(TurnAssist.GetGraceDistance(0.3f, 6f, 250, 250), Is.EqualTo(TurnAssist.MAX_GRACE_CELLS));
        }

        [Test]
        public void GetGraceDistance_GraceOff_IsZeroWhateverTheLag()
        {
            Assert.That(TurnAssist.GetGraceDistance(0f, 6f, 200, 250), Is.EqualTo(0f));
        }

        [Test]
        public void GetWantedTurn_HeldWins()
        {
            Assert.That(TurnAssist.GetWantedTurn(eFacing.Down, eFacing.Up, 100), Is.EqualTo(eFacing.Down));
        }

        [Test]
        public void GetWantedTurn_NothingHeld_UsesBufferWhileItLasts()
        {
            Assert.That(TurnAssist.GetWantedTurn(null, eFacing.Up, 100), Is.EqualTo(eFacing.Up));
            Assert.That(TurnAssist.GetWantedTurn(null, eFacing.Up, 0), Is.Null);
        }

        [Test]
        public void LagEstimate_BeforeFirstSample_IsMinusOne()
        {
            Assert.That(new LagEstimate().SmoothedMs, Is.EqualTo(-1));
        }

        [Test]
        public void LagEstimate_FirstSample_TakenAsIs()
        {
            var lag = new LagEstimate();

            lag.AddSample(120);

            Assert.That(lag.SmoothedMs, Is.EqualTo(120));
        }

        [Test]
        public void LagEstimate_LaterSamples_MovePartWay()
        {
            var lag = new LagEstimate();
            lag.AddSample(100);

            lag.AddSample(200);

            Assert.That(lag.SmoothedMs, Is.EqualTo(100 + (int)Math.Round(100 * LagEstimate.SMOOTHING)));
        }

        [Test]
        public void LagEstimate_NegativeSample_Ignored()
        {
            var lag = new LagEstimate();
            lag.AddSample(100);

            lag.AddSample(-5);

            Assert.That(lag.SmoothedMs, Is.EqualTo(100));
        }

        // ---- on the server ----

        private static GameServer MakeServer(int turnBufferTimeMs = 400, float turnGraceDistance = 0.3f, int lagCompensationMaxMs = 250)
        {
            var settings = new ServerSettings
            {
                ServerName = "Turns",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration
                {
                    GhostCount = 0,
                    TurnBufferTimeMs = turnBufferTimeMs,
                    TurnGraceDistance = turnGraceDistance,
                    LagCompensationMaxMs = lagCompensationMaxMs,
                },
            };

            return ServerTestHelper.SkipCountdown(new GameServer(settings, []));
        }

        private static (ServerPlayer Player, FakeConnection Connection) JoinRed(GameServer server)
        {
            var connection = new FakeConnection(1);
            server.HandleMessage(connection, new HelloMessage { PlayerName = "P1", PreferredTeam = eTeamPreference.Red });
            return (server.Players.Single(p => p.Connection == connection), connection);
        }

        private static bool IsPlain(eGridDef[,] map, int x, int y)
        {
            return (map[x, y] & (eGridDef.Wall | eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue)) == 0;
        }

        /// <summary>
        /// A cell well inside the map open to its left, right and above, none of them spawn tiles.
        /// </summary>
        private static (int X, int Y) FindJunction(eGridDef[,] map)
        {
            for (var x = 3; x < map.GetLength(0) - 3; x++)
                for (var y = 3; y < map.GetLength(1) - 3; y++)
                    if (IsPlain(map, x, y) && IsPlain(map, x - 1, y) && IsPlain(map, x + 1, y) && IsPlain(map, x, y - 1))
                        return (x, y);

            throw new InvalidOperationException("No junction");
        }

        private static float GetSpeed(Player pac)
        {
            var config = new GameConfiguration();
            return PacRules.GetSpeed(config.PacSpeed, pac.Size, config.PacMaxSize);
        }

        /// <summary>
        /// Puts the pac offset cells past the junction's centre going right, with nothing held.
        /// </summary>
        private static (int X, int Y) PlaceNearJunction(GameServer server, ServerPlayer serverPlayer, float offset)
        {
            var junction = FindJunction(server.Map);
            serverPlayer.Player.Location = new Point2(junction.X + 0.5f + offset, junction.Y + 0.5f);
            serverPlayer.Player.Facing = eFacing.Right;
            serverPlayer.Input = eDirection.None;
            return junction;
        }

        [Test]
        public void Server_LatePress_TurnsAtJunctionPassed()
        {
            var server = MakeServer();
            var (player, connection) = JoinRed(server);
            var junction = PlaceNearJunction(server, player, 0.2f);
            player.StraightDistance = 1f;

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.MovePacs(TimeSpan.FromMilliseconds(1));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Up));
            Assert.That(player.Player.Location.X, Is.EqualTo(junction.X + 0.5f));
            Assert.That(player.Player.Location.Y, Is.LessThan(junction.Y + 0.5f));
        }

        [Test]
        public void Server_LatePress_NeverPutBackFurtherThanItCame()
        {
            var server = MakeServer();
            var (player, connection) = JoinRed(server);
            PlaceNearJunction(server, player, 0.2f);
            player.StraightDistance = 0.1f;

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.MovePacs(TimeSpan.FromMilliseconds(1));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Server_LateTurnsOff_LatePressMissesTurn()
        {
            var server = MakeServer(turnGraceDistance: 0f);
            var (player, connection) = JoinRed(server);
            PlaceNearJunction(server, player, 0.2f);
            player.StraightDistance = 1f;

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.MovePacs(TimeSpan.FromMilliseconds(1));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Server_NoLag_PressFarPastJunctionMisses()
        {
            var server = MakeServer();
            var (player, connection) = JoinRed(server);
            PlaceNearJunction(server, player, 0.45f);
            player.StraightDistance = 1f;

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.MovePacs(TimeSpan.FromMilliseconds(1));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Server_Lag_WidensLateTurnWindow()
        {
            var server = MakeServer();
            var (player, connection) = JoinRed(server);
            var junction = PlaceNearJunction(server, player, 0.45f);
            player.StraightDistance = 1f;

            // a 100 ms round trip
            server.HandleMessage(connection, new ServerPongMessage { ServerTime = server.ClockMs - 100 });
            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.MovePacs(TimeSpan.FromMilliseconds(1));

            Assert.That(player.Lag.SmoothedMs, Is.EqualTo(100));
            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Up));
            Assert.That(player.Player.Location.X, Is.EqualTo(junction.X + 0.5f));
        }

        [Test]
        public void Server_TapAndReleaseBeforeJunction_StillTurns()
        {
            var server = MakeServer();
            var (player, connection) = JoinRed(server);
            var junction = PlaceNearJunction(server, player, -0.4f);

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.HandleMessage(connection, new InputMessage { Sequence = 2, Direction = eDirection.None });
            server.MovePacs(TimeSpan.FromSeconds(0.6f / GetSpeed(player.Player)));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Up));
            Assert.That(player.Player.Location.X, Is.EqualTo(junction.X + 0.5f));
            Assert.That(player.BufferedTurn, Is.Null, "used up by the turn");
        }

        [Test]
        public void Server_BufferedTap_ExpiresAfterTurnBufferTime()
        {
            var server = MakeServer(turnBufferTimeMs: 100);
            var (player, connection) = JoinRed(server);
            PlaceNearJunction(server, player, -0.9f);

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.HandleMessage(connection, new InputMessage { Sequence = 2, Direction = eDirection.None });

            // 0.06 s steps: the buffer runs out after the second, before the pac reaches the junction in the third
            for (var i = 0; i < 3; i++)
                server.MovePacs(TimeSpan.FromMilliseconds(60));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Right));
            Assert.That(player.BufferedTurn, Is.Null);
        }

        [Test]
        public void Server_BufferOff_TapAndReleaseMissesTurn()
        {
            var server = MakeServer(turnBufferTimeMs: 0);
            var (player, connection) = JoinRed(server);
            PlaceNearJunction(server, player, -0.4f);

            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Up });
            server.HandleMessage(connection, new InputMessage { Sequence = 2, Direction = eDirection.None });
            server.MovePacs(TimeSpan.FromSeconds(0.6f / GetSpeed(player.Player)));

            Assert.That(player.Player.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Server_PingsEachHuman_AndPongSetsLag()
        {
            var server = MakeServer();
            var (player, connection) = JoinRed(server);

            server.Update(TimeSpan.FromMilliseconds(16));
            var pings = connection.ReceivedOf<ServerPingMessage>();
            Assert.That(pings, Has.Count.EqualTo(1));

            server.Update(TimeSpan.FromMilliseconds(50));
            server.HandleMessage(connection, new ServerPongMessage { ServerTime = pings[0].ServerTime });

            Assert.That(player.Lag.SmoothedMs, Is.EqualTo(server.ClockMs - pings[0].ServerTime));
            Assert.That(player.Lag.SmoothedMs, Is.GreaterThan(40));
        }

        [Test]
        public void Server_PingsAgainEveryInterval()
        {
            var server = MakeServer();
            var (_, connection) = JoinRed(server);

            server.Update(TimeSpan.FromMilliseconds(16));
            server.Update(TimeSpan.FromMilliseconds(LagEstimate.PING_INTERVAL_MS / 2));
            Assert.That(connection.ReceivedOf<ServerPingMessage>(), Has.Count.EqualTo(1));

            server.Update(TimeSpan.FromMilliseconds(LagEstimate.PING_INTERVAL_MS / 2));
            Assert.That(connection.ReceivedOf<ServerPingMessage>(), Has.Count.EqualTo(2));
        }
    }
}
