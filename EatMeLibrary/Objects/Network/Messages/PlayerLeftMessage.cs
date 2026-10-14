using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: a player left or was disconnected.
    /// </summary>
    public class PlayerLeftMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.PlayerLeft;

        public int PlayerId { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
        }

        public void Read(BinaryReader reader)
        {
            PlayerId = reader.ReadInt32();
        }
    }
}
