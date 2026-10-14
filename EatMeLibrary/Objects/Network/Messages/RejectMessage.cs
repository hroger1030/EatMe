using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: the Hello was refused. The server closes the connection after sending it.
    /// </summary>
    public class RejectMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Reject;

        public eRejectReason Reason { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Reason);
        }

        public void Read(BinaryReader reader)
        {
            Reason = (eRejectReason)reader.ReadByte();
        }
    }
}
