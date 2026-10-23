using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    /// <summary>
    /// The action button's cooldown, flags and effects, the respawn countdown, and the pear, banana, heart container,
    /// tetromino, hammer, BFG, mushroom, energy tank, rolling thunder, pepper shaker, spread gun and triangle force.
    /// </summary>
    public class ItemEffectTests
    {
        private const int TICK_MS = 16;

        private static GameServer MakeServer(int ghosts = 0)
        {
            var settings = new ServerSettings
            {
                ServerName = "Items",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = ghosts },
            };

            return ServerTestHelper.SkipCountdown(new GameServer(settings, []));
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
                server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(TICK_MS));
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
            server.StartPowerUpEffect(red, ePowerUp.Pear);

            var start = red.Location;
            server.HandleAction(red);
            var warped = red.Location;
            Assert.That(warped, Is.Not.EqualTo(start));

            server.HandleAction(red);
            Assert.That(red.Location, Is.EqualTo(warped), "cooling down");

            server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(WeaponRules.ACTION_COOLDOWN_MS));
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

            server.StartPowerUpEffect(red, ePowerUp.Cherry);
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
            server.UpdatePowerUps(TimeSpan.Zero);

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
            server.UpdateEating(TimeSpan.FromMilliseconds(TICK_MS));

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.PowerPill));
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

            server.StartPowerUpEffect(blue, ePowerUp.Key);
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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Pear));

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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Banana));

            var cell = MazeMovement.GetCell(red.Location);
            server.HandleAction(red);
            server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(WeaponRules.ACTION_COOLDOWN_MS));
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
        public void HeartContainer_StartsFullHeal_FillsUpToMaxSize_AndResetsHunger()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.StartPowerUpEffect(red, ePowerUp.Cherry);
            red.Size = 10f;
            red.MsSinceEaten = 5000;

            server.ApplyPowerUp(red, ePowerUp.HeartContainer);

            Assert.That(red.Size, Is.EqualTo(new GameConfiguration().PacMaxSize));
            Assert.That(red.MsSinceEaten, Is.EqualTo(0));
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.HeartContainer), "a normal effect: it replaces the buff");
            Assert.That(red.PowerUpMs, Is.EqualTo(new GameConfiguration().PowerUpDurationMs));

            red.Size = 150f;
            server.ApplyPowerUp(red, ePowerUp.HeartContainer);
            Assert.That(red.Size, Is.EqualTo(150f), "never smaller");
        }

        [Test]
        public void FullHeal_ReplacingAPowerPill_StillFillsUpToMaxSize()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Size = 10f;
            server.StartPowerUpEffect(red, ePowerUp.PowerPill);

            server.ApplyPowerUp(red, ePowerUp.HeartContainer);

            Assert.That(red.Size, Is.EqualTo(new GameConfiguration().PacMaxSize), "the power pill's gain goes first");
        }

        [Test]
        public void FullHeal_NeverStarves_AndKeepsItsSizeWhenItEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.HeartContainer);
            red.Size = 70f;

            server.EndPowerUpEffect(red);

            Assert.That(red.Size, Is.EqualTo(70f));
            Assert.That(PowerUpEffects.PreventsStarving(ePowerUp.HeartContainer), Is.True);
        }

        // Tetromino

        [Test]
        public void BlockDrop_LeavesABlockInEachCellLeft_UntilTheEffectEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food);
            PutOn(red, (x, y));
            red.Facing = eFacing.Right;

            server.ApplyPowerUp(red, ePowerUp.Skyblock);
            server.HandleAction(red);
            server.UpdateWeaponEffects(TimeSpan.Zero);
            server.UpdateWeaponEffects(TimeSpan.Zero);

            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food), "no block until the pac leaves the cell, and the action button does nothing");
            Assert.That(server.WeaponEffects, Is.Empty);

            PutOn(red, (x + 1, y));
            server.UpdateWeaponEffects(TimeSpan.Zero);

            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food | eGridDef.Wall | eGridDef.TetrisBlock));
            Assert.That(server.Map[x + 1, y], Is.EqualTo(eGridDef.Food), "the cell the pac is in stays open");
            Assert.That(server.WeaponEffects.Single().Kind, Is.EqualTo(eWeaponEffect.TetrisBlock));

            Tick(server, 1);
            Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.True, "lasts while the effect works");

            server.EndPowerUpEffect(red);
            Tick(server, 1);
            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food));
            Assert.That(server.WeaponEffects, Is.Empty);
        }

        [Test]
        public void BlockDrop_SkipsACellWithAPacOrGhostInIt()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food);
            var ghost = server.Ghosts[0];
            server.ApplyPowerUp(red, ePowerUp.Skyblock);

            // a pac in the cell left
            PutOn(red, (x, y));
            PutOn(blue, (x, y));
            server.UpdateWeaponEffects(TimeSpan.Zero);
            PutOn(red, (x + 1, y));
            server.UpdateWeaponEffects(TimeSpan.Zero);
            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food), "a pac is in it");

            // a ghost in the cell left
            PutOn(blue, (x + 1, y));
            ghost.Alive = true;
            ghost.Location = new Point2(x + 0.5f, y + 0.5f);
            PutOn(red, (x, y));
            server.UpdateWeaponEffects(TimeSpan.Zero);
            PutOn(red, (x + 1, y));
            server.UpdateWeaponEffects(TimeSpan.Zero);
            Assert.That(server.Map[x, y], Is.EqualTo(eGridDef.Food), "a ghost is in it");
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

            server.ApplyPowerUp(red, ePowerUp.HammerTime);
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
            server.ApplyPowerUp(red, ePowerUp.HammerTime);

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

        // Doctor Away

        [Test]
        public void DoctorAway_KillsGhostsInRange_AndScoresThem()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.Apple);
            var ghost = server.Ghosts[0];

            ghost.Location = new Point2(red.Location.X + PowerUpEffects.EFFECT_RADIUS + 2f, red.Location.Y);
            Tick(server, 1);
            Assert.That(ghost.Alive, Is.True, "out of range");

            ghost.Location = new Point2(red.Location.X + PowerUpEffects.EFFECT_RADIUS - 1f, red.Location.Y);
            Tick(server, 1);
            Assert.That(ghost.Alive, Is.False);
            Assert.That(red.GhostKills, Is.EqualTo(1));
            Assert.That(red.Score, Is.EqualTo(new GameConfiguration().GhostKill));
        }

        [Test]
        public void DoctorAway_KillsNothingOnceItEnds()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.Apple);
            server.UpdateEating(TimeSpan.FromMilliseconds(new GameConfiguration().PowerUpDurationMs));
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None));

            server.Ghosts[0].Location = new Point2(red.Location.X + 3f, red.Location.Y);
            server.UpdateWeaponEffects(TimeSpan.Zero);

            Assert.That(server.Ghosts[0].Alive, Is.True);
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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.BFG));

            server.Ghosts[0].Location = red.Location;
            blue.Location = new Point2(red.Location.X + 3f, red.Location.Y);
            starman.Location = new Point2(red.Location.X - 3f, red.Location.Y);
            server.StartPowerUpEffect(starman, ePowerUp.Starman);

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

        // Melon

        [Test]
        public void MelonRoll_RollsStraightOnAtTripleSpeed_WhateverIsHeld()
        {
            var server = MakeServer();
            var config = new GameConfiguration();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food);
            PutOn(red, (x, y));
            red.Facing = eFacing.Right;
            server.ApplyPowerUp(red, ePowerUp.Melon);
            Assert.That(red.IsRolling, Is.False, "only the action button starts a roll");

            server.HandleAction(red);
            Assert.That(red.IsRolling, Is.True);
            Assert.That(server.BuildSnapshot().Pacs.Single(p => p.PlayerId == red.Id).Rolling, Is.True);

            GetServerPlayer(server, red).Input = eDirection.Left;
            server.MovePacs(TimeSpan.FromMilliseconds(TICK_MS));

            var expected = PacRules.GetSpeed(config.PacSpeed, red.Size, config.PacMaxSize) * MelonRoll.SPEED_SCALE * TICK_MS / 1000f;
            Assert.That(red.Facing, Is.EqualTo(eFacing.Right), "the player has no control");
            Assert.That(red.Location.X, Is.EqualTo(x + 0.5f + expected).Within(0.001f));
        }

        [Test]
        public void MelonRoll_SmashesWalls_StopsWhereItCantGoOn_AndCanRollAgain()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var map = server.Map;
            var width = map.GetLength(0);
            var (x, y) = (-1, -1);

            // a dot cell with an inner wall to its right, on a row with no warp tunnel so the roll can't wrap round forever
            for (var row = 2; row < map.GetLength(1) - 2 && x < 0; row++)
                for (var col = 2; col < width - 3 && x < 0; col++)
                    if (map[col, row] == eGridDef.Food && map[col + 1, row] == eGridDef.Wall
                        && map[0, row].HasFlag(eGridDef.Wall) && map[width - 1, row].HasFlag(eGridDef.Wall))
                        (x, y) = (col, row);

            PutOn(red, (x, y));
            red.Facing = eFacing.Right;
            server.ApplyPowerUp(red, ePowerUp.Melon);
            server.HandleAction(red);

            for (var i = 0; i < 1000 && red.IsRolling; i++)
                server.MovePacs(TimeSpan.FromMilliseconds(TICK_MS));

            var (cx, cy) = MazeMovement.GetCell(red.Location);
            Assert.That(red.IsRolling, Is.False, "stopped");
            Assert.That(map[x + 1, y], Is.EqualTo(eGridDef.Food), "the wall was smashed into a dot cell");
            Assert.That(connection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.WallSmash), Is.GreaterThan(0));
            Assert.That(red.Location, Is.EqualTo(new Point2(cx + 0.5f, cy + 0.5f)), "on a cell centre");
            Assert.That(MelonRoll.CanRollInto(map, cx + 1, cy, MazeMovement.GetPacBlocked(red.Team)), Is.False);

            red.ActionCooldownMs = 0;
            GetServerPlayer(server, red).Input = eDirection.Left;
            server.HandleAction(red);
            Assert.That(red.IsRolling, Is.True, "rolls again while the effect lasts");
            Assert.That(red.Facing, Is.EqualTo(eFacing.Left), "the way the player holds");
        }

        [Test]
        public void MelonRoll_StunsEveryoneItTouches_AndNobodyGetsHurt()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            var (blue, _) = Join(server, 3, eTeamPreference.Blue);
            PutOn(red, FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food));
            red.Facing = eFacing.Right;
            server.ApplyPowerUp(red, ePowerUp.Melon);
            server.HandleAction(red);

            // a bigger enemy, a teammate and a ghost right on it
            blue.Size = 100f;
            blue.Location = red.Location;
            mate.Location = red.Location;
            var ghost = server.Ghosts[0];
            ghost.Location = red.Location;

            server.MovePacs(TimeSpan.Zero);
            Assert.That(blue.StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(mate.StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(ghost.StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(red.IsStunned, Is.False);

            server.UpdatePacFights();
            server.UpdateGhosts(TimeSpan.Zero);
            Assert.That(red.Alive && mate.Alive && blue.Alive && ghost.Alive, Is.True);
        }

        [Test]
        public void MelonRoll_EndsWithTheEffect()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Food));
            red.Facing = eFacing.Right;
            server.ApplyPowerUp(red, ePowerUp.Melon);
            server.HandleAction(red);

            server.UpdateEating(TimeSpan.FromMilliseconds(new GameConfiguration().PowerUpDurationMs));
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None));
            Assert.That(red.IsRolling, Is.False);
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
                server.MovePacs(TimeSpan.FromMilliseconds(TICK_MS));

            Assert.That(MazeMovement.GetCell(red.Location), Is.EqualTo((x + 1, y)));
            Assert.That(server.Map[x + 1, y], Is.EqualTo(eGridDef.Food));
            Assert.That(connection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.WallSmash), Is.GreaterThan(0));
        }

        [Test]
        public void SuperSize_SmashesEveryWallItsBodyCovers()
        {
            var server = MakeServer();
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (x, y) = FindCell(server, (left, right) => left == eGridDef.Food && right == eGridDef.Wall);
            PutOn(red, (x, y));
            server.ApplyPowerUp(red, ePowerUp.Mushroom);

            // standing still in its own cell, its 4 cell wide body already reaches the wall next door
            server.MovePacs(TimeSpan.Zero);

            var cells = new List<(int X, int Y)>();
            WeaponRules.GetTouchedCells(red.Location, PacRules.GetRadius(red.Size), server.Map.GetLength(0), server.Map.GetLength(1), cells);
            Assert.That(MazeMovement.GetCell(red.Location), Is.EqualTo((x, y)));
            Assert.That(server.Map[x + 1, y], Is.EqualTo(eGridDef.Food));
            Assert.That(cells.Where(c => !WeaponRules.IsOuterCell(c.X, c.Y, server.Map.GetLength(0), server.Map.GetLength(1)))
                .All(c => !server.Map[c.X, c.Y].HasFlag(eGridDef.Wall) || server.Map[c.X, c.Y].HasFlag(eGridDef.TetrisBlock)), Is.True);
            Assert.That(connection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.WallSmash), Is.EqualTo(1));
        }

        [Test]
        public void SuperSize_KillsAnyEnemyAndGhost_ButNotTheInvincible()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.Mushroom);
            Assert.That(red.KillsOnContact, Is.True);

            // even an enemy as big as it is gets eaten
            blue.Size = PowerUpEffects.SUPER_SIZE;
            blue.Location = red.Location;
            server.UpdatePacFights();
            Assert.That(blue.Alive, Is.False);
            Assert.That(red.PlayerKills, Is.EqualTo(1));

            server.Ghosts[0].Location = red.Location;
            server.UpdateGhosts(TimeSpan.Zero);
            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(red.Alive, Is.True);

            var (star, _) = Join(server, 3, eTeamPreference.Blue);
            server.StartPowerUpEffect(star, ePowerUp.Starman);
            star.Location = red.Location;
            server.UpdatePacFights();
            Assert.That(red.Alive, Is.False);
        }

        [Test]
        public void KillsOnContact_StarRollingThunderAndSuperSizeOnly()
        {
            var killers = Enum.GetValues<ePowerUp>().Where(PowerUpEffects.KillsOnContact);
            Assert.That(killers, Is.EquivalentTo(new[] { ePowerUp.Starman, ePowerUp.RollingThunder, ePowerUp.Mushroom }));
        }

        [Test]
        public void GetTouchedCells_MatchesTheBodysSize()
        {
            var cells = new List<(int X, int Y)>();

            // size 0 is one cell wide: only its own cell
            WeaponRules.GetTouchedCells(new Point2(5.5f, 5.5f), PacRules.GetRadius(0f), 20, 20, cells);
            Assert.That(cells, Is.EquivalentTo(new[] { (5, 5) }));

            // Super Size is four cells wide: two cells each way, cut off at the corners
            WeaponRules.GetTouchedCells(new Point2(5.5f, 5.5f), PacRules.GetRadius(PowerUpEffects.SUPER_SIZE), 20, 20, cells);
            Assert.That(cells, Does.Contain((3, 5)).And.Contain((7, 5)).And.Contain((5, 3)).And.Contain((5, 7)).And.Contain((4, 4)));
            Assert.That(cells, Does.Not.Contain((3, 3)).And.Not.Contain((2, 5)));

            // off the map is left out
            WeaponRules.GetTouchedCells(new Point2(0.5f, 0.5f), PacRules.GetRadius(PowerUpEffects.SUPER_SIZE), 20, 20, cells);
            Assert.That(cells.All(c => c.X >= 0 && c.Y >= 0), Is.True);
            Assert.That(cells, Does.Contain((2, 0)));
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
        public void EnergyTank_KeepsSize_GrowsPastMaxSize_NeverStarves()
        {
            var server = MakeServer();
            var config = new GameConfiguration();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            red.Size = config.PacMaxSize;
            server.ApplyPowerUp(red, ePowerUp.EnergyTank);

            Assert.That(red.Size, Is.EqualTo(config.PacMaxSize), "the tank doesn't change the size");

            PutOn(red, FindCell(server, (cell, _) => cell.HasFlag(eGridDef.Food) && !cell.HasFlag(eGridDef.PowerPill)));
            server.UpdateEating(TimeSpan.FromMilliseconds(1));
            Assert.That(red.Size, Is.EqualTo(config.PacMaxSize + config.DotSizeGain), "dots grow it past PacMaxSize");

            red.Size = config.PacMaxSize * 2f;
            PutOn(red, FindCell(server, (cell, _) => cell.HasFlag(eGridDef.Food) && !cell.HasFlag(eGridDef.PowerPill)));
            server.UpdateEating(TimeSpan.FromMilliseconds(1));
            Assert.That(red.Size, Is.EqualTo(config.PacMaxSize * 2f), "but no further than twice it");

            PutOn(red, FindCell(server, (cell, _) => cell == eGridDef.Wall));
            red.MsSinceEaten = 999999;
            server.UpdateEating(TimeSpan.FromMilliseconds(1));
            Assert.That(red.MsSinceEaten, Is.EqualTo(0), "never starves");
            Assert.That(red.Size, Is.EqualTo(config.PacMaxSize * 2f));
        }

        [Test]
        public void EnergyTank_RunsOut_BackToMaxSize()
        {
            var server = MakeServer();
            var config = new GameConfiguration();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.EnergyTank);
            red.Size = config.PacMaxSize * 1.8f;

            Assert.That(red.PowerUpMs, Is.EqualTo(config.PowerUpDurationMs));
            red.PowerUpMs = 1;

            PutOn(red, FindCell(server, (cell, _) => cell == eGridDef.Wall));
            server.UpdateEating(TimeSpan.FromMilliseconds(1));

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.None), "a normal timed effect");
            Assert.That(red.Size, Is.EqualTo(config.PacMaxSize));
        }

        [Test]
        public void EnergyTank_Replaced_SmallerPacKeepsSize()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.EnergyTank);
            red.Size = 40f;

            server.ApplyPowerUp(red, ePowerUp.Cherry);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Cherry), "one buff at a time");
            Assert.That(red.Size, Is.EqualTo(40f));
        }

        // Rolling Thunder

        [Test]
        public void RollingThunder_EatsAnyEnemy_ButNotTheInvincible()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.RollingThunder);
            red.Size = 0f;
            blue.Size = 100f;
            blue.Location = red.Location;

            server.UpdatePacFights();
            Assert.That(blue.Alive, Is.False);
            Assert.That(red.PlayerKills, Is.EqualTo(1));

            var (star, _) = Join(server, 3, eTeamPreference.Blue);
            server.StartPowerUpEffect(star, ePowerUp.Starman);
            star.Location = red.Location;
            server.UpdatePacFights();
            Assert.That(red.Alive, Is.False);
        }

        [Test]
        public void RollingThunder_KillsGhosts_AndCantEat()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.RollingThunder);

            var dot = FindCell(server, (cell, _) => cell == eGridDef.Food);
            PutOn(red, dot);
            server.UpdateEating(TimeSpan.FromMilliseconds(TICK_MS));
            Assert.That(server.Map[dot.X, dot.Y], Is.EqualTo(eGridDef.Food));

            server.Ghosts[0].Location = red.Location;
            server.UpdateGhosts(TimeSpan.Zero);
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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.PepperShaker));

            server.Ghosts[0].Location = red.Location;
            server.UpdateGhosts(TimeSpan.Zero);

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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.SpreadGun));

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

        // Triangle Force

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

            server.ApplyPowerUp(red, ePowerUp.Triangleforce);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Triangleforce));
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
            server.ApplyPowerUp(red, ePowerUp.Triangleforce);
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

            server.ApplyPowerUp(mate, ePowerUp.Triangleforce);
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
            server.ApplyPowerUp(red, ePowerUp.Triangleforce);

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

            Assert.That(ForceField.GetPulse(TimeSpan.Zero), Is.EqualTo(ForceField.PULSE_MIN).Within(0.001f));
            Assert.That(ForceField.GetPulse(TimeSpan.FromMilliseconds(ForceField.PULSE_MS / 2)), Is.EqualTo(1f).Within(0.001f));
            Assert.That(ForceField.IsMiniMapGold(TimeSpan.Zero), Is.True);
            Assert.That(ForceField.IsMiniMapGold(TimeSpan.FromMilliseconds(ForceField.MINI_MAP_BLINK_MS * 1.5)), Is.False);
        }

        // drawing

        [Test]
        public void WeaponEffectFrames_LastAQuarterSecond()
        {
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.FromMilliseconds(240)), Is.EqualTo(0));
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.FromMilliseconds(260)), Is.EqualTo(1));
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.FromMilliseconds(510)), Is.EqualTo(0));
        }

        [Test]
        public void RollingThunder_FlashesWhite_AndSpinsOnlyWhenMoving()
        {
            Assert.That(PowerUpEffects.FlashesWhite(ePowerUp.RollingThunder), Is.True);
            Assert.That(PowerUpEffects.FlashesWhite(ePowerUp.SunFlower), Is.True);
            Assert.That(PowerUpEffects.FlashesWhite(ePowerUp.Cherry), Is.False);
            Assert.That(PowerUpEffects.GetRollingThunderAngle(3f, 4f), Is.EqualTo(PowerUpEffects.GetRollingThunderAngle(3f, 4f)));
            Assert.That(PowerUpEffects.GetRollingThunderAngle(3.1f, 4f), Is.Not.EqualTo(PowerUpEffects.GetRollingThunderAngle(3f, 4f)));
        }
    }
}
