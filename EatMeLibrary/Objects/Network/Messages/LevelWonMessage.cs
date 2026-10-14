using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: a team reached MaxScore. The results are shown, the game pauses for MsRemaining,
    /// then a LevelStart for the next level follows. Also sent to a player who joins during that pause.
    /// </summary>
    public class LevelWonMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.LevelWon;

        public eTeam WinningTeam { get; set; }
        public int RedScore { get; set; }
        public int BlueScore { get; set; }

        /// <summary>
        /// ms until the next level starts.
        /// </summary>
        public int MsRemaining { get; set; }

        /// <summary>
        /// Every player at the end of the level, bots included, in no particular order.
        /// </summary>
        public List<PlayerResult> Results { get; set; } = [];

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)WinningTeam);
            writer.Write(RedScore);
            writer.Write(BlueScore);
            writer.Write(MsRemaining);

            writer.Write(Results.Count);
            foreach (var result in Results)
                result.Write(writer);
        }

        public void Read(BinaryReader reader)
        {
            WinningTeam = (eTeam)reader.ReadByte();
            RedScore = reader.ReadInt32();
            BlueScore = reader.ReadInt32();
            MsRemaining = reader.ReadInt32();

            var resultCount = NetworkHelper.ReadCount(reader);
            Results = [];
            for (var i = 0; i < resultCount; i++)
                Results.Add(PlayerResult.Read(reader));
        }
    }
}
