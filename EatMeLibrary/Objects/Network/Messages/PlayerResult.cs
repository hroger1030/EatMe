using System;
using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One player's line on the results screen, inside a LevelWon.
    /// </summary>
    public struct PlayerResult
    {
        public int PlayerId { get; }
        public string Name { get; }
        public eTeam Team { get; }
        public int Score { get; }

        public PlayerResult(int playerId, string name, eTeam team, int score)
        {
            PlayerId = playerId;
            Name = name ?? string.Empty;
            Team = team;
            Score = score;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(Name);
            writer.Write((byte)Team);
            writer.Write(Score);
        }

        public static PlayerResult Read(BinaryReader reader)
        {
            return new PlayerResult(
                reader.ReadInt32(),
                reader.ReadString(),
                (eTeam)reader.ReadByte(),
                reader.ReadInt32());
        }

        /// <summary>
        /// The players on one team, best first: highest score, then by name.
        /// </summary>
        public static List<PlayerResult> RankTeam(IEnumerable<PlayerResult> results, eTeam team)
        {
            var ranked = new List<PlayerResult>();

            foreach (var result in results)
                if (result.Team == team)
                    ranked.Add(result);

            ranked.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return ranked;
        }
    }
}
