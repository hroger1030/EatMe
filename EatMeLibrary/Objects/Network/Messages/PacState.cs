using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One pac's state inside a Snapshot.
    /// </summary>
    public struct PacState
    {
        public int PlayerId { get; }
        public eTeam Team { get; }
        public float X { get; }
        public float Y { get; }
        public float Size { get; }
        public eFacing Facing { get; }
        public bool Alive { get; }
        public bool HasFlag { get; }
        /// <summary>
        /// Whether the last cell the pac entered had a dot or power pill in it.
        /// </summary>
        public bool Eating { get; }
        /// <summary>
        /// Enemy flags the player has captured.
        /// </summary>
        public int FlagCaps { get; }
        public int GhostKills { get; }
        /// <summary>
        /// Enemy pacs the player has eaten.
        /// </summary>
        public int PlayerKills { get; }
        /// <summary>
        /// Times the player has returned their own flag to base.
        /// </summary>
        public int Rescues { get; }
        /// <summary>
        /// The power up effect working on the pac, None when it has none.
        /// </summary>
        public ePowerUpEffect PowerUpEffect { get; }
        /// <summary>
        /// ms left on PowerUpEffect.
        /// </summary>
        public int PowerUpMsLeft { get; }
        /// <summary>
        /// Whether a power pill is working on the pac.
        /// </summary>
        public bool Powered => PowerUpEffect == ePowerUpEffect.PowerPill;
        /// <summary>
        /// Every point the player has won since connecting.
        /// </summary>
        public int TotalScore { get; }
        /// <summary>
        /// ms since the pac last ate a dot or power pill; it starts shrinking once this passes MsUntilStarvation.
        /// </summary>
        public int MsSinceEaten { get; }
        /// <summary>
        /// Whether the pac is stunned (can't move); the client spins it in place.
        /// </summary>
        public bool Stunned { get; }

        public PacState(int playerId, eTeam team, float x, float y, float size, eFacing facing, bool alive, bool hasFlag, bool eating,
            int flagCaps = 0, int ghostKills = 0, int playerKills = 0, int rescues = 0, ePowerUpEffect powerUpEffect = ePowerUpEffect.None, int powerUpMsLeft = 0, int totalScore = 0, int msSinceEaten = 0, bool stunned = false)
        {
            PlayerId = playerId;
            Team = team;
            X = x;
            Y = y;
            Size = size;
            Facing = facing;
            Alive = alive;
            HasFlag = hasFlag;
            Eating = eating;
            FlagCaps = flagCaps;
            GhostKills = ghostKills;
            PlayerKills = playerKills;
            Rescues = rescues;
            PowerUpEffect = powerUpEffect;
            PowerUpMsLeft = powerUpMsLeft;
            TotalScore = totalScore;
            MsSinceEaten = msSinceEaten;
            Stunned = stunned;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write((byte)Team);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Size);
            writer.Write((byte)Facing);
            writer.Write(Alive);
            writer.Write(HasFlag);
            writer.Write(Eating);
            writer.Write(FlagCaps);
            writer.Write(GhostKills);
            writer.Write(PlayerKills);
            writer.Write(Rescues);
            writer.Write((byte)PowerUpEffect);
            writer.Write(PowerUpMsLeft);
            writer.Write(TotalScore);
            writer.Write(MsSinceEaten);
            writer.Write(Stunned);
        }

        public static PacState Read(BinaryReader reader)
        {
            return new PacState(
                reader.ReadInt32(),
                (eTeam)reader.ReadByte(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                (eFacing)reader.ReadByte(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                (ePowerUpEffect)reader.ReadByte(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean());
        }
    }
}
