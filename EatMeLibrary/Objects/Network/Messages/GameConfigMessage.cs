using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: the game rules changed (a server console set command), so the client builds the next level's
    /// map and draws its meters from the same GameConfiguration as the server.
    /// </summary>
    public class GameConfigMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.GameConfig;

        public GameConfiguration GameConfig { get; set; } = new GameConfiguration();

        public void Write(BinaryWriter writer)
        {
            NetworkHelper.WriteGameConfiguration(writer, GameConfig);
        }

        public void Read(BinaryReader reader)
        {
            GameConfig = NetworkHelper.ReadGameConfiguration(reader);
        }
    }
}
