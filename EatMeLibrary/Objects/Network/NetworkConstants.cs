namespace EatMeLibrary
{
    /// <summary>
    /// Values client and server must agree on.
    /// </summary>
    public static class NetworkConstants
    {
        /// <summary>
        /// Version of the game, shared by client and server, so the client and server versions need to match.
        /// The client sends it to the server in HelloMessage.ClientVersion. There is no separate protocol version and
        /// no backwards compatibility: a client and server with the same APP_VERSION always speak the same messages.
        /// </summary>
        public const float APP_VERSION = 0.9f;

        public const int DEFAULT_PORT = 7777;
        public const int DEFAULT_DISCOVERY_PORT = 7778;

        /// <summary>
        /// Bytes in the length prefix in front of every message.
        /// </summary>
        public const int LENGTH_PREFIX_SIZE = sizeof(int);

        /// <summary>
        /// Largest message body accepted, in bytes. Anything bigger is treated as a corrupt stream.
        /// </summary>
        public const int MAX_MESSAGE_SIZE = 1024 * 1024;
    }
}
