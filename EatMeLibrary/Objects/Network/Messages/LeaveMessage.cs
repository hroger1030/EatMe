using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: the player is quitting. Has no fields.
    /// </summary>
    public class LeaveMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Leave;

        public void Write(BinaryWriter writer) { }

        public void Read(BinaryReader reader) { }
    }
}
