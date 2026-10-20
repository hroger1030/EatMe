using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to server: the player pressed the action button (space, or A on a gamepad), which triggers whatever
    /// their power up effect does on action (GameServer.HandleAction).
    /// </summary>
    public class ActionMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Action;

        public void Write(BinaryWriter writer)
        {
        }

        public void Read(BinaryReader reader)
        {
        }
    }
}
