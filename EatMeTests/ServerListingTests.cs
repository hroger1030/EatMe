using EatMeLibrary;

namespace EatMeTests
{
    public class ServerListingTests
    {
        [TestCase("Eat Me Server", 0.9f, "Eat Me Server (v0.9)")]
        [TestCase("LAN Party", 1.25f, "LAN Party (v1.25)")]
        [TestCase("Big", 2f, "Big (v2)")]
        public void GetDisplayName(string name, float version, string expected)
        {
            var listing = new ServerListing("192.168.1.5", new DiscoveryResponseMessage { ServerName = name, ServerVersion = version });

            Assert.That(listing.GetDisplayName(), Is.EqualTo(expected));
        }

        [Test]
        public void CanJoin_SameVersions_True()
        {
            var listing = new ServerListing("192.168.1.5", new DiscoveryResponseMessage());

            Assert.That(listing.CanJoin(), Is.True);
        }

        [Test]
        public void CanJoin_DifferentAppVersion_False()
        {
            var listing = new ServerListing("192.168.1.5", new DiscoveryResponseMessage { ServerVersion = NetworkConstants.APP_VERSION + 0.1f });

            Assert.That(listing.CanJoin(), Is.False);
        }
    }
}
