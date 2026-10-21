using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    /// <summary>
    /// The power pill item, Mega Man 1-up, Morph Ball, Super Leaf, White Sword, Fire Flower, Holy Water, Blue Shell and Poke Ball.
    /// </summary>
    public class BuffAndWeaponTests
    {
        private const int TICK_MS = 16;
        private const float TICK_SECONDS = TICK_MS / 1000f;

        private static GameServer MakeServer(int ghosts = 0)
        {
            var settings = new ServerSettings
            {
                ServerName = "Weapons",
                TargetFps = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = ghosts },
            };

            return new GameServer(settings, []);
        }

        private static GameConfiguration Config => new();

        private static (Player Player, FakeConnection Connection) Join(GameServer server, int id, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = team });
            return (server.Players.Single(p => p.Connection == connection).Player, connection);
        }

        private static void PutOn(Player player, (int X, int Y) cell)
        {
            player.Location = new Point2(cell.X + 0.5f, cell.Y + 0.5f);
        }

        /// <summary>
        /// An open, plain cell with an open cell to its right and a wall to its left, away from the edges.
        /// </summary>
        private static (int X, int Y) FindOpenCellWithWallLeft(GameServer server)
        {
            var map = server.Map;

            for (var y = 2; y < map.GetLength(1) - 2; y++)
                for (var x = 2; x < map.GetLength(0) - 2; x++)
                    if (map[x, y] == eGridDef.Food && map[x - 1, y] == eGridDef.Wall && !map[x + 1, y].HasFlag(eGridDef.Wall))
                        return (x, y);

            throw new InvalidOperationException("No cell with a wall on its left");
        }

        /// <summary>
        /// A far away open cell from the pac's spawn, so seekers have a way to go.
        /// </summary>
        private static (int X, int Y) FindFarOpenCell(GameServer server, Player from)
        {
            var paths = new PathMap();
            var (x, y) = MazeMovement.GetCell(from.Location);
            paths.Fill(server.Map, x, y);
            return paths.Order[paths.Order.Count / 2];
        }

        private static void Tick(GameServer server, int ticks)
        {
            for (var i = 0; i < ticks; i++)
                server.UpdateWeaponEffects(TICK_SECONDS, TICK_MS);
        }

        // Power pill item

        [Test]
        public void PowerPillItem_StartsThePowerPillEffect()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var size = red.Size;
            server.ApplyPowerUp(red, ePowerUp.PowerPill);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.PowerPill));
            Assert.That(red.IsPowered, Is.True);
            Assert.That(red.Size, Is.EqualTo(size + Config.PowerPillSizeGain));
        }

        // Mega Man 1-up

        [Test]
        public void Reinforcements_TeamRespawnsAtOnce_WithTheChorus_EvenUnderLockdown()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, mateConnection) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red; // teams are kept even on join
            var (blue, _) = Join(server, 3, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.MegaManOneUp);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.Reinforcements));
            server.StartPowerUpEffect(blue, ePowerUpEffect.Lockdown);

            mate.Alive = false;
            mate.RespawnTimer = Config.PlayerRespawnTimeMs;
            server.UpdateRespawns(1);

            Assert.That(mate.Alive, Is.True);
            Assert.That(mateConnection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.AngelicRespawn && e.TargetId == mate.Id), Is.EqualTo(1));
        }

        [Test]
        public void Reinforcements_OnlyHelpsItsOwnTeam_AndOnlyWhileItsPacLives()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.StartPowerUpEffect(red, ePowerUpEffect.Reinforcements);
            blue.Alive = false;
            blue.RespawnTimer = Config.PlayerRespawnTimeMs;

            server.UpdateRespawns(1);
            Assert.That(blue.Alive, Is.False);
            Assert.That(server.IsReinforced(eTeam.Red), Is.True);

            red.Alive = false;
            Assert.That(server.IsReinforced(eTeam.Red), Is.False);
        }

        // Morph Ball and Super Leaf

        [Test]
        public void MorphBall_PassesThroughEnemiesAndGhosts()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.MorphBall);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.MorphBall));
            red.Size = 10f;
            blue.Size = 90f;
            blue.Location = red.Location;

            server.UpdatePacFights();
            Assert.That(red.Alive && blue.Alive, Is.True, "no fight");

            red.Location = server.Ghosts[0].Location;
            server.UpdateGhosts(0f, 0);
            Assert.That(red.Alive && server.Ghosts[0].Alive, Is.True, "no ghost contact");
        }

        [Test]
        public void SuperLeaf_PassesThroughWalls_AndEndsInAnOpenCell()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var cell = FindOpenCellWithWallLeft(server);
            PutOn(red, cell);
            red.Facing = eFacing.Left;
            server.Players.Single(p => p.Player == red).Input = eDirection.Left;
            server.ApplyPowerUp(red, ePowerUp.SuperLeaf);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.RaccoonTail));
            Assert.That(red.PassesThrough, Is.True);

            server.MovePacs(1f / PacRules.GetSpeed(Config.PacSpeed, red.Size, Config.PacMaxSize), 0);
            Assert.That(MazeMovement.GetCell(red.Location), Is.EqualTo((cell.X - 1, cell.Y)), "into the wall");

            server.EndPowerUpEffect(red);
            var (x, y) = MazeMovement.GetCell(red.Location);
            Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.False, "out of the wall");
        }

        [Test]
        public void SuperLeaf_WithoutIt_TheWallStopsThePac()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var cell = FindOpenCellWithWallLeft(server);
            PutOn(red, cell);
            red.Facing = eFacing.Left;
            server.Players.Single(p => p.Player == red).Input = eDirection.Left;

            server.MovePacs(1f, 0);

            Assert.That(MazeMovement.GetCell(red.Location), Is.EqualTo(cell));
        }

        // White Sword and Fire Flower

        [Test]
        public void Sword_AtMostTwoInFlight_OnlyWhileBuffed()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.HandleAction(red);
            Assert.That(server.WeaponEffects, Is.Empty, "no buff, no sword");

            server.ApplyPowerUp(red, ePowerUp.WhiteSword);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.SwordSwing));

            // the action button cools down between presses (no ticks, so the swords stay where they are)
            for (var i = 0; i < 3; i++)
            {
                red.ActionCooldownMs = 0;
                server.HandleAction(red);
            }

            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.Sword), Is.EqualTo(WeaponRules.MAX_SWORDS));
            Assert.That(server.WeaponEffects.First().Facing, Is.EqualTo(red.Facing));
        }

        [Test]
        public void Sword_KillsTheFirstThingItHits_TeammatesToo()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red; // teams are kept even on join
            server.StartPowerUpEffect(red, ePowerUpEffect.SwordSwing);
            mate.Location = red.Location;

            server.HandleAction(red);
            Tick(server, 1);

            Assert.That(mate.Alive, Is.False);
            Assert.That(red.Alive, Is.True, "not the thrower");
            Assert.That(server.WeaponEffects, Is.Empty, "used up");
            Assert.That(red.PlayerKills, Is.Zero, "no credit for a teammate");
        }

        [Test]
        public void Sword_IsStoppedByAWall()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindOpenCellWithWallLeft(server));
            red.Facing = eFacing.Left;
            server.StartPowerUpEffect(red, ePowerUpEffect.SwordSwing);

            server.HandleAction(red);
            Tick(server, 1);

            Assert.That(server.WeaponEffects, Is.Empty);
        }

        [Test]
        public void Fireball_OnePerHalfSecond_KillsEnemyAndGhost_NotInvincible()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.FireFlower);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.FireFlower));

            // an invincible enemy stops it but doesn't die
            server.StartPowerUpEffect(blue, ePowerUpEffect.Invincible);
            blue.Location = red.Location;
            server.HandleAction(red);
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count, Is.EqualTo(1), "cooling down");
            Tick(server, 1);
            Assert.That(blue.Alive, Is.True);
            Assert.That(server.WeaponEffects, Is.Empty);

            // a ghost in the way dies, and the shooter gets the kill
            server.Ghosts[0].Location = red.Location;
            blue.Location = new Point2(1.5f, 1.5f);
            server.UpdateWeaponEffects(WeaponRules.ACTION_COOLDOWN_MS / 1000f, WeaponRules.ACTION_COOLDOWN_MS);
            server.HandleAction(red);
            Tick(server, 1);
            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(red.GhostKills, Is.EqualTo(1));
        }

        // Holy Water

        [Test]
        public void HolyFire_LaysFlamesThatKillAnything_AndGoWhenItEnds()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red; // teams are kept even on join
            mate.Location = new Point2(1.5f, 1.5f);
            var start = FindOpenCellWithWallLeft(server);
            PutOn(red, start);
            server.ApplyPowerUp(red, ePowerUp.HolyWater);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.HolyFire));

            Tick(server, 1);
            Assert.That(server.WeaponEffects, Is.Empty, "nothing until it leaves a cell");

            PutOn(red, (start.X + 1, start.Y));
            Tick(server, 1);
            var flame = server.WeaponEffects.Single();
            Assert.That(flame.Kind, Is.EqualTo(eWeaponEffect.HolyFlame));
            Assert.That(MazeMovement.GetCell(flame.Location), Is.EqualTo(start));

            PutOn(mate, start);
            Tick(server, 1);
            Assert.That(mate.Alive, Is.False, "anything, teammates too");

            // the pac that laid it once it has moved off
            PutOn(red, (start.X + 3, start.Y));
            Tick(server, 1);
            PutOn(red, start);
            Tick(server, 1);
            Assert.That(red.Alive, Is.False);

            Tick(server, 1);
            Assert.That(server.WeaponEffects.Where(e => e.Kind == eWeaponEffect.HolyFlame), Is.Empty, "dying put them out");
        }

        [Test]
        public void HolyFire_FlamesGoWhenTheBuffIsReplaced()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var start = FindOpenCellWithWallLeft(server);
            PutOn(red, start);
            server.StartPowerUpEffect(red, ePowerUpEffect.HolyFire);
            Tick(server, 1);
            PutOn(red, (start.X + 1, start.Y));
            Tick(server, 1);
            Assert.That(server.WeaponEffects, Has.Count.EqualTo(1));

            server.StartPowerUpEffect(red, ePowerUpEffect.SugarRush);
            Tick(server, 1);

            Assert.That(server.WeaponEffects, Is.Empty);
        }

        // Blue Shell

        [Test]
        public void BlueShell_FollowsTheMazeToItsTarget_AndKillsIt()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            PutOn(blue, FindFarOpenCell(server, red));

            server.ApplyPowerUp(red, ePowerUp.BlueShell);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUpEffect.None), "instant: the buff is left alone");
            Assert.That(server.WeaponEffects.Single().Kind, Is.EqualTo(eWeaponEffect.BlueShell));

            for (var i = 0; i < 2000 && blue.Alive; i++)
            {
                Tick(server, 1);

                var (x, y) = MazeMovement.GetCell(server.WeaponEffects.FirstOrDefault()?.Location ?? blue.Location);
                Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.False, "never in a wall");
            }

            Assert.That(blue.Alive, Is.False);
            Assert.That(server.WeaponEffects, Is.Empty);
            Assert.That(red.PlayerKills, Is.EqualTo(1));
        }

        [Test]
        public void BlueShell_IgnoresEveryoneButItsTarget_AndIsGoneWithNoEnemy()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red; // teams are kept even on join
            mate.Location = red.Location;

            server.ApplyPowerUp(red, ePowerUp.BlueShell);
            Tick(server, 1);

            Assert.That(mate.Alive && red.Alive, Is.True);
            Assert.That(server.WeaponEffects, Is.Empty, "no enemy to go after");
        }

        [Test]
        public void PickBlueShellTarget_CarrierFirst_ElseHighestScore()
        {
            var low = new Player { Id = 1, Team = eTeam.Blue, Alive = true, Score = 5 };
            var high = new Player { Id = 2, Team = eTeam.Blue, Alive = true, Score = 9 };
            var friend = new Player { Id = 3, Team = eTeam.Red, Alive = true, Score = 50 };

            Assert.That(WeaponRules.PickBlueShellTarget(eTeam.Red, [low, high, friend]), Is.SameAs(high));

            low.HasFlag = true;
            Assert.That(WeaponRules.PickBlueShellTarget(eTeam.Red, [low, high, friend]), Is.SameAs(low));

            low.Alive = false;
            high.Alive = false;
            Assert.That(WeaponRules.PickBlueShellTarget(eTeam.Red, [low, high, friend]), Is.Null);
        }

        // Poke Ball

        [Test]
        public void PokeBall_CatchesTheNearestGhost_UntilSomethingTouchesIt()
        {
            var server = MakeServer(ghosts: 1);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            PutOn(red, FindFarOpenCell(server, red));

            server.ApplyPowerUp(red, ePowerUp.PokeBall);

            for (var i = 0; i < 2000 && !ghost.IsCaptured; i++)
                Tick(server, 1);

            Assert.That(ghost.IsCaptured, Is.True);
            Assert.That(ghost.Alive, Is.False);
            var ball = server.WeaponEffects.Single();
            Assert.That(ball.IsHoldingGhost, Is.True);
            Assert.That(connection.ReceivedOf<GameEventMessage>().Any(e => e.EventType == eGameEvent.PokeBallCapture), Is.True);

            // it holds still, and the ghost doesn't count down to respawn
            var held = ball.Location;
            Tick(server, 100);
            server.UpdateGhosts(TICK_SECONDS, Config.GhostRegenTimeMs * 2);
            Assert.That(ball.Location, Is.EqualTo(held));
            Assert.That(ghost.Alive, Is.False);

            red.Location = held;
            Tick(server, 1);

            Assert.That(ghost.Alive, Is.True);
            Assert.That(ghost.IsCaptured, Is.False);
            Assert.That(ghost.Location, Is.EqualTo(held));
            Assert.That(server.WeaponEffects, Is.Empty);
            Assert.That(connection.ReceivedOf<GameEventMessage>().Any(e => e.EventType == eGameEvent.PokeBallRelease), Is.True);
        }

        [Test]
        public void PokeBall_WithNoGhosts_WandersTheMaze()
        {
            var server = MakeServer(ghosts: 0);
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.PokeBall);
            var start = server.WeaponEffects.Single().Location;
            Tick(server, 120);

            var ball = server.WeaponEffects.Single();
            Assert.That(ball.Location, Is.Not.EqualTo(start));
            var (x, y) = MazeMovement.GetCell(ball.Location);
            Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.False);
        }

        [Test]
        public void Snapshot_CarriesWeaponEffects()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.StartPowerUpEffect(red, ePowerUpEffect.SwordSwing);
            server.HandleAction(red);

            var state = server.BuildSnapshot().WeaponEffects.Single();

            Assert.That(state.Kind, Is.EqualTo(eWeaponEffect.Sword));
            Assert.That(state.Facing, Is.EqualTo(red.Facing));
        }

        // bots

        [Test]
        public void ShouldFire_AtAnEnemyOrGhostAhead_NotATeammate()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Facing = eFacing.Right, Alive = true, PowerUpEffect = ePowerUpEffect.SwordSwing, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(5.5f, 1.5f), Alive = true };
            var mate = new Player { Id = 3, Team = eTeam.Red, Location = new Point2(3.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, enemy] };

            Assert.That(PacBrain.ShouldFire(world, me), Is.True);

            world.Pacs = [me, enemy, mate];
            Assert.That(PacBrain.ShouldFire(world, me), Is.False, "a teammate is in the way");

            world.Pacs = [me];
            world.Ghosts = [new Ghost { Location = new Point2(4.5f, 1.5f), Alive = true }];
            Assert.That(PacBrain.ShouldFire(world, me), Is.True);

            me.Facing = eFacing.Left;
            Assert.That(PacBrain.ShouldFire(world, me), Is.False, "nothing behind it before the wall");

            me.Facing = eFacing.Right;
            me.PowerUpEffect = ePowerUpEffect.SugarRush;
            Assert.That(PacBrain.ShouldFire(world, me), Is.False, "nothing to fire");
        }

        [Test]
        public void PassingThrough_IsNeitherThreatNorPrey()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, Size = 10f };
            var big = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(3.5f, 1.5f), Alive = true, Size = 90f };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, big] };

            Assert.That(PacBrain.IsThreat(world, me, big), Is.True);

            me.PowerUpEffect = ePowerUpEffect.MorphBall;
            me.PowerUpMs = 1000;
            Assert.That(PacBrain.IsThreat(world, me, big), Is.False);
            Assert.That(PacBrain.IsPrey(world, big, me), Is.False);
        }

        // library

        [Test]
        public void FindNearestOpenCell_StepsOutOfWalls()
        {
            var map = MazeMovementTests.MakeMap(["#####", "#####", "##.##", "#####"]);

            Assert.That(MazeMovement.FindNearestOpenCell(map, 2, 2), Is.EqualTo((2, 2)));
            Assert.That(MazeMovement.FindNearestOpenCell(map, 2, 0), Is.EqualTo((2, 2)));
            Assert.That(MazeMovement.FindNearestOpenCell(MazeMovementTests.MakeMap(["###", "###"]), 1, 1), Is.Null);
        }

        [Test]
        public void GetClearCells_StopsAtWalls()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);

            Assert.That(WeaponRules.GetClearCells(map, 1, 1, eFacing.Right, 10), Is.EqualTo(6));
            Assert.That(WeaponRules.GetClearCells(map, 1, 1, eFacing.Right, 3), Is.EqualTo(3));
            Assert.That(WeaponRules.GetClearCells(map, 1, 1, eFacing.Left, 10), Is.Zero);
        }

        [Test]
        public void EffectHelpers()
        {
            Assert.That(PowerUpEffects.PassesThrough(ePowerUpEffect.MorphBall), Is.True);
            Assert.That(PowerUpEffects.PassesThrough(ePowerUpEffect.RaccoonTail), Is.True);
            Assert.That(PowerUpEffects.PassesThrough(ePowerUpEffect.Invincible), Is.False);
            Assert.That(PowerUpEffects.PassesThroughWalls(ePowerUpEffect.RaccoonTail), Is.True);
            Assert.That(PowerUpEffects.PassesThroughWalls(ePowerUpEffect.MorphBall), Is.False);

            Assert.That(PowerUpEffects.GetRollAngle(3.5f, 1.5f), Is.EqualTo(PowerUpEffects.GetRollAngle(3.5f, 1.5f)));
            Assert.That(PowerUpEffects.GetRollAngle(3.5f, 1.5f), Is.Not.EqualTo(PowerUpEffects.GetRollAngle(3.7f, 1.5f)), "turns as it moves");

            Assert.That(PowerUpEffects.IsFireFlashWhite(0f), Is.True);
            Assert.That(PowerUpEffects.IsFireFlashWhite(PowerUpEffects.FIRE_FLASH_SECONDS * 1.5f), Is.False);

            Assert.That(WeaponRules.CanThrowSword(WeaponRules.MAX_SWORDS - 1), Is.True);
            Assert.That(WeaponRules.CanThrowSword(WeaponRules.MAX_SWORDS), Is.False);
            Assert.That(WeaponEffectSprites.GetRotation(eFacing.Left), Is.EqualTo(MathF.PI));
        }

        [Test]
        public void EveryNewEffect_HasAName()
        {
            foreach (var effect in new[] { ePowerUpEffect.Reinforcements, ePowerUpEffect.MorphBall,
                ePowerUpEffect.RaccoonTail, ePowerUpEffect.SwordSwing, ePowerUpEffect.HolyFire, ePowerUpEffect.FireFlower })
                Assert.That(PowerUpEffects.GetName(effect), Is.Not.EqualTo(Const.STRING_POWER_UP_NONE), effect.ToString());
        }
    }
}
