using EatMeLibrary;
using EatMeServer;
using System.Diagnostics;
using System.Net;

namespace EatMeTests
{
    /// <summary>
    /// Real client and server talking over loopback TCP, with the game loop pumped by the test.
    /// </summary>
    public class NetworkLoopbackTests
    {
        private const int TIMEOUT_MS = 5000;
        private const int TICK_MS = 16;

        private CancellationTokenSource _Shutdown;
        private NetworkServer _Network;
        private GameServer _Game;

        [SetUp]
        public void StartServer()
        {
            var settings = new ServerSettings { ServerName = "Loopback", TargetFps = 30, AutoBalanceTeams = false, GameConfig = new GameConfiguration() };
            _Shutdown = new CancellationTokenSource();
            _Game = new GameServer(settings, ["Hello from loopback"]);
            _Network = new NetworkServer();

            // port 0 picks a free port; loopback only, so no firewall prompt
            _Network.Start(0, _Shutdown.Token, IPAddress.Loopback);
        }

        [TearDown]
        public void StopServer()
        {
            _Shutdown.Cancel();
            _Network.Stop();
            _Shutdown.Dispose();
        }

        /// <summary>
        /// Runs the server loop and feeds each client's messages into its state until the condition holds.
        /// </summary>
        private void PumpUntil(Func<bool> condition, params (NetworkClient Client, ClientGameState State)[] clients)
        {
            var timer = Stopwatch.StartNew();

            while (!condition())
            {
                if (timer.ElapsedMilliseconds > TIMEOUT_MS)
                    Assert.Fail("Timed out waiting for the server");

                while (_Network.TryGetEvent(out var networkEvent))
                    _Game.HandleEvent(networkEvent);

                _Game.Update(TICK_MS);

                foreach (var (client, state) in clients)
                    while (client.TryGetMessage(out var message))
                        state.Apply(message, timer.ElapsedMilliseconds);

                Thread.Sleep(5);
            }
        }

        private (NetworkClient Client, ClientGameState State) Connect(string name, eTeamPreference team)
        {
            var client = new NetworkClient();
            client.Connect("127.0.0.1", _Network.Port, new HelloMessage { PlayerName = name, PreferredTeam = team });
            return (client, new ClientGameState());
        }

        [Test]
        public void Client_Joins_GetsWelcomeLevelAndSnapshot()
        {
            var player = Connect("Pac", eTeamPreference.Blue);

            PumpUntil(() => player.State.Snapshot != null, player);

            Assert.That(player.Client.State, Is.EqualTo(eConnectionState.Connected));
            Assert.That(player.State.Team, Is.EqualTo(eTeam.Blue));
            Assert.That(player.State.LoadingScreenTips, Is.EqualTo(new[] { "Hello from loopback" }));
            Assert.That(player.State.Map, Is.EqualTo(_Game.Map), "client must build the same map as the server");
            Assert.That(player.State.GetOwnPac(), Is.Not.Null);
        }

        [Test]
        public void TwoClients_SeeEachOther_AndLeavingIsReported()
        {
            var first = Connect("First", eTeamPreference.Red);
            var second = Connect("Second", eTeamPreference.Red);

            PumpUntil(() => first.State.Snapshot?.Pacs.Count == 2 && second.State.Snapshot?.Pacs.Count == 2, first, second);

            Assert.That(first.State.GetPlayerName(second.State.PlayerId), Is.EqualTo("Second"));
            Assert.That(second.State.GetPlayerName(first.State.PlayerId), Is.EqualTo("First"));
            Assert.That(first.State.Team, Is.Not.EqualTo(second.State.Team), "teams should be balanced");

            second.Client.Disconnect();

            PumpUntil(() => !first.State.Players.ContainsKey(second.State.PlayerId), first);
            Assert.That(_Game.PlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void Client_BadName_Rejected()
        {
            var player = Connect("Not Valid!", eTeamPreference.Red);

            PumpUntil(() => player.State.RejectReason.HasValue, player);

            Assert.That(player.State.RejectReason, Is.EqualTo(eRejectReason.InvalidName));
            Assert.That(_Game.PlayerCount, Is.Zero);
        }

        [Test]
        public void Ping_ComesBackAsPong()
        {
            var player = Connect("Pinger", eTeamPreference.Red);
            PumpUntil(() => player.State.IsJoined, player);

            player.Client.Send(new PingMessage { ClientTime = 0 });

            PumpUntil(() => player.State.PingMs >= 0, player);
        }

        [Test]
        public void Connect_NoServer_Fails()
        {
            var client = new NetworkClient();
            _Network.Stop();

            client.Connect("127.0.0.1", _Network.Port, new HelloMessage { PlayerName = "Pac" });

            var timer = Stopwatch.StartNew();
            while (client.State == eConnectionState.Connecting && timer.ElapsedMilliseconds < TIMEOUT_MS * 2)
                Thread.Sleep(10);

            Assert.That(client.State, Is.EqualTo(eConnectionState.Failed));
            Assert.That(client.Error, Is.Not.Empty);
        }
    }
}
