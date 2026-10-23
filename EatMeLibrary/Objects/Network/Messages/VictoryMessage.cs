using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: a team reached MaxScore. The game freezes and "Victory!" shows for MsRemaining, then a
    /// LevelWonMessage brings up the results. Also sent to a player who joins during that pause.
    /// </summary>
    public class VictoryMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Victory;

        public eTeam WinningTeam { get; set; }

        /// <summary>
        /// ms until the results show.
        /// </summary>
        public int MsRemaining { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)WinningTeam);
            writer.Write(MsRemaining);
        }

        public void Read(BinaryReader reader)
        {
            WinningTeam = (eTeam)reader.ReadByte();
            MsRemaining = reader.ReadInt32();
        }
    }
}
