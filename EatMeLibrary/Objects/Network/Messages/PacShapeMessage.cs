using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: the player picked another pac shape on the settings screen during a game. The server keeps
    /// it and sends a PlayerJoinedMessage with it to everyone (GameServer.ChangePacShape).
    /// </summary>
    public class PacShapeMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.PacShape;

        public ePacShape PacShape { get; set; } = PacShapes.DEFAULT;

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)PacShape);
        }

        public void Read(BinaryReader reader)
        {
            PacShape = (ePacShape)reader.ReadByte();
        }
    }
}
