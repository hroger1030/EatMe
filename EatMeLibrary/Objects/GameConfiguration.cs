namespace EatMeLibrary
{
    public class GameConfiguration
    {
        public int MaxPlayers { get; set; } = 128;

        /// <summary>
        /// The server adds bots until humans plus bots reach this many, and removes them as humans join. 0 means no bots.
        /// </summary>
        public int MinPlayers { get; set; } = 0;

        /// <summary>
        /// Team score needed to win the level.
        /// </summary>
        public int MaxScore { get; set; } = 100;

        /// <summary>
        /// Points for capturing a flag.
        /// </summary>
        public int FlagScore { get; set; } = 20;

        /// <summary>
        /// Points for returning your own flag to base after it was dropped.
        /// </summary>
        public int FlagRescue { get; set; } = 3;

        /// <summary>
        /// Points for eating an enemy pac.
        /// </summary>
        public int PlayerKill { get; set; } = 1;

        /// <summary>
        /// Points for killing a ghost.
        /// </summary>
        public int GhostKill { get; set; } = 2;

        /// <summary>
        /// Number of descreet cells in the map
        /// </summary>
        public int MazeSize { get; set; } = 100;

        /// <summary>
        /// Number of warp tunnels through the outer wall. A hole on one side warps to the hole
        /// directly opposite it. Symmetry may add one extra tunnel.
        /// </summary>
        public int WarpTunnelCount { get; set; } = 4;

        /// <summary>
        /// Number of ghosts in the maze.
        /// </summary>
        public int GhostCount { get; set; } = 4;

        /// <summary>
        /// Number of power pills in the maze.
        /// </summary>
        public int PowerPillCount { get; set; } = 4;

        /// <summary>
        /// Number of flags per team
        /// </summary>
        public int FlagCount { get; set; } = 1;

        /// <summary>
        /// Number player spawn points per team.
        /// </summary>
        public int PlayerBaseCount { get; set; } = 1;

        /// <summary>
        /// Distance within which a ghost spots and chases a pac.
        /// </summary>
        public int GhostVisionDistance { get; set; } = 10;

        /// <summary>
        /// Time before a dead ghost reappears.
        /// </summary>
        public int GhostRegenTimeMs { get; set; } = 5000;

        /// <summary>
        /// Percent (0 to 100) of a level's dots that stay eaten: once that many are eaten, each dot eaten puts the
        /// oldest eaten one back (DotRegenQueue).
        /// </summary>
        public int DotRegenPercent { get; set; } = 50;

        /// <summary>
        /// Time before an eaten power pill reappears.
        /// </summary>
        public int PowerPillRegenTimeMs { get; set; } = 5000;

        /// <summary>
        /// Time before a dead player respawns at base.
        /// </summary>
        public int PlayerRespawnTimeMs { get; set; } = 5000;

        /// <summary>
        /// Dots can't make a pac bigger than this (power pills can), and a pac needs at least this size (and a power
        /// pill working) to eat ghosts.
        /// </summary>
        public float PacMaxSize { get; set; } = 100.0f;

        /// <summary>
        /// Pac speed in map cells per second.
        /// </summary>
        public float PacSpeed { get; set; } = 6.0f;

        /// <summary>
        /// Ghost speed in map cells per second.
        /// </summary>
        public float GhostSpeed { get; set; } = 5.0f;

        /// <summary>
        /// Size a starving pac loses each second.
        /// </summary>
        public float PacShrinkPerSecond { get; set; } = 3.0f;

        /// <summary>
        /// Time after a pac last ate a dot or power pill before it starts starving (shrinking).
        /// </summary>
        public int MsUntilStarvation { get; set; } = 1000;

        /// <summary>
        /// Size a pac gains for each dot it eats.
        /// </summary>
        public float DotSizeGain { get; set; } = 1.0f;

        /// <summary>
        /// Size a pac gains for each power pill it eats.
        /// </summary>
        public float PowerPillSizeGain { get; set; } = 100.0f;

        /// <summary>
        /// ms every power up effect (a power pill included) lasts. When a power pill wears off the pac loses PowerPillSizeGain.
        /// </summary>
        public int PowerUpDurationMs { get; set; } = 10000;

        /// <summary>
        /// ms the results screen is shown between levels. The game is paused until it runs out.
        /// </summary>
        public int IntermissionTimeMs { get; set; } = 10000;

        /// <summary>
        /// ms between power ups, counted from the start of the level, so by default one appears at 30, 60, 90 seconds and so on.
        /// 0 or less means no power ups.
        /// </summary>
        public int PowerUpSpawnTimerMs { get; set; } = 30000;

        /// <summary>
        /// ms a human pac remembers a direction pressed and let go, so an early tap still turns it at the next open
        /// junction. 0 turns the buffer off.
        /// </summary>
        public int TurnBufferTimeMs { get; set; } = 400;

        /// <summary>
        /// Cells past an open junction a human pac can be and still turn there when the turn is pressed late (it is
        /// put back on the junction). Lag adds to it (LagCompensationMaxMs), up to TurnAssist.MAX_GRACE_CELLS. 0 turns
        /// late turns off.
        /// </summary>
        public float TurnGraceDistance { get; set; } = 0.3f;

        /// <summary>
        /// Most ms of a player's measured round trip that widen their late turn window. 0 turns lag compensation off.
        /// </summary>
        public int LagCompensationMaxMs { get; set; } = 250;

        /// <summary>
        /// Picks the mazes. Each level's map is built from Levels.GetSeed(ServerSeed, level), so the same ServerSeed always gives the same mazes.
        /// </summary>
        public int ServerSeed { get; set; } = 0;

        /// <summary>
        /// The settings a level's map and ghost lineup are built from. Changing one mid-level only counts from the
        /// next level, so clients joining before then still build the map the server is playing.
        /// </summary>
        public static readonly string[] MAP_SETTINGS =
        [
            nameof(MazeSize), nameof(WarpTunnelCount), nameof(GhostCount), nameof(PowerPillCount),
            nameof(FlagCount), nameof(PlayerBaseCount), nameof(ServerSeed),
        ];

        public GameConfiguration Clone()
        {
            return (GameConfiguration)MemberwiseClone();
        }

        /// <summary>
        /// Copies the map settings (MAP_SETTINGS) from other, leaving the rest alone.
        /// </summary>
        public void CopyMapSettingsFrom(GameConfiguration other)
        {
            MazeSize = other.MazeSize;
            WarpTunnelCount = other.WarpTunnelCount;
            GhostCount = other.GhostCount;
            PowerPillCount = other.PowerPillCount;
            FlagCount = other.FlagCount;
            PlayerBaseCount = other.PlayerBaseCount;
            ServerSeed = other.ServerSeed;
        }
    }
}
