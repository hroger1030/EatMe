using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class GameServerTests
    {
        private const int CLIENT_UPDATES_PER_SECOND = 30;

        private static GameServer MakeServer(int maxPlayers = 128)
        {
            var settings = new ServerSettings
            {
                ServerName = "Test Server",
                ClientUpdatesPerSecond = CLIENT_UPDATES_PER_SECOND,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { MaxPlayers = maxPlayers },
            };

            return new GameServer(settings, ["Line one", "Line two"]);
        }

        private static FakeConnection Join(GameServer server, int id, string name, eTeamPreference team = eTeamPreference.Red)
        {
            var connection = new FakeConnection(id);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));
            server.HandleMessage(connection, new HelloMessage { PlayerName = name, PreferredTeam = team });
            return connection;
        }

        [Test]
        public void Hello_Accepted_SendsWelcomeHighScoresLevelStartAndPlayerJoined()
        {
            var server = MakeServer();

            var connection = Join(server, 1, "Pac");

            Assert.That(connection.Received[0], Is.TypeOf<WelcomeMessage>());
            Assert.That(connection.Received[1], Is.TypeOf<HighScoresMessage>());
            Assert.That(connection.Received[2], Is.TypeOf<LevelStartMessage>());
            Assert.That(connection.Received[3], Is.TypeOf<PlayerJoinedMessage>());

            var welcome = (WelcomeMessage)connection.Received[0];
            Assert.That(welcome.PlayerId, Is.EqualTo(1));
            Assert.That(welcome.Team, Is.EqualTo(eTeam.Red));
            Assert.That(welcome.ServerName, Is.EqualTo("Test Server"));
            Assert.That(welcome.LoadingScreenTips, Is.EqualTo(new[] { "Line one", "Line two" }));
            Assert.That(welcome.MessageOfTheDay, Is.EqualTo(Const.MOTD_DEFAULT));

            var levelStart = (LevelStartMessage)connection.Received[2];
            Assert.That(levelStart.Level, Is.EqualTo(1));

            Assert.That(server.PlayerCount, Is.EqualTo(1));
            Assert.That(connection.Closed, Is.False);
        }

        [Test]
        public void Hello_SecondPlayer_LearnsAboutFirstAndFirstLearnsAboutSecond()
        {
            var server = MakeServer();
            var first = Join(server, 1, "First");

            var second = Join(server, 2, "Second");

            Assert.That(second.ReceivedOf<PlayerJoinedMessage>().Select(m => m.Name), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(first.ReceivedOf<PlayerJoinedMessage>().Select(m => m.Name), Is.EqualTo(new[] { "First", "Second" }));
        }

        [Test]
        public void Hello_PacShape_SentToEveryPlayer()
        {
            var server = MakeServer();
            var first = new FakeConnection(1);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, first));
            server.HandleMessage(first, new HelloMessage { PlayerName = "First", PacShape = ePacShape.Doc });

            var second = Join(server, 2, "Second");

            Assert.That(second.ReceivedOf<PlayerJoinedMessage>().Select(m => m.PacShape), Is.EqualTo(new[] { ePacShape.Doc, PacShapes.DEFAULT }));
            Assert.That(first.ReceivedOf<PlayerJoinedMessage>().Select(m => m.PacShape), Is.EqualTo(new[] { ePacShape.Doc, PacShapes.DEFAULT }));
        }

        [Test]
        public void Hello_BadPacShape_GetsDefault()
        {
            var server = MakeServer();
            var connection = new FakeConnection(1);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));

            server.HandleMessage(connection, new HelloMessage { PlayerName = "Pac", PacShape = (ePacShape)200 });

            Assert.That(connection.ReceivedOf<PlayerJoinedMessage>().Single().PacShape, Is.EqualTo(PacShapes.DEFAULT));
        }

        [Test]
        public void Hello_PreferredTeamFull_BalancesToOtherTeam()
        {
            var server = MakeServer();
            Join(server, 1, "RedOne", eTeamPreference.Red);

            var second = Join(server, 2, "RedTwo", eTeamPreference.Red);

            Assert.That(second.ReceivedOf<WelcomeMessage>()[0].Team, Is.EqualTo(eTeam.Blue));
        }

        [Test]
        public void Hello_Either_JoinsSmallerTeam()
        {
            var server = MakeServer();
            Join(server, 1, "RedOne", eTeamPreference.Red);

            var second = Join(server, 2, "Any", eTeamPreference.Either);

            Assert.That(second.ReceivedOf<WelcomeMessage>()[0].Team, Is.EqualTo(eTeam.Blue));
        }

        [TestCase(eTeamPreference.Either)]
        [TestCase(eTeamPreference.Red)]
        [TestCase(eTeamPreference.Blue)]
        public void Hello_StoresPreferredTeam(eTeamPreference preferred)
        {
            var server = MakeServer();

            var connection = Join(server, 1, "Pac", preferred);

            Assert.That(server.Players.Single(p => p.Connection == connection).PreferredTeam, Is.EqualTo(preferred));
        }

        [Test]
        public void Hello_WrongClientVersion_Rejected()
        {
            var server = MakeServer();
            var connection = new FakeConnection(1);

            server.HandleMessage(connection, new HelloMessage { ClientVersion = NetworkConstants.APP_VERSION + 0.1f, PlayerName = "Pac" });

            Assert.That(connection.ReceivedOf<RejectMessage>()[0].Reason, Is.EqualTo(eRejectReason.VersionMismatch));
            Assert.That(connection.ClosedAfterSending, Is.True);
            Assert.That(server.PlayerCount, Is.Zero);
        }

        [TestCase("")]
        [TestCase("TwentyOneCharacterNam")]
        [TestCase("Bad Name")]
        public void Hello_InvalidName_Rejected(string name)
        {
            var server = MakeServer();

            var connection = Join(server, 1, name);

            Assert.That(connection.ReceivedOf<RejectMessage>()[0].Reason, Is.EqualTo(eRejectReason.InvalidName));
            Assert.That(server.PlayerCount, Is.Zero);
        }

        [Test]
        public void Hello_ServerFull_Rejected()
        {
            var server = MakeServer(maxPlayers: 1);
            Join(server, 1, "First");

            var second = Join(server, 2, "Second");

            Assert.That(second.ReceivedOf<RejectMessage>()[0].Reason, Is.EqualTo(eRejectReason.ServerFull));
            Assert.That(server.PlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void Message_BeforeHello_ClosesConnection()
        {
            var server = MakeServer();
            var connection = new FakeConnection(1);

            server.HandleMessage(connection, new InputMessage { Direction = eDirection.Up });

            Assert.That(connection.Closed, Is.True);
        }

        [Test]
        public void Message_ServerOnlyType_ClosesConnection()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");

            server.HandleMessage(connection, new SnapshotMessage());

            Assert.That(connection.Closed, Is.True);
        }

        [Test]
        public void Input_NewerSequence_Replaces_OlderIgnored()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");

            server.HandleMessage(connection, new InputMessage { Sequence = 5, Direction = eDirection.Up });
            server.HandleMessage(connection, new InputMessage { Sequence = 3, Direction = eDirection.Down });

            Assert.That(server.Players.Single().Input, Is.EqualTo(eDirection.Up));
        }

        [Test]
        public void Ping_AnsweredWithPong()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");

            server.HandleMessage(connection, new PingMessage { ClientTime = 4242 });

            Assert.That(connection.ReceivedOf<PongMessage>()[0].ClientTime, Is.EqualTo(4242));
        }

        [Test]
        public void Disconnect_TellsOtherPlayers()
        {
            var server = MakeServer();
            var first = Join(server, 1, "First");
            var second = Join(server, 2, "Second");

            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, second));

            Assert.That(first.ReceivedOf<PlayerLeftMessage>()[0].PlayerId, Is.EqualTo(2));
            Assert.That(server.PlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void Leave_RemovesPlayerAndCloses()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");

            server.HandleMessage(connection, new LeaveMessage());

            Assert.That(server.PlayerCount, Is.Zero);
            Assert.That(connection.Closed, Is.True);
        }

        [Test]
        public void Update_SendsSnapshotsAtClientUpdatesPerSecond()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");

            // one second at 50 ticks a second
            for (var i = 0; i < 50; i++)
                server.Update(20);

            Assert.That(connection.ReceivedOf<SnapshotMessage>().Count, Is.InRange(CLIENT_UPDATES_PER_SECOND - 1, CLIENT_UPDATES_PER_SECOND));
        }

        [Test]
        public void Snapshot_PlacesPacsOnTheirTeamsSpawnPoints()
        {
            var server = MakeServer();
            Join(server, 1, "RedPac", eTeamPreference.Red);
            Join(server, 2, "BluePac", eTeamPreference.Blue);

            foreach (var pac in server.BuildSnapshot().Pacs)
            {
                var cell = server.Map[(int)pac.X, (int)pac.Y];
                var spawnFlag = pac.Team == eTeam.Red ? eGridDef.SpawnPointRed : eGridDef.SpawnPointBlue;

                Assert.That(cell.HasFlag(spawnFlag), Is.True, pac.Team.ToString());
                Assert.That(pac.Alive, Is.True);
                Assert.That(pac.Size, Is.EqualTo(GameServer.PAC_START_SIZE));
            }
        }

        [Test]
        public void StartLevel_TellsEveryPlayer()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");

            server.StartLevel(2);

            var levelStarts = connection.ReceivedOf<LevelStartMessage>();
            Assert.That(levelStarts.Last().Level, Is.EqualTo(2));
        }

        [Test]
        public void DiscoveryResponse_ReportsPlayerCount()
        {
            var server = MakeServer(maxPlayers: 16);
            Join(server, 1, "Pac");

            var response = server.BuildDiscoveryResponse();

            Assert.That(response.ServerName, Is.EqualTo("Test Server"));
            Assert.That(response.PlayerCount, Is.EqualTo(1));
            Assert.That(response.MaxPlayers, Is.EqualTo(16));
        }
    }
}
