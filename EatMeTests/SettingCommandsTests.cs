using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class SettingCommandsTests
    {
        private static GameServer MakeServer()
        {
            var settings = new ServerSettings
            {
                ServerName = "Settings Server",
                TickRate = 60,
                TargetFps = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { MaxPlayers = 128, ServerSeed = 7 },
            };

            return new GameServer(settings, []);
        }

        private static FakeConnection Join(GameServer server, int id)
        {
            var connection = new FakeConnection(id);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = eTeamPreference.Red });
            return connection;
        }

        private static string Run(GameServer server, string line)
        {
            return ServerCommands.Execute(server, line, out _);
        }

        [Test]
        public void SettingsList_HasEverySettingButThePorts()
        {
            var rows = SettingCommands.GetSettingRows(MakeServer().Settings);
            var names = new List<string>();

            foreach (var row in rows)
                names.Add(row[0]);

            foreach (var property in typeof(ServerSettings).GetProperties())
            {
                var excluded = property.Name == nameof(ServerSettings.Port) || property.Name == nameof(ServerSettings.DiscoveryPort)
                    || property.Name == nameof(ServerSettings.GameConfig);
                Assert.That(names.Contains(property.Name), Is.EqualTo(!excluded), property.Name);
            }

            foreach (var property in typeof(GameConfiguration).GetProperties())
                Assert.That(names, Does.Contain(property.Name));
        }

        [Test]
        public void EveryListedCommand_IsACommand()
        {
            var rows = SettingCommands.GetSettingRows(MakeServer().Settings);

            for (var i = 1; i < rows.Count; i++)
                Assert.That(ServerCommands.Parse(rows[i][1], out _), Is.EqualTo(rows[i][1]), rows[i][0]);
        }

        [Test]
        public void EverySettingCommand_ShowsItsValueAndTakesItBack()
        {
            var server = MakeServer();

            for (var i = 0; i < SettingCommands.COMMANDS.GetLength(0); i++)
            {
                var command = SettingCommands.COMMANDS[i, 0];
                var shown = Run(server, command);

                Assert.That(shown, Does.Contain(" = "), command);

                var value = shown[(shown.IndexOf(" = ") + 3)..];
                Assert.That(Run(server, $"{command} {value}"), Does.StartWith(shown), command);
                Assert.That(Run(server, command), Is.EqualTo(shown), command);
            }
        }

        [Test]
        public void SettingCommands_SetEachType_IgnoringCase()
        {
            var server = MakeServer();

            Assert.That(Run(server, "PACSPEED 7.5"), Is.EqualTo("PacSpeed = 7.5"));
            Assert.That(server.Settings.GameConfig.PacSpeed, Is.EqualTo(7.5f));
            Assert.That(Run(server, "maxscore 250"), Is.EqualTo("MaxScore = 250"));
            Assert.That(server.Settings.GameConfig.MaxScore, Is.EqualTo(250));
            Assert.That(Run(server, "servername Big Pac Party"), Is.EqualTo("ServerName = Big Pac Party"));
            Assert.That(server.Settings.ServerName, Is.EqualTo("Big Pac Party"));
            Assert.That(Run(server, "tickrate 30"), Is.EqualTo("TickRate = 30"));
            Assert.That(server.Settings.TickRate, Is.EqualTo(30));
        }

        [Test]
        public void SettingCommands_RejectBadValues()
        {
            var server = MakeServer();
            var maxScore = server.Settings.GameConfig.MaxScore;
            var pacSpeed = server.Settings.GameConfig.PacSpeed;

            Assert.That(Run(server, "maxscore lots"), Does.StartWith("Usage"));
            Assert.That(Run(server, "pacspeed fast"), Does.StartWith("Usage"));
            Assert.That(server.Settings.GameConfig.MaxScore, Is.EqualTo(maxScore));
            Assert.That(server.Settings.GameConfig.PacSpeed, Is.EqualTo(pacSpeed));
            Assert.That(Run(server, "port 9999"), Is.EqualTo(ServerCommands.UNKNOWN), "the ports can't be changed");
            Assert.That(Run(server, "get pacspeed"), Is.EqualTo(ServerCommands.UNKNOWN), "no generic get");
            Assert.That(Run(server, "set pacspeed 8"), Is.EqualTo(ServerCommands.UNKNOWN), "no generic set");
        }

        [Test]
        public void MazeSize_BadValueIsRefused_AndOthersClampLikeLoading()
        {
            var server = MakeServer();
            var mazeSize = server.Settings.GameConfig.MazeSize;

            Assert.That(Run(server, "mazesize 51"), Does.Not.StartWith("MazeSize ="), "odd");
            Assert.That(server.Settings.GameConfig.MazeSize, Is.EqualTo(mazeSize), "left as it was");

            Run(server, "dotregenpercent 250");
            Assert.That(server.Settings.GameConfig.DotRegenPercent, Is.EqualTo(DotRegenQueue.MAX_PERCENT));
            Run(server, "tickrate 0");
            Assert.That(server.Settings.TickRate, Is.EqualTo(1));
        }

        [Test]
        public void GameRuleChange_IsSentToClients()
        {
            var server = MakeServer();
            var connection = Join(server, 1);

            Assert.That(Run(server, "pacspeed 8"), Is.EqualTo("PacSpeed = 8"));
            Assert.That(connection.ReceivedOf<GameConfigMessage>().Last().GameConfig.PacSpeed, Is.EqualTo(8f));
        }

        [Test]
        public void MapSettings_CountFromTheNextLevel()
        {
            var server = MakeServer();
            var connection = Join(server, 1);
            var mazeSize = server.Map.GetLength(0);

            var output = Run(server, "mazesize 40");

            Assert.That(output, Does.Contain("next level"));
            Assert.That(server.Map.GetLength(0), Is.EqualTo(mazeSize), "the level being played keeps its map");
            Assert.That(connection.ReceivedOf<GameConfigMessage>().Last().GameConfig.MazeSize, Is.EqualTo(mazeSize), "clients keep building the same map");
            Assert.That(server.BuildClientConfig().MazeSize, Is.EqualTo(mazeSize), "and so do players who join now");

            Run(server, "restart");

            Assert.That(server.Map.GetLength(0), Is.EqualTo(40));
            Assert.That(connection.ReceivedOf<GameConfigMessage>().Last().GameConfig.MazeSize, Is.EqualTo(40));
        }

        [Test]
        public void MaxPlayers_BelowMinPlayers_RebalancesBots()
        {
            var server = MakeServer();
            Join(server, 1);
            Run(server, "bots 6");

            Assert.That(server.BotCount, Is.EqualTo(5));

            Run(server, "maxplayers 3");

            Assert.That(server.Settings.GameConfig.MinPlayers, Is.EqualTo(3));
            Assert.That(server.BotCount, Is.EqualTo(2));
        }

        [Test]
        public void SettingsCommand_ShowsValuesAndCommands()
        {
            var server = MakeServer();
            var output = Run(server, "settings");

            Assert.That(output, Does.Contain("ServerName").And.Contain("Settings Server").And.Contain("GhostSpeed").And.Contain("ghostspeed"));
            Assert.That(output, Does.Not.Contain("DiscoveryPort"));
        }

        [Test]
        public void Help_ListsTheSettingCommands()
        {
            Assert.That(ServerCommands.GetHelp(), Does.Contain("pacspeed [x]").And.Contain("mazesize [n]"));
        }

        [Test]
        public void ActionMessage_IsAccepted()
        {
            var server = MakeServer();
            var connection = Join(server, 1);

            server.HandleMessage(connection, new ActionMessage());

            Assert.That(connection.Closed, Is.False, "an action press isn't a bad message");
        }

        [Test]
        public void ServerLog_ErrorsAreRedAndWarningsYellow()
        {
            Assert.That(ServerLog.GetColor(eLogType.Error), Is.EqualTo(ConsoleColor.Red));
            Assert.That(ServerLog.GetColor(eLogType.Warning), Is.EqualTo(ConsoleColor.Yellow));
            Assert.That(ServerLog.GetColor(eLogType.Info), Is.EqualTo(ConsoleColor.Gray));
        }

        [TestCase(0, 0, "[Up:0:00] ")]
        [TestCase(0, 5, "[Up:0:05] ")]
        [TestCase(2, 45, "[Up:2:45] ")]
        [TestCase(30, 7, "[Up:30:07] ")]
        public void ServerLog_PromptShowsHoursAndMinutes(int hours, int minutes, string prompt)
        {
            Assert.That(ServerLog.GetPrompt(new TimeSpan(hours, minutes, 59)), Is.EqualTo(prompt));
        }
    }
}
