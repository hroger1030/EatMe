using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: answer to a Ping.
    /// </summary>
    public class PongMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Pong;

        /// <summary>
        /// ClientTime copied from the Ping.
        /// </summary>
        public long ClientTime { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ClientTime);
        }

        public void Read(BinaryReader reader)
        {
            ClientTime = reader.ReadInt64();
        }
    }
}
