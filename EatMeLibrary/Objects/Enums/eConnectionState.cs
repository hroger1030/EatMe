namespace EatMeLibrary
{
    /// <summary>
    /// Where a client's connection to the server is at.
    /// </summary>
    public enum eConnectionState
    {
        NotConnected,
        Connecting,
        Connected,

        /// <summary>
        /// The connection couldn't be made.
        /// </summary>
        Failed,

        /// <summary>
        /// The connection was made and has since closed.
        /// </summary>
        Disconnected,
    }
}
