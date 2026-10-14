using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: asks for a ServerPong so the server can measure the player's lag.
    /// </summary>
    public class ServerPingMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.ServerPing;

        /// <summary>
        /// Server clock (GameServer.ClockMs) when sent; echoed back in the ServerPong.
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
