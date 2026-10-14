using System.Globalization;

namespace EatMeLibrary
{
    /// <summary>
    /// A game server found on the LAN.
    /// </summary>
    public class ServerListing
    {
        public string Host { get; }
        public DiscoveryResponseMessage Info { get; }

        /// <summary>
        /// "host:port", ready for ServerAddress.TryParse.
        /// </summary>
        public string Address => $"{Host}:{Info.Port}";

        public ServerListing(string host, DiscoveryResponseMessage info)
        {
            Host = host;
            Info = info;
        }

        /// <summary>
        /// The server name with its version, e.g. "Eat Me Server (v0.9)".
        /// </summary>
        public string GetDisplayName()
        {
            return $"{Info.ServerName} (v{Info.ServerVersion.ToString(CultureInfo.InvariantCulture)})";
        }

        /// <summary>
        /// Every server found is listed, but only one running this app version can be joined.
        /// </summary>
        public bool CanJoin()
        {
            return Info.ServerVersion == NetworkConstants.APP_VERSION;
        }
    }
}
