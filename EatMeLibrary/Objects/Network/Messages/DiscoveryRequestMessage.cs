using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Client to any server: UDP broadcast on the discovery port asking which games are running on the LAN.
    /// </summary>
    public class DiscoveryRequestMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.DiscoveryRequest;

        public void Write(BinaryWriter writer)
        {
        }

        public void Read(BinaryReader reader)
        {
        }
    }
}
