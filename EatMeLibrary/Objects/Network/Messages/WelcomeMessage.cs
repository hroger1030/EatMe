using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: the Hello was accepted.
    /// </summary>
    public class WelcomeMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Welcome;

        public int PlayerId { get; set; }

        /// <summary>
        /// Team actually assigned, which may differ from the preferred one after balancing.
        /// </summary>
        public eTeam Team { get; set; }
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// The server's message of the day from server.cfg, shown on the title screen scroller.
        /// </summary>
        public string MessageOfTheDay { get; set; } = string.Empty;

        /// <summary>
        /// Every loading screen tip line from the server's LoadingScreenTips.txt; the client picks one for each intermission.
        /// </summary>
        public List<string> LoadingScreenTips { get; set; } = [];
        public GameConfiguration GameConfig { get; set; } = new GameConfiguration();

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write((byte)Team);
            writer.Write(ServerName);

            writer.Write(MessageOfTheDay ?? string.Empty);

            writer.Write(LoadingScreenTips.Count);
            foreach (var line in LoadingScreenTips)
                writer.Write(line);

            NetworkHelper.WriteGameConfiguration(writer, GameConfig);
        }

        public void Read(BinaryReader reader)
        {
            PlayerId = reader.ReadInt32();
            Team = (eTeam)reader.ReadByte();
            ServerName = reader.ReadString();

            MessageOfTheDay = reader.ReadString();

            var tipCount = NetworkHelper.ReadCount(reader);
            LoadingScreenTips = [];
            for (var i = 0; i < tipCount; i++)
                LoadingScreenTips.Add(reader.ReadString());

            GameConfig = NetworkHelper.ReadGameConfiguration(reader);
        }
    }
}
