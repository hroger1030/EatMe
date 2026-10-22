using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class ServerCommandsTests
    {
        private static GameServer MakeServer(int minPlayers = 0, bool autoBalanceTeams = false, int startingLevel = 1)
        {
            var settings = new ServerSettings
            {
                ServerName = "Command Server",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = autoBalanceTeams,
                StartingLevel = startingLevel,
                GameConfig = new GameConfiguration { MinPlayers = minPlayers, MaxPlayers = 128 },
            };

            return new GameServer(settings, [], level: startingLevel);
        }

        private static FakeConnection Join(GameServer server, int id, string name, eTeamPreference team = eTeamPreference.Red)
        {
            var connection = new FakeConnection(id);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));
            server.HandleMessage(connection, new HelloMessage { PlayerName = name, PreferredTeam = team });
            return connection;
        }

        private static string Run(GameServer server, string line)
        {
            return ServerCommands.Execute(server, line, out _);
        }

        [TestCase("help", ServerCommands.HELP, "")]
        [TestCase("  HELP  ", ServerCommands.HELP, "")]
        [TestCase("next level", ServerCommands.NEXT_LEVEL, "")]
        [TestCase("Next   Level", ServerCommands.NEXT_LEVEL, "")]
        [TestCase("next", ServerCommands.NEXT_LEVEL, "")]
        [TestCase("level 5", ServerCommands.LEVEL, "5")]
        [TestCase("kick Big Pac", ServerCommands.KICK, "Big Pac")]
        [TestCase("add bot 3", ServerCommands.ADD_BOT, "3")]
        [TestCase("addbot", ServerCommands.ADD_BOT, "")]
        [TestCase("remove bot", ServerCommands.REMOVE_BOT, "")]
        [TestCase("new game", ServerCommands.NEW_GAME, "")]
        [TestCase("motd Hello  there", ServerCommands.MOTD, "Hello  there")]
        [TestCase("stop", ServerCommands.QUIT, "")]
        public void Parse_FindsCommandAndArgument(string line, string command, string argument)
        {
            Assert.That(ServerCommands.Parse(line, out var parsed), Is.EqualTo(command));
            Assert.That(parsed, Is.EqualTo(argument));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        [TestCase("ban someone")]
        [TestCase("add")]
        public void Parse_UnknownOrBlank_IsNull(string line)
        {
            Assert.That(ServerCommands.Parse(line, out _), Is.Null);
        }

        [Test]
        public void Commands_EveryOneHasHelpAndEveryAliasPointsAtOne()
        {
            var names = new List<string>();
            for (var i = 0; i < ServerCommands.COMMANDS.GetLength(0); i++)
                names.Add(ServerCommands.COMMANDS[i, 0]);

            Assert.That(names, Is.Unique);
            Assert.That(ServerCommands.ALIASES.Values.Distinct(), Is.SubsetOf(names));

            var help = ServerCommands.GetHelp();
            for (var i = 0; i < ServerCommands.COMMANDS.GetLength(0); i++)
                Assert.That(help, Does.Contain(ServerCommands.COMMANDS[i, 1]));
        }

        [TestCase("on", true, true)]
        [TestCase("OFF", true, false)]
        [TestCase("yes", true, true)]
        [TestCase("0", true, false)]
        [TestCase("maybe", false, false)]
        public void TryParseOnOff_ReadsCommonSpellings(string text, bool ok, bool value)
        {
            Assert.That(ServerCommands.TryParseOnOff(text, out var parsed), Is.EqualTo(ok));
            Assert.That(parsed, Is.EqualTo(value));
        }

        [Test]
        public void Execute_Unknown_SaysSo()
        {
            var server = MakeServer();

            Assert.That(Run(server, "dance"), Is.EqualTo(ServerCommands.UNKNOWN));
            Assert.That(Run(server, ""), Is.Empty);
        }

        [Test]
        public void Execute_Quit_SetsQuit()
        {
            var server = MakeServer();

            ServerCommands.Execute(server, "quit", out var quit);
            Assert.That(quit, Is.True);

            ServerCommands.Execute(server, "status", out quit);
            Assert.That(quit, Is.False);
        }

        [Test]
        public void Execute_NextLevel_StartsTheNextLevelAndTellsPlayers()
        {
            var server = MakeServer();
            var connection = Join(server, 1, "Pac");
            connection.Received.Clear();

            Run(server, "next level");

            Assert.That(server.CurrentLevel.Level, Is.EqualTo(2));
            Assert.That(connection.ReceivedOf<LevelStartMessage>().Single().Level, Is.EqualTo(2));
        }

        [Test]
        public void Execute_LevelRestartAndNewGame_PickTheRightLevel()
        {
            var server = MakeServer(startingLevel: 3);

            Run(server, "level 7");
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(7));

            Run(server, "restart");
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(7));

            Run(server, "new game");
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(3));

            Assert.That(Run(server, "level 0"), Does.StartWith("Usage"));
            Assert.That(Run(server, "level x"), Does.StartWith("Usage"));
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(3));
        }

        [Test]
        public void Execute_Kick_RemovesThePlayerAndSendsKicked()
        {
            var server = MakeServer();
            var kicked = Join(server, 1, "Pac");
            var other = Join(server, 2, "Other");
            other.Received.Clear();

            var output = Run(server, "kick pac");

            Assert.That(output, Is.EqualTo("Kicked Pac"));
            Assert.That(kicked.ReceivedOf<RejectMessage>().Single().Reason, Is.EqualTo(eRejectReason.Kicked));
            Assert.That(kicked.ClosedAfterSending, Is.True);
            Assert.That(server.PlayerCount, Is.EqualTo(1));
            Assert.That(other.ReceivedOf<PlayerLeftMessage>().Single().PlayerId, Is.EqualTo(1));

            // the connection closing afterwards changes nothing more
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, kicked));
            Assert.That(server.PlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void Execute_Kick_ById()
        {
            var server = MakeServer();
            Join(server, 1, "Pac");
            Join(server, 2, "Other");

            Run(server, "kick 2");

            Assert.That(server.Players.Select(p => p.Player.Name), Is.EqualTo(new[] { "Pac" }));
        }

        [Test]
        public void Execute_Kick_SameNameTwice_AsksForAnId()
        {
            var server = MakeServer();
            Join(server, 1, "Pac");
            Join(server, 2, "Pac");

            Assert.That(Run(server, "kick Pac"), Does.Contain("kick by id: 1, 2"));
            Assert.That(server.PlayerCount, Is.EqualTo(2));
        }

        [Test]
        public void Execute_Kick_NoneOrBot_KicksNobody()
        {
            var server = MakeServer(minPlayers: 1);
            var bot = server.Players.Single();

            Assert.That(Run(server, "kick nobody"), Is.EqualTo("No player nobody"));
            Assert.That(Run(server, $"kick {bot.Player.Name}"), Does.Contain("is a bot"));
            Assert.That(Run(server, "kick"), Does.StartWith("Usage"));
            Assert.That(server.BotCount, Is.EqualTo(1));
        }

        [Test]
        public void Execute_AddAndRemoveBot_ChangeMinPlayers()
        {
            var server = MakeServer();

            Run(server, "add bot 3");
            Assert.That(server.BotCount, Is.EqualTo(3));
            Assert.That(server.Settings.GameConfig.MinPlayers, Is.EqualTo(3));

            Run(server, "remove bot");
            Assert.That(server.BotCount, Is.EqualTo(2));

            // and a join doesn't undo it
            Join(server, 1, "Pac");
            Assert.That(server.PlayerCount + server.BotCount, Is.EqualTo(2));

            Run(server, "remove bot 10");
            Assert.That(server.BotCount, Is.Zero);
            Assert.That(server.Settings.GameConfig.MinPlayers, Is.Zero);
        }

        [Test]
        public void Execute_AddBot_WithHumansAboveMinPlayers_StillAddsOne()
        {
            var server = MakeServer();
            Join(server, 1, "Pac");
            Join(server, 2, "Other");

            Run(server, "add bot");

            Assert.That(server.BotCount, Is.EqualTo(1));
        }

        [Test]
        public void Execute_AddBot_WithAutoBalance_AddsOnePerTeam()
        {
            var server = MakeServer(autoBalanceTeams: true);

            Run(server, "add bot");

            Assert.That(server.BotCount, Is.EqualTo(2));
            Assert.That(server.Players.Count(p => p.Player.Team == eTeam.Red), Is.EqualTo(1));
            Assert.That(server.Players.Count(p => p.Player.Team == eTeam.Blue), Is.EqualTo(1));
        }

        [Test]
        public void Execute_Bots_SetsMinPlayersWithinMaxPlayers()
        {
            var server = MakeServer();

            Run(server, "bots 4");
            Assert.That(server.BotCount, Is.EqualTo(4));

            Run(server, "bots 1000");
            Assert.That(server.Settings.GameConfig.MinPlayers, Is.EqualTo(128));

            Assert.That(Run(server, "bots -1"), Does.StartWith("Usage"));
            Assert.That(Run(server, "bots"), Does.StartWith("MinPlayers 128"));
        }

        [Test]
        public void Execute_AutoBalance_TogglesAndRebalances()
        {
            var server = MakeServer(minPlayers: 1);
            Assert.That(server.BotCount, Is.EqualTo(1));

            Run(server, "autobalance on");

            Assert.That(server.Settings.AutoBalanceTeams, Is.True);
            Assert.That(server.BotCount, Is.EqualTo(2));
            Assert.That(Run(server, "autobalance sideways"), Does.StartWith("Usage"));
        }

        [Test]
        public void Execute_Motd_ChangesTheWelcomeForNewPlayers()
        {
            var server = MakeServer();

            Assert.That(Run(server, "motd"), Is.EqualTo($"MOTD: {Const.MOTD_DEFAULT}"));

            Run(server, "motd Double  points tonight");
            var connection = Join(server, 1, "Pac");

            Assert.That(connection.ReceivedOf<WelcomeMessage>().Single().MessageOfTheDay, Is.EqualTo("Double  points tonight"));
        }

        [Test]
        public void FormatTable_SizesEachColumnToItsWidestCell()
        {
            var rows = new List<string[]>
            {
                new[] { "Id", "Name", "Score" },
                new[] { "1000000", "A Very Long Bot Name", "5" },
                new[] { "2", "Pac", "12345" },
            };

            var lines = ServerCommands.FormatTable(rows, [false, false, true]).Split('\n');

            Assert.That(lines, Is.EqualTo(new[]
            {
                "Id       Name                  Score",
                "1000000  A Very Long Bot Name      5",
                "2        Pac                   12345",
            }));
        }

        [Test]
        public void Execute_Players_ColumnsLineUpWithLongBotNames()
        {
            var server = MakeServer();
            Join(server, 1, "Pac");
            Run(server, "bots 3");

            var lines = Run(server, "players").Split('\n');

            // every line ends its Score column at the same place
            var scoreEnd = lines[0].IndexOf("Score") + "Score".Length;
            foreach (var line in lines.Skip(1))
                Assert.That(char.IsDigit(line[scoreEnd - 1]) && line[scoreEnd] == ' ', Is.True, line);
        }

        [Test]
        public void Execute_StatusAndPlayers_DescribeTheGame()
        {
            var server = MakeServer(minPlayers: 2);
            Join(server, 1, "Pac");

            Assert.That(Run(server, "status"), Does.StartWith($"Level 1 {server.CurrentLevel.Name} (playing), Blue 0, Red 0"));

            var players = Run(server, "players");
            Assert.That(players, Does.Contain("Pac"));
            Assert.That(players, Does.Contain("Human"));
            Assert.That(players, Does.Contain("Bot"));
        }
    }
}
