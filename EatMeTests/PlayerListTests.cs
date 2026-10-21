using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class PlayerListTests
    {
        private static GameServer MakeServer()
        {
            var settings = new ServerSettings
            {
                ServerName = "Test Server",
                TargetFps = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { MaxPlayers = 16 },
            };

            return new GameServer(settings, []);
        }

        private static FakeConnection Join(GameServer server, int id, string name, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));
            server.HandleMessage(connection, new HelloMessage { PlayerName = name, PreferredTeam = team });
            return connection;
        }

        [Test]
        public void Entry_Bot_HasNoPing()
        {
            var entry = new PlayerListEntry(1000, "Blinky", eTeam.Blue, true, 80);

            Assert.That(entry.PingMs, Is.EqualTo(PlayerListEntry.NO_PING));
        }

        [Test]
        public void Entry_NegativePing_IsNoPing()
        {
            var entry = new PlayerListEntry(1, "Pac", eTeam.Red, false, -5);

            Assert.That(entry.PingMs, Is.EqualTo(PlayerListEntry.NO_PING));
        }

        [Test]
        public void SortTeam_KeepsOneTeam_Alphabetical_IgnoringCase()
        {
            var entries = new[]
            {
                new PlayerListEntry(1, "zed", eTeam.Red, false, 10),
                new PlayerListEntry(2, "Amy", eTeam.Blue, false, 10),
                new PlayerListEntry(3, "bob", eTeam.Red, true, -1),
                new PlayerListEntry(4, "Al", eTeam.Red, false, 10),
            };

            var red = PlayerListRules.SortTeam(entries, eTeam.Red);

            Assert.That(red.Select(e => e.Name), Is.EqualTo(new[] { "Al", "bob", "zed" }));
        }

        [Test]
        public void SortTeam_SameName_OrderedById()
        {
            var entries = new[]
            {
                new PlayerListEntry(9, "Pac", eTeam.Blue, false, 10),
                new PlayerListEntry(2, "pac", eTeam.Blue, false, 10),
            };

            var blue = PlayerListRules.SortTeam(entries, eTeam.Blue);

            Assert.That(blue.Select(e => e.PlayerId), Is.EqualTo(new[] { 2, 9 }));
        }

        [Test]
        public void GetPingText_BotUnknownAndMeasured()
        {
            Assert.That(PlayerListRules.GetPingText(new PlayerListEntry(1000, "Bot", eTeam.Red, true, -1)), Is.EqualTo("<bot>"));
            Assert.That(PlayerListRules.GetPingText(new PlayerListEntry(1, "Pac", eTeam.Red, false, -1)), Is.EqualTo(Const.STRING_PING_UNKNOWN));
            Assert.That(PlayerListRules.GetPingText(new PlayerListEntry(1, "Pac", eTeam.Red, false, 57)), Is.EqualTo("57 ms"));
        }

        [TestCase(0, 10, 1)]
        [TestCase(3, 10, 1)]
        [TestCase(10, 10, 1)]
        [TestCase(11, 10, 2)]
        [TestCase(20, 10, 2)]
        [TestCase(21, 10, 3)]
        [TestCase(5, 0, 5)]
        public void GetColumnCount_SplitsWhenTooLong(int count, int maxRows, int expected)
        {
            Assert.That(PlayerListRules.GetColumnCount(count, maxRows), Is.EqualTo(expected));
        }

        [TestCase(0, 1, 0)]
        [TestCase(7, 1, 7)]
        [TestCase(11, 2, 6)]
        [TestCase(12, 2, 6)]
        [TestCase(21, 3, 7)]
        public void GetRowCount_FillsColumnsEvenly(int count, int columns, int expected)
        {
            Assert.That(PlayerListRules.GetRowCount(count, columns), Is.EqualTo(expected));
        }

        [Test]
        public void GetHeading_TeamAndCount()
        {
            Assert.That(PlayerListRules.GetHeading(eTeam.Blue, 4), Is.EqualTo("Blue (4)"));
            Assert.That(PlayerListRules.GetHeading(eTeam.Red, 0), Is.EqualTo("Red (0)"));
        }

        [Test]
        public void Request_SendsListToRequesterOnly_WithBotsAndPings()
        {
            var server = MakeServer();
            var pac = Join(server, 1, "Pac", eTeamPreference.Red);
            var other = Join(server, 2, "Other", eTeamPreference.Blue);
            server.SetMinPlayers(3);

            server.HandleMessage(pac, new ServerPongMessage { ServerTime = server.ClockMs - 40 });
            server.HandleMessage(pac, new PlayerListRequestMessage());

            Assert.That(other.ReceivedOf<PlayerListMessage>(), Is.Empty);
            var list = pac.ReceivedOf<PlayerListMessage>().Single();
            Assert.That(list.Entries, Has.Count.EqualTo(3));

            var own = list.Entries.Single(e => e.PlayerId == 1);
            Assert.That(own.Name, Is.EqualTo("Pac"));
            Assert.That(own.Team, Is.EqualTo(eTeam.Red));
            Assert.That(own.IsBot, Is.False);
            Assert.That(own.PingMs, Is.EqualTo(40));

            // no pong from Other yet
            Assert.That(list.Entries.Single(e => e.PlayerId == 2).PingMs, Is.EqualTo(PlayerListEntry.NO_PING));

            var bot = list.Entries.Single(e => e.PlayerId >= GameServer.BOT_ID_BASE);
            Assert.That(bot.IsBot, Is.True);
            Assert.That(bot.PingMs, Is.EqualTo(PlayerListEntry.NO_PING));
        }

        [Test]
        public void ClientState_PlayerList_SortedByTeam_AndCleared()
        {
            var state = new ClientGameState();
            var message = new PlayerListMessage
            {
                Entries =
                [
                    new PlayerListEntry(1, "Zoe", eTeam.Blue, false, 30),
                    new PlayerListEntry(2, "Max", eTeam.Red, false, 20),
                    new PlayerListEntry(3, "Abe", eTeam.Blue, true, -1),
                ],
            };

            state.Apply(message, 0);

            Assert.That(state.HasPlayerList, Is.True);
            Assert.That(state.BluePlayerList.Select(e => e.Name), Is.EqualTo(new[] { "Abe", "Zoe" }));
            Assert.That(state.RedPlayerList.Select(e => e.Name), Is.EqualTo(new[] { "Max" }));

            state.ClearPlayerList();

            Assert.That(state.HasPlayerList, Is.False);
            Assert.That(state.BluePlayerList, Is.Empty);
            Assert.That(state.RedPlayerList, Is.Empty);
        }
    }
}
