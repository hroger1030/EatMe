using EatMeLibrary;

namespace EatMeServer
{
    /// <summary>
    /// A player who has joined, with the connection and input the server tracks for them. Bots have no connection.
    /// </summary>
    public class ServerPlayer
    {
        public IClientConnection Connection { get; }
        public Player Player { get; }

        /// <summary>
        /// Direction from the newest Input message received, or the way a bot has chosen to go.
        /// </summary>
        public eDirection Input { get; set; }
        public uint LastInputSequence { get; set; }

        /// <summary>
        /// The last direction a human pressed, kept for BufferedTurnMs after it is let go (GameConfig.TurnBufferTimeMs)
        /// so an early tap still turns at the next open junction. Null when there is none.
        /// </summary>
        public eFacing? BufferedTurn { get; set; }
        public int BufferedTurnMs { get; set; }

        /// <summary>
        /// True from a direction being pressed until the next MovePacs, which may make it a late turn (TurnAssist.TryLateTurn).
        /// </summary>
        public bool HasNewTurnRequest { get; set; }

        /// <summary>
        /// Cells the pac has moved since it last changed direction, so a late turn never puts it back further than
        /// it has come.
        /// </summary>
        public float StraightDistance { get; set; }

        /// <summary>
        /// True from a human's pac spawning (joining, a level's start or a respawn) until a direction is held or
        /// buffered; the pac stays put until then. Always false for bots.
        /// </summary>
        public bool AwaitingInput { get; set; }

        /// <summary>
        /// This player's round trip, measured with ServerPing/ServerPong.
        /// </summary>
        public LagEstimate Lag { get; } = new();

        /// <summary>
        /// ms since the last ServerPing; starts full so the first goes out on the first tick.
        /// </summary>
        public int PingTimerMs { get; set; } = LagEstimate.PING_INTERVAL_MS;

        /// <summary>
        /// This player's entry on the high score list, made the first time they score at a level's end and updated
        /// after that, so one connection holds at most one entry. Null until then.
        /// </summary>
        public HighScoreEntry HighScore { get; set; }

        /// <summary>
        /// The team the player asked for in their Hello, kept for rebalancing teams between levels. Either for bots.
        /// </summary>
        public eTeamPreference PreferredTeam { get; set; } = eTeamPreference.Either;

        /// <summary>
        /// What a bot remembers between cell centres (its goal, fleeing and what it keeps away from). Unused for humans.
        /// </summary>
        public BotMemory Memory { get; } = new();

        public bool IsBot => Connection == null;

        public ServerPlayer(IClientConnection connection, Player player)
        {
            Connection = connection;
            Player = player;
            AwaitingInput = connection != null;
        }
    }
}
