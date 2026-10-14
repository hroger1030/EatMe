using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: first message after connecting, asks to join the game.
    /// </summary>
    public class HelloMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Hello;

        public float ClientVersion { get; set; } = NetworkConstants.APP_VERSION;
        public string PlayerName { get; set; } = string.Empty;
        public eTeamPreference PreferredTeam { get; set; } = eTeamPreference.Either;

        public void Write(BinaryWriter writer)
        {
            writer.Write(ClientVersion);
            writer.Write(PlayerName);
            writer.Write((byte)PreferredTeam);
        }

        public void Read(BinaryReader reader)
        {
            ClientVersion = reader.ReadSingle();
            PlayerName = reader.ReadString();
            PreferredTeam = (eTeamPreference)reader.ReadByte();
        }
    }
}
