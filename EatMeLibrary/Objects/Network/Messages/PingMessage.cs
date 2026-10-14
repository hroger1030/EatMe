using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: asks for a Pong to measure round trip time.
    /// </summary>
    public class PingMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Ping;

        /// <summary>
        /// Client clock in ms when sent; echoed back in the Pong.
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
