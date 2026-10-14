using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// A message sent between client and server. Write and Read must handle the same fields in the same order.
    /// </summary>
    public interface INetworkMessage
    {
        eMessageType MessageType { get; }

        void Write(BinaryWriter writer);

        void Read(BinaryReader reader);
    }
}
