using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: answer to a ServerPing, sent as soon as the client reads it.
    /// </summary>
    public class ServerPongMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.ServerPong;

        /// <summary>
        /// ServerTime copied from the ServerPing.
        /// </summary>
        public long ServerTime { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ServerTime);
        }

        public void Read(BinaryReader reader)
        {
            ServerTime = reader.ReadInt64();
        }
    }
}
