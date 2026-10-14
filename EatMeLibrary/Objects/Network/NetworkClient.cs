using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EatMeLibrary
{
    /// <summary>
    /// The client's connection to a server. Connecting happens in the background; the game loop polls
    /// State and takes received messages off with TryGetMessage, so nothing here ever blocks a frame.
    /// </summary>
    public class NetworkClient
    {
        private const int CONNECT_TIMEOUT_MS = 5000;

        private readonly ConcurrentQueue<INetworkMessage> _Inbox = new();
        private MessageConnection _Connection;
        private int _State = (int)eConnectionState.NotConnected;

        public eConnectionState State => (eConnectionState)Volatile.Read(ref _State);

        /// <summary>
        /// Why connecting failed or the connection dropped, for showing to the player.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Starts connecting in the background and sends hello as soon as the connection is made.
        /// </summary>
        public void Connect(string host, int port, HelloMessage hello)
        {
            Error = null;
            SetState(eConnectionState.Connecting);
            _ = ConnectAsync(host, port, hello);
        }

        public bool TryGetMessage(out INetworkMessage message)
        {
            return _Inbox.TryDequeue(out message);
        }

        public void Send(INetworkMessage message)
        {
            if (State == eConnectionState.Connected)
                _Connection.Send(message);
        }

        /// <summary>
        /// Tells the server we're leaving and closes the connection.
        /// </summary>
        public void Disconnect()
        {
            var connection = _Connection;
            SetState(eConnectionState.NotConnected);

            if (connection == null)
                return;

            connection.Send(new LeaveMessage());
            connection.CloseAfterSending();
        }

        private async Task ConnectAsync(string host, int port, HelloMessage hello)
        {
            var client = new TcpClient();

            try
            {
                using var timeout = new CancellationTokenSource(CONNECT_TIMEOUT_MS);
                await client.ConnectAsync(host, port, timeout.Token);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or ArgumentException)
            {
                client.Dispose();
                Error = ex is OperationCanceledException ? "Server did not answer" : ex.Message;
                SetState(eConnectionState.Failed);
                return;
            }

            // the player may have backed out while we were connecting
            if (State != eConnectionState.Connecting)
            {
                client.Dispose();
                return;
            }

            var connection = new MessageConnection(client);
            _Connection = connection;
            SetState(eConnectionState.Connected);

            connection.Start(
                (_, message) => _Inbox.Enqueue(message),
                closed =>
                {
                    // only an unexpected close counts; Disconnect has already moved the state on
                    if (State != eConnectionState.Connected)
                        return;

                    Error = closed.CloseReason ?? "Server closed the connection";
                    Interlocked.CompareExchange(ref _State, (int)eConnectionState.Disconnected, (int)eConnectionState.Connected);
                });

            connection.Send(hello);
        }

        private void SetState(eConnectionState state)
        {
            Volatile.Write(ref _State, (int)state);
        }
    }
}
