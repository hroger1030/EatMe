using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One player's line in the player list (TAB), inside a PlayerList: name, team, whether it is a bot and its ping.
    /// </summary>
    public struct PlayerListEntry
    {
        /// <summary>
        /// PingMs for a bot, or for a human the server hasn't measured yet.
        /// </summary>
        public const int NO_PING = -1;

        public int PlayerId { get; }
        public string Name { get; }
        public eTeam Team { get; }
        public bool IsBot { get; }
        public int PingMs { get; }

        public PlayerListEntry(int playerId, string name, eTeam team, bool isBot, int pingMs)
        {
            PlayerId = playerId;
            Name = name ?? string.Empty;
            Team = team;
            IsBot = isBot;
            PingMs = isBot || pingMs < 0 ? NO_PING : pingMs;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(Name);
            writer.Write((byte)Team);
            writer.Write(IsBot);
            writer.Write(PingMs);
        }

        public static PlayerListEntry Read(BinaryReader reader)
        {
            return new PlayerListEntry(
                reader.ReadInt32(),
                reader.ReadString(),
                (eTeam)reader.ReadByte(),
                reader.ReadBoolean(),
                reader.ReadInt32());
        }
    }
}
