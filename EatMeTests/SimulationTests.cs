using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    public class SimulationTests
    {
        private const int TICK_MS = 16;

        private static GameServer MakeServer(int ghosts = 0, int maxScore = 100, int intermissionTimeMs = 10000)
        {
            var settings = new ServerSettings
            {
                ServerName = "Sim",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = ghosts, MaxScore = maxScore, IntermissionTimeMs = intermissionTimeMs },
            };

            return ServerTestHelper.SkipCountdown(new GameServer(settings, []));
        }

        private static GameConfiguration Config => new();

        private static (Player Player, FakeConnection Connection) Join(GameServer server, int id, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = team });
            return (server.Players.Single(p => p.Connection == connection).Player, connection);
        }

        private static (int X, int Y) FindCell(GameServer server, eGridDef item)
        {
            for (var x = 0; x < server.Map.GetLength(0); x++)
                for (var y = 0; y < server.Map.GetLength(1); y++)
                    if (server.Map[x, y].HasFlag(item))
                        return (x, y);

            throw new InvalidOperationException($"No {item} cell");
        }

        private static void PutOn(Player player, (int X, int Y) cell)
        {
            player.Location = new Point2(cell.X + 0.5f, cell.Y + 0.5f);
        }

        [Test]
        public void EatingDot_GrowsPacAndQueuesCell()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var cell = FindCell(server, eGridDef.Food);
            PutOn(red, cell);

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.Size, Is.EqualTo(PacRules.GrowFromDot(GameServer.PAC_START_SIZE, Config.DotSizeGain, Config.PacMaxSize)));
            Assert.That(red.DotsEaten, Is.EqualTo(1));
            Assert.That(server.Map[cell.X, cell.Y].HasFlag(eGridDef.Food), Is.False);
            Assert.That(server.EatenDots.Count, Is.EqualTo(1));
            Assert.That(server.Regens, Is.Empty);
        }

        private static GameServer MakeDotRegenServer(int percent)
        {
            var settings = new ServerSettings
            {
                ServerName = "Sim",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = 0, MinPlayers = 0, DotRegenPercent = percent },
            };

            return ServerTestHelper.SkipCountdown(new GameServer(settings, []));
        }

        private static List<(int X, int Y)> FindCells(GameServer server, eGridDef item)
        {
            var cells = new List<(int X, int Y)>();

            for (var x = 0; x < server.Map.GetLength(0); x++)
                for (var y = 0; y < server.Map.GetLength(1); y++)
                    if (server.Map[x, y].HasFlag(item))
                        cells.Add((x, y));

            return cells;
        }

        [Test]
        public void DotRegen_ThresholdIsPercentOfLevelDots()
        {
            var server = MakeDotRegenServer(25);

            Assert.That(server.EatenDots.Threshold,
                Is.EqualTo(DotRegenQueue.GetThreshold(GameServer.CountDots(server.Map), 25)));
        }

        [Test]
        public void DotRegen_OverThreshold_PutsOldestDotBack()
        {
            var server = MakeDotRegenServer(5);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var dots = FindCells(server, eGridDef.Food);
            var threshold = server.EatenDots.Threshold;

            for (var i = 0; i < threshold; i++)
            {
                PutOn(red, dots[i]);
                server.UpdateEating(TimeSpan.Zero);
            }

            Assert.That(server.Map[dots[0].X, dots[0].Y].HasFlag(eGridDef.Food), Is.False);
            Assert.That(server.EatenDots.Count, Is.EqualTo(threshold));

            PutOn(red, dots[threshold]);
            server.UpdateEating(TimeSpan.Zero);
            server.Update(TimeSpan.Zero);

            Assert.That(server.Map[dots[0].X, dots[0].Y].HasFlag(eGridDef.Food), Is.True);
            Assert.That(server.Map[dots[threshold].X, dots[threshold].Y].HasFlag(eGridDef.Food), Is.False);
            Assert.That(server.EatenDots.Count, Is.EqualTo(threshold));

            // clients are told the dot came back
            var changes = connection.ReceivedOf<CellsChangedMessage>().SelectMany(m => m.Cells);
            Assert.That(changes.Any(c => c.X == dots[0].X && c.Y == dots[0].Y && c.Cell.HasFlag(eGridDef.Food)), Is.True);
        }

        [Test]
        public void DotRegen_ZeroPercent_DotComesStraightBack()
        {
            var server = MakeDotRegenServer(0);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var cell = FindCell(server, eGridDef.Food);
            PutOn(red, cell);

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.DotsEaten, Is.EqualTo(1));
            Assert.That(server.Map[cell.X, cell.Y].HasFlag(eGridDef.Food), Is.True);
            Assert.That(server.EatenDots.Count, Is.Zero);
        }

        [Test]
        public void DotRegen_NewLevel_EmptiesQueue()
        {
            var server = MakeDotRegenServer(50);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.Food));
            server.UpdateEating(TimeSpan.Zero);

            server.StartLevel(2);

            Assert.That(server.EatenDots.Count, Is.Zero);
        }

        [Test]
        public void EatingPowerPill_RegrowsAfterTimer()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var cell = FindCell(server, eGridDef.PowerPill);
            PutOn(red, cell);

            server.UpdateEating(TimeSpan.Zero);
            Assert.That(server.Map[cell.X, cell.Y].HasFlag(eGridDef.PowerPill), Is.False);

            // move off so it isn't eaten again as soon as it grows back
            PutOn(red, FindCell(server, eGridDef.SpawnPointRed));
            server.UpdateRegens(TimeSpan.FromMilliseconds(Config.PowerPillRegenTimeMs - 1));
            Assert.That(server.Map[cell.X, cell.Y].HasFlag(eGridDef.PowerPill), Is.False);

            server.UpdateRegens(TimeSpan.FromMilliseconds(1));
            Assert.That(server.Map[cell.X, cell.Y].HasFlag(eGridDef.PowerPill), Is.True);
            Assert.That(server.Regens, Is.Empty);
        }

        [Test]
        public void EatingDot_TellsClientsTheCellChanged()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var cell = FindCell(server, eGridDef.Food);
            PutOn(red, cell);

            server.Update(TimeSpan.Zero);

            var change = connection.ReceivedOf<CellsChangedMessage>().Single().Cells.Single();
            Assert.That((change.X, change.Y), Is.EqualTo(((ushort)cell.X, (ushort)cell.Y)));
            Assert.That(change.Cell.HasFlag(eGridDef.Food), Is.False);
        }

        [Test]
        public void Eating_SetOnEnteringAFoodCell_KeptWhileThere_ClearedOnEnteringAnEmptyOne()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.Food));

            server.UpdateEating(TimeSpan.Zero);
            Assert.That(red.Eating, Is.True);
            Assert.That(server.BuildSnapshot().Pacs.Single().Eating, Is.True);

            server.UpdateEating(TimeSpan.Zero);
            Assert.That(red.Eating, Is.True, "still in the cell it just emptied");

            PutOn(red, FindCell(server, eGridDef.SpawnPointRed));
            server.UpdateEating(TimeSpan.Zero);
            Assert.That(red.Eating, Is.False);
            Assert.That(server.BuildSnapshot().Pacs.Single().Eating, Is.False);
        }

        [Test]
        public void PowerPill_AddsPowerPillSizeGain()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.PowerPill));
            red.Size = 10f;

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.Size, Is.EqualTo(PacRules.GrowFromPowerPill(10f, Config.PowerPillSizeGain)));
        }

        [Test]
        public void PowerPill_WearsOffAfterDuration()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.PowerPill));
            red.Size = 50f;

            server.UpdateEating(TimeSpan.Zero);
            Assert.That(red.Size, Is.EqualTo(150f), "can go past the max size without exploding");
            Assert.That(red.Alive && red.IsPowered, Is.True);
            Assert.That(server.BuildSnapshot().Pacs.Single().Powered, Is.True);

            // onto the enemy spawn (never any food, and only the own spawn feeds); short ticks so it doesn't start starving
            PutOn(red, FindCell(server, eGridDef.SpawnPointBlue));
            server.UpdateEating(TimeSpan.FromMilliseconds(Config.PowerUpDurationMs - 1));
            Assert.That(red.IsPowered, Is.True);

            server.UpdateEating(TimeSpan.FromMilliseconds(1));
            Assert.That(red.IsPowered, Is.False);
            Assert.That(red.Size, Is.LessThanOrEqualTo(50f));
            Assert.That(server.BuildSnapshot().Pacs.Single().Powered, Is.False);
        }

        [Test]
        public void PowerPill_WhilePowered_OnlyRestartsTimer()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.PowerPill));
            red.Size = 120f;
            red.PowerUpEffect = ePowerUp.PowerPill;
            red.PowerUpMs = 1;

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.Size, Is.EqualTo(120f));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs));
        }

        [Test]
        public void PowerUpEffect_IsSentInSnapshotAndEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Size = 50f;

            server.StartPowerUpEffect(red, ePowerUp.PowerPill);
            Assert.That(red.Size, Is.EqualTo(150f));

            var pac = server.BuildSnapshot().Pacs.Single();
            Assert.That(pac.PowerUpEffect, Is.EqualTo(ePowerUp.PowerPill));
            Assert.That(pac.PowerUpMsLeft, Is.EqualTo(Config.PowerUpDurationMs));

            server.EndPowerUpEffect(red);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None));
            Assert.That(red.PowerUpMs, Is.EqualTo(0));
            Assert.That(red.Size, Is.EqualTo(50f), "the pill's size wears off");
            Assert.That(server.BuildSnapshot().Pacs.Single().PowerUpEffect, Is.EqualTo(ePowerUp.None));
        }

        [Test]
        public void Cherry_StartsCherryTwin()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Size = 50f;

            server.ApplyPowerUp(red, ePowerUp.Cherry);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Cherry));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs));
            Assert.That(red.Size, Is.EqualTo(50f), "Cherry Twin doesn't change size");
        }

        [Test]
        public void Strawberry_StartsSweetTooth()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.Strawberry);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Strawberry));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs));
        }

        [Test]
        public void SweetTooth_DotsGiveTripleGrowth()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.Food));
            red.Size = 10f;
            server.StartPowerUpEffect(red, ePowerUp.Strawberry);

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.Size, Is.EqualTo(10f + Config.DotSizeGain * PacRules.SWEET_TOOTH_DOT_GAIN_SCALE).Within(0.0001f));
        }

        [Test]
        public void Orange_StartsVitaminC()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.Orange);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Orange));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs));
        }

        [Test]
        public void VitaminC_NeverStarvesWhileItLasts()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.Food));
            server.UpdateEating(TimeSpan.Zero);
            red.Size = 50f;
            red.MsSinceEaten = Config.MsUntilStarvation;
            server.StartPowerUpEffect(red, ePowerUp.Orange);

            server.UpdateEating(TimeSpan.FromMilliseconds(Config.PowerUpDurationMs - 1000));

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Orange));
            Assert.That(red.PowerUpMs, Is.EqualTo(1000), "the buff bar counts down");
            Assert.That(red.MsSinceEaten, Is.EqualTo(0), "the hunger bar stays empty");
            Assert.That(red.Size, Is.EqualTo(50f), "never shrinks");
        }

        [Test]
        public void VitaminC_RunsOutAfterTheStandardDuration()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.StartPowerUpEffect(red, ePowerUp.Orange);

            server.UpdateEating(TimeSpan.FromMilliseconds(Config.PowerUpDurationMs));

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None));
        }

        [Test]
        public void VitaminC_EndsWhenReplacedOrOnRespawn()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.StartPowerUpEffect(red, ePowerUp.Orange);
            server.StartPowerUpEffect(red, ePowerUp.PowerPill);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.PowerPill));

            server.StartPowerUpEffect(red, ePowerUp.Orange);
            red.Alive = false;
            red.RespawnTimer = 0;
            server.UpdateRespawns(TimeSpan.FromMilliseconds(1));

            Assert.That(red.Alive, Is.True);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None));
        }

        [Test]
        public void Apple_StartsDoctorAway_ForTheStandardDuration()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.Apple);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Apple));
            Assert.That(red.RepelsGhosts, Is.True);

            server.UpdateEating(TimeSpan.FromMilliseconds(Config.PowerUpDurationMs / 2));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs - Config.PowerUpDurationMs / 2), "the buff bar counts down");

            server.UpdateEating(TimeSpan.FromMilliseconds(Config.PowerUpDurationMs));
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None));
            Assert.That(red.RepelsGhosts, Is.False);
        }

        [Test]
        public void BellAndKey_StartTheirEffects()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.Bell);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Bell));

            server.ApplyPowerUp(red, ePowerUp.Key);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Key));
        }

        [Test]
        public void Lockdown_KeepsDeadEnemiesFromRespawning_UntilItEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.StartPowerUpEffect(red, ePowerUp.Key);
            blue.Alive = false;
            blue.RespawnTimer = 0;

            Assert.That(server.IsSpawnLocked(eTeam.Blue), Is.True);
            Assert.That(server.IsSpawnLocked(eTeam.Red), Is.False, "doesn't lock its own team");

            server.UpdateRespawns(TimeSpan.FromMilliseconds(1000));
            Assert.That(blue.Alive, Is.False);

            server.EndPowerUpEffect(red);
            server.UpdateRespawns(TimeSpan.FromMilliseconds(1));
            Assert.That(blue.Alive, Is.True, "comes back as soon as the lock lifts");
        }

        [Test]
        public void Lockdown_LiftsWhenItsPacDies()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.StartPowerUpEffect(red, ePowerUp.Key);
            blue.Alive = false;
            blue.RespawnTimer = 0;

            red.Alive = false;
            red.RespawnTimer = 5000;
            server.UpdateRespawns(TimeSpan.FromMilliseconds(1));

            Assert.That(blue.Alive, Is.True);
        }

        [Test]
        public void Pretzel_StartsTwister()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.Pretzel);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Pretzel));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs));
        }

        [Test]
        public void Twister_SlowsOthersInRangeToHalfSpeed()
        {
            var normal = MakeServer();
            var (alone, _) = Join(normal, 2, eTeamPreference.Blue);
            var twisted = MakeServer();
            var (slowed, _) = Join(twisted, 2, eTeamPreference.Blue);
            var (red, _) = Join(twisted, 1, eTeamPreference.Red);
            red.Location = slowed.Location;
            twisted.StartPowerUpEffect(red, ePowerUp.Pretzel);

            var start = alone.Location;
            normal.MovePacs(TimeSpan.FromMilliseconds(100));
            twisted.MovePacs(TimeSpan.FromMilliseconds(100));

            var aloneMoved = MathF.Abs(alone.Location.X - start.X) + MathF.Abs(alone.Location.Y - start.Y);
            var slowedMoved = MathF.Abs(slowed.Location.X - start.X) + MathF.Abs(slowed.Location.Y - start.Y);
            Assert.That(aloneMoved, Is.GreaterThan(0f));
            Assert.That(slowedMoved, Is.EqualTo(aloneMoved * PowerUpEffects.TWISTER_SPEED_SCALE).Within(0.0001f));
        }

        [Test]
        public void WarpSpeed_MovesFaster()
        {
            var normal = MakeServer();
            var (slow, _) = Join(normal, 1, eTeamPreference.Red);
            var rushed = MakeServer();
            var (fast, _) = Join(rushed, 1, eTeamPreference.Red);
            rushed.StartPowerUpEffect(fast, ePowerUp.Galaxian);

            var start = slow.Location;
            normal.MovePacs(TimeSpan.FromMilliseconds(100));
            rushed.MovePacs(TimeSpan.FromMilliseconds(100));

            var slowMoved = MathF.Abs(slow.Location.X - start.X) + MathF.Abs(slow.Location.Y - start.Y);
            var fastMoved = MathF.Abs(fast.Location.X - start.X) + MathF.Abs(fast.Location.Y - start.Y);
            Assert.That(slowMoved, Is.GreaterThan(0f));
            Assert.That(fastMoved, Is.EqualTo(slowMoved * PacRules.WARP_SPEED_SCALE).Within(0.0001f));
        }

        [Test]
        public void Dots_StopAtMaxSize()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, eGridDef.Food));
            red.Size = Config.PacMaxSize;

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.Size, Is.EqualTo(Config.PacMaxSize));
            Assert.That(red.Alive, Is.True);
        }

        [Test]
        public void Starving_ShrinksOnlyAfterMsUntilStarvation()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            red.Size = 50f;

            // spawn cells never have food, and only the own spawn feeds
            PutOn(red, FindCell(server, eGridDef.SpawnPointBlue));
            server.UpdateEating(TimeSpan.FromMilliseconds(Config.MsUntilStarvation - 1));
            Assert.That(red.Size, Is.EqualTo(50f));

            server.UpdateEating(TimeSpan.FromMilliseconds(500));
            Assert.That(red.Size, Is.EqualTo(PacRules.Starve(50f, Config.PacShrinkPerSecond, TimeSpan.FromMilliseconds(500))).Within(0.0001f));
        }

        [Test]
        public void BigPac_DoesNotExplode()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            red.Size = Config.PacMaxSize * 3f;

            server.UpdateEating(TimeSpan.Zero);

            Assert.That(red.Alive, Is.True);
            Assert.That(connection.ReceivedOf<GameEventMessage>(), Is.Empty);
        }

        [Test]
        public void DeadPac_RespawnsAtBaseAfterRespawnTime()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Location = server.Ghosts[0].Location;
            server.UpdateGhosts(TimeSpan.Zero);

            server.UpdateRespawns(TimeSpan.FromMilliseconds(Config.PlayerRespawnTimeMs - 1));
            Assert.That(red.Alive, Is.False);

            server.UpdateRespawns(TimeSpan.FromMilliseconds(1));
            Assert.That(red.Alive, Is.True);
            Assert.That(red.Size, Is.EqualTo(GameServer.PAC_START_SIZE));
            Assert.That(server.Map[(int)red.Location.X, (int)red.Location.Y].HasFlag(eGridDef.SpawnPointRed), Is.True);
        }

        [Test]
        public void WarpSpeed_ScoresNormalPoints()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.ApplyPowerUp(red, ePowerUp.Galaxian);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Galaxian));

            server.UpdatePacFights();

            Assert.That(blue.Alive, Is.False);
            Assert.That(server.RedScore, Is.EqualTo(Config.PlayerKill));
            Assert.That(red.Score, Is.EqualTo(Config.PlayerKill));
            Assert.That(red.TotalScore, Is.EqualTo(Config.PlayerKill));
        }

        [Test]
        public void Starman_StartsInvincible()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.Starman);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Starman));
            Assert.That(red.IsInvincible, Is.True);
            Assert.That(red.RepelsGhosts, Is.True, "ghosts flee it like a power pill");
        }

        [Test]
        public void Invincible_EatsBiggerEnemy_AndScores()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 10f;
            blue.Size = 90f;
            server.StartPowerUpEffect(red, ePowerUp.Starman);

            server.UpdatePacFights();

            Assert.That(red.Alive, Is.True);
            Assert.That(blue.Alive, Is.False);
            Assert.That(red.PlayerKills, Is.EqualTo(1));
            Assert.That(server.RedScore, Is.EqualTo(Config.PlayerKill));
        }

        [Test]
        public void TwoInvinciblePacs_Bounce()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 10f;
            blue.Size = 90f;
            server.StartPowerUpEffect(red, ePowerUp.Starman);
            server.StartPowerUpEffect(blue, ePowerUp.Starman);

            server.UpdatePacFights();

            Assert.That(red.Alive && blue.Alive, Is.True);
        }

        [Test]
        public void Invincible_KillsGhostItTouches_AndSurvives()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Location = server.Ghosts[0].Location;
            red.Size = 0f;
            server.StartPowerUpEffect(red, ePowerUp.Starman);

            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(red.Alive, Is.True);
            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(server.RedScore, Is.EqualTo(Config.GhostKill));
        }

        [Test]
        public void BiggerEnemy_EatsSmaller_AndScores()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;

            server.UpdatePacFights();

            Assert.That(blue.Alive, Is.False);
            Assert.That(red.Alive, Is.True);
            Assert.That(red.Size, Is.EqualTo(60f), "eating a pac doesn't change size");
            Assert.That(red.PlayerKills, Is.EqualTo(1));
            Assert.That(server.RedScore, Is.EqualTo(Config.PlayerKill));
        }

        [Test]
        public void EatenFlagCarrier_DropsFlag()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var redFlag = server.Flags.First(f => f.Team == eTeam.Red);
            blue.Location = new Point2(redFlag.X, redFlag.Y);
            server.UpdateFlags();
            Assert.That(blue.HasFlag, Is.True);

            red.Location = blue.Location;
            red.Size = 60f;
            server.UpdatePacFights();

            Assert.That(blue.HasFlag, Is.False);
            Assert.That(redFlag.IsCarried, Is.False);
        }

        /// <summary>
        /// A blue spawn tile with an open cell beside it that isn't a spawn tile, and the way from that cell into it.
        /// </summary>
        private static ((int X, int Y) Spawn, (int X, int Y) Outside, eFacing Inward) FindSpawnEntrance(GameServer server)
        {
            for (var x = 1; x < server.Map.GetLength(0) - 1; x++)
                for (var y = 1; y < server.Map.GetLength(1) - 1; y++)
                {
                    if (!server.Map[x, y].HasFlag(eGridDef.SpawnPointBlue))
                        continue;

                    foreach (var inward in Enum.GetValues<eFacing>())
                    {
                        var (ox, oy) = MazeMovement.GetNeighbor(x, y, MazeMovement.GetOpposite(inward), server.Map.GetLength(0), server.Map.GetLength(1));
                        var outside = server.Map[ox, oy];

                        if (!outside.HasFlag(eGridDef.Wall) && !outside.HasFlag(eGridDef.SpawnPointBlue))
                            return ((x, y), (ox, oy), inward);
                    }
                }

            throw new InvalidOperationException("No way into the blue spawn");
        }

        [Test]
        public void MovePacs_PacStopsAtGhostGenerator([Values(eTeamPreference.Red, eTeamPreference.Blue)] eTeamPreference team)
        {
            var server = MakeServer();
            var (pac, _) = Join(server, 1, team);
            var width = server.Map.GetLength(0);
            var height = server.Map.GetLength(1);

            foreach (var (x, y) in FindCells(server, eGridDef.GhostGenerator))
                foreach (var inward in Enum.GetValues<eFacing>())
                {
                    var (ox, oy) = MazeMovement.GetNeighbor(x, y, MazeMovement.GetOpposite(inward), width, height);

                    if ((server.Map[ox, oy] & (eGridDef.Wall | eGridDef.GhostGenerator)) != 0)
                        continue;

                    PutOn(pac, (ox, oy));
                    pac.Facing = inward;
                    server.Players.Single(p => p.Player == pac).Input = MazeMovement.ToDirection(inward);

                    server.MovePacs(TimeSpan.FromSeconds(1.5f / PacRules.GetSpeed(Config.PacSpeed, pac.Size, Config.PacMaxSize)));

                    Assert.That(pac.Location.X, Is.EqualTo(ox + 0.5f).Within(0.0001f));
                    Assert.That(pac.Location.Y, Is.EqualTo(oy + 0.5f).Within(0.0001f));
                    return;
                }

            Assert.Fail("No way into the ghost generator");
        }

        [Test]
        public void FlagCarrier_SteppingOntoSpawn_DropsFlagOutside()
        {
            var server = MakeServer();
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var redFlag = server.Flags.First(f => f.Team == eTeam.Red);
            blue.Location = new Point2(redFlag.X, redFlag.Y);
            server.UpdateFlags();
            Assert.That(blue.HasFlag, Is.True);

            var (spawn, outside, inward) = FindSpawnEntrance(server);
            PutOn(blue, outside);
            blue.Facing = inward;
            server.Players.Single(p => p.Player == blue).Input = MazeMovement.ToDirection(inward);

            server.MovePacs(TimeSpan.FromSeconds(0.75f / PacRules.GetSpeed(Config.PacSpeed, blue.Size, Config.PacMaxSize)));
            Assert.That(MazeMovement.GetCell(blue.Location), Is.EqualTo(spawn));

            server.UpdateFlags();

            Assert.That(blue.HasFlag, Is.False);
            Assert.That(redFlag.IsCarried, Is.False, "can't pick it up again from a spawn tile");
            Assert.That((redFlag.X, redFlag.Y), Is.EqualTo((outside.X + 0.5f, outside.Y + 0.5f)));
        }

        [Test]
        public void EqualSizeEnemies_BounceOffEachOther()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            red.Location = new Point2(10.5f, 10.5f);
            red.Facing = eFacing.Right;
            blue.Location = new Point2(11.2f, 10.5f);
            blue.Facing = eFacing.Left;

            server.UpdatePacFights();

            Assert.That(red.Alive && blue.Alive, Is.True);
            Assert.That(red.Facing, Is.EqualTo(eFacing.Left));
            Assert.That(blue.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Teammates_PassThroughEachOther()
        {
            var server = MakeServer();
            var (first, _) = Join(server, 1, eTeamPreference.Red);
            var (second, _) = Join(server, 2, eTeamPreference.Red);
            second.Team = eTeam.Red;
            second.Location = first.Location;
            first.Size = 60f;

            server.UpdatePacFights();

            Assert.That(first.Alive && second.Alive, Is.True);
        }

        [Test]
        public void Ghost_KillsFullPacWithoutPowerPill()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Location = server.Ghosts[0].Location;
            red.Size = Config.PacMaxSize;

            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(red.Alive, Is.False);
        }

        [Test]
        public void PowBlock_StunsEveryoneInRangeButTheEater()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            var (blue, _) = Join(server, 3, eTeamPreference.Blue);
            var (far, _) = Join(server, 4, eTeamPreference.Blue);
            var ghost = server.Ghosts[0];
            red.Location = new Point2(10.5f, 10.5f);
            mate.Location = new Point2(12.5f, 10.5f);
            blue.Location = new Point2(10.5f, 10.5f + PowerUpEffects.EFFECT_RADIUS);
            far.Location = new Point2(10.5f + PowerUpEffects.EFFECT_RADIUS + 1f, 10.5f);
            ghost.Location = new Point2(15.5f, 15.5f);
            server.ApplyPowerUp(red, ePowerUp.PowBlock);

            server.HandleAction(red);

            Assert.That(red.IsStunned, Is.False, "not the eater");
            Assert.That(mate.StunMs, Is.EqualTo(PowerUpEffects.POW_STUN_MS), "teammates too");
            Assert.That(blue.IsStunned, Is.True);
            Assert.That(far.IsStunned, Is.False, "out of range");
            Assert.That(ghost.StunMs, Is.EqualTo(PowerUpEffects.POW_STUN_MS));

            var snapshot = server.BuildSnapshot();
            Assert.That(snapshot.Pacs.Single(p => p.PlayerId == blue.Id).Stunned, Is.True);
            Assert.That(snapshot.Ghosts.Single().Stunned, Is.True);
        }

        [Test]
        public void PowBlock_IsAnEffect_ThatReplacesTheOldOne_AndStunsNothingUntilAction()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            red.Location = new Point2(10.5f, 10.5f);
            blue.Location = new Point2(12.5f, 10.5f);
            server.StartPowerUpEffect(red, ePowerUp.Cherry);

            server.ApplyPowerUp(red, ePowerUp.PowBlock);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.PowBlock));
            Assert.That(red.PowerUpMs, Is.EqualTo(Config.PowerUpDurationMs));
            Assert.That(blue.IsStunned, Is.False);
            Assert.That(connection.ReceivedOf<GameEventMessage>().Any(e => e.EventType == eGameEvent.Blast), Is.False);
        }

        [Test]
        public void PowBlock_GoesOffOnEveryAction_AfterTheCooldown()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            red.Location = new Point2(10.5f, 10.5f);
            server.ApplyPowerUp(red, ePowerUp.PowBlock);

            server.HandleAction(red);
            server.HandleAction(red);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);

            Assert.That(connection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.Blast), Is.EqualTo(2), "the second press is in the cooldown");
        }

        [Test]
        public void PowBlock_BroadcastsBlast()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            red.Location = new Point2(10.5f, 12.5f);

            server.ApplyPowerUp(red, ePowerUp.PowBlock);
            server.HandleAction(red);

            var pow = connection.ReceivedOf<GameEventMessage>().Single(e => e.EventType == eGameEvent.Blast);
            Assert.That(pow.ActorId, Is.EqualTo(red.Id));
            Assert.That((pow.X, pow.Y), Is.EqualTo((10.5f, 12.5f)));
        }

        [Test]
        public void Stunned_PacsAndGhostsDontMove_UntilItWearsOff()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            red.StunMs = PowerUpEffects.POW_STUN_MS;
            ghost.StunMs = PowerUpEffects.POW_STUN_MS;
            var pacStart = red.Location;
            var ghostStart = ghost.Location;

            server.UpdateStuns(TimeSpan.FromMilliseconds(PowerUpEffects.POW_STUN_MS - 1));
            server.MovePacs(TimeSpan.FromMilliseconds(500));
            server.UpdateGhosts(TimeSpan.FromMilliseconds(500));

            Assert.That(red.Location, Is.EqualTo(pacStart));
            Assert.That(ghost.Location, Is.EqualTo(ghostStart));

            server.UpdateStuns(TimeSpan.FromMilliseconds(1));
            Assert.That(red.IsStunned, Is.False);
            Assert.That(ghost.IsStunned, Is.False);

            server.MovePacs(TimeSpan.FromMilliseconds(500));
            Assert.That(red.Location, Is.Not.EqualTo(pacStart), "moves again once the stun wears off");
        }

        [Test]
        public void StunnedGhost_DoesNotKillPacThatTouchesIt()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.Ghosts[0].StunMs = PowerUpEffects.POW_STUN_MS;
            red.Location = server.Ghosts[0].Location;

            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(red.Alive, Is.True);
            Assert.That(server.Ghosts[0].Alive, Is.True);
        }

        [Test]
        public void StunnedGhost_IsStillKilledByAPacThatCanKillGhosts()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.Ghosts[0].StunMs = PowerUpEffects.POW_STUN_MS;
            red.Location = server.Ghosts[0].Location;
            red.PowerUpEffect = ePowerUp.Starman;
            red.PowerUpMs = 1000;

            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(red.Alive, Is.True);
            Assert.That(server.Ghosts[0].Alive, Is.False);
        }

        [Test]
        public void Ghost_KillsPacItTouches()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Location = server.Ghosts[0].Location;

            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(red.Alive, Is.False);
            Assert.That(server.Ghosts[0].Alive, Is.True);
        }

        [Test]
        public void BigPac_KillsGhost_ScoresAndGhostComesBack()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            red.Location = ghost.Location;
            red.Size = Config.PacMaxSize;
            red.PowerUpEffect = ePowerUp.PowerPill;
            red.PowerUpMs = Config.PowerUpDurationMs;

            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(red.Alive, Is.True);
            Assert.That(ghost.Alive, Is.False);
            Assert.That(red.GhostKills, Is.EqualTo(1));
            Assert.That(server.RedScore, Is.EqualTo(Config.GhostKill));

            server.UpdateGhosts(TimeSpan.FromMilliseconds(Config.GhostRegenTimeMs));
            Assert.That(ghost.Alive, Is.True);
        }

        [Test]
        public void Starving_StopsAtStartSize()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Size = 10f;
            PutOn(red, FindCell(server, eGridDef.SpawnPointBlue));

            for (var i = 0; i < 100; i++)
                server.UpdateEating(TimeSpan.FromMilliseconds(Config.MsUntilStarvation));

            Assert.That(red.Size, Is.EqualTo(PacRules.START_SIZE));
        }

        [Test]
        public void Ghost_RunsFromPacThatCanKillIt()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            red.Size = Config.PacMaxSize;
            red.PowerUpEffect = ePowerUp.PowerPill;
            red.PowerUpMs = Config.PowerUpDurationMs;

            // put the pac four steps away through the maze: in sight but not yet touching
            var (gx, gy) = MazeMovement.GetCell(ghost.Location);
            var distances = GhostBrain.GetPathDistances(server.Map, gx, gy);
            var pacCell = (X: -1, Y: -1);
            for (var x = 0; x < distances.GetLength(0) && pacCell.X < 0; x++)
                for (var y = 0; y < distances.GetLength(1) && pacCell.X < 0; y++)
                    if (distances[x, y] == 4)
                        pacCell = (x, y);
            PutOn(red, pacCell);
            var before = GhostBrain.GetDistanceSquared(ghost.Location, red.Location);

            for (var i = 0; i < 10; i++)
                server.UpdateGhosts(TimeSpan.FromMilliseconds(TICK_MS));

            Assert.That(ghost.Alive, Is.True);
            Assert.That(ghost.CurrentTarget, Is.Null);
            Assert.That(GhostBrain.GetDistanceSquared(ghost.Location, red.Location), Is.GreaterThan(before));
        }

        [Test]
        public void Ghosts_AreInTheSnapshot()
        {
            var server = MakeServer(ghosts: 2);

            var snapshot = server.BuildSnapshot();

            Assert.That(snapshot.Ghosts.Count, Is.EqualTo(2));
            Assert.That(server.Map[(int)snapshot.Ghosts[0].X, (int)snapshot.Ghosts[0].Y].HasFlag(eGridDef.GhostGenerator), Is.True);
        }

        [Test]
        public void LongGame_PacsAndGhostsStayOnOpenCells()
        {
            var server = MakeServer(ghosts: 4);
            var players = new List<(Player Player, FakeConnection Connection)>();
            for (var id = 1; id <= 6; id++)
                players.Add(Join(server, id, id % 2 == 0 ? eTeamPreference.Blue : eTeamPreference.Red));

            var directions = new[] { eDirection.Up, eDirection.Left, eDirection.None, eDirection.Down, eDirection.Right };
            uint sequence = 0;

            for (var tick = 0; tick < 3000; tick++)
            {
                if (tick % 45 == 0)
                    foreach (var (player, connection) in players)
                        server.HandleMessage(connection, new InputMessage { Sequence = ++sequence, Direction = directions[(tick / 45 + player.Id) % directions.Length] });

                server.Update(TimeSpan.FromMilliseconds(TICK_MS));

                foreach (var pac in server.BuildSnapshot().Pacs.Where(p => p.Alive))
                    Assert.That(server.Map[(int)pac.X, (int)pac.Y].HasFlag(eGridDef.Wall), Is.False, $"pac {pac.PlayerId} tick {tick}");

                foreach (var ghost in server.Ghosts.Where(g => g.Alive))
                    Assert.That(server.Map[(int)ghost.Location.X, (int)ghost.Location.Y].HasFlag(eGridDef.Wall), Is.False, $"ghost {ghost.Id} tick {tick}");
            }

            Assert.That(players.Sum(p => p.Player.DotsEaten), Is.GreaterThan(0), "pacs moved and ate");
        }

        [Test]
        public void ReachingMaxScore_ShowsVictory_ThenSendsResultsAndStartsIntermission()
        {
            var server = MakeServer(maxScore: 1);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();

            Assert.That(server.CheckLevelWon(), Is.True);
            Assert.That(server.CheckLevelWon(), Is.False, "only won once");

            var victory = connection.ReceivedOf<VictoryMessage>().Single();
            Assert.That(victory.WinningTeam, Is.EqualTo(eTeam.Red));
            Assert.That(victory.MsRemaining, Is.EqualTo(LevelPhases.VICTORY_MS));
            Assert.That(server.IsVictory, Is.True);
            Assert.That(connection.ReceivedOf<LevelWonMessage>(), Is.Empty, "the results wait for the victory pause");

            server.ShowResults();

            Assert.That(server.IsVictory, Is.False);
            var won = connection.ReceivedOf<LevelWonMessage>().Single();
            Assert.That(won.WinningTeam, Is.EqualTo(eTeam.Red));
            Assert.That(won.RedScore, Is.EqualTo(1));
            Assert.That(won.MsRemaining, Is.EqualTo(10000));
            Assert.That(won.Results.Single(r => r.PlayerId == red.Id).Score, Is.EqualTo(1));
            Assert.That(won.Results.Single(r => r.PlayerId == blue.Id).Score, Is.Zero);
            Assert.That(server.IsIntermission, Is.True);
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(1), "next level waits for the intermission");
        }

        [Test]
        public void Intermission_PausesGameThenStartsNextLevel()
        {
            var server = MakeServer(maxScore: 1, intermissionTimeMs: 1000);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();
            server.CheckLevelWon();
            server.ShowResults();

            var location = red.Location;
            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Left });
            server.Update(TimeSpan.FromMilliseconds(500));

            Assert.That(red.Location, Is.EqualTo(location), "nothing moves during the intermission");
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(1));

            server.Update(TimeSpan.FromMilliseconds(500));

            Assert.That(server.IsIntermission, Is.False);
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(2));
            Assert.That(connection.ReceivedOf<LevelStartMessage>().Last().Level, Is.EqualTo(2));
            Assert.That(server.RedScore, Is.Zero);
            Assert.That(red.Score, Is.Zero, "player scores start again each level");
            Assert.That(blue.Alive, Is.True, "everyone respawns for the new level");
        }

        [Test]
        public void ZeroIntermissionTime_StartsNextLevelStraightAway()
        {
            var server = MakeServer(maxScore: 1, intermissionTimeMs: 0);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();

            Assert.That(server.CheckLevelWon(), Is.True);
            server.ShowResults();

            Assert.That(server.IsIntermission, Is.False);
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(2));
        }

        [Test]
        public void Victory_FreezesTheGame_ThenShowsTheResults()
        {
            var server = MakeServer(maxScore: 1, intermissionTimeMs: 1000);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();
            server.CheckLevelWon();

            var location = red.Location;
            red.PowerUpEffect = ePowerUp.PowBlock;
            red.PowerUpMs = 5000;
            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Left });
            server.HandleAction(red);
            server.Update(TimeSpan.FromMilliseconds(LevelPhases.VICTORY_MS - 1));

            Assert.That(red.Location, Is.EqualTo(location), "nothing moves while Victory shows");
            Assert.That(red.ActionCooldownMs, Is.Zero, "the action button does nothing");
            Assert.That(red.PowerUpMs, Is.EqualTo(5000), "timers don't run");
            Assert.That(server.IsVictory, Is.True);
            Assert.That(connection.ReceivedOf<LevelWonMessage>(), Is.Empty);

            server.Update(TimeSpan.FromMilliseconds(1));

            Assert.That(server.IsVictory, Is.False);
            Assert.That(server.IsIntermission, Is.True);
            Assert.That(connection.ReceivedOf<LevelWonMessage>().Single().MsRemaining, Is.EqualTo(1000));
        }

        [Test]
        public void JoiningDuringVictory_GetsVictoryWithTimeLeft()
        {
            var server = MakeServer(maxScore: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();
            server.CheckLevelWon();
            server.Update(TimeSpan.FromMilliseconds(1000));

            var (_, late) = Join(server, 3, eTeamPreference.Blue);

            var victory = late.ReceivedOf<VictoryMessage>().Single();
            Assert.That(victory.WinningTeam, Is.EqualTo(eTeam.Red));
            Assert.That(victory.MsRemaining, Is.EqualTo(LevelPhases.VICTORY_MS - 1000));
            Assert.That(late.Received.IndexOf(victory), Is.GreaterThan(late.Received.IndexOf(late.ReceivedOf<LevelStartMessage>().Single())), "after the LevelStart, so the client keeps it");
        }

        [Test]
        public void StartCountdown_FreezesTheGame_ThenPlayBegins()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            server.StartLevel(2);

            Assert.That(connection.ReceivedOf<LevelStartMessage>().Last().CountdownMs, Is.EqualTo(LevelPhases.START_COUNTDOWN_MS));
            Assert.That(server.IsCountdown, Is.True);

            var location = red.Location;
            red.PowerUpEffect = ePowerUp.PowBlock;
            red.PowerUpMs = 5000;
            var snapshots = connection.ReceivedOf<SnapshotMessage>().Count();
            server.HandleMessage(connection, new InputMessage { Sequence = 1, Direction = eDirection.Left });
            server.HandleAction(red);
            server.Update(TimeSpan.FromMilliseconds(LevelPhases.START_COUNTDOWN_MS - 1));

            Assert.That(red.Location, Is.EqualTo(location), "nothing moves during the countdown");
            Assert.That(red.ActionCooldownMs, Is.Zero, "the action button does nothing");
            Assert.That(red.PowerUpMs, Is.EqualTo(5000), "timers don't run");
            Assert.That(connection.ReceivedOf<SnapshotMessage>().Count(), Is.GreaterThan(snapshots), "snapshots still go out");
            Assert.That(server.IsCountdown, Is.True);

            server.Update(TimeSpan.FromMilliseconds(1));

            Assert.That(server.IsCountdown, Is.False);
        }

        [Test]
        public void JoiningDuringCountdown_GetsTheTimeLeft()
        {
            var server = MakeServer();
            Join(server, 1, eTeamPreference.Red);
            server.StartLevel(2);
            server.Update(TimeSpan.FromMilliseconds(1000));

            var (_, late) = Join(server, 2, eTeamPreference.Blue);

            Assert.That(late.ReceivedOf<LevelStartMessage>().Single().CountdownMs, Is.EqualTo(LevelPhases.START_COUNTDOWN_MS - 1000));
        }

        [Test]
        public void JoiningDuringIntermission_GetsResultsWithTimeLeft()
        {
            var server = MakeServer(maxScore: 1, intermissionTimeMs: 1000);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();
            server.CheckLevelWon();
            server.ShowResults();
            server.Update(TimeSpan.FromMilliseconds(250));

            var (_, late) = Join(server, 3, eTeamPreference.Blue);

            var won = late.ReceivedOf<LevelWonMessage>().Single();
            Assert.That(won.WinningTeam, Is.EqualTo(eTeam.Red));
            Assert.That(won.MsRemaining, Is.EqualTo(750));
            Assert.That(late.Received.IndexOf(won), Is.GreaterThan(late.Received.IndexOf(late.ReceivedOf<LevelStartMessage>().Single())), "after the LevelStart, so the client keeps it");
        }

        [Test]
        public void NoHumans_PausesGame()
        {
            var server = MakeServer(ghosts: 4);
            var ghosts = server.Ghosts.Select(g => g.Location).ToList();

            Assert.That(server.IsPaused, Is.True);

            for (var i = 0; i < 60; i++)
                server.Update(TimeSpan.FromMilliseconds(TICK_MS));

            Assert.That(server.Tick, Is.Zero);
            Assert.That(server.Ghosts.Select(g => g.Location), Is.EqualTo(ghosts), "ghosts don't move");
        }

        [Test]
        public void HumanJoining_ResumesGameAndLeavingPausesIt()
        {
            var server = MakeServer(ghosts: 4);
            var (_, connection) = Join(server, 1, eTeamPreference.Red);

            Assert.That(server.IsPaused, Is.False);
            server.Update(TimeSpan.FromMilliseconds(TICK_MS));
            Assert.That(server.Tick, Is.EqualTo(1));

            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, connection));

            Assert.That(server.IsPaused, Is.True);
            server.Update(TimeSpan.FromMilliseconds(TICK_MS));
            Assert.That(server.Tick, Is.EqualTo(1));
        }

        [Test]
        public void NoHumans_PausesIntermissionCountdown()
        {
            var server = MakeServer(maxScore: 1, intermissionTimeMs: 1000);
            var (red, redConnection) = Join(server, 1, eTeamPreference.Red);
            var (blue, blueConnection) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;
            server.UpdatePacFights();
            server.CheckLevelWon();
            server.ShowResults();

            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, redConnection));
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, blueConnection));
            server.Update(TimeSpan.FromMilliseconds(2000));

            Assert.That(server.IsIntermission, Is.True);
            Assert.That(server.CurrentLevel.Level, Is.EqualTo(1));
        }

        [Test]
        public void EatingEnemyPac_ScoresForPlayerAndTeam()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            blue.Location = red.Location;
            red.Size = 60f;

            server.UpdatePacFights();

            Assert.That(red.Score, Is.EqualTo(Config.PlayerKill));
            Assert.That(server.RedScore, Is.EqualTo(Config.PlayerKill));
            Assert.That(blue.Score, Is.Zero);
        }

        [TestCase(90, 100, false)]
        [TestCase(91, 100, true)]
        [TestCase(99, 100, true)]
        [TestCase(18, 21, false)]
        [TestCase(20, 22, true)]
        public void IsAboutToWin(int score, int maxScore, bool expected)
        {
            Assert.That(GameServer.IsAboutToWin(score, maxScore), Is.EqualTo(expected));
        }

        [Test]
        public void GetWinner()
        {
            Assert.That(GameServer.GetWinner(99, 50, 100), Is.Null);
            Assert.That(GameServer.GetWinner(100, 50, 100), Is.EqualTo(eTeam.Red));
            Assert.That(GameServer.GetWinner(90, 102, 100), Is.EqualTo(eTeam.Blue));
            Assert.That(GameServer.GetWinner(105, 105, 100), Is.EqualTo(eTeam.Red));
        }

        [Test]
        public void LateJoiner_IsToldWhichDotsAreEaten()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var cell = FindCell(server, eGridDef.Food);
            PutOn(red, cell);
            server.UpdateEating(TimeSpan.Zero);

            var (_, late) = Join(server, 2, eTeamPreference.Blue);

            var eaten = late.ReceivedOf<CellsChangedMessage>().Single().Cells.Single();
            Assert.That((eaten.X, eaten.Y), Is.EqualTo(((ushort)cell.X, (ushort)cell.Y)));

            var state = new ClientGameState();
            foreach (var message in late.Received)
                state.Apply(message, 0);
            Assert.That(state.Map[cell.X, cell.Y], Is.EqualTo(server.Map[cell.X, cell.Y]));
        }

        [Test]
        public void CountDots_CountsFoodCells()
        {
            var map = new eGridDef[2, 2];
            map[0, 0] = eGridDef.Food;
            map[1, 1] = eGridDef.Food | eGridDef.PowerPill;
            map[0, 1] = eGridDef.Wall;

            Assert.That(GameServer.CountDots(map), Is.EqualTo(2));
        }
    }
}
