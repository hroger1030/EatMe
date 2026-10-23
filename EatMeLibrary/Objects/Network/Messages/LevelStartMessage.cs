using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: a new level begins. The client builds the map itself with MapMaker.Generate(config, Levels.GetSeed(config.ServerSeed, Level)).
    /// </summary>
    public class LevelStartMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.LevelStart;

        public int Level { get; set; }
        public FastColor WallColor { get; set; }
        public FastColor WallFillColor { get; set; }
        public FastColor BackgroundColor { get; set; }

        /// <summary>
        /// ms of the start countdown left (LevelPhases.START_COUNTDOWN_MS when the level begins, less for a player
        /// who joins during it), during which the game is frozen.
        /// </summary>
        public int CountdownMs { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Level);
            NetworkHelper.WriteColor(writer, WallColor);
            NetworkHelper.WriteColor(writer, WallFillColor);
            NetworkHelper.WriteColor(writer, BackgroundColor);
            writer.Write(CountdownMs);
        }

        public void Read(BinaryReader reader)
        {
            Level = reader.ReadInt32();
            WallColor = NetworkHelper.ReadColor(reader);
            WallFillColor = NetworkHelper.ReadColor(reader);
            BackgroundColor = NetworkHelper.ReadColor(reader);
            CountdownMs = reader.ReadInt32();
        }
    }
}
