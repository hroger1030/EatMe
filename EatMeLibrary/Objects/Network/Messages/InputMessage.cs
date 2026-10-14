using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: the direction the player is holding. Sent when it changes.
    /// </summary>
    public class InputMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Input;

        /// <summary>
        /// Counts up with each input sent, so the server can ignore stale ones.
        /// </summary>
        public uint Sequence { get; set; }
        public eDirection Direction { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Sequence);
            writer.Write((byte)Direction);
        }

        public void Read(BinaryReader reader)
        {
            Sequence = reader.ReadUInt32();
            Direction = (eDirection)reader.ReadByte();
        }
    }
}
