namespace EatMeLibrary
{
    /// <summary>
    /// Why the server refused a client's Hello.
    /// </summary>
    public enum eRejectReason : byte
    {
        ServerFull,
        InvalidName,
        VersionMismatch,
    }
}
