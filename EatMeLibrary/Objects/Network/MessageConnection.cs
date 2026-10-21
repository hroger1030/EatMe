using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EatMeLibrary
{
    /// <summary>
    /// A TCP connection that sends and receives whole messages. Reading and writing run as background tasks,
    /// so the caller never waits on the socket. Used by both the server (one per client) and the client.
    /// </summary>
    public class MessageConnection
    {
        private const int READ_BUFFER_SIZE = 8192;

        /// <summary>
        /// A peer this many frames behind is too slow to keep up and gets dropped.
        /// </summary>
        private const int MAX_QUEUED_FRAMES = 256;

        private readonly TcpClient _Client;
        private readonly NetworkStream _Stream;
        private readonly CancellationTokenSource _Cancel = new();
        private readonly Channel<byte[]> _Outbound = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(MAX_QUEUED_FRAMES) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

        // bytes every connection in this process has read and written, for perf logging
        private static long _TotalBytesReceived;
        private static long _TotalBytesSent;

        private Action<MessageConnection, INetworkMessage> _OnMessage;
        private Action<MessageConnection> _OnClosed;
        private int _Closing;
        private int _Closed;

        public string RemoteAddress { get; }

        /// <summary>
        /// Why the connection closed, or null if it is open or closed normally.
        /// </summary>
        public string CloseReason { get; private set; }

        public bool IsClosed => Volatile.Read(ref _Closed) != 0;

        /// <summary>
        /// Bytes read by every connection in this process since it started.
        /// </summary>
        public static long TotalBytesReceived => Interlocked.Read(ref _TotalBytesReceived);

        /// <summary>
        /// Bytes written by every connection in this process since it started.
        /// </summary>
        public static long TotalBytesSent => Interlocked.Read(ref _TotalBytesSent);

        public MessageConnection(TcpClient client)
        {
            _Client = client;
            _Client.NoDelay = true;
            _Stream = client.GetStream();
            RemoteAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Starts the read and write tasks. onMessage and onClosed are called from background threads.
        /// </summary>
        public void Start(Action<MessageConnection, INetworkMessage> onMessage, Action<MessageConnection> onClosed)
        {
            _OnMessage = onMessage;
            _OnClosed = onClosed;

            _ = ReadLoopAsync();
            _ = WriteLoopAsync();
        }

        public void Send(INetworkMessage message)
        {
            Send(MessageSerializer.Serialize(message));
        }

        /// <summary>
        /// Queues a complete frame from MessageSerializer.Serialize. Never blocks.
        /// </summary>
        public void Send(byte[] frame)
        {
            if (Volatile.Read(ref _Closing) != 0)
                return;

            if (!_Outbound.Writer.TryWrite(frame))
                Close("Too slow to keep up");
        }

        /// <summary>
        /// Sends whatever is already queued, then closes.
        /// </summary>
        public void CloseAfterSending()
        {
            if (Interlocked.Exchange(ref _Closing, 1) != 0)
                return;

            // the write loop drains what's queued, then closes
            _Outbound.Writer.TryComplete();
        }

        /// <summary>
        /// Closes straight away, dropping anything still queued. Only the first reason given is kept.
        /// </summary>
        public void Close(string reason = null)
        {
            Interlocked.Exchange(ref _Closing, 1);

            if (Interlocked.Exchange(ref _Closed, 1) != 0)
                return;

            CloseReason = reason;
            _Outbound.Writer.TryComplete();
            _Cancel.Cancel();
            _Client.Dispose();
            _OnClosed?.Invoke(this);
        }

        private async Task ReadLoopAsync()
        {
            var buffer = new byte[READ_BUFFER_SIZE];
            var frames = new FrameBuffer();
            string reason = null;

            try
            {
                while (true)
                {
                    var read = await _Stream.ReadAsync(buffer, _Cancel.Token);

                    // zero bytes means the other end closed
                    if (read == 0)
                        break;

                    Interlocked.Add(ref _TotalBytesReceived, read);
                    frames.Append(buffer, 0, read);

                    while (frames.TryReadMessage(out var message))
                        _OnMessage(this, message);
                }
            }
            catch (InvalidDataException ex)
            {
                reason = $"Bad data: {ex.Message}";
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                reason = IsClosing() ? null : "Connection lost";
            }

            Close(reason);
        }

        private async Task WriteLoopAsync()
        {
            string reason = null;

            try
            {
                await foreach (var frame in _Outbound.Reader.ReadAllAsync(_Cancel.Token))
                {
                    await _Stream.WriteAsync(frame, _Cancel.Token);
                    Interlocked.Add(ref _TotalBytesSent, frame.Length);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                reason = IsClosing() ? null : "Connection lost";
            }

            Close(reason);
        }

        private bool IsClosing()
        {
            return Volatile.Read(ref _Closing) != 0;
        }
    }
}
