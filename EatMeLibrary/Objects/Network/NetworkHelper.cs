using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Reads and writes the shared types that several messages carry.
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// Reads a list length and rejects negative or impossibly large values from a corrupt or hostile stream.
        /// </summary>
        public static int ReadCount(BinaryReader reader)
        {
            var count = reader.ReadInt32();

            if (count < 0 || count > NetworkConstants.MAX_MESSAGE_SIZE)
                throw new InvalidDataException($"Invalid list count: {count}");

            return count;
        }

        public static void WriteColor(BinaryWriter writer, FastColor color)
        {
            writer.Write(color.PackedValue);
        }

        public static FastColor ReadColor(BinaryReader reader)
        {
            return new FastColor(reader.ReadUInt32());
        }

        /// <summary>
        /// Writes every game rule. Clients need them all to rebuild the map from the seed.
        /// Add new GameConfiguration properties here and in ReadGameConfiguration.
        /// </summary>
        public static void WriteGameConfiguration(BinaryWriter writer, GameConfiguration config)
        {
            writer.Write(config.MaxPlayers);
            writer.Write(config.MinPlayers);
            writer.Write(config.MaxScore);
            writer.Write(config.FlagScore);
            writer.Write(config.FlagRescue);
            writer.Write(config.PlayerKill);
            writer.Write(config.GhostKill);
            writer.Write(config.MazeSize);
            writer.Write(config.WarpTunnelCount);
            writer.Write(config.GhostCount);
            writer.Write(config.PowerPillCount);
            writer.Write(config.FlagCount);
            writer.Write(config.PlayerBaseCount);
            writer.Write(config.GhostVisionDistance);
            writer.Write(config.GhostRegenTimeMs);
            writer.Write(config.DotRegenPercent);
            writer.Write(config.PowerPillRegenTimeMs);
            writer.Write(config.PlayerRespawnTimeMs);
            writer.Write(config.PacMaxSize);
            writer.Write(config.PacSpeed);
            writer.Write(config.GhostSpeed);
            writer.Write(config.PowerUpSpeed);
            writer.Write(config.PacShrinkPerSecond);
            writer.Write(config.MsUntilStarvation);
            writer.Write(config.DotSizeGain);
            writer.Write(config.PowerPillSizeGain);
            writer.Write(config.PowerUpDurationMs);
            writer.Write(config.IntermissionTimeMs);
            writer.Write(config.PowerUpSpawnTimerMs);
            writer.Write(config.TurnBufferTimeMs);
            writer.Write(config.TurnGraceDistance);
            writer.Write(config.LagCompensationMaxMs);
            writer.Write(config.ServerSeed);
        }

        public static GameConfiguration ReadGameConfiguration(BinaryReader reader)
        {
            return new GameConfiguration
            {
                MaxPlayers = reader.ReadInt32(),
                MinPlayers = reader.ReadInt32(),
                MaxScore = reader.ReadInt32(),
                FlagScore = reader.ReadInt32(),
                FlagRescue = reader.ReadInt32(),
                PlayerKill = reader.ReadInt32(),
                GhostKill = reader.ReadInt32(),
                MazeSize = reader.ReadInt32(),
                WarpTunnelCount = reader.ReadInt32(),
                GhostCount = reader.ReadInt32(),
                PowerPillCount = reader.ReadInt32(),
                FlagCount = reader.ReadInt32(),
                PlayerBaseCount = reader.ReadInt32(),
                GhostVisionDistance = reader.ReadInt32(),
                GhostRegenTimeMs = reader.ReadInt32(),
                DotRegenPercent = reader.ReadInt32(),
                PowerPillRegenTimeMs = reader.ReadInt32(),
                PlayerRespawnTimeMs = reader.ReadInt32(),
                PacMaxSize = reader.ReadSingle(),
                PacSpeed = reader.ReadSingle(),
                GhostSpeed = reader.ReadSingle(),
                PowerUpSpeed = reader.ReadSingle(),
                PacShrinkPerSecond = reader.ReadSingle(),
                MsUntilStarvation = reader.ReadInt32(),
                DotSizeGain = reader.ReadSingle(),
                PowerPillSizeGain = reader.ReadSingle(),
                PowerUpDurationMs = reader.ReadInt32(),
                IntermissionTimeMs = reader.ReadInt32(),
                PowerUpSpawnTimerMs = reader.ReadInt32(),
                TurnBufferTimeMs = reader.ReadInt32(),
                TurnGraceDistance = reader.ReadSingle(),
                LagCompensationMaxMs = reader.ReadInt32(),
                ServerSeed = reader.ReadInt32(),
            };
        }
    }
}
