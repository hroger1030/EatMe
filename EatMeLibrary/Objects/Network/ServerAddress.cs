namespace EatMeLibrary
{
    /// <summary>
    /// Parses the "host" or "host:port" text a player types to join a server.
    /// </summary>
    public static class ServerAddress
    {
        /// <summary>
        /// Splits text into host and port. The port defaults to NetworkConstants.DEFAULT_PORT when left off.
        /// Returns false for empty text or a port that isn't 1 to 65535.
        /// </summary>
        public static bool TryParse(string text, out string host, out int port)
        {
            host = null;
            port = NetworkConstants.DEFAULT_PORT;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            var colon = text.LastIndexOf(':');

            // more than one colon is an IPv6 address without a port
            if (colon < 0 || text.IndexOf(':') != colon)
            {
                host = text;
                return true;
            }

            if (!int.TryParse(text[(colon + 1)..], out port) || port < 1 || port > 65535)
                return false;

            host = text[..colon];
            return host.Length > 0;
        }
    }
}
