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
        /// Power ups the player has eaten.
        /// </summary>
        public int PowerUps { get; }
        /// <summary>
        /// The power up effect working on the pac, None when it has none.
        /// </summary>
        public ePowerUp PowerUpEffect { get; }
        /// <summary>
        /// ms left on PowerUpEffect.
        /// </summary>
        public int PowerUpMsLeft { get; }
        /// <summary>
        /// Whether a power pill is working on the pac.
        /// </summary>
        public bool Powered => PowerUpEffect == ePowerUp.PowerPill;
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

        /// <summary>
        /// ms before a dead pac respawns (0 while alive). The client shows its own as a countdown.
        /// </summary>
        public int RespawnMsLeft { get; }

        /// <summary>
        /// Whether the pac's team can't respawn right now because a living enemy has Lockdown (key) working.
        /// </summary>
        public bool SpawnLocked { get; }

        /// <summary>
        /// Whether the pac is growing from standing on its own team's spawn; the client plays the charge loop for its own.
        /// </summary>
        public bool SpawnFeeding { get; }

        /// <summary>
        /// Whether the pac is rolling with Melon Roll; the client spins it.
        /// </summary>
        public bool Rolling { get; }

        public PacState(int playerId, eTeam team, float x, float y, float size, eFacing facing, bool alive, bool hasFlag, bool eating,
            int flagCaps = 0, int ghostKills = 0, int playerKills = 0, int rescues = 0, ePowerUp powerUpEffect = ePowerUp.None, int powerUpMsLeft = 0, int totalScore = 0, int msSinceEaten = 0, bool stunned = false,
            int respawnMsLeft = 0, bool spawnLocked = false, int powerUps = 0, bool spawnFeeding = false, bool rolling = false)
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
            RespawnMsLeft = respawnMsLeft;
            SpawnLocked = spawnLocked;
            PowerUps = powerUps;
            SpawnFeeding = spawnFeeding;
            Rolling = rolling;
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
            writer.Write(RespawnMsLeft);
            writer.Write(SpawnLocked);
            writer.Write(PowerUps);
            writer.Write(SpawnFeeding);
            writer.Write(Rolling);
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
                (ePowerUp)reader.ReadByte(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadBoolean());
        }
    }
}
