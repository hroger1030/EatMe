using EatMeLibrary;

namespace EatMeTests
{
    public class AnnouncementsTests
    {
        [TestCase(eGameEvent.FlagLost, eTeam.Red, Const.SFX_MSG_RED_FLAG_DROPPED)]
        [TestCase(eGameEvent.FlagLost, eTeam.Blue, Const.SFX_MSG_BLUE_FLAG_DROPPED)]
        [TestCase(eGameEvent.FlagReturned, eTeam.Red, Const.SFX_MSG_RED_FLAG_RETURNED)]
        [TestCase(eGameEvent.FlagReturned, eTeam.Blue, Const.SFX_MSG_BLUE_FLAG_RETURNED)]
        [TestCase(eGameEvent.FlagCaptured, eTeam.Red, Const.SFX_MSG_RED_FLAG_CAPTURED)]
        [TestCase(eGameEvent.FlagCaptured, eTeam.Blue, Const.SFX_MSG_BLUE_FLAG_CAPTURED)]
        [TestCase(eGameEvent.AboutToWin, eTeam.Red, Const.SFX_MSG_RED_ABOUT_TO_WIN)]
        [TestCase(eGameEvent.AboutToWin, eTeam.Blue, Const.SFX_MSG_BLUE_ABOUT_TO_WIN)]
        public void GetSound(eGameEvent eventType, eTeam team, string expected)
        {
            Assert.That(Announcements.GetSound(eventType, team), Is.EqualTo(expected));
        }

        [TestCase(eGameEvent.PacEaten, eTeam.Red)]
        [TestCase(eGameEvent.GhostKilled, eTeam.Blue)]
        [TestCase(eGameEvent.FlagLost, eTeam.Computer)]
        public void GetSound_NoAnnouncement_Null(eGameEvent eventType, eTeam team)
        {
            Assert.That(Announcements.GetSound(eventType, team), Is.Null);
        }

        [TestCase(eTeam.Red, eTeam.Red, Const.SFX_LEVEL_WON)]
        [TestCase(eTeam.Blue, eTeam.Blue, Const.SFX_LEVEL_WON)]
        [TestCase(eTeam.Red, eTeam.Blue, Const.SFX_LEVEL_LOST)]
        [TestCase(eTeam.Blue, eTeam.Red, Const.SFX_LEVEL_LOST)]
        public void GetLevelEndSound(eTeam winner, eTeam ownTeam, string expected)
        {
            Assert.That(Announcements.GetLevelEndSound(winner, ownTeam), Is.EqualTo(expected));
        }
    }
}
