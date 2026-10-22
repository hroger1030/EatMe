using System.IO;
using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    /// <summary>
    /// A pac standing on its own team's spawn is fed at the rate of eating dots, and the client plays the charge loop for it.
    /// </summary>
    public class SpawnFeedingTests
    {
        private const int TICK_MS = 100;

        private static GameServer MakeServer()
        {
            var settings = new ServerSettings
            {
                ServerName = "Feeding",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = 0 },
            };

            return new GameServer(settings, []);
        }

        private static Player Join(GameServer server, int id, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = team });
            return server.Players.Single(p => p.Connection == connection).Player;
        }

        /// <summary>
        /// Puts the player on a spawn tile with the flag given, with its dot taken away.
        /// </summary>
        private static void PutOnSpawn(GameServer server, Player player, eGridDef spawn)
        {
            var map = server.Map;

            for (var y = 0; y < map.GetLength(1); y++)
                for (var x = 0; x < map.GetLength(0); x++)
                    if (map[x, y].HasFlag(spawn))
                    {
                        map[x, y] &= ~(eGridDef.Food | eGridDef.PowerPill);
                        player.Location = new Point2(x + 0.5f, y + 0.5f);
                        return;
                    }

            throw new InvalidOperationException("No spawn tile");
        }

        [Test]
        public void GetSpawnFeedGain_IsOneDotPerCellMoved()
        {
            Assert.That(PacRules.GetSpawnFeedGain(2f, 5f, 1f), Is.EqualTo(10f));
            Assert.That(PacRules.GetSpawnFeedGain(2f, 5f, 0.5f), Is.EqualTo(5f));
            Assert.That(PacRules.GetSpawnFeedGain(2f, 5f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void OwnSpawn_FeedsAtTheDotRateAndKeepsHungerAway()
        {
            var server = MakeServer();
            var config = server.Settings.GameConfig;
            var red = Join(server, 1, eTeamPreference.Red);
            PutOnSpawn(server, red, eGridDef.SpawnPointRed);
            red.Size = 0f;
            red.MsSinceEaten = 1234;

            server.UpdateEating(TICK_MS);

            var expected = PacRules.GetSpawnFeedGain(config.DotSizeGain, PacRules.GetSpeed(config.PacSpeed, 0f, config.PacMaxSize), TICK_MS / 1000f);
            Assert.That(red.Size, Is.EqualTo(Math.Min(expected, config.PacMaxSize)).Within(0.0001f));
            Assert.That(red.MsSinceEaten, Is.EqualTo(0));
            Assert.That(red.SpawnFeeding, Is.True);
        }

        [Test]
        public void EnemySpawn_DoesNotFeed()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            PutOnSpawn(server, red, eGridDef.SpawnPointBlue);
            red.Size = 0f;

            server.UpdateEating(TICK_MS);

            Assert.That(red.Size, Is.EqualTo(0f));
            Assert.That(red.SpawnFeeding, Is.False);
            Assert.That(red.MsSinceEaten, Is.EqualTo(TICK_MS));
        }

        [Test]
        public void OwnSpawn_StopsAtMaxSizeButStillKeepsHungerAway()
        {
            var server = MakeServer();
            var config = server.Settings.GameConfig;
            var blue = Join(server, 1, eTeamPreference.Blue);
            PutOnSpawn(server, blue, eGridDef.SpawnPointBlue);
            blue.Size = config.PacMaxSize;
            blue.MsSinceEaten = 1234;

            server.UpdateEating(TICK_MS);

            Assert.That(blue.Size, Is.EqualTo(config.PacMaxSize));
            Assert.That(blue.SpawnFeeding, Is.False);
            Assert.That(blue.MsSinceEaten, Is.EqualTo(0));
        }

        [Test]
        public void ScrewAttack_IsNotFed()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            PutOnSpawn(server, red, eGridDef.SpawnPointRed);
            red.Size = 0f;
            server.StartPowerUpEffect(red, ePowerUpEffect.ScrewAttack);

            server.UpdateEating(TICK_MS);

            Assert.That(red.Size, Is.EqualTo(0f));
            Assert.That(red.SpawnFeeding, Is.False);
        }

        [Test]
        public void LeavingTheSpawn_StopsFeeding()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            PutOnSpawn(server, red, eGridDef.SpawnPointRed);
            red.Size = 0f;
            server.UpdateEating(TICK_MS);
            Assert.That(red.SpawnFeeding, Is.True);

            PutOnSpawn(server, red, eGridDef.SpawnPointBlue);
            server.UpdateEating(TICK_MS);

            Assert.That(red.SpawnFeeding, Is.False);
        }

        [Test]
        public void PacState_RoundTripsSpawnFeeding()
        {
            var pac = new PacState(1, eTeam.Red, 1f, 2f, 3f, eFacing.Up, true, false, false, spawnFeeding: true);

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                pac.Write(writer);

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            Assert.That(PacState.Read(reader).SpawnFeeding, Is.True);
        }

        [Test]
        public void IsOwnPacSpawnFeeding_FollowsOwnPacAndSilencesTheSiren()
        {
            var state = new ClientGameState();
            state.Apply(new WelcomeMessage { PlayerId = 3, Team = eTeam.Blue, ServerName = "Srv", MessageOfTheDay = "Hi", LoadingScreenTips = ["Hi"] }, 0);
            state.Apply(new LevelStartMessage { Level = 2, WallColor = FastColor.FromRgb(1, 2, 3), WallFillColor = FastColor.FromRgb(7, 8, 9), BackgroundColor = FastColor.FromRgb(4, 5, 6) }, 0);

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false, spawnFeeding: true)] }, 0);
            Assert.That(state.IsOwnPacSpawnFeeding(), Is.True);
            Assert.That(state.IsOwnPacNotEating(), Is.False, "no siren while fed");

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false), new PacState(4, eTeam.Red, 1.5f, 1.5f, 50f, eFacing.Left, true, false, false, spawnFeeding: true)] }, 0);
            Assert.That(state.IsOwnPacSpawnFeeding(), Is.False, "only another pac is fed");

            state.Apply(new SnapshotMessage { Tick = 3, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, false, spawnFeeding: true)] }, 0);
            Assert.That(state.IsOwnPacSpawnFeeding(), Is.False, "a dead pac isn't fed");
        }
    }
}
