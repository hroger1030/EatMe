using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: every player and bot in the game with its team and ping, as they were when the PlayerListRequest
    /// arrived.
    /// </summary>
    public class PlayerListMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.PlayerList;

        public List<PlayerListEntry> Entries { get; set; } = [];

        public void Write(BinaryWriter writer)
        {
            writer.Write(Entries.Count);
            foreach (var entry in Entries)
                entry.Write(writer);
        }

        public void Read(BinaryReader reader)
        {
            var count = NetworkHelper.ReadCount(reader);
            Entries = [];
            for (var i = 0; i < count; i++)
                Entries.Add(PlayerListEntry.Read(reader));
        }
    }
}
