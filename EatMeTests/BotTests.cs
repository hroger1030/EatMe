using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class BotTests
    {
        private static GameServer MakeServer(int minPlayers, int maxPlayers = 128, List<string> botNames = null,
            bool autoBalanceTeams = false)
        {
            var settings = new ServerSettings
            {
                ServerName = "Bot Server",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = autoBalanceTeams,
                GameConfig = new GameConfiguration { MinPlayers = minPlayers, MaxPlayers = maxPlayers },
            };

            return new GameServer(settings, [], botNames: botNames);
        }

        private static FakeConnection Join(GameServer server, int id, string name, eTeamPreference team = eTeamPreference.Red)
        {
            var connection = new FakeConnection(id);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));
            server.HandleMessage(connection, new HelloMessage { PlayerName = name, PreferredTeam = team });
            return connection;
        }

        private static int CountTeam(GameServer server, eTeam team)
        {
            return server.Players.Count(p => p.Player.Team == team);
        }

        [TestCase(0, 0, 128, 0)]
        [TestCase(0, 8, 128, 8)]
        [TestCase(3, 8, 128, 5)]
        [TestCase(8, 8, 128, 0)]
        [TestCase(10, 8, 128, 0)]
        [TestCase(2, 8, 4, 2)]
        [TestCase(4, 8, 4, 0)]
        public void GetBotCount(int humans, int minPlayers, int maxPlayers, int expected)
        {
            Assert.That(BotPlanner.GetBotCount(humans, minPlayers, maxPlayers), Is.EqualTo(expected));
        }

        [TestCase(0, 0, 0, 128, 0, 0)]
        [TestCase(1, 0, 0, 128, 0, 1)]
        [TestCase(3, 0, 0, 128, 0, 3)]
        [TestCase(2, 2, 0, 128, 0, 0)]
        [TestCase(0, 0, 4, 128, 2, 2)]
        [TestCase(0, 0, 3, 128, 2, 2)]
        [TestCase(1, 0, 4, 128, 1, 2)]
        [TestCase(5, 1, 4, 128, 0, 4)]
        [TestCase(0, 0, 8, 4, 2, 2)]
        [TestCase(4, 0, 0, 4, 0, 4)]
        public void GetBalancedBotCounts(int redHumans, int blueHumans, int minPlayers, int maxPlayers, int expectedRed, int expectedBlue)
        {
            BotPlanner.GetBalancedBotCounts(redHumans, blueHumans, minPlayers, maxPlayers, out var redBots, out var blueBots);

            Assert.That(redBots, Is.EqualTo(expectedRed));
            Assert.That(blueBots, Is.EqualTo(expectedBlue));
        }

        [Test]
        public void Bots_AreGears()
        {
            var server = MakeServer(0, autoBalanceTeams: true);

            var human = Join(server, 1, "Human", eTeamPreference.Red);

            var bot = server.Players.Single(p => p.IsBot).Player;
            Assert.That(bot.PacShape, Is.EqualTo(PacShapes.BOT));
            Assert.That(human.ReceivedOf<PlayerJoinedMessage>().Single(m => m.PlayerId == bot.Id).PacShape, Is.EqualTo(ePacShape.Gears));
        }

        [Test]
        public void AutoBalance_OneHuman_BotJoinsOtherTeam()
        {
            var server = MakeServer(0, autoBalanceTeams: true);

            Join(server, 1, "Human", eTeamPreference.Red);

            Assert.That(server.BotCount, Is.EqualTo(1));
            Assert.That(server.Players.Single(p => p.IsBot).Player.Team, Is.EqualTo(eTeam.Blue));
        }

        [Test]
        public void AutoBalance_HumanJoinsBotsTeam_TakesItsPlace()
        {
            var server = MakeServer(0, autoBalanceTeams: true);
            Join(server, 1, "One", eTeamPreference.Red);

            var connection = Join(server, 2, "Two", eTeamPreference.Blue);

            Assert.That(connection.ReceivedOf<WelcomeMessage>()[0].Team, Is.EqualTo(eTeam.Blue));
            Assert.That(server.BotCount, Is.Zero);
        }

        [Test]
        public void AutoBalance_HumansLeave_BotsFillTheGap()
        {
            var server = MakeServer(0, autoBalanceTeams: true);
            var red1 = Join(server, 1, "Red1", eTeamPreference.Red);
            var red2 = Join(server, 2, "Red2", eTeamPreference.Red);
            Join(server, 3, "Red3", eTeamPreference.Red);
            Join(server, 4, "Blue1", eTeamPreference.Blue);
            Join(server, 5, "Blue2", eTeamPreference.Blue);
            Join(server, 6, "Blue3", eTeamPreference.Blue);
            Assert.That(server.BotCount, Is.Zero);

            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, red1));
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, red2));

            Assert.That(server.BotCount, Is.EqualTo(2));
            Assert.That(server.Players.Where(p => p.IsBot).All(p => p.Player.Team == eTeam.Red), Is.True);
            Assert.That(CountTeam(server, eTeam.Red), Is.EqualTo(3));
            Assert.That(CountTeam(server, eTeam.Blue), Is.EqualTo(3));
        }

        [Test]
        public void AutoBalance_MinPlayers_TeamsStayEven()
        {
            var server = MakeServer(4, autoBalanceTeams: true);
            Assert.That(server.BotCount, Is.EqualTo(4));

            for (var id = 1; id <= 5; id++)
            {
                Join(server, id, $"Human{id}", eTeamPreference.Red);
                Assert.That(CountTeam(server, eTeam.Red), Is.EqualTo(CountTeam(server, eTeam.Blue)));
            }

            // the teams are always even, so each human gets red and blue is all bots
            Assert.That(server.BotCount, Is.EqualTo(5));
            Assert.That(server.Players.Where(p => p.IsBot).All(p => p.Player.Team == eTeam.Blue), Is.True);
        }

        [Test]
        public void AutoBalanceOff_OddHumans_NoExtraBot()
        {
            var server = MakeServer(0);

            Join(server, 1, "Human", eTeamPreference.Red);

            Assert.That(server.BotCount, Is.Zero);
        }

        [TestCase(eTeamPreference.Red, 1, 1, 2, 2, eTeam.Red)]
        [TestCase(eTeamPreference.Blue, 1, 1, 2, 2, eTeam.Blue)]
        [TestCase(eTeamPreference.Red, 0, 1, 2, 2, eTeam.Blue)]
        [TestCase(eTeamPreference.Blue, 1, 0, 2, 2, eTeam.Red)]
        [TestCase(eTeamPreference.Either, 1, 1, 3, 2, eTeam.Red)]
        [TestCase(eTeamPreference.Either, 1, 1, 2, 3, eTeam.Blue)]
        public void PickTeamToRemoveFrom(eTeamPreference preferred, int redBots, int blueBots, int redCount, int blueCount, eTeam expected)
        {
            Assert.That(BotPlanner.PickTeamToRemoveFrom(preferred, redBots, blueBots, redCount, blueCount), Is.EqualTo(expected));
        }

        [Test]
        public void PickTeamToRemoveFrom_NoBots_Null()
        {
            Assert.That(BotPlanner.PickTeamToRemoveFrom(eTeamPreference.Red, 0, 0, 3, 3), Is.Null);
        }

        [Test]
        public void PickName_PicksUnusedNamesThenFallsBack()
        {
            var names = new List<string> { "Toys-R-Us", "Mr.Robotto" };
            var inUse = new HashSet<string> { "Toys-R-Us" };
            var random = new Random(1);

            Assert.That(BotNameManager.PickName(names, inUse, random), Is.EqualTo("Mr.Robotto"));

            inUse.Add("Mr.Robotto");
            Assert.That(BotNameManager.PickName(names, inUse, random), Is.EqualTo("Bot1"));

            inUse.Add("Bot1");
            Assert.That(BotNameManager.PickName(names, inUse, random), Is.EqualTo("Bot2"));
            Assert.That(BotNameManager.PickName([], new HashSet<string>(), random), Is.EqualTo("Bot1"));
        }

        [Test]
        public void NoMinPlayers_NoBots()
        {
            var server = MakeServer(0);

            Assert.That(server.BotCount, Is.Zero);
            Assert.That(server.Players, Is.Empty);
        }

        [Test]
        public void EmptyServer_FilledWithBalancedBots()
        {
            var server = MakeServer(6);

            Assert.That(server.BotCount, Is.EqualTo(6));
            Assert.That(server.PlayerCount, Is.Zero);
            Assert.That(CountTeam(server, eTeam.Red), Is.EqualTo(3));
            Assert.That(CountTeam(server, eTeam.Blue), Is.EqualTo(3));
            Assert.That(server.Players.All(p => p.IsBot && p.Player.Id >= GameServer.BOT_ID_BASE && p.Player.Alive), Is.True);
        }

        [Test]
        public void Bots_NamedFromList_NoDuplicates()
        {
            var server = MakeServer(3, botNames: ["Toys-R-Us", "Mr.Robotto"]);

            var names = server.Players.Select(p => p.Player.Name).ToList();

            Assert.That(names, Is.Unique);
            Assert.That(names, Does.Contain("Toys-R-Us"));
            Assert.That(names, Does.Contain("Mr.Robotto"));
            Assert.That(names, Does.Contain("Bot1"));
        }

        [Test]
        public void HumanJoins_BotFromPreferredTeamLeaves()
        {
            var server = MakeServer(4);

            var connection = Join(server, 1, "Human", eTeamPreference.Blue);

            Assert.That(server.PlayerCount, Is.EqualTo(1));
            Assert.That(server.BotCount, Is.EqualTo(3));
            Assert.That(connection.ReceivedOf<WelcomeMessage>()[0].Team, Is.EqualTo(eTeam.Blue));
            Assert.That(CountTeam(server, eTeam.Red), Is.EqualTo(2));
            Assert.That(CountTeam(server, eTeam.Blue), Is.EqualTo(2));

            // the new player hears about the three bots still playing, then about themselves
            Assert.That(connection.ReceivedOf<PlayerJoinedMessage>(), Has.Count.EqualTo(4));
        }

        [Test]
        public void HumanLeaves_BotTakesTheirPlace()
        {
            var server = MakeServer(4);
            var leaving = Join(server, 1, "Leaver", eTeamPreference.Red);
            var staying = Join(server, 2, "Stayer", eTeamPreference.Blue);
            Assert.That(server.BotCount, Is.EqualTo(2));

            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, leaving));

            Assert.That(server.PlayerCount, Is.EqualTo(1));
            Assert.That(server.BotCount, Is.EqualTo(3));
            Assert.That(CountTeam(server, eTeam.Red), Is.EqualTo(2));
            Assert.That(CountTeam(server, eTeam.Blue), Is.EqualTo(2));
            Assert.That(staying.ReceivedOf<PlayerLeftMessage>().Last().PlayerId, Is.EqualTo(1));
            Assert.That(staying.ReceivedOf<PlayerJoinedMessage>().Last().PlayerId, Is.GreaterThanOrEqualTo(GameServer.BOT_ID_BASE));
        }

        [Test]
        public void EnoughHumans_AllBotsGone()
        {
            var server = MakeServer(2);

            Join(server, 1, "One");
            Join(server, 2, "Two");
            Join(server, 3, "Three");

            Assert.That(server.BotCount, Is.Zero);
            Assert.That(server.PlayerCount, Is.EqualTo(3));
        }

        [Test]
        public void ServerFullOfBots_HumanStillJoins()
        {
            var server = MakeServer(4, maxPlayers: 4);

            var connection = Join(server, 1, "Human");

            Assert.That(connection.ClosedAfterSending, Is.False);
            Assert.That(server.Players, Has.Count.EqualTo(4));
            Assert.That(server.BotCount, Is.EqualTo(3));
        }

        [Test]
        public void ServerFullOfHumans_Rejected()
        {
            var server = MakeServer(4, maxPlayers: 2);
            Join(server, 1, "One");
            Join(server, 2, "Two");

            var third = Join(server, 3, "Three");

            Assert.That(third.ReceivedOf<RejectMessage>()[0].Reason, Is.EqualTo(eRejectReason.ServerFull));
            Assert.That(server.BotCount, Is.Zero);
        }

        [Test]
        public void Bots_MoveAround()
        {
            // the game only runs with a human connected; they take one bot's place
            var server = MakeServer(4);
            Join(server, 1, "Human");
            var bots = server.Players.Where(p => p.IsBot).ToList();
            var start = bots.ToDictionary(p => p.Player.Id, p => p.Player.Location);

            for (var i = 0; i < 100; i++)
                server.Update(20);

            Assert.That(bots.Count(p => p.Player.Location != start[p.Player.Id]), Is.EqualTo(3));
        }

        [Test]
        public void NewLevel_BotsRespawnAtBase()
        {
            var server = MakeServer(3);
            Join(server, 1, "Human");

            for (var i = 0; i < 50; i++)
                server.Update(20);

            server.StartLevel(2);

            Assert.That(server.BotCount, Is.EqualTo(2));
            Assert.That(server.Players.All(p => p.Player.Alive && p.Player.Size == GameServer.PAC_START_SIZE), Is.True);
        }

        [Test]
        public void ChooseWanderDirection_NeverTurnsBackInACorridor()
        {
            var map = new eGridDef[5, 5];
            for (var x = 0; x < 5; x++)
                for (var y = 0; y < 5; y++)
                    map[x, y] = y == 2 && x > 0 && x < 4 ? 0 : eGridDef.Wall;

            var random = new Random(1);

            for (var i = 0; i < 20; i++)
                Assert.That(PacBrain.ChooseWanderDirection(map, 2, 2, eFacing.Right, random), Is.EqualTo(eFacing.Right));

            // at the end of the corridor turning back is the only way
            Assert.That(PacBrain.ChooseWanderDirection(map, 3, 2, eFacing.Right, random), Is.EqualTo(eFacing.Left));
        }

        [TestCase(eFacing.Up, eDirection.Up)]
        [TestCase(eFacing.Down, eDirection.Down)]
        [TestCase(eFacing.Left, eDirection.Left)]
        [TestCase(eFacing.Right, eDirection.Right)]
        [TestCase(null, eDirection.None)]
        public void ToDirection(eFacing? facing, eDirection expected)
        {
            Assert.That(MazeMovement.ToDirection(facing), Is.EqualTo(expected));
        }
    }
}
