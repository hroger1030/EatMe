using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    /// <summary>
    /// Shared setup for tests that drive a GameServer.
    /// </summary>
    public static class ServerTestHelper
    {
        /// <summary>
        /// Runs out the level's start countdown (LevelPhases.START_COUNTDOWN_MS), so the game is no longer frozen and
        /// the test can play at once. Returns the server.
        /// </summary>
        public static GameServer SkipCountdown(GameServer server)
        {
            server.UpdateLevelPhases(TimeSpan.FromMilliseconds(LevelPhases.START_COUNTDOWN_MS));
            return server;
        }
    }
}
