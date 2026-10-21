using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: the player pressed TAB (or a gamepad shoulder button) to see the player list. The server answers
    /// with a PlayerList to that player only.
    /// </summary>
    public class PlayerListRequestMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.PlayerListRequest;

        public void Write(BinaryWriter writer)
        {
        }

        public void Read(BinaryReader reader)
        {
        }
    }
}
