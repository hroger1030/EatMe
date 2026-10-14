using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: a player is in the game. Also sent for every existing player when someone joins.
    /// </summary>
    public class PlayerJoinedMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.PlayerJoined;

        public int PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public eTeam Team { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(Name);
            writer.Write((byte)Team);
        }

        public void Read(BinaryReader reader)
        {
            PlayerId = reader.ReadInt32();
            Name = reader.ReadString();
            Team = (eTeam)reader.ReadByte();
        }
    }
}
