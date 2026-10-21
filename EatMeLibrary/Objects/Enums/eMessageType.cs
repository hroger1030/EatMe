namespace EatMeLibrary
{
    /// <summary>
    /// First byte of every message body; says which message class follows.
    /// </summary>
    public enum eMessageType : byte
    {
        // client to server
        Hello = 1,
        Input = 2,
        Ping = 3,
        Leave = 4,
        ServerPong = 5,
        Action = 6,
        PlayerListRequest = 7,

        // server to client
        Welcome = 20,
        Reject = 21,
        LevelStart = 22,
        Snapshot = 23,
        CellsChanged = 24,
        PlayerJoined = 25,
        PlayerLeft = 26,
        GameEvent = 27,
        LevelWon = 28,
        Pong = 29,
        HighScores = 30,
        ServerPing = 31,
        GameConfig = 32,
        PlayerList = 33,

        // LAN discovery over UDP
        DiscoveryRequest = 40,
        DiscoveryResponse = 41,
    }
}
