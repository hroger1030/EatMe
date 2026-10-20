using EatMeLibrary;

namespace EatMeServer
{
    /// <summary>
    /// Server configuration, loaded from server.cfg on startup.
    /// </summary>
    public class ServerSettings
    {
        public string ServerName { get; set; }

        /// <summary>
        /// Shown on every connected client's title screen scroller. Empty or missing means Const.MOTD_DEFAULT.
        /// </summary>
        public string MessageOfTheDay { get; set; } = Const.MOTD_DEFAULT;
        public int Port { get; set; } = NetworkConstants.DEFAULT_PORT;

        /// <summary>
        /// UDP port the server listens on for LAN discovery broadcasts.
        /// </summary>
        public int DiscoveryPort { get; set; } = NetworkConstants.DEFAULT_DISCOVERY_PORT;
        public int TickRate { get; set; }

        /// <summary>
        /// Times per second the server sends game state to clients.
        /// </summary>
        public int TargetFps { get; set; } = 30;

        /// <summary>
        /// When true, bots are added to or removed from each team so both teams always have the same number of players.
        /// </summary>
        public bool AutoBalanceTeams { get; set; } = true;

        /// <summary>
        /// The level the server starts on. Below 1 becomes 1; past the last level the list starts over.
        /// </summary>
        public int StartingLevel { get; set; } = 1;

        /// <summary>
        /// When true, every level after the starting one is picked at random (never the one just played) instead of
        /// going on to the next level in the list.
        /// </summary>
        public bool RandomLevels { get; set; } = false;

        public GameConfiguration GameConfig { get; set; } = new GameConfiguration();

        public ServerSettings() { }
    }
}
