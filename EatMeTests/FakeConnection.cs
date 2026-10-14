using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    /// <summary>
    /// Stands in for a client connection and records what the server sends to it.
    /// </summary>
    public class FakeConnection : IClientConnection
    {
        private readonly FrameBuffer _Frames = new();

        public int Id { get; }
        public List<INetworkMessage> Received { get; } = [];
        public bool Closed { get; private set; }
        public bool ClosedAfterSending { get; private set; }

        public FakeConnection(int id)
        {
            Id = id;
        }

        public void Send(byte[] frame)
        {
            _Frames.Append(frame, 0, frame.Length);

            while (_Frames.TryReadMessage(out var message))
                Received.Add(message);
        }

        public void CloseAfterSending()
        {
            ClosedAfterSending = true;
        }

        public void Close()
        {
            Closed = true;
        }

        public List<T> ReceivedOf<T>() where T : INetworkMessage
        {
            return Received.OfType<T>().ToList();
        }
    }
}
