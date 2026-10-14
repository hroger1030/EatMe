using EatMeLibrary;
using FastG;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EatMeServer
{
    /// <summary>
    /// The running game: who has joined, the current level, and what gets sent to whom.
    /// Everything here runs on the game loop thread.
    /// </summary>
    public class GameServer
    {
        /// <summary>
        /// Size a pac starts (and respawns) at.
        /// </summary>
        public const float PAC_START_SIZE = PacRules.START_SIZE;

        /// <summary>
        /// Positions are in map cells; this puts a pac in the middle of its cell.
        /// </summary>
        private const float CELL_CENTER = 0.5f;

        private const int MS_PER_SECOND = 1000;

        /// <summary>
        /// Bot player ids count up from here, well clear of the connection ids humans get.
        /// </summary>
        public const int BOT_ID_BASE = 1_000_000;

        /// <summary>
        /// A team whose score passes this percent of MaxScore is announced as about to win, once per level.
        /// </summary>
        public const int ABOUT_TO_WIN_PERCENT = 90;

        private readonly ServerSettings _Settings;
        private readonly List<string> _LoadingScreenTips;
        private readonly List<string> _BotNames;
        private readonly Dictionary<int, ServerPlayer> _Players = [];
        private readonly List<(int X, int Y)> _RedSpawns = [];
        private readonly List<(int X, int Y)> _BlueSpawns = [];
        private readonly List<(int X, int Y)> _GhostSpawns = [];
        private readonly List<GameFlag> _Flags = [];
        private readonly List<Ghost> _Ghosts = [];
        private readonly List<PowerUp> _PowerUps = [];
        private readonly List<(int X, int Y)> _PowerUpSpawns = [];
        private readonly List<(int X, int Y)> _PowerUpExits = [];

        // a full PathMap from each warp exit, made once per level, for power ups to step downhill on
        private readonly Dictionary<(int X, int Y), PathMap> _PowerUpExitPaths = [];
        private int _PowerUpTimerMs;
        private int _NextPowerUpId;
        private readonly List<CellRegen> _Regens = [];
        private readonly DotRegenQueue _EatenDots = new();
        private readonly List<CellChange> _CellChanges = [];
        private readonly List<Player> _Living = [];
        private Random _Random = new();

        // kept apart from _Random so joining players don't change what the ghosts do
        private readonly Random _BotRandom = new();

        // scratch space for PacBrain, reused by every bot
        private readonly PathMap _BotPaths = new();
        private readonly PathMap _BotThreatPaths = new();

        // a full PathMap from every flag base for each kind of bot: red, blue, and carrying a flag (PacBrain.GetBlocked)
        private readonly Dictionary<(int X, int Y), PathMap> _RedBasePaths = [];
        private readonly Dictionary<(int X, int Y), PathMap> _BlueBasePaths = [];
        private readonly Dictionary<(int X, int Y), PathMap> _CarrierBasePaths = [];
        // ms since the last snapshot, times TargetFps, so a snapshot is due at exactly MS_PER_SECOND with no rounding
        private int _SnapshotTimer;
        private int _PlayerCount;
        private int _BotCount;
        private int _NextBotId = BOT_ID_BASE;
        private int _DotCount;
        private int _LevelSeed;

        // the results of the level just won, kept to send to players joining while they're shown
        private LevelWonMessage _LevelWon;
        private int _IntermissionMs;
        private bool _RedAboutToWinSent;
        private bool _BlueAboutToWinSent;

        private readonly List<HighScoreEntry> _HighScores;
        private readonly Action<List<HighScoreEntry>> _OnHighScoresChanged;

        public LevelInfo CurrentLevel { get; private set; }
        public eGridDef[,] Map { get; private set; }
        public uint Tick { get; private set; }

        /// <summary>
        /// ms of game time since the server started, counted by Update while anyone is connected. Times ServerPings.
        /// </summary>
        public long ClockMs { get; private set; }
        public int RedScore { get; private set; }
        public int BlueScore { get; private set; }

        /// <summary>
        /// Humans who have joined, not counting bots. Safe to read from any thread.
        /// </summary>
        public int PlayerCount => Volatile.Read(ref _PlayerCount);

        public int BotCount => _BotCount;

        /// <summary>
        /// True while the results of a won level are shown. Nothing moves until the next level starts.
        /// </summary>
        public bool IsIntermission => _LevelWon != null;

        /// <summary>
        /// True while no humans are connected. Update does nothing until one joins.
        /// </summary>
        public bool IsPaused => _PlayerCount == 0;

        public IReadOnlyCollection<ServerPlayer> Players => _Players.Values;

        public IReadOnlyList<GameFlag> Flags => _Flags;

        public IReadOnlyList<Ghost> Ghosts => _Ghosts;

        /// <summary>
        /// Power ups on the board now, drifting toward their exits.
        /// </summary>
        public IReadOnlyList<PowerUp> PowerUps => _PowerUps;

        /// <summary>
        /// The level's power up spawn points (eGridDef.PowerUpSpawn).
        /// </summary>
        public IReadOnlyList<(int X, int Y)> PowerUpSpawns => _PowerUpSpawns;

        /// <summary>
        /// ms of play so far this level, for the power up spawn timer.
        /// </summary>
        public int PowerUpTimerMs => _PowerUpTimerMs;

        /// <summary>
        /// Power pills that have been eaten and are waiting to grow back.
        /// </summary>
        public IReadOnlyList<CellRegen> Regens => _Regens;

        /// <summary>
        /// Eaten dots, oldest first. Once it holds GameConfig.DotRegenPercent of the level's dots, each dot eaten puts
        /// the oldest back.
        /// </summary>
        public DotRegenQueue EatenDots => _EatenDots;

        /// <summary>
        /// The high score list, highest first. Humans' running totals are put on it whenever a level is won.
        /// </summary>
        public List<HighScoreEntry> HighScores => _HighScores;

        /// <summary>
        /// highScores is the list to keep up to date (a new empty one if null); onHighScoresChanged, if given, is called
        /// with it whenever a level's end changes it, so it can be saved.
        /// </summary>
        public GameServer(ServerSettings settings, List<string> loadingScreenTips, int level = 1, List<string> botNames = null,
            List<HighScoreEntry> highScores = null, Action<List<HighScoreEntry>> onHighScoresChanged = null)
        {
            _Settings = settings;
            _LoadingScreenTips = loadingScreenTips;
            _BotNames = botNames ?? [];
            _HighScores = highScores ?? [];
            _OnHighScoresChanged = onHighScoresChanged;
            StartLevel(level);
            BalanceBots();
        }

        /// <summary>
        /// Builds the level's map, sends every player back to base and tells all clients about the new level.
        /// </summary>
        public void StartLevel(int level)
        {
            CurrentLevel = Levels.GetLevel(level);
            var seed = Levels.GetSeed(_Settings.GameConfig.ServerSeed, CurrentLevel.Level);
            Map = MapMaker.Generate(_Settings.GameConfig, seed);
            _LevelSeed = seed;
            _DotCount = CountDots(Map);
            RedScore = 0;
            BlueScore = 0;
            _LevelWon = null;
            _IntermissionMs = 0;
            _RedAboutToWinSent = false;
            _BlueAboutToWinSent = false;

            // seeded by the level, so ghosts make the same choices given the same moves
            _Random = new Random(seed);
            _Regens.Clear();
            _EatenDots.Reset(_DotCount, _Settings.GameConfig.DotRegenPercent);
            _CellChanges.Clear();

            FindSpawns(Map, _RedSpawns, _BlueSpawns);
            FindGhostSpawns(Map, _GhostSpawns);
            CreateFlags(Map, _Flags);
            BuildFlagBasePaths();
            CreateGhosts();
            ResetPowerUps();

            foreach (var serverPlayer in _Players.Values)
            {
                serverPlayer.Player.Score = 0;
                serverPlayer.Memory.Reset();
                Respawn(serverPlayer.Player);
            }

            Broadcast(BuildLevelStart());
            Console.WriteLine($"Level {CurrentLevel.Level}: {CurrentLevel.Name} (seed {seed})");
        }

        public void HandleEvent(NetworkEvent networkEvent)
        {
            switch (networkEvent.Type)
            {
                case eNetworkEventType.Message:
                    HandleMessage(networkEvent.Connection, networkEvent.Message);
                    break;

                case eNetworkEventType.Disconnected:
                    RemovePlayer(networkEvent.Connection);
                    break;

                // nothing to do until the client says Hello
                case eNetworkEventType.Connected:
                    break;
            }
        }

        public void HandleMessage(IClientConnection connection, INetworkMessage message)
        {
            _Players.TryGetValue(connection.Id, out var serverPlayer);

            // a client must say Hello before anything else
            if (serverPlayer == null && message is not HelloMessage)
            {
                connection.Close();
                return;
            }

            switch (message)
            {
                case HelloMessage hello:
                    if (serverPlayer == null)
                        HandleHello(connection, hello);
                    break;

                case InputMessage input:
                    if (input.Sequence >= serverPlayer.LastInputSequence)
                    {
                        serverPlayer.LastInputSequence = input.Sequence;
                        serverPlayer.Input = input.Direction;
                        BufferTurn(serverPlayer, MazeMovement.ToFacing(input.Direction));
                    }
                    break;

                case PingMessage ping:
                    Send(connection, new PongMessage { ClientTime = ping.ClientTime });
                    break;

                case ServerPongMessage serverPong:
                    serverPlayer.Lag.AddSample((int)(ClockMs - serverPong.ServerTime));
                    break;

                case LeaveMessage:
                    RemovePlayer(connection);
                    connection.Close();
                    break;

                // anything else is a server-to-client message a client shouldn't send
                default:
                    connection.Close();
                    break;
            }
        }

        /// <summary>
        /// Advances the game by one tick and sends a snapshot when one is due. While the results of a won level
        /// are shown the game is paused, and only the countdown to the next level runs. With no humans connected
        /// nothing runs at all, the countdown included.
        /// </summary>
        public void Update(int elapsedMs)
        {
            if (IsPaused)
                return;

            Tick++;

            var deltaSeconds = (float)elapsedMs / MS_PER_SECOND;
            ClockMs += elapsedMs;
            UpdateLagPings(elapsedMs);

            if (IsIntermission)
            {
                UpdateIntermission(elapsedMs);
                return;
            }

            UpdateRespawns(elapsedMs);
            UpdateStuns(elapsedMs);
            SteerBots(deltaSeconds, elapsedMs);
            MovePacs(deltaSeconds, elapsedMs);
            UpdateEating(elapsedMs);
            UpdatePacFights();
            UpdateGhosts(deltaSeconds, elapsedMs);
            UpdatePowerUps(deltaSeconds, elapsedMs);
            UpdateFlags();
            UpdateRegens(elapsedMs);
            SendCellChanges();
            CheckAboutToWin();
            CheckLevelWon();

            _SnapshotTimer += elapsedMs * Math.Max(1, _Settings.TargetFps);

            if (_SnapshotTimer >= MS_PER_SECOND)
            {
                // drop any backlog rather than sending several snapshots in a row
                _SnapshotTimer = Math.Min(_SnapshotTimer - MS_PER_SECOND, MS_PER_SECOND);

                if (_PlayerCount > 0)
                    Broadcast(BuildSnapshot());
            }
        }

        public SnapshotMessage BuildSnapshot()
        {
            var snapshot = new SnapshotMessage { Tick = Tick, RedScore = RedScore, BlueScore = BlueScore };

            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;
                snapshot.Pacs.Add(new PacState(player.Id, player.Team, player.Location.X, player.Location.Y, player.Size, player.Facing, player.Alive, player.HasFlag, player.Eating,
                    player.FlagCaps, player.GhostKills, player.PlayerKills, player.FlagRescues, player.PowerUpEffect, player.PowerUpMs, player.TotalScore, player.MsSinceEaten, player.IsStunned));
            }

            foreach (var ghost in _Ghosts)
                snapshot.Ghosts.Add(new GhostState(ghost.Id, ghost.Location.X, ghost.Location.Y, ghost.Facing, ghost.Alive, ghost.IsStunned));

            foreach (var flag in _Flags)
                snapshot.Flags.Add(flag.ToState());

            foreach (var powerUp in _PowerUps)
                snapshot.PowerUps.Add(powerUp.ToState());

            return snapshot;
        }

        public DiscoveryResponseMessage BuildDiscoveryResponse()
        {
            return new DiscoveryResponseMessage
            {
                ServerName = _Settings.ServerName,
                Port = _Settings.Port,
                PlayerCount = PlayerCount,
                MaxPlayers = _Settings.GameConfig.MaxPlayers,
            };
        }

        /// <summary>
        /// Collects the red and blue spawn cells from a map.
        /// </summary>
        public static void FindSpawns(eGridDef[,] map, List<(int X, int Y)> redSpawns, List<(int X, int Y)> blueSpawns)
        {
            redSpawns.Clear();
            blueSpawns.Clear();

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y].HasFlag(eGridDef.SpawnPointRed))
                        redSpawns.Add((x, y));

                    if (map[x, y].HasFlag(eGridDef.SpawnPointBlue))
                        blueSpawns.Add((x, y));
                }
            }
        }

        /// <summary>
        /// Collects the ghost generator cells from a map.
        /// </summary>
        public static void FindGhostSpawns(eGridDef[,] map, List<(int X, int Y)> ghostSpawns)
        {
            ghostSpawns.Clear();

            for (var x = 0; x < map.GetLength(0); x++)
                for (var y = 0; y < map.GetLength(1); y++)
                    if (map[x, y].HasFlag(eGridDef.GhostGenerator))
                        ghostSpawns.Add((x, y));
        }

        /// <summary>
        /// The team that has won with these scores, or null if neither has reached maxScore.
        /// If both have, the higher score wins, and red wins a tie.
        /// </summary>
        public static eTeam? GetWinner(int redScore, int blueScore, int maxScore)
        {
            if (redScore < maxScore && blueScore < maxScore)
                return null;

            return blueScore > redScore ? eTeam.Blue : eTeam.Red;
        }

        /// <summary>
        /// True when a score has passed ABOUT_TO_WIN_PERCENT of maxScore.
        /// </summary>
        public static bool IsAboutToWin(int score, int maxScore)
        {
            return score * 100 > maxScore * ABOUT_TO_WIN_PERCENT;
        }

        /// <summary>
        /// Tells everyone when a team is about to win, once per team per level. Nothing is sent for a team that has
        /// already won, since the results follow straight away.
        /// </summary>
        public void CheckAboutToWin()
        {
            var maxScore = _Settings.GameConfig.MaxScore;

            if (IsIntermission || GetWinner(RedScore, BlueScore, maxScore).HasValue)
                return;

            if (!_RedAboutToWinSent && IsAboutToWin(RedScore, maxScore))
            {
                _RedAboutToWinSent = true;
                Broadcast(new GameEventMessage { EventType = eGameEvent.AboutToWin, Team = eTeam.Red });
            }

            if (!_BlueAboutToWinSent && IsAboutToWin(BlueScore, maxScore))
            {
                _BlueAboutToWinSent = true;
                Broadcast(new GameEventMessage { EventType = eGameEvent.AboutToWin, Team = eTeam.Blue });
            }
        }

        /// <summary>
        /// Counts down dead pacs and brings them back at base when their time is up, unless their team's spawn is locked
        /// (IsSpawnLocked); then they wait, and come back as soon as the lock is lifted.
        /// </summary>
        public void UpdateRespawns(int elapsedMs)
        {
            var redLocked = IsSpawnLocked(eTeam.Red);
            var blueLocked = IsSpawnLocked(eTeam.Blue);

            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;

                if (player.Alive)
                    continue;

                player.RespawnTimer -= elapsedMs;

                if (player.RespawnTimer <= 0 && !(player.Team == eTeam.Red ? redLocked : blueLocked))
                    Respawn(player);
            }
        }

        /// <summary>
        /// Counts down the stuns on pacs and ghosts (PowStun).
        /// </summary>
        public void UpdateStuns(int elapsedMs)
        {
            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;

                if (player.StunMs > 0)
                    player.StunMs = Math.Max(0, player.StunMs - elapsedMs);
            }

            foreach (var ghost in _Ghosts)
            {
                if (ghost.StunMs > 0)
                    ghost.StunMs = Math.Max(0, ghost.StunMs - elapsedMs);
            }
        }

        /// <summary>
        /// The POW block (instant): stuns every living pac (either team) and ghost within PowerUpEffects.POW_RADIUS
        /// cells of the pac that ate it, except that pac, for POW_STUN_MS. A stun already running starts over. The
        /// pac's own power up effect is left alone.
        /// </summary>
        public void PowStun(Player player)
        {
            foreach (var serverPlayer in _Players.Values)
            {
                var other = serverPlayer.Player;

                if (other != player && other.Alive && PowerUpEffects.IsInPowRange(player.Location, other.Location))
                    other.StunMs = PowerUpEffects.POW_STUN_MS;
            }

            foreach (var ghost in _Ghosts)
            {
                if (ghost.Alive && PowerUpEffects.IsInPowRange(player.Location, ghost.Location))
                    ghost.StunMs = PowerUpEffects.POW_STUN_MS;
            }
        }

        /// <summary>
        /// True while a living enemy pac has Lockdown (key) working, so the team's dead pacs can't respawn.
        /// </summary>
        public bool IsSpawnLocked(eTeam team)
        {
            foreach (var serverPlayer in _Players.Values)
            {
                if (PowerUpEffects.LocksSpawn(serverPlayer.Player, team))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Full PathMaps from every flag base, so bots can head for a base from anywhere (see BotWorld.FlagBasePaths).
        /// Each team keeps out of the other's spawn room, and carriers out of both, so there is a set for each.
        /// </summary>
        private void BuildFlagBasePaths()
        {
            BuildFlagBasePaths(_RedBasePaths, MazeMovement.GetPacBlocked(eTeam.Red));
            BuildFlagBasePaths(_BlueBasePaths, MazeMovement.GetPacBlocked(eTeam.Blue));
            BuildFlagBasePaths(_CarrierBasePaths, eGridDef.Wall | eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue);
        }

        private void BuildFlagBasePaths(Dictionary<(int X, int Y), PathMap> basePaths, eGridDef blocked)
        {
            basePaths.Clear();

            foreach (var flag in _Flags)
            {
                var cell = MazeMovement.GetCell(new Point2(flag.BaseX, flag.BaseY));

                if (basePaths.ContainsKey(cell))
                    continue;

                var paths = new PathMap();
                paths.Fill(Map, cell.X, cell.Y, int.MaxValue, blocked);
                basePaths[cell] = paths;
            }
        }

        private Dictionary<(int X, int Y), PathMap> GetBasePaths(Player bot)
        {
            if (bot.HasFlag)
                return _CarrierBasePaths;

            return bot.Team == eTeam.Blue ? _BlueBasePaths : _RedBasePaths;
        }

        /// <summary>
        /// Each living bot picks which way to go (PacBrain) when it reaches a cell centre, remembering its goal and
        /// what it fled from in ServerPlayer.Memory, and turns back at once if it is heading straight at something
        /// that can kill it (at most once every BotMemory.TURN_BACK_COOLDOWN_MS). A dead bot forgets everything.
        /// </summary>
        public void SteerBots(float deltaSeconds, int elapsedMs)
        {
            if (_BotCount == 0)
                return;

            var config = _Settings.GameConfig;
            var width = Map.GetLength(0);
            var height = Map.GetLength(1);

            CollectLiving();
            var world = new BotWorld { Map = Map, MaxSize = config.PacMaxSize, PerceptionSeed = _LevelSeed, Pacs = _Living, Ghosts = _Ghosts, Flags = _Flags, PowerUps = _PowerUps };

            foreach (var serverPlayer in _Players.Values)
            {
                var pac = serverPlayer.Player;
                var memory = serverPlayer.Memory;

                if (!serverPlayer.IsBot)
                    continue;

                if (!pac.Alive)
                {
                    memory.Reset();
                    continue;
                }

                memory.Update(elapsedMs);

                if (pac.IsStunned)
                    continue;

                var distance = PacRules.GetSpeed(config.PacSpeed, pac.Size, config.PacMaxSize) * PacRules.GetEffectSpeedScale(pac.PowerUpEffect) * deltaSeconds;
                world.FlagBasePaths = GetBasePaths(pac);

                if (MazeMovement.GetDistanceToCentre(pac.Location, pac.Facing) <= distance)
                {
                    var (cx, cy) = MazeMovement.GetNextCentreCell(pac.Location, pac.Facing, width, height);
                    serverPlayer.Input = MazeMovement.ToDirection(PacBrain.ChooseDirection(world, pac, cx, cy, _BotPaths, _BotThreatPaths, _BotRandom, memory));
                }
                else if (memory.CanTurnBack && PacBrain.ShouldTurnBack(world, pac))
                {
                    memory.TurnedBack();
                    serverPlayer.Input = MazeMovement.ToDirection(MazeMovement.GetOpposite(pac.Facing));
                }
            }
        }

        /// <summary>
        /// Moves every living pac at the speed for its size (PacRules.GetSpeed) times its effect's scale
        /// (PacRules.GetEffectSpeedScale), turning the way its player is holding when it can.
        /// The enemy spawn room counts as wall (MazeMovement.GetPacBlocked). A pac carrying a flag onto a spawn tile drops it
        /// in the cell it came from. A stunned pac (PowStun) doesn't move.
        /// </summary>
        public void MovePacs(float deltaSeconds, int elapsedMs)
        {
            var config = _Settings.GameConfig;

            foreach (var serverPlayer in _Players.Values)
            {
                var pac = serverPlayer.Player;

                if (!pac.Alive || pac.IsStunned)
                    continue;

                var from = MazeMovement.GetCell(pac.Location);
                var speed = PacRules.GetSpeed(config.PacSpeed, pac.Size, config.PacMaxSize) * PacRules.GetEffectSpeedScale(pac.PowerUpEffect);
                var distance = speed * deltaSeconds;
                var blocked = MazeMovement.GetPacBlocked(pac.Team);
                var wanted = MazeMovement.ToFacing(serverPlayer.Input);
                var facing = pac.Facing;

                // bots steer exactly; only humans get the turn buffer and late turns
                if (!serverPlayer.IsBot)
                {
                    wanted = TurnAssist.GetWantedTurn(wanted, serverPlayer.BufferedTurn, serverPlayer.BufferedTurnMs);

                    if (serverPlayer.HasNewTurnRequest && wanted.HasValue)
                    {
                        var grace = TurnAssist.GetGraceDistance(config.TurnGraceDistance, speed, serverPlayer.Lag.SmoothedMs, config.LagCompensationMaxMs);
                        distance += TurnAssist.TryLateTurn(Map, pac, wanted.Value, Math.Min(grace, serverPlayer.StraightDistance), blocked: blocked);
                    }

                    serverPlayer.HasNewTurnRequest = false;
                }

                MazeMovement.Move(Map, pac, wanted, distance, blocked: blocked);

                if (!serverPlayer.IsBot)
                    UpdateTurnBuffer(serverPlayer, facing, distance, elapsedMs);

                if (pac.HasFlag && IsSpawnCell(MazeMovement.GetCell(pac.Location)))
                    DropFlag(pac, from.X + CELL_CENTER, from.Y + CELL_CENTER);
            }
        }

        /// <summary>
        /// Remembers a direction a human just pressed (nothing for None) for GameConfig.TurnBufferTimeMs, and marks it
        /// as new so the next MovePacs may make it a late turn.
        /// </summary>
        public void BufferTurn(ServerPlayer serverPlayer, eFacing? pressed)
        {
            if (!pressed.HasValue)
                return;

            serverPlayer.BufferedTurn = pressed;
            serverPlayer.BufferedTurnMs = _Settings.GameConfig.TurnBufferTimeMs;
            serverPlayer.HasNewTurnRequest = true;
        }

        /// <summary>
        /// After a human's move: counts the cells moved straight (back to 0 on a change of direction), and drops the
        /// buffered turn once the pac faces that way or its time is up.
        /// </summary>
        private static void UpdateTurnBuffer(ServerPlayer serverPlayer, eFacing facingBefore, float distance, int elapsedMs)
        {
            var pac = serverPlayer.Player;
            serverPlayer.StraightDistance = pac.Facing == facingBefore ? serverPlayer.StraightDistance + distance : 0f;

            if (!serverPlayer.BufferedTurn.HasValue)
                return;

            serverPlayer.BufferedTurnMs -= elapsedMs;

            if (pac.Facing == serverPlayer.BufferedTurn || serverPlayer.BufferedTurnMs <= 0)
                serverPlayer.BufferedTurn = null;
        }

        /// <summary>
        /// Sends each human a ServerPing every LagEstimate.PING_INTERVAL_MS; their ServerPong updates ServerPlayer.Lag.
        /// </summary>
        public void UpdateLagPings(int elapsedMs)
        {
            foreach (var serverPlayer in _Players.Values)
            {
                if (serverPlayer.IsBot)
                    continue;

                serverPlayer.PingTimerMs += elapsedMs;

                if (serverPlayer.PingTimerMs < LagEstimate.PING_INTERVAL_MS)
                    continue;

                serverPlayer.PingTimerMs = 0;
                Send(serverPlayer.Connection, new ServerPingMessage { ServerTime = ClockMs });
            }
        }

        /// <summary>
        /// True if the cell is a spawn tile of either team. Flags are never allowed on one.
        /// </summary>
        public bool IsSpawnCell((int X, int Y) cell)
        {
            return (Map[cell.X, cell.Y] & (eGridDef.SpawnPointRed | eGridDef.SpawnPointBlue)) != 0;
        }

        /// <summary>
        /// Living pacs eat the dot or power pill in the cell they are in. A dot adds DotSizeGain (times the effect's
        /// PacRules.GetDotGainScale) to a pac's size, up to PacMaxSize. A power pill starts the PowerPill effect (StartPowerUpEffect); the effect's timer counts down here
        /// and EndPowerUpEffect runs when it is up, except for a permanent effect (PowerUpEffects.IsPermanent), whose timer stays full.
        /// A pac with an effect that PowerUpEffects.PreventsStarving never gets hungry. Otherwise a pac that hasn't eaten for MsUntilStarvation shrinks by PacShrinkPerSecond each second.
        /// </summary>
        public void UpdateEating(int elapsedMs)
        {
            var config = _Settings.GameConfig;

            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;

                if (!player.Alive)
                    continue;

                if (player.PowerUpEffect != ePowerUpEffect.None && !PowerUpEffects.IsPermanent(player.PowerUpEffect))
                {
                    player.PowerUpMs -= elapsedMs;

                    if (player.PowerUpMs <= 0)
                        EndPowerUpEffect(player);
                }

                var (x, y) = MazeMovement.GetCell(player.Location);
                var cell = Map[x, y];

                if (x != player.LastCellX || y != player.LastCellY)
                {
                    player.Eating = (cell & (eGridDef.Food | eGridDef.PowerPill)) != 0;
                    player.LastCellX = x;
                    player.LastCellY = y;
                }

                if (cell.HasFlag(eGridDef.Food))
                {
                    EatDot(x, y);
                    player.Size = PacRules.GrowFromDot(player.Size, config.DotSizeGain * PacRules.GetDotGainScale(player.PowerUpEffect), config.PacMaxSize);
                    player.DotsEaten++;
                    player.MsSinceEaten = 0;
                }
                else if (cell.HasFlag(eGridDef.PowerPill))
                {
                    EatPowerPill(x, y, config.PowerPillRegenTimeMs);
                    StartPowerUpEffect(player, ePowerUpEffect.PowerPill);
                    player.MsSinceEaten = 0;
                }
                else if (PowerUpEffects.PreventsStarving(player.PowerUpEffect))
                {
                    player.MsSinceEaten = 0;
                }
                else
                {
                    player.MsSinceEaten += elapsedMs;

                    if (player.MsSinceEaten >= config.MsUntilStarvation)
                        player.Size = PacRules.Starve(player.Size, config.PacShrinkPerSecond, (float)elapsedMs / MS_PER_SECOND);
                }
            }
        }

        /// <summary>
        /// Gives the pac a power up effect for PowerUpDurationMs. A pac has only one: a different effect ends the one it
        /// has first, and the same effect only restarts the timer. An effect with a size gain (PacRules.GetEffectSizeGain:
        /// power pill, Melon Belly) adds it (past PacMaxSize if need be) when it starts.
        /// </summary>
        public void StartPowerUpEffect(Player player, ePowerUpEffect effect)
        {
            var config = _Settings.GameConfig;

            if (player.PowerUpEffect != effect)
            {
                EndPowerUpEffect(player);

                player.Size = PacRules.GrowFromPowerPill(player.Size, PacRules.GetEffectSizeGain(effect, config.PowerPillSizeGain));
            }

            player.PowerUpEffect = effect;
            player.PowerUpMs = config.PowerUpDurationMs;
        }

        /// <summary>
        /// Ends the pac's power up effect, if it has one. An effect with a size gain takes it away again.
        /// </summary>
        public void EndPowerUpEffect(Player player)
        {
            var gain = PacRules.GetEffectSizeGain(player.PowerUpEffect, _Settings.GameConfig.PowerPillSizeGain);

            if (gain > 0f)
                player.Size = PacRules.WearOffPowerPill(player.Size, gain);

            player.PowerUpEffect = ePowerUpEffect.None;
            player.PowerUpMs = 0;
        }

        /// <summary>
        /// Clears the dot at (x, y) and queues it; once DotRegenPercent of the dots are eaten, this puts the oldest
        /// eaten dot back.
        /// </summary>
        private void EatDot(int x, int y)
        {
            Map[x, y] &= ~eGridDef.Food;
            _CellChanges.Add(new CellChange((ushort)x, (ushort)y, Map[x, y]));

            if (!_EatenDots.Add(x, y, out var restored))
                return;

            Map[restored.X, restored.Y] |= eGridDef.Food;
            _CellChanges.Add(new CellChange(restored.X, restored.Y, Map[restored.X, restored.Y]));
        }

        private void EatPowerPill(int x, int y, int regenMs)
        {
            Map[x, y] &= ~eGridDef.PowerPill;
            _Regens.Add(new CellRegen(x, y, eGridDef.PowerPill, regenMs));
            _CellChanges.Add(new CellChange((ushort)x, (ushort)y, Map[x, y]));
        }

        /// <summary>
        /// Counts down eaten power pills and puts them back when their time is up.
        /// </summary>
        public void UpdateRegens(int elapsedMs)
        {
            for (var i = _Regens.Count - 1; i >= 0; i--)
            {
                var regen = _Regens[i];
                regen.MsLeft -= elapsedMs;

                if (regen.MsLeft > 0)
                    continue;

                Map[regen.X, regen.Y] |= regen.Item;
                _CellChanges.Add(new CellChange((ushort)regen.X, (ushort)regen.Y, Map[regen.X, regen.Y]));
                _Regens.RemoveAt(i);
            }
        }

        private void SendCellChanges()
        {
            if (_CellChanges.Count == 0)
                return;

            Broadcast(new CellsChangedMessage { Cells = [.. _CellChanges] });
            _CellChanges.Clear();
        }

        /// <summary>
        /// Every cell that differs from the freshly generated map, for a player who joins part way through a level.
        /// </summary>
        public CellsChangedMessage BuildEatenCells()
        {
            var message = new CellsChangedMessage();

            foreach (var regen in _Regens)
                message.Cells.Add(new CellChange((ushort)regen.X, (ushort)regen.Y, Map[regen.X, regen.Y]));

            foreach (var eaten in _EatenDots)
                message.Cells.Add(new CellChange(eaten.X, eaten.Y, Map[eaten.X, eaten.Y]));

            return message;
        }

        /// <summary>
        /// Enemy pacs that touch: the bigger one eats the smaller (PlayerKill points), and equal sizes bounce
        /// off each other. Friendly pacs pass through each other.
        /// </summary>
        public void UpdatePacFights()
        {
            CollectLiving();

            for (var i = 0; i < _Living.Count; i++)
            {
                for (var j = i + 1; j < _Living.Count; j++)
                {
                    var a = _Living[i];
                    var b = _Living[j];

                    if (!a.Alive || !b.Alive || a.Team == b.Team || !PacRules.IsTouchingPac(a.Location, a.Size, b.Location, b.Size))
                        continue;

                    if (a.Size == b.Size)
                    {
                        Bounce(a, b);
                        Bounce(b, a);
                        continue;
                    }

                    var (eater, eaten) = a.Size > b.Size ? (a, b) : (b, a);
                    eater.PlayerKills++;
                    AddScore(eater, _Settings.GameConfig.PlayerKill);
                    KillPac(eaten, eGameEvent.PacEaten, eater.Id);
                    Console.WriteLine($"{eater.Name} ate {eaten.Name}");
                }
            }
        }

        private void CollectLiving()
        {
            _Living.Clear();

            foreach (var serverPlayer in _Players.Values)
            {
                if (serverPlayer.Player.Alive)
                    _Living.Add(serverPlayer.Player);
            }
        }

        /// <summary>
        /// Turns a pac round if it is heading toward the other one.
        /// </summary>
        private static void Bounce(Player pac, Player other)
        {
            if (MazeMovement.IsHeadingToward(pac.Location, pac.Facing, other.Location))
                pac.Facing = MazeMovement.GetOpposite(pac.Facing);
        }

        /// <summary>
        /// Kills a pac: drops any flag where it is, tells everyone, and starts its respawn timer.
        /// </summary>
        private void KillPac(Player player, eGameEvent cause, int actorId)
        {
            DropFlag(player);
            player.Alive = false;
            player.RespawnTimer = _Settings.GameConfig.PlayerRespawnTimeMs;
            Broadcast(new GameEventMessage { EventType = cause, ActorId = actorId, TargetId = player.Id, X = player.Location.X, Y = player.Location.Y });
        }

        /// <summary>
        /// Makes the level's ghosts, each with its personality from MapMaker.PickGhostLineup (named after it).
        /// </summary>
        private void CreateGhosts()
        {
            _Ghosts.Clear();

            if (_GhostSpawns.Count == 0)
                return;

            var lineup = MapMaker.PickGhostLineup(_Settings.GameConfig, _LevelSeed, CurrentLevel.Level);

            for (var i = 0; i < lineup.Length; i++)
            {
                var ghost = new Ghost
                {
                    Id = i,
                    Name = lineup[i].Name,
                    Team = eTeam.Computer,
                    Personality = lineup[i],
                    HomeCell = GhostBrain.FindHomeCorner(Map, i),
                    PatrolIndex = i,
                };

                SpawnGhost(ghost);
                _Ghosts.Add(ghost);
            }
        }

        /// <summary>
        /// Puts a ghost on a ghost generator, alive, with no target and its personality's state cleared. A grudge
        /// outlives the ghost.
        /// </summary>
        private void SpawnGhost(Ghost ghost)
        {
            var (x, y) = _GhostSpawns[ghost.Id % _GhostSpawns.Count];

            ghost.Location = new Point2(x + CELL_CENTER, y + CELL_CENTER);
            ghost.Facing = eFacing.Up;
            ghost.Size = PacRules.START_SIZE;
            ghost.Alive = true;
            ghost.RespawnTimer = 0;
            ghost.StunMs = 0;
            ghost.CurrentTarget = null;
            ghost.SpawnCell = (x, y);
            ghost.IsScattering = false;
            ghost.LifeMs = 0;
            ghost.ChaseMs = 0;
            ghost.RestMs = 0;
            ghost.LurkCell = null;
            ghost.TargetHistory.Clear();
        }

        /// <summary>
        /// Moves the ghosts and settles ghosts touching pacs. Each ghost goes by its personality (GhostBrain): it picks a
        /// target, and at each cell centre the way toward its goal; some stand still for a while. Every ghost runs from
        /// any pac within its vision big enough to kill it (the Scaredy from any pac at all), whatever else it is doing.
        /// A pac it touches dies, unless the pac is big enough to kill it (GhostKill points).
        /// Dead ghosts come back at a ghost generator after GhostRegenTimeMs.
        /// </summary>
        public void UpdateGhosts(float deltaSeconds, int elapsedMs)
        {
            var config = _Settings.GameConfig;
            var width = Map.GetLength(0);
            var height = Map.GetLength(1);

            CollectLiving();
            var bellScale = PowerUpEffects.GetGhostSpeedScale(_Living);

            foreach (var ghost in _Ghosts)
            {
                if (!ghost.Alive)
                {
                    ghost.RespawnTimer -= elapsedMs;

                    if (ghost.RespawnTimer <= 0)
                        SpawnGhost(ghost);

                    continue;
                }

                ghost.LifeMs += elapsedMs;

                // a stunned ghost stays put, but still kills a pac that runs into it
                if (ghost.IsStunned)
                {
                    ResolveGhostContacts(ghost);
                    continue;
                }

                var vision = config.GhostVisionDistance * ghost.Personality.VisionScale;
                var target = PickGhostTarget(ghost, vision, elapsedMs);

                if (ghost.Personality.Behavior == eGhostBehavior.SlowThinker)
                {
                    if (target != ghost.CurrentTarget || target == null)
                        ghost.TargetHistory.Clear();

                    if (target != null)
                        GhostBrain.RecordTargetCell(ghost.TargetHistory, ghost.LifeMs, MazeMovement.GetCell(target.Location));
                }

                ghost.CurrentTarget = target;

                var threat = ghost.Personality.Behavior == eGhostBehavior.Scaredy
                    ? GhostBrain.FindNearestPac(ghost.Location, _Living, vision)
                    : GhostBrain.FindThreat(ghost.Location, _Living, vision);
                var distance = config.GhostSpeed * GetGhostSpeedScale(ghost) * bellScale * deltaSeconds;
                eFacing? wanted = null;

                if (threat != null)
                {
                    // decided every tick, so a ghost heading toward a threat turns back at once
                    var (cx, cy) = MazeMovement.GetNextCentreCell(ghost.Location, ghost.Facing, width, height);
                    var (tx, ty) = MazeMovement.GetCell(threat.Location);
                    wanted = GhostBrain.ChooseFleeDirection(Map, cx, cy, tx, ty);
                }
                else if (IsGhostStill(ghost, (int)MathF.Ceiling(vision)))
                {
                    ResolveGhostContacts(ghost);
                    continue;
                }
                // otherwise only decide on ticks that reach a cell centre, so a wandering ghost doesn't turn back early
                else if (MazeMovement.GetDistanceToCentre(ghost.Location, ghost.Facing) <= distance)
                {
                    var (cx, cy) = MazeMovement.GetNextCentreCell(ghost.Location, ghost.Facing, width, height);

                    // the Lurker stops dead on its junction
                    if (ghost.CurrentTarget == null && ghost.LurkCell == (cx, cy))
                        distance = MazeMovement.GetDistanceToCentre(ghost.Location, ghost.Facing);

                    wanted = ChooseGhostDirection(ghost, cx, cy, vision);
                }

                MazeMovement.Move(Map, ghost, wanted, distance, canWarp: false, blocked: MazeMovement.GHOST_BLOCKED);
                ResolveGhostContacts(ghost);
            }
        }

        /// <summary>
        /// The pac a ghost goes after this tick, by its personality. Never a pac with a power pill working.
        /// </summary>
        private Player PickGhostTarget(Ghost ghost, float vision, int elapsedMs)
        {
            var location = ghost.Location;

            switch (ghost.Personality.Behavior)
            {
                case eGhostBehavior.Hunter:
                    return GhostBrain.PickStubbornTarget(location, ghost.CurrentTarget, _Living, vision);

                case eGhostBehavior.FlagHound:
                    return GhostBrain.PickFlagHoundTarget(location, _Living, vision);

                case eGhostBehavior.Guard:
                    var guarded = GhostBrain.PickTarget(location, ghost.CurrentTarget, _Living, vision);
                    return guarded != null && GhostBrain.IsOutsideLeash(MazeMovement.GetCell(guarded.Location), ghost.SpawnCell, GhostBrain.LEASH_CELLS) ? null : guarded;

                case eGhostBehavior.Sprinter:
                    ghost.CurrentTarget = GhostBrain.PickTarget(location, ghost.CurrentTarget, _Living, vision);
                    return GhostBrain.UpdateAttention(ghost, elapsedMs) ? ghost.CurrentTarget : null;

                case eGhostBehavior.Lurker:
                    var prey = GhostBrain.PickTarget(location, ghost.CurrentTarget, _Living, vision);

                    if (prey != null)
                        ghost.LurkCell = null;
                    else if (ghost.LurkCell == null)
                    {
                        var (x, y) = MazeMovement.GetNextCentreCell(location, ghost.Facing, Map.GetLength(0), Map.GetLength(1));
                        ghost.LurkCell = GhostBrain.FindLurkJunction(Map, x, y, GhostBrain.LURK_MIN_STEPS) ?? (x, y);
                    }

                    return prey;

                case eGhostBehavior.Bully:
                    return GhostBrain.PickSmallest(location, _Living, vision);

                case eGhostBehavior.Glutton:
                    return GhostBrain.PickBiggest(location, _Living, vision);

                case eGhostBehavior.Grudge:
                    return GhostBrain.PickGrudgeTarget(location, ghost.CurrentTarget, _Living, vision, ghost.GrudgeId);

                // picks a new one at junctions (ChooseGhostDirection)
                case eGhostBehavior.Confused:
                    return GhostBrain.IsChaseable(ghost.CurrentTarget, _Living) ? ghost.CurrentTarget : null;

                case eGhostBehavior.Scaredy:
                    return null;

                case eGhostBehavior.Sidekick:
                    var leader = GhostBrain.FindLeader(ghost, _Ghosts);

                    if (leader == null)
                        return GhostBrain.PickTarget(location, ghost.CurrentTarget, _Living, vision);

                    return GhostBrain.IsChaseable(leader.CurrentTarget, _Living) ? leader.CurrentTarget : null;

                default:
                    return GhostBrain.PickTarget(location, ghost.CurrentTarget, _Living, vision);
            }
        }

        /// <summary>
        /// True if the ghost stands still this tick: the Sleeper napping, the Shy while its target faces it, the Lurker
        /// waiting on its junction.
        /// </summary>
        private bool IsGhostStill(Ghost ghost, int visionSteps)
        {
            switch (ghost.Personality.Behavior)
            {
                case eGhostBehavior.Sleeper:
                    return GhostBrain.IsNapping(ghost.LifeMs);

                case eGhostBehavior.Shy:
                    if (ghost.CurrentTarget == null)
                        return false;

                    var (px, py) = MazeMovement.GetCell(ghost.CurrentTarget.Location);
                    var (gx, gy) = MazeMovement.GetCell(ghost.Location);
                    return GhostBrain.IsFacedBy(Map, px, py, ghost.CurrentTarget.Facing, gx, gy, visionSteps);

                case eGhostBehavior.Lurker:
                    return ghost.CurrentTarget == null
                        && ghost.LurkCell.HasValue
                        && MazeMovement.GetDistanceToCentre(ghost.Location, ghost.Facing) == 0f
                        && MazeMovement.GetCell(ghost.Location) == ghost.LurkCell.Value;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Which way a ghost reaching the centre of cell (cx, cy) goes, by its personality.
        /// </summary>
        private eFacing? ChooseGhostDirection(Ghost ghost, int cx, int cy, float vision)
        {
            var target = ghost.CurrentTarget;
            (int X, int Y)? goal = target == null ? null : MazeMovement.GetCell(target.Location);

            switch (ghost.Personality.Behavior)
            {
                case eGhostBehavior.Ambusher:
                    if (target != null)
                    {
                        var ambush = GhostBrain.GetAmbushCell(Map, goal.Value.X, goal.Value.Y, target.Facing, GhostBrain.AMBUSH_STEPS);
                        if (ambush != (cx, cy))
                            goal = ambush;
                    }
                    break;

                case eGhostBehavior.Ditherer:
                    var targetDistance = target == null ? float.MaxValue : GhostBrain.GetDistanceSquared(ghost.Location, target.Location);
                    ghost.IsScattering = GhostBrain.ShouldScatter(ghost.IsScattering, targetDistance, (cx, cy) == ghost.HomeCell);

                    if (ghost.IsScattering)
                        goal = ghost.HomeCell;
                    break;

                case eGhostBehavior.FlagHound:
                    if (target == null && _Flags.Count > 0)
                    {
                        var flag = _Flags[ghost.PatrolIndex % _Flags.Count];
                        var flagBase = MazeMovement.GetCell(new Point2(flag.BaseX, flag.BaseY));

                        if (flagBase == (cx, cy))
                        {
                            ghost.PatrolIndex++;
                            flag = _Flags[ghost.PatrolIndex % _Flags.Count];
                            flagBase = MazeMovement.GetCell(new Point2(flag.BaseX, flag.BaseY));
                        }

                        goal = flagBase;
                    }
                    break;

                case eGhostBehavior.Guard:
                    if (target == null && GhostBrain.IsOutsideLeash((cx, cy), ghost.SpawnCell, GhostBrain.LEASH_CELLS))
                        goal = ghost.SpawnCell;
                    break;

                case eGhostBehavior.Lurker:
                    if (target == null)
                        goal = ghost.LurkCell;
                    break;

                case eGhostBehavior.SlowThinker:
                    goal = target == null ? null : GhostBrain.GetLaggedCell(ghost.TargetHistory, ghost.LifeMs, GhostBrain.LAG_MS);
                    break;

                case eGhostBehavior.StraightLiner:
                    return GhostBrain.ChooseStraightLineDirection(Map, cx, cy, ghost.Facing, goal, _Random);

                case eGhostBehavior.Copycat:
                    if (target != null)
                        return GhostBrain.GetMimicDirection(Map, cx, cy, ghost.Facing, target.Facing, _Random);
                    break;

                case eGhostBehavior.Confused:
                    if (GhostBrain.IsJunction(Map, cx, cy) || target == null)
                    {
                        ghost.CurrentTarget = GhostBrain.PickRandomTarget(ghost.Location, _Living, vision, _Random);
                        goal = ghost.CurrentTarget == null ? null : MazeMovement.GetCell(ghost.CurrentTarget.Location);
                    }
                    break;

                case eGhostBehavior.Sidekick:
                    var leader = target == null ? GhostBrain.FindLeader(ghost, _Ghosts) : null;

                    if (leader != null && GhostBrain.GetDistanceSquared(ghost.Location, leader.Location) > GhostBrain.FOLLOW_DISTANCE * GhostBrain.FOLLOW_DISTANCE)
                        goal = MazeMovement.GetCell(leader.Location);
                    break;
            }

            return GhostBrain.ChooseDirection(Map, cx, cy, ghost.Facing, goal, _Random);
        }

        /// <summary>
        /// Times GhostSpeed for this ghost: its personality's scale, the Elroy's frenzy as the leading team nears MaxScore,
        /// and the Lurker's charge while it chases.
        /// </summary>
        private float GetGhostSpeedScale(Ghost ghost)
        {
            var scale = ghost.Personality.SpeedScale;

            if (ghost.Personality.Behavior == eGhostBehavior.Elroy)
                scale *= GhostBrain.GetFrenzySpeedScale(Math.Max(RedScore, BlueScore), _Settings.GameConfig.MaxScore);
            else if (ghost.Personality.Behavior == eGhostBehavior.Lurker && ghost.CurrentTarget != null)
                scale *= GhostBrain.LURK_CHARGE_SCALE;

            return scale;
        }

        private void ResolveGhostContacts(Ghost ghost)
        {
            var config = _Settings.GameConfig;
            var ghostRadius = PacRules.GetGhostRadius(CurrentLevel.IsRainbow);

            foreach (var player in _Living)
            {
                if (!player.Alive || !PacRules.IsTouchingGhost(player.Location, player.Size, ghost.Location, ghostRadius))
                    continue;

                if (PacRules.CanKillGhost(player.Size, player.IsPowered, config.PacMaxSize))
                {
                    ghost.Alive = false;
                    ghost.RespawnTimer = config.GhostRegenTimeMs;
                    ghost.CurrentTarget = null;
                    ghost.GrudgeId = player.Id;
                    player.GhostKills++;
                    AddScore(player, config.GhostKill);
                    Broadcast(new GameEventMessage { EventType = eGameEvent.GhostKilled, ActorId = player.Id, TargetId = ghost.Id, X = ghost.Location.X, Y = ghost.Location.Y });
                    Console.WriteLine($"{player.Name} killed {ghost.Name}");
                    return;
                }

                ghost.PlayerKills++;

                if (player.Id == ghost.GrudgeId)
                    ghost.GrudgeId = Ghost.NO_GRUDGE;

                KillPac(player, eGameEvent.PacEaten, GameEventMessage.NO_ID);
                Console.WriteLine($"{ghost.Name} got {player.Name}");
            }
        }

        /// <summary>
        /// Clears the power ups and restarts the spawn timer, and finds the new map's spawn points and warp exits, with a
        /// full PathMap from each exit.
        /// </summary>
        private void ResetPowerUps()
        {
            _PowerUps.Clear();
            _PowerUpTimerMs = 0;
            _PowerUpSpawns.Clear();
            _PowerUpSpawns.AddRange(PowerUpRules.FindSpawns(Map));
            _PowerUpExits.Clear();
            _PowerUpExits.AddRange(PowerUpRules.FindExits(Map));
            _PowerUpExitPaths.Clear();

            foreach (var exit in _PowerUpExits)
            {
                var paths = new PathMap();
                paths.Fill(Map, exit.X, exit.Y, blocked: PowerUpRules.BLOCKED);
                _PowerUpExitPaths[exit] = paths;
            }
        }

        /// <summary>
        /// Runs the power ups: one appears each time the level's play time passes a multiple of PowerUpSpawnTimerMs, each
        /// drifts toward its exit at PowerUpRules.SPEED and leaves on reaching it, and any living pac touching one eats it.
        /// Ghosts ignore them.
        /// </summary>
        public void UpdatePowerUps(float deltaSeconds, int elapsedMs)
        {
            var before = _PowerUpTimerMs;
            _PowerUpTimerMs += elapsedMs;

            var due = PowerUpRules.GetSpawnsDue(before, _PowerUpTimerMs, _Settings.GameConfig.PowerUpSpawnTimerMs);
            for (var i = 0; i < due; i++)
                SpawnPowerUp();

            var distance = PowerUpRules.SPEED * deltaSeconds;

            for (var i = _PowerUps.Count - 1; i >= 0; i--)
            {
                var powerUp = _PowerUps[i];

                if (PowerUpRules.Move(Map, powerUp, _PowerUpExitPaths[powerUp.Exit], distance))
                {
                    _PowerUps.RemoveAt(i);
                    continue;
                }

                foreach (var serverPlayer in _Players.Values)
                {
                    var player = serverPlayer.Player;

                    if (player.Alive && PowerUpRules.IsTouching(player.Location, player.Size, powerUp.Location))
                    {
                        _PowerUps.RemoveAt(i);
                        EatPowerUp(player, powerUp);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Puts a power up on a random spawn point, heading for a random warp exit, holding a random item from the level's
        /// set. Does nothing if the map has no spawn points or exits, or the level has no items. Returns it, or null.
        /// </summary>
        public PowerUp SpawnPowerUp()
        {
            if (_PowerUpSpawns.Count == 0 || _PowerUpExits.Count == 0)
                return null;

            var item = PowerUpRules.PickItem(CurrentLevel.PowerUps, _Random);
            if (item == ePowerUp.None)
                return null;

            var (x, y) = _PowerUpSpawns[_Random.Next(_PowerUpSpawns.Count)];
            var powerUp = new PowerUp
            {
                Id = _NextPowerUpId++,
                Item = item,
                Exit = _PowerUpExits[_Random.Next(_PowerUpExits.Count)],
                Location = new Point2(x + CELL_CENTER, y + CELL_CENTER),
                Facing = eFacing.Up,
                Alive = true,
            };

            _PowerUps.Add(powerUp);
            return powerUp;
        }

        private void EatPowerUp(Player player, PowerUp powerUp)
        {
            Broadcast(new GameEventMessage
            {
                EventType = eGameEvent.PowerUpEaten,
                ActorId = player.Id,
                TargetId = powerUp.Id,
                X = powerUp.Location.X,
                Y = powerUp.Location.Y,
                Team = eTeam.Computer,
            });

            ApplyPowerUp(player, powerUp.Item);
            Console.WriteLine($"{player.Name} ate a {powerUp.Item}");
        }

        /// <summary>
        /// What eating a power up does to the pac that ate it, by item. An item with an effect starts it with
        /// StartPowerUpEffect, which replaces any effect the pac has; items without one do nothing yet.
        /// </summary>
        public void ApplyPowerUp(Player player, ePowerUp item)
        {
            switch (item)
            {
                case ePowerUp.Cherry:
                    StartPowerUpEffect(player, ePowerUpEffect.SugarRush);
                    break;

                case ePowerUp.Strawberry:
                    StartPowerUpEffect(player, ePowerUpEffect.SweetTooth);
                    break;

                case ePowerUp.Orange:
                    StartPowerUpEffect(player, ePowerUpEffect.VitaminC);
                    break;

                case ePowerUp.Apple:
                    StartPowerUpEffect(player, ePowerUpEffect.DoctorAway);
                    break;

                case ePowerUp.Melon:
                    StartPowerUpEffect(player, ePowerUpEffect.MelonBelly);
                    break;

                case ePowerUp.Galaxian:
                    StartPowerUpEffect(player, ePowerUpEffect.RocketScience);
                    break;

                case ePowerUp.Bell:
                    StartPowerUpEffect(player, ePowerUpEffect.DinnerBell);
                    break;

                case ePowerUp.Key:
                    StartPowerUpEffect(player, ePowerUpEffect.Lockdown);
                    break;

                case ePowerUp.PowBlock:
                    PowStun(player);
                    break;
            }
        }

        /// <summary>
        /// When a team reaches MaxScore, tells everyone who won, with every player's score, and starts the
        /// intermission: the game pauses for IntermissionTimeMs, then the next level starts. Returns true if it did.
        /// </summary>
        public bool CheckLevelWon()
        {
            if (IsIntermission)
                return false;

            var winner = GetWinner(RedScore, BlueScore, _Settings.GameConfig.MaxScore);

            if (!winner.HasValue)
                return false;

            Console.WriteLine($"{winner.Value} wins level {CurrentLevel.Level} ({RedScore} to {BlueScore})");
            UpdateHighScores(DateTime.Now);

            var intermissionTimeMs = _Settings.GameConfig.IntermissionTimeMs;

            if (intermissionTimeMs <= 0)
            {
                Broadcast(BuildLevelWon(winner.Value, 0));
                StartLevel(CurrentLevel.Level + 1);
                return true;
            }

            _IntermissionMs = intermissionTimeMs;
            _LevelWon = BuildLevelWon(winner.Value, intermissionTimeMs);
            Broadcast(_LevelWon);
            return true;
        }

        /// <summary>
        /// Counts down the results screen and starts the next level when it runs out.
        /// </summary>
        public void UpdateIntermission(int elapsedMs)
        {
            if (!IsIntermission)
                return;

            _IntermissionMs -= elapsedMs;

            if (_IntermissionMs <= 0)
                StartLevel(CurrentLevel.Level + 1);
        }

        /// <summary>
        /// Puts every human's running total with any points on the high score list (bots are left off), one entry per
        /// connection: a player already on it has their entry updated rather than a second one added. Calls
        /// onHighScoresChanged if the list changed. Returns true if it did.
        /// </summary>
        public bool UpdateHighScores(DateTime now)
        {
            var changed = false;

            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;

                if (serverPlayer.IsBot || player.TotalScore <= 0)
                    continue;

                var entry = serverPlayer.HighScore;

                if (entry != null && entry.Score == player.TotalScore && _HighScores.Contains(entry))
                    continue;

                entry ??= new HighScoreEntry();
                entry.Name = player.Name;
                entry.Score = player.TotalScore;
                entry.Date = HighScoreManager.GetDate(now);
                serverPlayer.HighScore = entry;

                if (HighScoreRules.Submit(_HighScores, entry))
                    changed = true;
            }

            if (changed)
            {
                _OnHighScoresChanged?.Invoke(_HighScores);
                Broadcast(BuildHighScores());
            }

            return changed;
        }

        /// <summary>
        /// The high score list as it stands, copied so later changes don't touch a message still being sent.
        /// </summary>
        public HighScoresMessage BuildHighScores()
        {
            var message = new HighScoresMessage();
            foreach (var entry in _HighScores)
                message.Scores.Add(new HighScoreEntry { Name = entry.Name, Score = entry.Score, Date = entry.Date });
            return message;
        }

        /// <summary>
        /// Who won, the team scores and every player's score for the level.
        /// </summary>
        private LevelWonMessage BuildLevelWon(eTeam winner, int msRemaining)
        {
            var levelWon = new LevelWonMessage { WinningTeam = winner, RedScore = RedScore, BlueScore = BlueScore, MsRemaining = msRemaining };

            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;
                levelWon.Results.Add(new PlayerResult(player.Id, player.Name, player.Team, player.Score));
            }

            return levelWon;
        }

        /// <summary>
        /// Makes one flag, sitting at its base, for every flag base cell in a map.
        /// </summary>
        public static void CreateFlags(eGridDef[,] map, List<GameFlag> flags)
        {
            flags.Clear();

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y].HasFlag(eGridDef.FlagBaseRed))
                        flags.Add(new GameFlag(flags.Count, eTeam.Red, x + CELL_CENTER, y + CELL_CENTER));

                    if (map[x, y].HasFlag(eGridDef.FlagBaseBlue))
                        flags.Add(new GameFlag(flags.Count, eTeam.Blue, x + CELL_CENTER, y + CELL_CENTER));
                }
            }
        }

        /// <summary>
        /// Runs the flag rules for one tick. Carried flags follow their carrier. A living pac touching a loose flag:
        /// an enemy flag is picked up if the pac isn't already carrying one; its own flag away from base is
        /// returned (a rescue, FlagRescue points); its own flag at base while carrying an enemy flag is a
        /// capture (FlagScore points), which sends the enemy flag home. A pac on a spawn tile can't pick a flag up.
        /// </summary>
        public void UpdateFlags()
        {
            foreach (var flag in _Flags)
            {
                if (!flag.IsCarried || !_Players.TryGetValue(flag.CarrierId, out var carrier))
                    continue;

                flag.X = carrier.Player.Location.X;
                flag.Y = carrier.Player.Location.Y;
            }

            foreach (var serverPlayer in _Players.Values)
            {
                var player = serverPlayer.Player;

                if (!player.Alive)
                    continue;

                foreach (var flag in _Flags)
                {
                    if (flag.IsCarried || !PacRules.IsTouchingFlag(player.Location, player.Size, flag.X, flag.Y))
                        continue;

                    if (flag.Team != player.Team)
                    {
                        if (!IsSpawnCell(MazeMovement.GetCell(player.Location)))
                            PickUpFlag(player, flag);
                    }
                    else if (!flag.IsAtBase)
                        RescueFlag(player, flag);
                    else if (player.HasFlag)
                        CaptureFlag(player);
                }
            }
        }

        private void PickUpFlag(Player player, GameFlag flag)
        {
            if (player.HasFlag)
                return;

            flag.CarrierId = player.Id;
            flag.X = player.Location.X;
            flag.Y = player.Location.Y;
            player.HasFlag = true;
        }

        private void RescueFlag(Player player, GameFlag flag)
        {
            flag.ReturnToBase();
            player.FlagRescues++;
            AddScore(player, _Settings.GameConfig.FlagRescue);
            Broadcast(new GameEventMessage { EventType = eGameEvent.FlagReturned, ActorId = player.Id, X = flag.X, Y = flag.Y, Team = flag.Team });
            Console.WriteLine($"{player.Name} returned the {flag.Team} flag");
        }

        /// <summary>
        /// The player brings the enemy flag they carry to their own flag at base.
        /// </summary>
        private void CaptureFlag(Player player)
        {
            // the flag carried is the enemy's, so the capture is announced as that team's flag
            var capturedTeam = player.Team == eTeam.Red ? eTeam.Blue : eTeam.Red;

            foreach (var flag in _Flags)
            {
                if (flag.CarrierId != player.Id)
                    continue;

                capturedTeam = flag.Team;
                flag.ReturnToBase();
            }

            player.HasFlag = false;
            player.FlagCaps++;
            AddScore(player, _Settings.GameConfig.FlagScore);
            Broadcast(new GameEventMessage { EventType = eGameEvent.FlagCaptured, ActorId = player.Id, X = player.Location.X, Y = player.Location.Y, Team = capturedTeam });
            Console.WriteLine($"{player.Name} captured a flag for {player.Team}");
        }

        /// <summary>
        /// Gives points to the player and to their team, multiplied by the player's effect's PowerUpEffects.GetScoreScale.
        /// </summary>
        private void AddScore(Player player, int points)
        {
            points *= PowerUpEffects.GetScoreScale(player.PowerUpEffect);

            player.Score += points;
            player.TotalScore += points;

            if (player.Team == eTeam.Red)
                RedScore += points;
            else if (player.Team == eTeam.Blue)
                BlueScore += points;
        }

        /// <summary>
        /// Lets go of any flag the player is carrying, leaving it where they are. Call when a carrier dies or leaves.
        /// </summary>
        public void DropFlag(Player player)
        {
            DropFlag(player, player.Location.X, player.Location.Y);
        }

        /// <summary>
        /// Lets go of any flag the player is carrying, leaving it at (x, y), and tells everyone the flag was lost.
        /// </summary>
        public void DropFlag(Player player, float x, float y)
        {
            foreach (var flag in _Flags)
            {
                if (flag.CarrierId != player.Id)
                    continue;

                flag.CarrierId = FlagState.NO_CARRIER;
                flag.X = x;
                flag.Y = y;
                Broadcast(new GameEventMessage { EventType = eGameEvent.FlagLost, ActorId = player.Id, X = x, Y = y, Team = flag.Team });
            }

            player.HasFlag = false;
        }

        private void HandleHello(IClientConnection connection, HelloMessage hello)
        {
            if (hello.ClientVersion != NetworkConstants.APP_VERSION)
            {
                Reject(connection, eRejectReason.VersionMismatch);
                return;
            }

            if (!PlayerNameRules.IsValid(hello.PlayerName))
            {
                Reject(connection, eRejectReason.InvalidName);
                return;
            }

            var config = _Settings.GameConfig;

            // only humans count; a joining human takes a bot's place
            if (_PlayerCount >= config.MaxPlayers)
            {
                Reject(connection, eRejectReason.ServerFull);
                return;
            }

            if (_Settings.AutoBalanceTeams)
            {
                // the human takes the place of a bot on their preferred team, and BalanceBots evens the teams up after
                CountTeams(out _, out _, out var preferredRedBots, out var preferredBlueBots);

                if ((hello.PreferredTeam == eTeamPreference.Red && preferredRedBots > 0) || (hello.PreferredTeam == eTeamPreference.Blue && preferredBlueBots > 0))
                    RemoveBot(hello.PreferredTeam);
            }
            else if (_BotCount > BotPlanner.GetBotCount(_PlayerCount + 1, config.MinPlayers, config.MaxPlayers))
                RemoveBot(hello.PreferredTeam);

            CountTeams(out var redCount, out var blueCount, out _, out _);

            var player = new Player
            {
                Id = connection.Id,
                Name = hello.PlayerName,
                Team = TeamBalancer.AssignTeam(hello.PreferredTeam, redCount, blueCount),
            };
            Respawn(player);

            Send(connection, new WelcomeMessage
            {
                PlayerId = player.Id,
                Team = player.Team,
                ServerName = _Settings.ServerName,
                MessageOfTheDay = ConfigManager.GetMessageOfTheDay(_Settings.MessageOfTheDay),
                LoadingScreenTips = _LoadingScreenTips,
                GameConfig = _Settings.GameConfig,
            });
            Send(connection, BuildHighScores());
            Send(connection, BuildLevelStart());

            // the map they build from the seed still has every dot, so tell them which have been eaten
            if (_Regens.Count > 0 || _EatenDots.Count > 0)
                Send(connection, BuildEatenCells());

            // tell the new player who's already here
            foreach (var other in _Players.Values)
                Send(connection, BuildPlayerJoined(other.Player));

            var wasPaused = IsPaused;
            _Players[connection.Id] = new ServerPlayer(connection, player) { PreferredTeam = hello.PreferredTeam };
            Volatile.Write(ref _PlayerCount, _Players.Count - _BotCount);

            if (wasPaused)
                Console.WriteLine("Game resumed");

            // and tell everyone, the new player included, about the new player
            Broadcast(BuildPlayerJoined(player));

            // joining while the results are shown: show them too, with the time that's left
            if (IsIntermission)
            {
                _LevelWon.MsRemaining = _IntermissionMs;
                Send(connection, _LevelWon);
            }

            Console.WriteLine($"{player.Name} joined the {player.Team} team ({_PlayerCount} players, {_BotCount} bots)");
            BalanceBots();
        }

        private void Reject(IClientConnection connection, eRejectReason reason)
        {
            Console.WriteLine($"Client {connection.Id} rejected: {reason}");
            Send(connection, new RejectMessage { Reason = reason });
            connection.CloseAfterSending();
        }

        private void RemovePlayer(IClientConnection connection)
        {
            if (!_Players.TryGetValue(connection.Id, out var serverPlayer))
                return;

            Remove(serverPlayer);
            BalanceBots();
        }

        /// <summary>
        /// Takes a human or bot out of the game and tells everyone they left.
        /// </summary>
        private void Remove(ServerPlayer serverPlayer)
        {
            var player = serverPlayer.Player;

            _Players.Remove(player.Id);
            DropFlag(player);

            if (serverPlayer.IsBot)
                _BotCount--;

            Volatile.Write(ref _PlayerCount, _Players.Count - _BotCount);
            Broadcast(new PlayerLeftMessage { PlayerId = player.Id });
            Console.WriteLine($"{player.Name} left ({_PlayerCount} players, {_BotCount} bots)");

            if (!serverPlayer.IsBot && IsPaused)
                Console.WriteLine("Game paused: no players connected");
        }

        /// <summary>
        /// Adds or removes bots so humans plus bots make MinPlayers, never more than MaxPlayers (see BotPlanner). With
        /// AutoBalanceTeams on, each team also gets as many bots as it takes to be the same size as the other.
        /// </summary>
        public void BalanceBots()
        {
            var config = _Settings.GameConfig;

            if (_Settings.AutoBalanceTeams)
            {
                BalanceTeams();
            }
            else
            {
                var wanted = BotPlanner.GetBotCount(_PlayerCount, config.MinPlayers, config.MaxPlayers);

                while (_BotCount < wanted)
                    AddBot();

                while (_BotCount > wanted)
                    RemoveBot(eTeamPreference.Either);
            }
        }

        /// <summary>
        /// Number of cells in the map holding a dot.
        /// </summary>
        public static int CountDots(eGridDef[,] map)
        {
            var count = 0;

            foreach (var cell in map)
                if (cell.HasFlag(eGridDef.Food))
                    count++;

            return count;
        }

        /// <summary>
        /// Gives each team the bots BotPlanner.GetBalancedBotCounts asks for, removing before adding.
        /// </summary>
        private void BalanceTeams()
        {
            var config = _Settings.GameConfig;
            CountTeams(out var redCount, out var blueCount, out var redBots, out var blueBots);
            BotPlanner.GetBalancedBotCounts(redCount - redBots, blueCount - blueBots, config.MinPlayers, config.MaxPlayers,
                out var redWanted, out var blueWanted);

            for (var i = redWanted; i < redBots; i++)
                RemoveBot(eTeamPreference.Red);

            for (var i = blueWanted; i < blueBots; i++)
                RemoveBot(eTeamPreference.Blue);

            for (var i = redBots; i < redWanted; i++)
                AddBot(eTeam.Red);

            for (var i = blueBots; i < blueWanted; i++)
                AddBot(eTeam.Blue);
        }

        /// <summary>
        /// Adds a bot, named from bot_names.txt, to the given team, or to the smaller team for eTeam.Computer.
        /// </summary>
        private void AddBot(eTeam team = eTeam.Computer)
        {
            CountTeams(out var redCount, out var blueCount, out _, out _);

            var namesInUse = new HashSet<string>();
            foreach (var other in _Players.Values)
                namesInUse.Add(other.Player.Name);

            var bot = new Player
            {
                Id = _NextBotId++,
                Name = BotNameManager.PickName(_BotNames, namesInUse, _BotRandom),
                Team = team == eTeam.Computer ? TeamBalancer.AssignTeam(eTeamPreference.Either, redCount, blueCount) : team,
            };
            Respawn(bot);

            _Players[bot.Id] = new ServerPlayer(null, bot);
            _BotCount++;

            Broadcast(BuildPlayerJoined(bot));
            Console.WriteLine($"Bot {bot.Name} joined the {bot.Team} team ({_PlayerCount} players, {_BotCount} bots)");
        }

        /// <summary>
        /// Removes the newest bot from the team BotPlanner.PickTeamToRemoveFrom picks for a human preferring that team.
        /// </summary>
        private void RemoveBot(eTeamPreference preferred)
        {
            CountTeams(out var redCount, out var blueCount, out var redBots, out var blueBots);
            var team = BotPlanner.PickTeamToRemoveFrom(preferred, redBots, blueBots, redCount, blueCount);

            if (team == null)
                return;

            ServerPlayer newest = null;

            foreach (var serverPlayer in _Players.Values)
                if (serverPlayer.IsBot && serverPlayer.Player.Team == team && (newest == null || serverPlayer.Player.Id > newest.Player.Id))
                    newest = serverPlayer;

            Remove(newest);
        }

        /// <summary>
        /// Players on each team, bots included, and how many of them are bots.
        /// </summary>
        private void CountTeams(out int redCount, out int blueCount, out int redBots, out int blueBots)
        {
            redCount = blueCount = redBots = blueBots = 0;

            foreach (var serverPlayer in _Players.Values)
            {
                var red = serverPlayer.Player.Team == eTeam.Red;

                if (red)
                    redCount++;
                else
                    blueCount++;

                if (serverPlayer.IsBot && red)
                    redBots++;
                else if (serverPlayer.IsBot)
                    blueBots++;
            }
        }

        /// <summary>
        /// Puts a player at one of their team's spawn points, alive and at starting size.
        /// </summary>
        private void Respawn(Player player)
        {
            var spawns = player.Team == eTeam.Blue ? _BlueSpawns : _RedSpawns;

            if (spawns.Count == 0)
                throw new InvalidOperationException($"Level {CurrentLevel.Level} has no {player.Team} spawn points");

            var (x, y) = spawns[player.Id % spawns.Count];
            player.Location = new Point2(x + CELL_CENTER, y + CELL_CENTER);
            player.Facing = player.Team == eTeam.Blue ? eFacing.Left : eFacing.Right;
            player.Size = PAC_START_SIZE;
            player.Alive = true;
            player.HasFlag = false;
            player.RespawnTimer = 0;
            player.StunMs = 0;
            player.MsSinceEaten = 0;
            player.PowerUpEffect = ePowerUpEffect.None;
            player.PowerUpMs = 0;
            player.Eating = false;
            player.LastCellX = -1;
            player.LastCellY = -1;
        }

        private LevelStartMessage BuildLevelStart()
        {
            return new LevelStartMessage { Level = CurrentLevel.Level, WallColor = CurrentLevel.WallColor, WallFillColor = CurrentLevel.WallFillColor, BackgroundColor = CurrentLevel.BackgroundColor };
        }

        private static PlayerJoinedMessage BuildPlayerJoined(Player player)
        {
            return new PlayerJoinedMessage { PlayerId = player.Id, Name = player.Name, Team = player.Team };
        }

        private static void Send(IClientConnection connection, INetworkMessage message)
        {
            connection.Send(MessageSerializer.Serialize(message));
        }

        /// <summary>
        /// Sends a message to every human who has joined. It is serialized once and the same bytes go to everyone.
        /// </summary>
        private void Broadcast(INetworkMessage message)
        {
            if (_PlayerCount == 0)
                return;

            var frame = MessageSerializer.Serialize(message);

            foreach (var serverPlayer in _Players.Values)
                if (!serverPlayer.IsBot)
                    serverPlayer.Connection.Send(frame);
        }
    }
}
