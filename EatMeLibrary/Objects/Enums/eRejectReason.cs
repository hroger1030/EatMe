namespace EatMeLibrary
{
    /// <summary>
    /// Why the server refused a client's Hello, or (Kicked) removed a player already in the game.
    /// </summary>
    public enum eRejectReason : byte
    {
        ServerFull,
        InvalidName,
        VersionMismatch,
        Kicked,
    }
}
