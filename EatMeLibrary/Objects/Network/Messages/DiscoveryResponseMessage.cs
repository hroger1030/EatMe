using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: UDP reply to a DiscoveryRequest describing this server.
    /// </summary>
    public class DiscoveryResponseMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.DiscoveryResponse;

        public float ServerVersion { get; set; } = NetworkConstants.APP_VERSION;
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// TCP port to connect to for the game itself.
        /// </summary>
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ServerVersion);
            writer.Write(ServerName);
            writer.Write(Port);
            writer.Write(PlayerCount);
            writer.Write(MaxPlayers);
        }

        public void Read(BinaryReader reader)
        {
            ServerVersion = reader.ReadSingle();
            ServerName = reader.ReadString();
            Port = reader.ReadInt32();
            PlayerCount = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
        }
    }
}
