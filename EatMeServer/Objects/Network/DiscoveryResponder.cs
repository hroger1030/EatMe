using EatMeLibrary;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EatMeServer
{
    /// <summary>
    /// Answers LAN discovery broadcasts so clients can list this server.
    /// </summary>
    public class DiscoveryResponder
    {
        private readonly Func<DiscoveryResponseMessage> _BuildResponse;
        private UdpClient _Udp;

        /// <param name="buildResponse">Called on a background thread for each request; must be safe to call from there.</param>
        public DiscoveryResponder(Func<DiscoveryResponseMessage> buildResponse)
        {
            _BuildResponse = buildResponse;
        }

        public void Start(int port, CancellationToken token)
        {
            _Udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            _ = ListenLoopAsync(token);
        }

        public void Stop()
        {
            _Udp?.Dispose();
        }

        /// <summary>
        /// True if a datagram is a discovery request this server should answer.
        /// </summary>
        public static bool IsDiscoveryRequest(byte[] datagram)
        {
            var frames = new FrameBuffer();
            frames.Append(datagram, 0, datagram.Length);

            try
            {
                return frames.TryReadMessage(out var message) && message is DiscoveryRequestMessage;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult request;

                try
                {
                    request = await _Udp.ReceiveAsync(token);
                }
                catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    // e.g. an ICMP port unreachable from an earlier reply; keep listening
                    continue;
                }

                if (!IsDiscoveryRequest(request.Buffer))
                    continue;

                try
                {
                    var reply = MessageSerializer.Serialize(_BuildResponse());
                    await _Udp.SendAsync(reply, request.RemoteEndPoint, token);
                }
                catch (Exception ex) when (ex is SocketException or OperationCanceledException or ObjectDisposedException)
                {
                    // nothing to do if the reply can't go out
                }
            }
        }
    }
}
