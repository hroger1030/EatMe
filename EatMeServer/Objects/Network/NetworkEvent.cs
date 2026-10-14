using EatMeLibrary;

namespace EatMeServer
{
    /// <summary>
    /// Something that happened on a connection, queued by the network threads for the game loop.
    /// Message is only set when Type is Message.
    /// </summary>
    public struct NetworkEvent
    {
        public eNetworkEventType Type { get; }
        public IClientConnection Connection { get; }
        public INetworkMessage Message { get; }

        public NetworkEvent(eNetworkEventType type, IClientConnection connection, INetworkMessage message = null)
        {
            Type = type;
            Connection = connection;
            Message = message;
        }
    }
}
