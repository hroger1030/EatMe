using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EatMeLibrary
{
    /// <summary>
    /// Finds servers on the LAN by broadcasting a DiscoveryRequest and collecting the replies.
    /// </summary>
    public class ServerBrowser
    {
        private readonly ConcurrentDictionary<string, ServerListing> _Found = new();
        private UdpClient _Udp;
        private CancellationTokenSource _Cancel;
        private int _DiscoveryPort;

        /// <summary>
        /// Servers found so far, sorted by name.
        /// </summary>
        public IReadOnlyList<ServerListing> Servers => _Found.Values.OrderBy(s => s.Info.ServerName).ThenBy(s => s.Address).ToList();

        /// <summary>
        /// Opens the socket replies come back on and sends the first search. Returns false if the socket can't be opened.
        /// </summary>
        public bool Start(int discoveryPort = NetworkConstants.DEFAULT_DISCOVERY_PORT)
        {
            Stop();
            _Found.Clear();
            _DiscoveryPort = discoveryPort;

            try
            {
                _Udp = new UdpClient(0) { EnableBroadcast = true };
            }
            catch (SocketException)
            {
                return false;
            }

            _Cancel = new CancellationTokenSource();
            _ = ListenLoopAsync(_Udp, _Cancel.Token);
            Search();
            return true;
        }

        /// <summary>
        /// Broadcasts another request. Servers already found stay listed.
        /// </summary>
        public void Search()
        {
            if (_Udp == null)
                return;

            var request = MessageSerializer.Serialize(new DiscoveryRequestMessage());

            try
            {
                _Udp.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, _DiscoveryPort));
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                // no network; nothing will be found
            }
        }

        public void Stop()
        {
            _Cancel?.Cancel();
            _Udp?.Dispose();
            _Udp = null;
        }

        private async Task ListenLoopAsync(UdpClient udp, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult reply;

                try
                {
                    reply = await udp.ReceiveAsync(token);
                }
                catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    continue;
                }

                var frames = new FrameBuffer();
                frames.Append(reply.Buffer, 0, reply.Buffer.Length);

                try
                {
                    if (frames.TryReadMessage(out var message) && message is DiscoveryResponseMessage response)
                    {
                        var listing = new ServerListing(reply.RemoteEndPoint.Address.ToString(), response);
                        _Found[listing.Address] = listing;
                    }
                }
                catch (System.IO.InvalidDataException)
                {
                    // not one of ours
                }
            }
        }
    }
}
