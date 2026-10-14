using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Everything the client knows about the game, built up from the messages the server sends.
    /// Holds no drawing code, so it can be unit tested.
    /// </summary>
    public class ClientGameState
    {
        public const int NO_PLAYER = -1;

        private const long MS_PER_SECOND = 1000;

        private readonly Dictionary<int, PlayerInfo> _Players = [];

        /// <summary>
        /// Our own player id, or NO_PLAYER until the server's Welcome arrives.
        /// </summary>
        public int PlayerId { get; private set; } = NO_PLAYER;
        public eTeam Team { get; private set; }
        public string ServerName { get; private set; } = string.Empty;

        /// <summary>
        /// The server's message of the day, empty until the server's Welcome arrives.
        /// </summary>
        public string MessageOfTheDay { get; private set; } = string.Empty;
        public List<string> LoadingScreenTips { get; private set; } = [];

        /// <summary>
        /// The tip shown on the results, picked at random from LoadingScreenTips on each LevelWon. Empty if there are none.
        /// </summary>
        public string IntermissionTip { get; private set; } = string.Empty;
        private readonly Random _TipRandom = new();
        public GameConfiguration GameConfig { get; private set; }

        public int Level { get; private set; }
        public LevelInfo LevelInfo { get; private set; }
        public FastColor WallColor { get; private set; }
        public FastColor WallFillColor { get; private set; }
        public FastColor BackgroundColor { get; private set; }

        /// <summary>
        /// The current level's map, generated here from the seed. Null until a LevelStart arrives.
        /// </summary>
        public eGridDef[,] Map { get; private set; }

        /// <summary>
        /// The color of the dot or power pill in each cell ([x, y], the same size as Map), so each can be set on its own.
        /// Rebuilt with the map on every LevelStart: BlueDotColor on blue's (left) half, RedDotColor on red's (right) half.
        /// Null until a LevelStart arrives.
        /// </summary>
        public FastColor[,] DotColors { get; private set; }

        /// <summary>
        /// The level's ghost personalities, indexed by ghost id (MapMaker.PickGhostLineup, worked out here like the map).
        /// Empty until a LevelStart arrives.
        /// </summary>
        public GhostPersonality[] GhostLineup { get; private set; } = [];

        /// <summary>
        /// The color every dot and power pill on blue's (left) half starts a level with.
        /// </summary>
        public FastColor BlueDotColor { get; set; }

        /// <summary>
        /// The color every dot and power pill on red's (right) half starts a level with.
        /// </summary>
        public FastColor RedDotColor { get; set; }

        /// <summary>
        /// Newest snapshot received, or null before the first.
        /// </summary>
        public SnapshotMessage Snapshot { get; private set; }

        public eRejectReason? RejectReason { get; private set; }

        /// <summary>
        /// The server's high score list, highest first, from the newest HighScores message. Empty until one arrives.
        /// </summary>
        public List<HighScoreEntry> HighScores { get; private set; } = [];

        /// <summary>
        /// Winner of the most recent level, until the next level starts.
        /// </summary>
        public eTeam? LevelWinner { get; private set; }

        /// <summary>
        /// Final scores of the most recent level, until the next level starts.
        /// </summary>
        public int LevelRedScore { get; private set; }
        public int LevelBlueScore { get; private set; }

        /// <summary>
        /// Blue players at the end of the most recent level, best first. Empty while a level is being played.
        /// </summary>
        public List<PlayerResult> BlueResults { get; private set; } = [];

        /// <summary>
        /// Red players at the end of the most recent level, best first. Empty while a level is being played.
        /// </summary>
        public List<PlayerResult> RedResults { get; private set; } = [];

        /// <summary>
        /// Client clock time (ms) the next level is due to start, while the results are shown.
        /// </summary>
        public long IntermissionEndMs { get; private set; }

        /// <summary>
        /// True between a LevelWon and the next LevelStart, while the results are shown.
        /// </summary>
        public bool IsIntermission => LevelWinner.HasValue;

        /// <summary>
        /// Round trip time to the server in ms from the last Pong, or -1 before the first.
        /// </summary>
        public long PingMs { get; private set; } = -1;

        /// <summary>
        /// The ping for the stats bar: "Ping n ms", or "Ping --" before the first Pong.
        /// </summary>
        public string GetPingText()
        {
            return PingMs < 0
                ? $"{Const.STRING_PING} {Const.STRING_PING_UNKNOWN}"
                : $"{Const.STRING_PING} {PingMs} {Const.STRING_MS}";
        }

        /// <summary>
        /// The personality of the ghost with this id, or Norm if the id isn't in the lineup.
        /// </summary>
        public GhostPersonality GetGhostPersonality(int ghostId)
        {
            return ghostId >= 0 && ghostId < GhostLineup.Length ? GhostLineup[ghostId] : GhostPersonalities.NORM;
        }

        /// <summary>
        /// The level title for the score bar: "Level n name", or empty before the first LevelStart.
        /// </summary>
        public string GetLevelTitle()
        {
            return LevelInfo == null
                ? string.Empty
                : $"{Const.STRING_LEVEL} {Level} {LevelInfo.Name}";
        }

        public bool IsJoined => PlayerId != NO_PLAYER;

        public IReadOnlyDictionary<int, PlayerInfo> Players => _Players;

        /// <summary>
        /// Updates the state from one server message. nowMs is the client clock, used to time pings.
        /// </summary>
        public void Apply(INetworkMessage message, long nowMs)
        {
            switch (message)
            {
                case WelcomeMessage welcome:
                    PlayerId = welcome.PlayerId;
                    Team = welcome.Team;
                    ServerName = welcome.ServerName;
                    MessageOfTheDay = welcome.MessageOfTheDay;
                    LoadingScreenTips = welcome.LoadingScreenTips;
                    GameConfig = welcome.GameConfig;
                    break;

                case RejectMessage reject:
                    RejectReason = reject.Reason;
                    break;

                case LevelStartMessage levelStart:
                    Level = levelStart.Level;
                    LevelInfo = Levels.GetLevel(levelStart.Level);
                    WallColor = levelStart.WallColor;
                    WallFillColor = levelStart.WallFillColor;
                    BackgroundColor = levelStart.BackgroundColor;
                    var config = GameConfig ?? new GameConfiguration();
                    var seed = Levels.GetSeed(config.ServerSeed, levelStart.Level);
                    Map = MapMaker.Generate(config, seed);
                    GhostLineup = MapMaker.PickGhostLineup(config, seed, levelStart.Level);
                    DotColors = LevelInfo.IsRainbow
                        ? Rainbow.BuildDotColors(Map.GetLength(0), Map.GetLength(1))
                        : BuildDotColors(Map.GetLength(0), Map.GetLength(1), BlueDotColor, RedDotColor);
                    Snapshot = null;
                    LevelWinner = null;
                    BlueResults = [];
                    RedResults = [];
                    break;

                case SnapshotMessage snapshot:
                    // TCP keeps order, but a snapshot from before a level change is still stale
                    if (Snapshot == null || snapshot.Tick > Snapshot.Tick)
                        Snapshot = snapshot;
                    break;

                case CellsChangedMessage cellsChanged:
                    ApplyCellChanges(cellsChanged);
                    break;

                case PlayerJoinedMessage joined:
                    _Players[joined.PlayerId] = new PlayerInfo(joined.PlayerId, joined.Name, joined.Team);
                    break;

                case PlayerLeftMessage left:
                    _Players.Remove(left.PlayerId);
                    break;

                case LevelWonMessage won:
                    LevelWinner = won.WinningTeam;
                    LevelRedScore = won.RedScore;
                    LevelBlueScore = won.BlueScore;
                    BlueResults = PlayerResult.RankTeam(won.Results, eTeam.Blue);
                    RedResults = PlayerResult.RankTeam(won.Results, eTeam.Red);
                    IntermissionEndMs = nowMs + won.MsRemaining;
                    IntermissionTip = TipRules.PickTip(LoadingScreenTips, _TipRandom);
                    break;

                case PongMessage pong:
                    PingMs = nowMs - pong.ClientTime;
                    break;

                case HighScoresMessage highScores:
                    HighScores = highScores.Scores;
                    break;
            }
        }

        /// <summary>
        /// Our own pac from the newest snapshot, or null if it isn't in one yet.
        /// </summary>
        public PacState? GetOwnPac()
        {
            if (Snapshot == null)
                return null;

            foreach (var pac in Snapshot.Pacs)
                if (pac.PlayerId == PlayerId)
                    return pac;

            return null;
        }

        /// <summary>
        /// True while our own pac is alive and the server says the last cell it entered had a dot or power pill.
        /// </summary>
        public bool IsOwnPacEating()
        {
            var ownPac = GetOwnPac();
            return ownPac.HasValue && ownPac.Value.Alive && ownPac.Value.Eating;
        }

        /// <summary>
        /// True while our own pac is alive and the last cell it entered had no dot or power pill.
        /// </summary>
        public bool IsOwnPacNotEating()
        {
            var ownPac = GetOwnPac();
            return ownPac.HasValue && ownPac.Value.Alive && !ownPac.Value.Eating;
        }

        /// <summary>
        /// True while our own pac is alive and has a power pill working.
        /// </summary>
        public bool IsOwnPacPowered()
        {
            var ownPac = GetOwnPac();
            return ownPac.HasValue && ownPac.Value.Alive && ownPac.Value.Powered;
        }

        /// <summary>
        /// The power up effect working on our own pac, None when it has none or is dead.
        /// </summary>
        public ePowerUpEffect GetOwnPowerUpEffect()
        {
            var ownPac = GetOwnPac();
            return ownPac.HasValue && ownPac.Value.Alive ? ownPac.Value.PowerUpEffect : ePowerUpEffect.None;
        }

        /// <summary>
        /// How full the player state panel's meter is, 0 to 1: the share of PowerUpDurationMs left on our own pac's effect.
        /// </summary>
        public float GetOwnPowerUpFill()
        {
            if (GetOwnPowerUpEffect() == ePowerUpEffect.None)
                return 0f;

            var duration = (GameConfig ?? new GameConfiguration()).PowerUpDurationMs;
            return PowerUpEffects.GetFill(GetOwnPac().Value.PowerUpMsLeft, duration);
        }

        /// <summary>
        /// How full the player state panel's hunger meter is, 0 to 1: how close our own pac is to starving
        /// (PacRules.GetHungerFill). 0 when it has no pac or is dead.
        /// </summary>
        public float GetOwnHungerFill()
        {
            var ownPac = GetOwnPac();
            if (!ownPac.HasValue || !ownPac.Value.Alive)
                return 0f;

            var msUntilStarvation = (GameConfig ?? new GameConfiguration()).MsUntilStarvation;
            return PacRules.GetHungerFill(ownPac.Value.MsSinceEaten, msUntilStarvation);
        }

        /// <summary>
        /// Seconds left before the next level starts, to the nearest second, so a 10 second wait shows 10 down to 0.
        /// </summary>
        public int GetIntermissionSecondsLeft(long nowMs)
        {
            var msLeft = Math.Max(0L, IntermissionEndMs - nowMs);
            return (int)((msLeft + MS_PER_SECOND / 2) / MS_PER_SECOND);
        }

        public string GetPlayerName(int playerId)
        {
            return _Players.TryGetValue(playerId, out var player) ? player.Name : string.Empty;
        }

        /// <summary>
        /// A width x height grid of dot colors: blueColor in the left half (x below width / 2), redColor in the right.
        /// Maps are always an even width, so every column belongs to one side.
        /// </summary>
        public static FastColor[,] BuildDotColors(int width, int height, FastColor blueColor, FastColor redColor)
        {
            var colors = new FastColor[width, height];
            var halfWidth = (int)(width * 0.5f);

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    colors[x, y] = x < halfWidth ? blueColor : redColor;

            return colors;
        }

        private void ApplyCellChanges(CellsChangedMessage cellsChanged)
        {
            if (Map == null)
                return;

            var width = Map.GetLength(0);
            var height = Map.GetLength(1);

            foreach (var cell in cellsChanged.Cells)
                if (cell.X < width && cell.Y < height)
                    Map[cell.X, cell.Y] = cell.Cell;
        }
    }
}
