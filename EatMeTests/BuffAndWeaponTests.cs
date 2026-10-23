using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    /// <summary>
    /// The power pill item, Reinforcements, Bomb, Super Leaf, White Sword, Fire Flower, Holy Water, Blue Shell, Poke Ball
    /// and Cherry Twin.
    /// </summary>
    public class BuffAndWeaponTests
    {
        private const int TICK_MS = 16;

        private static GameServer MakeServer(int ghosts = 0)
        {
            var settings = new ServerSettings
            {
                ServerName = "Weapons",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { GhostCount = ghosts },
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
                server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(TICK_MS));
        }

        // Power pill item

        [Test]
        public void PowerPillItem_StartsThePowerPillEffect()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var size = red.Size;
            server.ApplyPowerUp(red, ePowerUp.PowerPill);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.PowerPill));
            Assert.That(red.IsPowered, Is.True);
            Assert.That(red.Size, Is.EqualTo(size + Config.PowerPillSizeGain));
        }

        // Reinforcements (fairy)

        [Test]
        public void Reinforcements_TeamRespawnsAtOnce_WithTheChorus_EvenUnderLockdown()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, mateConnection) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red; // teams are kept even on join
            var (blue, _) = Join(server, 3, eTeamPreference.Blue);
            server.ApplyPowerUp(red, ePowerUp.Reinforcements);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Reinforcements));
            server.StartPowerUpEffect(blue, ePowerUp.Key);

            mate.Alive = false;
            mate.RespawnTimer = Config.PlayerRespawnTimeMs;
            server.UpdateRespawns(TimeSpan.FromMilliseconds(1));

            Assert.That(mate.Alive, Is.True);
            Assert.That(mateConnection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.AngelicRespawn && e.TargetId == mate.Id), Is.EqualTo(1));
        }

        [Test]
        public void Reinforcements_OnlyHelpsItsOwnTeam_AndOnlyWhileItsPacLives()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            server.StartPowerUpEffect(red, ePowerUp.Reinforcements);
            blue.Alive = false;
            blue.RespawnTimer = Config.PlayerRespawnTimeMs;

            server.UpdateRespawns(TimeSpan.FromMilliseconds(1));
            Assert.That(blue.Alive, Is.False);
            Assert.That(server.IsReinforced(eTeam.Red), Is.True);

            red.Alive = false;
            Assert.That(server.IsReinforced(eTeam.Red), Is.False);
        }

        // Cherry Twin

        [Test]
        public void CherryTwin_DropsOneDecoyAtATime_HeadingBackTheWayThePacCame()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindFarOpenCell(server, red));
            red.Facing = eFacing.Right;
            server.ApplyPowerUp(red, ePowerUp.Cherry);

            server.HandleAction(red);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);

            var decoy = server.WeaponEffects.Single();
            Assert.That(decoy.Kind, Is.EqualTo(eWeaponEffect.Decoy));
            Assert.That(decoy.OwnerId, Is.EqualTo(red.Id));
            Assert.That(decoy.Facing, Is.EqualTo(eFacing.Left));
            Assert.That(decoy.Location, Is.EqualTo(red.Location));
            Assert.That(decoy.StandIn.Id, Is.EqualTo(red.Id));
            Assert.That(decoy.StandIn.Team, Is.EqualTo(eTeam.Red));
        }

        [Test]
        public void CherryTwin_DecoyMovesThroughTheMaze_WithItsStandIn()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.Cherry);
            server.HandleAction(red);
            var decoy = server.WeaponEffects.Single();
            var start = decoy.Location;

            Tick(server, 60);

            Assert.That(server.WeaponEffects, Has.Count.EqualTo(1));
            Assert.That(decoy.Location, Is.Not.EqualTo(start));
            var (x, y) = MazeMovement.GetCell(decoy.Location);
            Assert.That(server.Map[x, y].HasFlag(eGridDef.Wall), Is.False);
            Assert.That(decoy.StandIn.Location, Is.EqualTo(decoy.Location));
            Assert.That(decoy.StandIn.Facing, Is.EqualTo(decoy.Facing));
        }

        [Test]
        public void CherryTwin_EnemyTouchingTheDecoy_PopsIt_AndNobodyDies()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, blueConnection) = Join(server, 2, eTeamPreference.Blue);
            PutOn(red, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.Cherry);
            server.HandleAction(red);
            blue.Location = server.WeaponEffects.Single().Location;
            blue.Size = red.Size;

            Tick(server, 1);

            Assert.That(server.WeaponEffects, Is.Empty);
            Assert.That(red.Alive, Is.True);
            Assert.That(blue.Alive, Is.True);
            Assert.That(blueConnection.ReceivedOf<GameEventMessage>().Count(e => e.EventType == eGameEvent.DecoyPopped && e.ActorId == red.Id), Is.EqualTo(1));
        }

        [Test]
        public void CherryTwin_TeammateTouchingTheDecoy_LeavesIt()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red;
            PutOn(red, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.Cherry);
            server.HandleAction(red);
            mate.Location = server.WeaponEffects.Single().Location;

            Tick(server, 1);

            Assert.That(server.WeaponEffects, Has.Count.EqualTo(1));
        }

        [Test]
        public void CherryTwin_GhostTouchingTheDecoy_PopsIt_AndLives()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            PutOn(red, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.Cherry);
            server.HandleAction(red);
            ghost.Alive = true;
            ghost.Location = server.WeaponEffects.Single().Location;

            Tick(server, 1);

            Assert.That(server.WeaponEffects, Is.Empty);
            Assert.That(ghost.Alive, Is.True);
        }

        [Test]
        public void CherryTwin_GhostsChaseTheDecoyInsteadOfThePac()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            var cell = FindFarOpenCell(server, red);
            PutOn(red, cell);

            var paths = new PathMap();
            paths.Fill(server.Map, cell.X, cell.Y, blocked: MazeMovement.GHOST_BLOCKED);
            var ghostCell = paths.Order.First(c => paths.GetDistance(c.X, c.Y) == 4);
            ghost.Personality = GhostPersonalities.NORM;
            ghost.Alive = true;
            PutOn(red, cell);
            ghost.Location = new Point2(ghostCell.X + 0.5f, ghostCell.Y + 0.5f);

            server.UpdateGhosts(TimeSpan.FromMilliseconds(TICK_MS));
            Assert.That(ghost.CurrentTarget, Is.SameAs(red), "no decoy yet");

            server.ApplyPowerUp(red, ePowerUp.Cherry);
            server.HandleAction(red);
            server.UpdateGhosts(TimeSpan.FromMilliseconds(TICK_MS));

            Assert.That(ghost.CurrentTarget, Is.SameAs(server.WeaponEffects.Single().StandIn));
        }

        [Test]
        public void CherryTwin_DecoyGoes_WhenTheEffectEndsOrThePacDies()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            PutOn(red, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.Cherry);
            server.HandleAction(red);

            server.EndPowerUpEffect(red);
            Tick(server, 1);
            Assert.That(server.WeaponEffects, Is.Empty);

            server.ApplyPowerUp(red, ePowerUp.Cherry);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            Assert.That(server.WeaponEffects, Has.Count.EqualTo(1));

            red.Alive = false;
            Tick(server, 1);
            Assert.That(server.WeaponEffects, Is.Empty);
        }

        [Test]
        public void CherryTwin_BotDropsADecoy_WhenAThreatIsNear()
        {
            var map = MazeMovementTests.MakeMap(["############", "#..........#", "############"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, Size = 10f, PowerUpEffect = ePowerUp.Cherry, PowerUpMs = 1000 };
            var ghost = new Ghost { Id = 0, Location = new Point2(10.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me], Ghosts = [ghost] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "the ghost is far away");

            ghost.Location = new Point2(4.5f, 1.5f);
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);
        }

        // Bomb

        [Test]
        public void Bomb_DropsOneBombPerBuff()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.Bomb);
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.Bomb));

            server.HandleAction(red);
            var bomb = server.WeaponEffects.Single(e => e.Kind == eWeaponEffect.Bomb);
            var (x, y) = MazeMovement.GetCell(red.Location);
            Assert.That(bomb.Location, Is.EqualTo(new Point2(x + 0.5f, y + 0.5f)), "on the centre of its cell");
            Assert.That(bomb.FuseMs, Is.EqualTo(WeaponRules.BOMB_FUSE_MS));

            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.Bomb), Is.EqualTo(1), "only one per buff");

            server.ApplyPowerUp(red, ePowerUp.Bomb);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.Bomb), Is.EqualTo(2), "a new buff gives a new bomb");
        }

        [Test]
        public void Bomb_GoesOffAfterItsFuse_StunningEnemiesAndGhostsInRange_WithABlast()
        {
            var server = MakeServer(ghosts: 1);
            var (red, connection) = Join(server, 1, eTeamPreference.Red);
            var (mate, _) = Join(server, 2, eTeamPreference.Red);
            mate.Team = eTeam.Red; // teams are kept even on join
            var (blue, _) = Join(server, 3, eTeamPreference.Blue);
            var (farBlue, _) = Join(server, 4, eTeamPreference.Blue);
            farBlue.Team = eTeam.Blue;
            var ghost = server.Ghosts[0];
            ghost.Alive = true;
            server.ApplyPowerUp(red, ePowerUp.Bomb);
            server.HandleAction(red);
            var at = server.WeaponEffects.Single(e => e.Kind == eWeaponEffect.Bomb).Location;
            mate.Location = at;
            blue.Location = new Point2(at.X + 1f, at.Y);
            ghost.Location = new Point2(at.X, at.Y + 1f);
            farBlue.Location = new Point2(at.X + PowerUpEffects.EFFECT_RADIUS + 2f, at.Y);
            server.EndPowerUpEffect(red);
            red.PowerUpEffect = ePowerUp.None;

            server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(WeaponRules.BOMB_FUSE_MS - 1));
            Assert.That(server.WeaponEffects.Any(e => e.Kind == eWeaponEffect.Bomb), Is.True, "still burning, buff or not");
            Assert.That(blue.IsStunned, Is.False);

            server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(1));
            Assert.That(server.WeaponEffects.Any(e => e.Kind == eWeaponEffect.Bomb), Is.False, "gone");
            Assert.That(blue.StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(ghost.StunMs, Is.EqualTo(PowerUpEffects.STUN_MS));
            Assert.That(red.IsStunned || mate.IsStunned, Is.False, "not its own team");
            Assert.That(farBlue.IsStunned, Is.False, "out of range");

            var blast = connection.ReceivedOf<GameEventMessage>().Single(e => e.EventType == eGameEvent.Blast);
            Assert.That(blast.ActorId, Is.EqualTo(red.Id));
            Assert.That((blast.X, blast.Y), Is.EqualTo((at.X, at.Y)));
        }

        [Test]
        public void ShouldUseAction_DropsTheBombWithSomethingInRange_Once()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, PowerUpEffect = ePowerUp.Bomb, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(6.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, enemy] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);

            me.BombDropped = true;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "already dropped");
        }

        // Super Leaf

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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.SuperLeaf));
            Assert.That(red.PassesThrough, Is.True);

            server.MovePacs(TimeSpan.FromSeconds(1f / PacRules.GetSpeed(Config.PacSpeed, red.Size, Config.PacMaxSize)));
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

            server.MovePacs(TimeSpan.FromSeconds(1f));

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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.WhiteSword));

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
            server.StartPowerUpEffect(red, ePowerUp.WhiteSword);
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
            server.StartPowerUpEffect(red, ePowerUp.WhiteSword);

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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.FireFlower));

            // an invincible enemy stops it but doesn't die
            server.StartPowerUpEffect(blue, ePowerUp.Starman);
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
            server.UpdateWeaponEffects(TimeSpan.FromMilliseconds(WeaponRules.ACTION_COOLDOWN_MS));
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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.HolyWater));

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
            server.StartPowerUpEffect(red, ePowerUp.HolyWater);
            Tick(server, 1);
            PutOn(red, (start.X + 1, start.Y));
            Tick(server, 1);
            Assert.That(server.WeaponEffects, Has.Count.EqualTo(1));

            server.StartPowerUpEffect(red, ePowerUp.Cherry);
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
            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.BlueShell));
            Assert.That(server.WeaponEffects, Is.Empty, "not fired until the action button");

            server.HandleAction(red);
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
            server.HandleAction(red);
            Tick(server, 1);

            Assert.That(mate.Alive && red.Alive, Is.True);
            Assert.That(server.WeaponEffects, Is.Empty, "no enemy to go after");
        }

        [Test]
        public void BlueShell_FiresOneShellPerBuff()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            PutOn(blue, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.BlueShell);

            server.HandleAction(red);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.BlueShell), Is.EqualTo(1), "only one per buff");

            server.ApplyPowerUp(red, ePowerUp.BlueShell);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.BlueShell), Is.EqualTo(2), "a new buff gives a new shell");
        }

        [Test]
        public void ShouldUseAction_FiresTheBlueShellWithAnEnemyAlive_Once()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, PowerUpEffect = ePowerUp.BlueShell, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(6.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, enemy] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);

            me.BlueShellFired = true;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "already fired");

            me.BlueShellFired = false;
            enemy.Alive = false;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "no enemy to send it after");
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
            server.HandleAction(red);

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
            server.UpdateGhosts(TimeSpan.FromMilliseconds(Config.GhostRegenTimeMs * 2));
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
        public void PokeBall_StartsGotcha_AndEachPressAfterTheCooldownThrowsABall()
        {
            var server = MakeServer(ghosts: 0);
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.PokeBall);

            Assert.That(red.PowerUpEffect, Is.EqualTo(ePowerUp.PokeBall), "a normal effect: it replaces the buff");
            Assert.That(server.WeaponEffects, Is.Empty, "eating it throws nothing");

            server.HandleAction(red);
            Assert.That(red.ActionCooldownMs, Is.EqualTo(WeaponRules.ACTION_COOLDOWN_MS));
            server.HandleAction(red);
            Assert.That(server.WeaponEffects, Has.Count.EqualTo(1), "still cooling down");

            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);
            Assert.That(server.WeaponEffects.Count(e => e.Kind == eWeaponEffect.PokeBall), Is.EqualTo(3), "no limit in flight");
        }

        [Test]
        public void PokeBall_FlyingBallsGo_WhenGotchaEnds()
        {
            var server = MakeServer(ghosts: 0);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.PokeBall);
            server.HandleAction(red);
            red.ActionCooldownMs = 0;
            server.HandleAction(red);

            server.StartPowerUpEffect(red, ePowerUp.Cherry);
            Tick(server, 1);

            Assert.That(server.WeaponEffects, Is.Empty);
        }

        [Test]
        public void PokeBall_HoldingAGhost_StaysWhenGotchaEnds()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var ghost = server.Ghosts[0];
            PutOn(red, FindFarOpenCell(server, red));
            server.ApplyPowerUp(red, ePowerUp.PokeBall);
            server.HandleAction(red);

            for (var i = 0; i < 2000 && !ghost.IsCaptured; i++)
                Tick(server, 1);

            Assert.That(ghost.IsCaptured, Is.True);
            server.StartPowerUpEffect(red, ePowerUp.Cherry);
            Tick(server, 1);

            Assert.That(server.WeaponEffects.Single().IsHoldingGhost, Is.True);
        }

        [Test]
        public void PokeBall_WithNoGhosts_WandersTheMaze()
        {
            var server = MakeServer(ghosts: 0);
            var (red, _) = Join(server, 1, eTeamPreference.Red);

            server.ApplyPowerUp(red, ePowerUp.PokeBall);
            server.HandleAction(red);
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
            server.StartPowerUpEffect(red, ePowerUp.WhiteSword);
            server.HandleAction(red);

            var state = server.BuildSnapshot().WeaponEffects.Single();

            Assert.That(state.Kind, Is.EqualTo(eWeaponEffect.Sword));
            Assert.That(state.Facing, Is.EqualTo(red.Facing));
        }

        // bots

        [Test]
        public void ShouldUseAction_AtAnEnemyOrGhostAhead_NotATeammate()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Facing = eFacing.Right, Alive = true, PowerUpEffect = ePowerUp.WhiteSword, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(5.5f, 1.5f), Alive = true };
            var mate = new Player { Id = 3, Team = eTeam.Red, Location = new Point2(3.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, enemy] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);

            world.Pacs = [me, enemy, mate];
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "a teammate is in the way");

            world.Pacs = [me];
            world.Ghosts = [new Ghost { Location = new Point2(4.5f, 1.5f), Alive = true }];
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);

            me.Facing = eFacing.Left;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "nothing behind it before the wall");

            me.Facing = eFacing.Right;
            me.PowerUpEffect = ePowerUp.Orange;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "nothing to fire");
        }

        [Test]
        public void PassingThrough_IsNeitherThreatNorPrey()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, Size = 10f };
            var big = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(3.5f, 1.5f), Alive = true, Size = 90f };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, big] };

            Assert.That(PacBrain.IsThreat(world, me, big), Is.True);

            me.PowerUpEffect = ePowerUp.SuperLeaf;
            me.PowerUpMs = 1000;
            Assert.That(PacBrain.IsThreat(world, me, big), Is.False);
            Assert.That(PacBrain.IsPrey(world, big, me), Is.False);
        }

        [Test]
        public void ShouldUseAction_FiresTheBfgAtSomethingAhead_EvenAtCloseRange()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Facing = eFacing.Right, Alive = true, PowerUpEffect = ePowerUp.BFG, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(2.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, enemy] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True, "the burst will catch it too, but it fires anyway");

            me.Facing = eFacing.Left;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False);
        }

        [Test]
        public void ShouldUseAction_Pow_WhenAnUnstunnedEnemyOrGhostIsInRange()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, PowerUpEffect = ePowerUp.PowBlock, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(6.5f, 1.5f), Alive = true };
            var mate = new Player { Id = 3, Team = eTeam.Red, Location = new Point2(4.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, mate] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "a teammate isn't worth it");

            world.Pacs = [me, mate, enemy];
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);

            enemy.StunMs = 1000;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "already stunned");

            world.Ghosts = [new Ghost { Location = new Point2(5.5f, 1.5f), Alive = true }];
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True);
        }

        [Test]
        public void ShouldUseAction_PeelAndWarp_WhenSomethingIsClose()
        {
            var map = MazeMovementTests.MakeMap(["####################", "#..................#", "####################"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, Size = 10f, PowerUpMs = 1000 };
            var enemy = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(1.5f + PacBrain.DANGER_DISTANCE + 5, 1.5f), Alive = true, Size = 90f };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, enemy] };

            foreach (var effect in new[] { ePowerUp.Banana, ePowerUp.Pear })
            {
                me.PowerUpEffect = effect;
                enemy.Location = new Point2(1.5f + PacBrain.DANGER_DISTANCE + 5, 1.5f);
                Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, $"{effect}: nothing close");

                enemy.Location = new Point2(1.5f + PacBrain.DANGER_DISTANCE, 1.5f);
                Assert.That(PacBrain.ShouldUseAction(world, me), Is.True, $"{effect}: enemy close");
            }

            // warping is only for getting away from something that can kill it
            me.PowerUpEffect = ePowerUp.Pear;
            enemy.Size = 1f;
            me.Size = 90f;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False);
        }

        [Test]
        public void StunnedPac_CannotEatAnEnemy()
        {
            var server = MakeServer();
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            var (blue, _) = Join(server, 2, eTeamPreference.Blue);
            red.Size = 90f;
            blue.Size = 10f;
            blue.Location = red.Location;
            red.StunMs = 1000;

            server.UpdatePacFights();
            Assert.That(blue.Alive, Is.True);
            Assert.That(red.Alive, Is.True);

            red.StunMs = 0;
            server.UpdatePacFights();
            Assert.That(blue.Alive, Is.False);
        }

        [Test]
        public void StunnedPac_IsNoThreatToBots()
        {
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);
            var me = new Player { Id = 1, Team = eTeam.Red, Location = new Point2(1.5f, 1.5f), Alive = true, Size = 10f };
            var big = new Player { Id = 2, Team = eTeam.Blue, Location = new Point2(3.5f, 1.5f), Alive = true, Size = 90f, StunMs = 1000 };
            var ghost = new Ghost { Location = new Point2(5.5f, 1.5f), Alive = true, StunMs = 1000 };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me, big], Ghosts = [ghost] };
            var paths = new PathMap();
            paths.Fill(map, 1, 1);

            Assert.That(PacBrain.IsThreat(world, me, big), Is.False);
            Assert.That(PacBrain.FindThreats(world, me, paths), Is.Empty);

            big.StunMs = 0;
            ghost.StunMs = 0;
            Assert.That(PacBrain.FindThreats(world, me, paths), Has.Count.EqualTo(2));
        }

        [Test]
        public void Hammer_SitsPastTheEdgeOfThePac_AndKillsGhostsBeforeTheyReachIt()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.Hammer);
            red.Size = 200f;
            red.Facing = eFacing.Right;

            server.UpdateWeaponEffects(TimeSpan.Zero);
            var hammer = server.WeaponEffects.Single(e => e.Kind == eWeaponEffect.Hammer);
            var expected = WeaponRules.GetHammerLocation(red.Location, eFacing.Right, PacRules.GetRadius(red.Size), server.Map.GetLength(0), server.Map.GetLength(1));
            Assert.That(hammer.Location, Is.EqualTo(expected));

            server.Ghosts[0].Location = hammer.Location;
            server.UpdateGhosts(TimeSpan.Zero);

            Assert.That(server.Ghosts[0].Alive, Is.False);
            Assert.That(red.Alive, Is.True);
            Assert.That(red.GhostKills, Is.EqualTo(1));
        }

        [Test]
        public void Hammer_DoesNothingWhileItsPacIsStunned()
        {
            var server = MakeServer(ghosts: 1);
            var (red, _) = Join(server, 1, eTeamPreference.Red);
            server.ApplyPowerUp(red, ePowerUp.Hammer);
            red.Facing = eFacing.Right;
            server.UpdateWeaponEffects(TimeSpan.Zero);
            red.StunMs = 1000;

            server.Ghosts[0].Location = server.WeaponEffects.Single(e => e.Kind == eWeaponEffect.Hammer).Location;
            server.UpdateGhosts(TimeSpan.Zero);
            server.UpdateWeaponEffects(TimeSpan.Zero);

            Assert.That(server.Ghosts[0].Alive, Is.True);
        }

        [Test]
        public void SpawnPowerUp_NeverPicksThePowerPill()
        {
            var server = MakeServer();
            Join(server, 1, eTeamPreference.Red);

            for (var i = 0; i < 300; i++)
                Assert.That(server.SpawnPowerUp()?.Item, Is.Not.EqualTo(ePowerUp.PowerPill));

            Assert.That(PowerUpRules.SPAWNABLE, Does.Not.Contain(ePowerUp.PowerPill));
            Assert.That(PowerUpRules.SPAWNABLE.Append(ePowerUp.PowerPill), Is.EquivalentTo(PowerUpRules.ALL));
        }

        // library

        [Test]
        public void GetHammerLocation_MovesOutAsThePacGrows()
        {
            var at = new Point2(5.5f, 5.5f);

            Assert.That(WeaponRules.GetHammerLocation(at, eFacing.Right, PacRules.GetRadius(0f), 20, 20), Is.EqualTo(new Point2(5.5f + PacRules.GetRadius(0f) + WeaponRules.HAMMER_GAP, 5.5f)));
            Assert.That(WeaponRules.GetHammerLocation(at, eFacing.Up, 2f, 20, 20), Is.EqualTo(new Point2(5.5f, 5.5f - 2f - WeaponRules.HAMMER_GAP)));
            Assert.That(WeaponRules.GetHammerLocation(new Point2(19.5f, 5.5f), eFacing.Right, 0.5f, 20, 20), Is.EqualTo(new Point2(0.5f, 5.5f)), "wraps round the map");
        }

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
            Assert.That(PowerUpEffects.PassesThrough(ePowerUp.SuperLeaf), Is.True);
            Assert.That(PowerUpEffects.PassesThrough(ePowerUp.Starman), Is.False);
            Assert.That(PowerUpEffects.PassesThroughWalls(ePowerUp.SuperLeaf), Is.True);
            Assert.That(PowerUpEffects.PassesThroughWalls(ePowerUp.Bomb), Is.False);

            Assert.That(PowerUpEffects.IsFireFlashWhite(TimeSpan.Zero), Is.True);
            Assert.That(PowerUpEffects.IsFireFlashWhite(TimeSpan.FromMilliseconds(PowerUpEffects.FIRE_FLASH_MS * 1.5)), Is.False);

            Assert.That(WeaponRules.CanThrowSword(WeaponRules.MAX_SWORDS - 1), Is.True);
            Assert.That(WeaponRules.CanThrowSword(WeaponRules.MAX_SWORDS), Is.False);
            Assert.That(WeaponEffectSprites.GetRotation(eFacing.Left), Is.EqualTo(MathF.PI));
        }

        [Test]
        public void EveryNewEffect_HasAName()
        {
            foreach (var effect in new[] { ePowerUp.Reinforcements, ePowerUp.Bomb,
                ePowerUp.SuperLeaf, ePowerUp.WhiteSword, ePowerUp.HolyWater, ePowerUp.FireFlower })
                Assert.That(PowerUpEffects.GetName(effect), Is.Not.EqualTo(Const.STRING_POWER_UP_NONE), effect.ToString());
        }
    }
}
