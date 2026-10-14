using EatMeLibrary;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EatMeServer
{
    /// <summary>
    /// Accepts TCP clients and queues everything that happens on their connections.
    /// The game loop takes the events off the queue with TryGetEvent, so game state is only touched on one thread.
    /// </summary>
    public class NetworkServer
    {
        private readonly ConcurrentQueue<NetworkEvent> _Events = new();
        private readonly ConcurrentDictionary<int, ClientConnection> _Connections = new();
        private TcpListener _Listener;
        private int _LastConnectionId;

        /// <summary>
        /// Port actually being listened on; useful when Start was given port 0.
        /// </summary>
        public int Port => ((IPEndPoint)_Listener.LocalEndpoint).Port;

        /// <param name="address">Network interface to listen on; all of them if null.</param>
        public void Start(int port, CancellationToken token, IPAddress address = null)
        {
            _Listener = new TcpListener(address ?? IPAddress.Any, port);
            _Listener.Start();
            _ = AcceptLoopAsync(token);
        }

        public bool TryGetEvent(out NetworkEvent networkEvent)
        {
            return _Events.TryDequeue(out networkEvent);
        }

        /// <summary>
        /// Stops accepting clients and closes every connection.
        /// </summary>
        public void Stop()
        {
            _Listener?.Stop();

            foreach (var connection in _Connections.Values)
                connection.Close();
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await _Listener.AcceptTcpClientAsync(token);
                }
                catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Accept failed: {ex.Message}");
                    continue;
                }

                var connection = new ClientConnection(Interlocked.Increment(ref _LastConnectionId), client);
                _Connections[connection.Id] = connection;
                Console.WriteLine($"Client {connection.Id} connected from {connection.RemoteAddress}");

                // queued before Start so it always comes ahead of the connection's messages
                _Events.Enqueue(new NetworkEvent(eNetworkEventType.Connected, connection));
                connection.Start(OnMessage, OnClosed);
            }
        }

        private void OnMessage(IClientConnection connection, INetworkMessage message)
        {
            _Events.Enqueue(new NetworkEvent(eNetworkEventType.Message, connection, message));
        }

        private void OnClosed(IClientConnection connection)
        {
            _Connections.TryRemove(connection.Id, out _);
            Console.WriteLine($"Client {connection.Id} disconnected");
            _Events.Enqueue(new NetworkEvent(eNetworkEventType.Disconnected, connection));
        }
    }
}
