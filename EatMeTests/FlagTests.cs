using EatMeLibrary;
using EatMeServer;
using FastG;

namespace EatMeTests
{
    public class FlagTests
    {
        private static GameServer MakeServer(int maxScore = 100)
        {
            var settings = new ServerSettings { ServerName = "Flags", TargetFps = 30, AutoBalanceTeams = false, GameConfig = new GameConfiguration { MaxScore = maxScore } };
            return new GameServer(settings, []);
        }

        private static Player Join(GameServer server, int id, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = team });
            return server.Players.Single(p => p.Connection == connection).Player;
        }

        private static GameFlag FlagOf(GameServer server, eTeam team)
        {
            return server.Flags.First(f => f.Team == team);
        }

        private static void MoveOnto(Player player, GameFlag flag)
        {
            player.Location = new Point2(flag.X, flag.Y);
        }

        [Test]
        public void Level_HasOneFlagPerTeamAtItsBase()
        {
            var server = MakeServer();

            Assert.That(server.Flags.Count(f => f.Team == eTeam.Red), Is.EqualTo(1));
            Assert.That(server.Flags.Count(f => f.Team == eTeam.Blue), Is.EqualTo(1));

            foreach (var flag in server.Flags)
            {
                var baseFlag = flag.Team == eTeam.Red ? eGridDef.FlagBaseRed : eGridDef.FlagBaseBlue;
                Assert.That(server.Map[(int)flag.BaseX, (int)flag.BaseY].HasFlag(baseFlag), Is.True);
                Assert.That(flag.IsCarried, Is.False);
            }
        }

        [Test]
        public void EnemyPac_TouchingFlag_PicksItUp()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(red, blueFlag);
            server.UpdateFlags();

            Assert.That(blueFlag.CarrierId, Is.EqualTo(red.Id));
            Assert.That(red.HasFlag, Is.True);
        }

        [Test]
        public void OwnTeam_TouchingFlag_DoesNotPickItUp()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            var redFlag = FlagOf(server, eTeam.Red);

            MoveOnto(red, redFlag);
            server.UpdateFlags();

            Assert.That(redFlag.IsCarried, Is.False);
            Assert.That(red.HasFlag, Is.False);
        }

        [Test]
        public void DeadPac_DoesNotPickUpFlag()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            red.Alive = false;
            MoveOnto(red, blueFlag);
            server.UpdateFlags();

            Assert.That(blueFlag.IsCarried, Is.False);
        }

        [Test]
        public void CarriedFlag_CantBeTakenBySecondPac()
        {
            var server = MakeServer();
            var first = Join(server, 1, eTeamPreference.Red);
            Join(server, 2, eTeamPreference.Blue);
            var second = Join(server, 3, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(first, blueFlag);
            server.UpdateFlags();
            MoveOnto(second, blueFlag);
            server.UpdateFlags();

            Assert.That(blueFlag.CarrierId, Is.EqualTo(first.Id));
            Assert.That(second.HasFlag, Is.False);
        }

        [Test]
        public void CarriedFlag_FollowsCarrier()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(red, blueFlag);
            server.UpdateFlags();
            red.Location = new Point2(10.5f, 20.5f);
            server.UpdateFlags();

            Assert.That(blueFlag.X, Is.EqualTo(10.5f));
            Assert.That(blueFlag.Y, Is.EqualTo(20.5f));
        }

        [Test]
        public void CarrierLeaving_DropsFlagWhereTheyWere()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(red, blueFlag);
            server.UpdateFlags();
            red.Location = new Point2(10.5f, 20.5f);
            server.UpdateFlags();
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, server.Players.Single().Connection));

            Assert.That(blueFlag.IsCarried, Is.False);
            Assert.That((blueFlag.X, blueFlag.Y), Is.EqualTo((10.5f, 20.5f)));
        }

        [Test]
        public void DroppedFlag_CanBePickedUpWhereItLies()
        {
            var server = MakeServer();
            var first = Join(server, 1, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(first, blueFlag);
            server.UpdateFlags();
            first.Location = new Point2(10.5f, 20.5f);
            server.UpdateFlags();
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Disconnected, server.Players.Single().Connection));

            var second = Join(server, 2, eTeamPreference.Red);
            second.Location = new Point2(10.5f, 20.5f);
            server.UpdateFlags();

            Assert.That(blueFlag.CarrierId, Is.EqualTo(second.Id));
        }

        [Test]
        public void Snapshot_IncludesFlags()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(red, blueFlag);
            server.UpdateFlags();

            var flags = server.BuildSnapshot().Flags;
            Assert.That(flags.Count, Is.EqualTo(2));
            Assert.That(flags.Single(f => f.Team == eTeam.Blue).CarrierId, Is.EqualTo(red.Id));
            Assert.That(flags.Single(f => f.Team == eTeam.Red).IsCarried, Is.False);
        }

        /// <summary>
        /// Gives a red pac the blue flag.
        /// </summary>
        private static Player RedCarrier(GameServer server)
        {
            var red = Join(server, 1, eTeamPreference.Red);
            MoveOnto(red, FlagOf(server, eTeam.Blue));
            server.UpdateFlags();
            return red;
        }

        [Test]
        public void Capture_CarrierTouchesOwnFlagAtBase_ScoresAndSendsEnemyFlagHome()
        {
            var server = MakeServer();
            var red = RedCarrier(server);
            var redConnection = (FakeConnection)server.Players.Single().Connection;
            var blueFlag = FlagOf(server, eTeam.Blue);

            MoveOnto(red, FlagOf(server, eTeam.Red));
            server.UpdateFlags();

            Assert.That(server.RedScore, Is.EqualTo(new GameConfiguration().FlagScore));
            Assert.That(server.BlueScore, Is.Zero);
            Assert.That(red.HasFlag, Is.False);
            Assert.That(blueFlag.IsAtBase, Is.True);
            var captured = redConnection.ReceivedOf<GameEventMessage>().Single();
            Assert.That(captured.EventType, Is.EqualTo(eGameEvent.FlagCaptured));
            Assert.That(captured.Team, Is.EqualTo(eTeam.Blue), "announced as the flag's team");
        }

        /// <summary>
        /// The red pac takes the blue flag and brings it home to the red flag.
        /// </summary>
        private static void Capture(GameServer server, Player red)
        {
            MoveOnto(red, FlagOf(server, eTeam.Blue));
            server.UpdateFlags();
            MoveOnto(red, FlagOf(server, eTeam.Red));
            server.UpdateFlags();
        }

        private static List<GameEventMessage> AboutToWins(FakeConnection connection)
        {
            return connection.ReceivedOf<GameEventMessage>().Where(e => e.EventType == eGameEvent.AboutToWin).ToList();
        }

        [Test]
        public void AboutToWin_SentOncePerLevel()
        {
            // one capture (FlagScore 20) is over 90% of 22 without winning
            var server = MakeServer(maxScore: 22);
            var red = Join(server, 1, eTeamPreference.Red);
            var connection = (FakeConnection)server.Players.Single().Connection;

            Capture(server, red);
            server.CheckAboutToWin();
            server.CheckAboutToWin();

            var aboutToWin = AboutToWins(connection).Single();
            Assert.That(aboutToWin.Team, Is.EqualTo(eTeam.Red));

            server.StartLevel(2);
            Capture(server, red);
            server.CheckAboutToWin();

            Assert.That(AboutToWins(connection), Has.Count.EqualTo(2), "sent again on the next level");
        }

        [Test]
        public void AboutToWin_NotSentBelowThreshold()
        {
            var server = MakeServer(maxScore: 23);
            var red = Join(server, 1, eTeamPreference.Red);
            var connection = (FakeConnection)server.Players.Single().Connection;

            Capture(server, red);
            server.CheckAboutToWin();

            Assert.That(AboutToWins(connection), Is.Empty);
        }

        [Test]
        public void AboutToWin_NotSentWhenTeamWinsOutright()
        {
            var server = MakeServer(maxScore: 20);
            var red = Join(server, 1, eTeamPreference.Red);
            var connection = (FakeConnection)server.Players.Single().Connection;

            Capture(server, red);
            server.CheckAboutToWin();

            Assert.That(AboutToWins(connection), Is.Empty);
        }

        [Test]
        public void OwnFlagAwayFromBase_IsReturnedNotCaptured_EvenWhileCarrying()
        {
            var server = MakeServer();
            var red = RedCarrier(server);
            var redFlag = FlagOf(server, eTeam.Red);
            redFlag.X = 30.5f;
            redFlag.Y = 30.5f;

            red.Location = new Point2(30.5f, 30.5f);
            server.UpdateFlags();

            Assert.That(redFlag.IsAtBase, Is.True);
            Assert.That(server.RedScore, Is.EqualTo(new GameConfiguration().FlagRescue));
            Assert.That(red.HasFlag, Is.True, "a return isn't a capture, so the enemy flag is kept");
        }

        [Test]
        public void OwnFlagAwayFromBase_ReturnedWithoutCarrying()
        {
            var server = MakeServer();
            var blue = Join(server, 1, eTeamPreference.Blue);
            var blueFlag = FlagOf(server, eTeam.Blue);
            blueFlag.X = 60.5f;
            blueFlag.Y = 60.5f;

            blue.Location = new Point2(60.5f, 60.5f);
            server.UpdateFlags();

            Assert.That(blueFlag.IsAtBase, Is.True);
            Assert.That(server.BlueScore, Is.EqualTo(new GameConfiguration().FlagRescue));
            Assert.That(blue.FlagRescues, Is.EqualTo(1));

            var returned = ((FakeConnection)server.Players.Single().Connection).ReceivedOf<GameEventMessage>().Single();
            Assert.That(returned.EventType, Is.EqualTo(eGameEvent.FlagReturned));
            Assert.That(returned.Team, Is.EqualTo(eTeam.Blue));
        }

        [Test]
        public void Snapshot_CarriesPlayerStats()
        {
            var server = MakeServer();
            var blue = Join(server, 1, eTeamPreference.Blue);
            blue.FlagCaps = 1;
            blue.GhostKills = 2;
            blue.PlayerKills = 3;
            blue.FlagRescues = 4;
            blue.PowerUpsEaten = 5;

            var pac = server.BuildSnapshot().Pacs.Single();

            Assert.That(pac.FlagCaps, Is.EqualTo(1));
            Assert.That(pac.GhostKills, Is.EqualTo(2));
            Assert.That(pac.PlayerKills, Is.EqualTo(3));
            Assert.That(pac.Rescues, Is.EqualTo(4));
            Assert.That(pac.PowerUps, Is.EqualTo(5));
        }

        [Test]
        public void OwnFlagAtBase_WithoutEnemyFlag_DoesNothing()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);

            MoveOnto(red, FlagOf(server, eTeam.Red));
            server.UpdateFlags();

            Assert.That(server.RedScore, Is.Zero);
        }

        [Test]
        public void DropFlag_LeavesFlagWhereCarrierWas()
        {
            var server = MakeServer();
            var red = RedCarrier(server);
            var blueFlag = FlagOf(server, eTeam.Blue);
            red.Location = new Point2(40.5f, 41.5f);
            server.UpdateFlags();

            server.DropFlag(red);

            Assert.That(red.HasFlag, Is.False);
            Assert.That(blueFlag.IsCarried, Is.False);
            Assert.That((blueFlag.X, blueFlag.Y), Is.EqualTo((40.5f, 41.5f)));

            var lost = ((FakeConnection)server.Players.Single().Connection).ReceivedOf<GameEventMessage>().Single();
            Assert.That(lost.EventType, Is.EqualTo(eGameEvent.FlagLost));
            Assert.That(lost.Team, Is.EqualTo(eTeam.Blue));
        }

        [Test]
        public void DropFlag_NotCarrying_SendsNothing()
        {
            var server = MakeServer();
            var red = Join(server, 1, eTeamPreference.Red);

            server.DropFlag(red);

            Assert.That(((FakeConnection)server.Players.Single().Connection).ReceivedOf<GameEventMessage>(), Is.Empty);
        }

        [TestCase(0f, 0f, 0f, true)]
        [TestCase(0.99f, 0f, 0f, true)]
        [TestCase(1.01f, 0f, 0f, false)]
        [TestCase(1.4f, 0f, 100f, true)]
        [TestCase(0.7f, 0.7f, 0f, true)]
        [TestCase(0.8f, 0.8f, 0f, false)]
        public void IsTouchingFlag(float offsetX, float offsetY, float size, bool expected)
        {
            var flagX = 10.5f;
            var flagY = 10.5f;

            Assert.That(PacRules.IsTouchingFlag(new Point2(flagX + offsetX, flagY + offsetY), size, flagX, flagY), Is.EqualTo(expected));
        }

        [Test]
        public void PacDiameter_OneCellAtZero_TwoAtOneHundred()
        {
            Assert.That(PacRules.GetDiameter(0f), Is.EqualTo(1f));
            Assert.That(PacRules.GetDiameter(50f), Is.EqualTo(1.5f));
            Assert.That(PacRules.GetDiameter(100f), Is.EqualTo(2f));
        }
    }
}
