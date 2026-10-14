using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: the server's high score list, highest first. Sent when a player joins and again whenever the list changes.
    /// </summary>
    public class HighScoresMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.HighScores;

        public List<HighScoreEntry> Scores { get; set; } = [];

        public void Write(BinaryWriter writer)
        {
            writer.Write(Scores.Count);
            foreach (var entry in Scores)
            {
                writer.Write(entry.Name ?? string.Empty);
                writer.Write(entry.Score);
                writer.Write(entry.Date ?? string.Empty);
            }
        }

        public void Read(BinaryReader reader)
        {
            var count = NetworkHelper.ReadCount(reader);
            Scores = [];
            for (var i = 0; i < count; i++)
                Scores.Add(new HighScoreEntry { Name = reader.ReadString(), Score = reader.ReadInt32(), Date = reader.ReadString() });
        }
    }
}
