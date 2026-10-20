using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    /// <summary>
    /// The action button's cooldown, flags and effects, the respawn countdown, and the pear, banana, heart container,
    /// tetromino, hammer, BFG, mushroom, energy tank, screw attack, pepper shaker, spread gun and triforce.
    /// </summary>
    public class ItemEffectTests
    {
        private const int TICK_MS = 16;
        private const float TICK_SECONDS = TICK_MS / 1000f;

        private static GameServer MakeServer(int ghosts = 0)
        {
            var settings = new ServerSettings
            {
                ServerName = "Items",
                TargetFps = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = ghosts },
            };

            return new GameServer(settings, []);
        }

        private static (Player Player, FakeConnection Connection) Join(GameServer server, int id, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = team });
            return (server.Players.Single(p => p.Connection == connection).Player, connection);
        }

        private static ServerPlayer GetServerPlayer(GameServer server, Player player)
        {
            return server.Players.Single(p => p.Player == player);
        }

        private static void PutOn(Player player, (int X, int Y) cell)
        {
            player.Location = new Point2(cell.X + 0.5f, cell.Y + 0.5f);
        }

        private static void Tick(GameServer server, int ticks)
        {
            for (var i = 0; i < ticks; i++)
                server.UpdateWeaponEffects(TICK_SECONDS, TICK_MS);
        }

        /// <summary>
        /// The first cell (away from the outer wall) where test is true for the cell and its right hand neighbour.
        /// </summary>
        private static (int X, int Y) FindCell(GameServer server, Func<eGridDef, eGridDef, bool> test)
        {
            var map = server.Map;

            for (var y = 2; y < map.GetLength(1) - 2; y++)
                for (var x = 2; x < map.GetLength(0) - 3; x++)
                    if (test(map[x, y], map[x + 1, y]))
                        return (x, y);

            throw new InvalidOperationException("No such cell");
        }

        private static GameFlag CarryBlueFlag(GameServer server, Player red)
        {
            var flag = server.Flags.First(f => f.Team == eTeam.Blue);
            red.Location = new Point2(flag.X, flag.Y);
            server.UpdateFlags();
            Assert.That(red.HasFlag, Is.True, "picked up");
            return flag;
        }

        // the action button

        [Test]
        public void ActionButton_DoesNothingForHalfASecondAfterAPress()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.StartPowerUpEffect(red, ePowerUpEffect.Warp);

            var start = red.Location;
            server.HandleAction(red);
            var warped = red.Location;
            Assert.That(warped, Is.Not.EqualTo(start));

            server.HandleAction(red);
            Assert.That(red.Location, Is.EqualTo(warped), "cooling down");

            server.UpdateWeaponEffects(WeaponRules.ACTION_COOLDOWN_MS / 1000f, WeaponRules.ACTION_COOLDOWN_MS);
            server.HandleAction(red);
            Assert.That(red.Location, Is.Not.EqualTo(warped));
        }

        // flags

        [Test]
        public void Flag_CantBePickedUpWhileAnEffectWorks()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var flag = server.Flags.First(f => f.Team == eTeam.Blue);

            server.StartPowerUpEffect(red, ePowerUpEffect.SugarRush);
            red.Location = new Point2(flag.X, flag.Y);
            server.UpdateFlags();
            Assert.That(red.HasFlag, Is.False);

            server.EndPowerUpEffect(red);
            server.UpdateFlags();
            Assert.That(red.HasFlag, Is.True);
        }

        [Test]
        public void EatingAPowerUp_DropsTheFlag()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var flag = CarryBlueFlag(server, red);

            var powerUp = server.SpawnPowerUp();
            Assert.That(powerUp, Is.Not.Null);
            red.Location = powerUp.Location;
            server.UpdatePowerUps(0f, 0);

            Assert.That(red.HasFlag, Is.False);
            Assert.That(flag.IsCarried, Is.False);
        }

        [Test]
        public void EatingAPowerPill_DropsTheFlag()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var flag = CarryBlueFlag(server, red);

            PutOn(red, FindCell(server, (cell, _) => cell.HasFlag(eGridDef.PowerPill)));
            server.UpdateEating(TICK_MS);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.PowerPill));
            Assert.That(red.HasFlag, Is.False);
            Assert.That(flag.IsCarried, Is.False);
        }

        // respawn countdown

        [TestCase(4000, false, "Respawning in 4 seconds")]
        [TestCase(3001, false, "Respawning in 4 seconds")]
        [TestCase(1, false, "Respawning in 1 seconds")]
        [TestCase(0, false, "Respawning in 0 seconds")]
        [TestCase(2500, true, "Locked out!")]
        public void GetRespawnText_CountsUpToWholeSeconds_OrSaysLockedOut(int msLeft, bool locked, string expected)
        {
            Assert.That(ClientGameState.GetRespawnText(msLeft, locked), Is.EqualTo(expected));
        }

        [Test]
        public void Snapshot_SendsTheRespawnTimeAndTheLockdown()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);

            red.Alive = false;
            red.RespawnTimer = 2500;
            var pac = server.BuildSnapshot().Pacs.Single(p => p.PlayerId == red.Id);
            Assert.That(pac.RespawnMsLeft, Is.EqualTo(2500));
            Assert.That(pac.SpawnLocked, Is.False);

            server.StartPowerUpEffect(blue, ePowerUpEffect.Lockdown);
            pac = server.BuildSnapshot().Pacs.Single(p => p.PlayerId == red.Id);
            Assert.That(pac.SpawnLocked, Is.True);

            Assert.That(server.BuildSnapshot().Pacs.Single(p => p.PlayerId == blue.Id).RespawnMsLeft, Is.EqualTo(0), "alive");
        }

        [Test]
        public void PacState_RoundTripsTheRespawnFields()
        {
            var pac = new PacState(1, eTeam.Red, 1f, 2f, 3f, eFacing.Up, false, false, false, respawnMsLeft: 1234, spawnLocked: true, powerUps: 7);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
                pac.Write(writer);

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var read = PacState.Read(reader);

            Assert.That(read.RespawnMsLeft, Is.EqualTo(1234));
            Assert.That(read.SpawnLocked, Is.True);
            Assert.That(read.PowerUps, Is.EqualTo(7));
        }

        // Pear

        [Test]
        public void Warp_MovesThePacToAFreeDotCell()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.Pear);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.Warp));

            var start = MazeMovement.GetCell(red.Location);
            server.HandleAction(red);
            var cell = MazeMovement.GetCell(red.Location);

            Assert.That(cell, Is.Not.EqualTo(start));
            Assert.That(cell, Is.Not.EqualTo(MazeMovement.GetCell(blue.Location)));
            Assert.That(server.Map[cell.X, cell.Y].HasFlag(eGridDef.Food), Is.True);
            Assert.That(red.Location.X - cell.X, Is.EqualTo(0.5f), "on the centre");
        }

        [Test]
        public void PickWarpCell_SkipsOccupiedCells_AndIsNullWithNoneFree()
        {
            var map = new eGridDef[3, 1];
            map[0, 0] = eGridDef.Food;
            map[1, 0] = eGridDef.Wall;
            map[2, 0] = eGridDef.Food;

            Assert.That(WeaponRules.PickWarpCell(map, [(0, 0)], new Random(1)), Is.EqualTo(((int, int)?)(2, 0)));
            Assert.That(WeaponRules.PickWarpCell(map, [(0, 0), (2, 0)], new Random(1)), Is.Null);
        }

        // Banana

        [Test]
        public void BananaPeel_OnePerCell_StaysUntilSomethingSlipsOnIt()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.Banana);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.BananaPeel));

            var cell = MazeMovement.GetCell(red.Location);
            server.HandleAction(red);
            server.UpdateWeaponEffects(WeaponRules.ACTION_COOLDOWN_MS / 1000f, WeaponRules.ACTION_COOLDOWN_MS);
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.BananaPeel), Is.EqualTo(1), "one per cell");

            // the owner steps off; the peel outlasts the buff
            red.Location = blue.Location;
            Tick(server, 1);
            server.EndPowerUpEffect(red);
            Tick(server, 1);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.BananaPeel), Is.EqualTo(1));

            PutOn(blue, cell);
            Tick(server, 1);
            Assert.That(blue.StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(server.WeaponEffects, Is.Empty);
        }

        // Heart container

        [Test]
        public void HeartContainer_FillsUpToMaxSize_AndResetsHunger()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.StartPowerUpEffect(red, ePowerUpEffect.SugarRush);
            red.Size = 10f;
            red.MsSinceEaten = 5000;

            server.ApplyPowerUp(red, ePowerUp.HeartContainer);

            Assert.That(red.Size, Is.EqualTo(new GameConfiguration().PacMaxSize));
            Assert.That(red.MsSinceEaten, Is.EqualTo(0));
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.SugarRush), "instant: the buff is kept");

            red.Size = 150f;
            server.ApplyPowerUp(red, ePowerUp.HeartContainer);
            Assert.That(red.Size, Is.EqualTo(150f), "never smaller");
        }

        // Tetromino

        [Test]
        public void BlockDrop_WallsTheCellBehind_UntilTheEffectEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food);
            PutOn(red, (x + 1, y));
            red.Facing = eFacing.Right;

            server.ApplyPowerUp(red, ePowerUp.Tetromino);
            server.HandleAction(red);

            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food | eGridDef.Wall | eGridDef.TetrisBlock));
            Assert.That(server.WeaponEffects.Single().Kind, Is.EqualTo(eWeaponEffect.TetrisBlock));

            Tick(server, 1);
            Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.True, "lasts while the effect works");

            server.EndPowerUpEffect(red);
            Tick(server, 1);
            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food));
            Assert.That(server.WeaponEffects, Is.Empty);
        }

        [Test]
        public void CanDropBlock_OnlyOnPlainOpenCells()
        {
            Assert.That(WeaponRules.CanDropBlock(eGridDef.Food), Is.True);
            Assert.That(WeaponRules.CanDropBlock(eGridDef.PowerPill), Is.True);
            Assert.That(WeaponRules.CanDropBlock(eGridDef.Wall), Is.False);
            Assert.That(WeaponRules.CanDropBlock(eGridDef.SpawnPointRed), Is.False);
            Assert.That(WeaponRules.CanDropBlock(eGridDef.FlagBaseBlue), Is.False);
            Assert.That(WeaponRules.CanDropBlock(eGridDef.GhostGenerator), Is.False);
        }

        [Test]
        public void WallTiles_DontJoinOntoTetrisBlocks()
        {
            var map = new eGridDef[3, 3];
            map[1, 1] = eGridDef.Wall;
            WallTiles.GetQuarter(map, 1, 1, 0, out var alone, out _);

            map[0, 1] = eGridDef.Wall | eGridDef.TetrisBlock;
            WallTiles.GetQuarter(map, 1, 1, 0, out var nextToBlock, out _);

            Assert.That(nextToBlock, Is.EqualTo(alone));
        }

        // Hammer

        [Test]
        public void Hammer_SmashesTheWallAheadIntoADot_ButNotTheOuterWall()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Wall);
            PutOn(red, (x, y));
            red.Facing = eFacing.Right;

            server.ApplyPowerUp(red, ePowerUp.Hammer);
            Tick(server, 1);

            var hammer = server.WeaponEffects.Single();
            Assert.That(hammer.Kind, Is.EqualTo(eWeaponEffect.Hammer));
            Assert.That(MazeMovement.GetCell(hammer.Location), Is.EqualTo((x + 1, y)));
            Assert.That(server.Map[x + 1, y], Is.EqualTo(eGridDef.Food));

            // facing the outer wall
            var row = Enumerable.Range(1, server.Map.GetLength(1) - 2).First(r => server.Map[0, r] == eGridDef.Wall && !server.Map[1, r].HasFlag(eGridDef.Wall));
            PutOn(red, (1, row));
            red.Facing = eFacing.Left;
            Tick(server, 1);
            Assert.That(server.Map[0, row], Is.EqualTo(eGridDef.Wall));

            server.EndPowerUpEffect(red);
            Tick(server, 1);
            Assert.That(server.WeaponEffects, Is.Empty, "gone with the effect");
        }

        [Test]
        public void Hammer_KillsEnemiesAndGhosts_NotTeammates()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var (mate, _) = Join(server, 3, eTeamPreference.Red);
            mate.Team = eTeam.Red;
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food);
            PutOn(red, (x, y));
            red.Facing = eFacing.Right;
            server.ApplyPowerUp(red, ePowerUp.Hammer);

            PutOn(mate, (x + 1, y));
            Tick(server, 1);
            Assert.That(mate.Alive, Is.True);

            PutOn(blue, (x + 1, y));
            server.Ghosts[0].Location = blue.Location;
            Tick(server, 1);
            Assert.That(blue.Alive, Is.False);
            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(red.PlayerKills, Is.EqualTo(1));
            Assert.That(red.GhostKills, Is.EqualTo(1));
            Assert.That(red.Alive, Is.True, "the hammer doesn't protect, but nothing hit it");
        }

        // BFG

        [Test]
        public void Bfg_BurstsOnTheFirstThingItHits_KillingEverythingInRangeButTheInvincible()
        {
            var server = MakeServer(ghosts: 1);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var (starman, _) = Join(server, 3, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.BFG);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.Bfg));

            server.Ghosts[0].Location = red.Location;
            blue.Location = new Point2(red.Location.X + 3f, red.Location.Y);
            starman.Location = new Point2(red.Location.X - 3f, red.Location.Y);
            server.StartPowerUpEffect(starman, ePowerUpEffect.Invincible);

            server.HandleAction(red);
            Tick(server, 1);

            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(blue.Alive, Is.False);
            Assert.That(red.Alive, Is.False, "the shooter too");
            Assert.That(starman.Alive, Is.True);
            Assert.That(red.GhostKills, Is.EqualTo(1));
            Assert.That(red.PlayerKills, Is.EqualTo(1));
            Assert.That(server.WeaponEffects, Is.Empty);
            Assert.That(connection.ReceivedOf<GameEventMessage>().Any(e => e.EventType == eGameEvent.BfgBurst), Is.True);
        }

        // Mushroom

        [Test]
        public void SuperSize_GrowsTo300_AndBackTo0()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Size = 40f;

            server.ApplyPowerUp(red, ePowerUp.Mushroom);
            Assert.That(red.Size, Is.EqualTo(PowerUpEffects.SUPER_SIZE));
            Assert.That(red.PassesThroughWalls, Is.True);
            Assert.That(red.PassesThrough, Is.False, "still meets pacs and ghosts");

            server.EndPowerUpEffect(red);
            Assert.That(red.Size, Is.EqualTo(PacRules.START_SIZE));
        }

        [Test]
        public void SuperSize_SmashesWallsItMovesInto_AndBooms()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Wall);
            PutOn(red, (x, y));
            red.Facing = eFacing.Right;
            GetServerPlayer(server, red).Input = eDirection.Right;
            server.ApplyPowerUp(red, ePowerUp.Mushroom);

            for (var i = 0; i < 20 && MazeMovement.GetCell(red.Location).X == x; i++)
                server.MovePacs(TICK_SECONDS, TICK_MS);

            Assert.That(MazeMovement.GetCell(red.Location), Is.EqualTo((x + 1, y)));
            Assert.That(server.Map[x + 1, y], Is.EqualTo(eGridDef.Food));
            Assert.That(connection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.WallSmash), Is.EqualTo(1));
        }

        [Test]
        public void SmashWall_NeverTheOuterWall()
        {
            var server = MakeServer();
            Assert.That(server.SmashWall(0, 0), Is.False);
            Assert.That(server.Map[0, 0], Is.EqualTo(eGridDef.Wall));
            Assert.That(WeaponRules.IsOuterCell(server.Map.GetLength(0) - 1, 5, server.Map.GetLength(0), server.Map.GetLength(1)), Is.True);
        }

        // Energy tank

        [Test]
        public void EnergyTank_Size150_Permanent_NeverStarves_UntilReplacedThenMaxSize()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.EnergyTank);

            Assert.That(red.Size, Is.EqualTo(PowerUpEffects.ENERGY_TANK_SIZE));
            Assert.That(PowerUpEffects.IsPermanent(ePowerUpEffect.EnergyTank), Is.True);

            PutOn(red, FindCell(server, (cell, _) => cell == eGridDef.Wall));
            red.MsSinceEaten = 999999;
            server.UpdateEating(10000);
            Assert.That(red.Size, Is.EqualTo(PowerUpEffects.ENERGY_TANK_SIZE));
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.EnergyTank));

            server.ApplyPowerUp(red, ePowerUp.Cherry);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.SugarRush), "one buff at a time");
            Assert.That(red.Size, Is.EqualTo(new GameConfiguration().PacMaxSize), "losing the tank drops to the normal max size");
        }

        // Screw attack

        [Test]
        public void ScrewAttack_EatsAnyEnemy_ButNotTheInvincible()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.ScrewAttack);
            red.Size = 0f;
            blue.Size = 100f;
            blue.Location = red.Location;

            server.UpdatePacFights();
            Assert.That(blue.Alive, Is.False);
            Assert.That(red.PlayerKills, Is.EqualTo(1));

            var (star, _) = Join(server, 3, eTeamPreference.Blue);
            server.StartPowerUpEffect(star, ePowerUpEffect.Invincible);
            star.Location = red.Location;
            server.UpdatePacFights();
            Assert.That(red.Alive, Is.False);
        }

        [Test]
        public void ScrewAttack_KillsGhosts_AndCantEat()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.ScrewAttack);

            var dot = FindCell(server, (cell, _) => cell == eGridDef.Food);
            PutOn(red, dot);
            server.UpdateEating(TICK_MS);
            Assert.That(server.Map[dot.X, dot.Y], Is.EqualTo(eGridDef.Food));

            server.Ghosts[0].Location = red.Location;
            server.UpdateGhosts(0f, 0);
            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(red.Alive, Is.True);
        }

        // Pepper shaker

        [Test]
        public void Pepper_StunsAGhostInsteadOfDying()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.PepperShaker);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.Pepper));

            server.Ghosts[0].Location = red.Location;
            server.UpdateGhosts(0f, 0);

            Assert.That(red.Alive, Is.True);
            Assert.That(server.Ghosts[0].Alive, Is.True);
            Assert.That(server.Ghosts[0].StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
        }

        // Spread gun

        [Test]
        public void SpreadShot_FiresFourWaysEveryHalfSecond_AndStuns()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.SpreadGun);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.SpreadShot));

            // on a crossroads, so no shot meets a wall straight away
            var map = server.Map;
            var crossroads = Enumerable.Range(2, map.GetLength(0) - 4)
                .SelectMany(x => Enumerable.Range(2, map.GetLength(1) - 4).Select(y => (X: x, Y: y)))
                .First(c => !map[c.X, c.Y].HasFlag(eGridDef.Wall) && !map[c.X - 1, c.Y].HasFlag(eGridDef.Wall) && !map[c.X + 1, c.Y].HasFlag(eGridDef.Wall)
                    && !map[c.X, c.Y - 1].HasFlag(eGridDef.Wall) && !map[c.X, c.Y + 1].HasFlag(eGridDef.Wall));
            PutOn(red, crossroads);

            server.Ghosts[0].Alive = false;
            Tick(server, 1);
            var shots = server.WeaponEffects.Where(e => e.Kind == eWeaponEffect.SpreadShot).ToList();
            Assert.That(shots.Select(s => s.Facing), Is.EquivalentTo(Enum.GetValues<eFacing>()));
            Assert.That(red.SpreadShotMs, Is.EqualTo(WeaponRules.SPREAD_SHOT_INTERVAL_MS - TICK_MS), "next volley in half a second");

            // a ghost on the pac is hit by the next volley
            server.Ghosts[0].Alive = true;
            server.Ghosts[0].Location = red.Location;
            Tick(server, WeaponRules.SPREAD_SHOT_INTERVAL_MS / TICK_MS);
            Assert.That(server.Ghosts[0].StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(server.Ghosts[0].Alive, Is.True);
        }

        // Triforce

        [Test]
        public void ForceField_BringsTheTeamsFlagsHome_AsRescues_EvenFromACarrier()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var redFlag = server.Flags.First(f => f.Team == eTeam.Red);
            blue.Location = new Point2(redFlag.X, redFlag.Y);
            server.UpdateFlags();
            Assert.That(blue.HasFlag, Is.True);

            server.ApplyPowerUp(red, ePowerUp.Triforce);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.ForceField));
            Assert.That(redFlag.IsAtBase, Is.True);
            Assert.That(blue.HasFlag, Is.False);
            Assert.That(red.FlagRescues, Is.EqualTo(1));
            Assert.That(server.RedScore, Is.EqualTo(new GameConfiguration().FlagRescue));
            Assert.That(connection.ReceivedOf<GameEventMessage>().Any(e => e.EventType == eGameEvent.FlagReturned && e.Team == eTeam.Red), Is.True);
            Assert.That(redFlag.IsSealed, Is.True);
        }

        [Test]
        public void ForceField_EnemiesCantTakeASealedFlag_UntilTheBuffEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var redFlag = server.Flags.First(f => f.Team == eTeam.Red);
            server.ApplyPowerUp(red, ePowerUp.Triforce);
            Assert.That(red.FlagRescues, Is.EqualTo(0), "nothing to bring home");

            blue.Location = new Point2(redFlag.X, redFlag.Y);
            server.UpdateFlags();
            Assert.That(blue.HasFlag, Is.False);
            Assert.That(server.BuildSnapshot().Flags.Single(f => f.FlagId == redFlag.Id).IsSealed, Is.True);

            server.EndPowerUpEffect(red);
            server.UpdateFlags();
            Assert.That(redFlag.IsSealed, Is.False);
            Assert.That(blue.HasFlag, Is.True);
        }

        [Test]
        public void ForceField_OwnTeamStillCapturesOntoASealedFlag()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red;
            CarryBlueFlag(server, red);

            server.ApplyPowerUp(mate, ePowerUp.Triforce);
            var redFlag = server.Flags.First(f => f.Team == eTeam.Red);
            Assert.That(redFlag.IsSealed, Is.True);

            red.Location = new Point2(redFlag.X, redFlag.Y);
            server.UpdateFlags();
            Assert.That(red.FlagCaps, Is.EqualTo(1));
        }

        [Test]
        public void ForceField_SealEndsWhenThePacDies()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.Triforce);

            red.Alive = false;
            server.UpdateFlags();
            Assert.That(server.Flags.Where(f => f.Team == eTeam.Red).All(f => !f.IsSealed), Is.True);
        }

        [Test]
        public void CanBePickedUpBy_OnlyEnemiesOfALooseUnsealedFlag()
        {
            var flag = new GameFlag(0, eTeam.Red, 1.5f, 1.5f);
            Assert.That(flag.CanBePickedUpBy(eTeam.Blue), Is.True);
            Assert.That(flag.CanBePickedUpBy(eTeam.Red), Is.False);

            flag.IsSealed = true;
            Assert.That(flag.CanBePickedUpBy(eTeam.Blue), Is.False);

            flag.IsSealed = false;
            flag.CarrierId = 7;
            Assert.That(flag.CanBePickedUpBy(eTeam.Blue), Is.False);
        }

        [Test]
        public void FlagState_RoundTripsIsSealed()
        {
            var flag = new FlagState(3, eTeam.Blue, 1f, 2f, FlagState.NO_CARRIER, isSealed: true);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
                flag.Write(writer);

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            Assert.That(FlagState.Read(reader).IsSealed, Is.True);
        }

        [Test]
        public void ForceFieldBubble_SolidRim_FaintCentre_NothingOutside()
        {
            const int SIZE = 96;
            Assert.That(ForceField.GetAlpha(0, 0, SIZE), Is.EqualTo(0f), "corner");
            Assert.That(ForceField.GetAlpha(SIZE / 2, 1, SIZE), Is.EqualTo(1f), "rim");
            Assert.That(ForceField.GetAlpha(SIZE / 2, SIZE / 2, SIZE), Is.EqualTo(ForceField.CENTRE_ALPHA).Within(0.01f), "centre");

            Assert.That(ForceField.GetPulse(0f), Is.EqualTo(ForceField.PULSE_MIN).Within(0.001f));
            Assert.That(ForceField.GetPulse(ForceField.PULSE_SECONDS * 0.5f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(ForceField.IsMiniMapGold(0f), Is.True);
            Assert.That(ForceField.IsMiniMapGold(ForceField.MINI_MAP_BLINK_SECONDS * 1.5f), Is.False);
        }

        // drawing

        [Test]
        public void WeaponEffectFrames_LastAQuarterSecond()
        {
            Assert.That(WeaponEffectSprites.GetFrame(0.24f), Is.EqualTo(0));
            Assert.That(WeaponEffectSprites.GetFrame(0.26f), Is.EqualTo(1));
            Assert.That(WeaponEffectSprites.GetFrame(0.51f), Is.EqualTo(0));
        }

        [Test]
        public void ScrewAttack_FlashesWhite_AndSpinsOnlyWhenMoving()
        {
            Assert.That(PowerUpEffects.FlashesWhite(ePowerUpEffect.ScrewAttack), Is.True);
            Assert.That(PowerUpEffects.FlashesWhite(ePowerUpEffect.FireFlower), Is.True);
            Assert.That(PowerUpEffects.FlashesWhite(ePowerUpEffect.SugarRush), Is.False);
            Assert.That(PowerUpEffects.GetScrewAngle(3f, 4f), Is.EqualTo(PowerUpEffects.GetScrewAngle(3f, 4f)));
            Assert.That(PowerUpEffects.GetScrewAngle(3.1f, 4f), Is.Not.EqualTo(PowerUpEffects.GetScrewAngle(3f, 4f)));
        }
    }
}
