namespace EatMeLibrary
{
    /// <summary>
    /// Picks the announcer voice clip for a game event. Announcements are global: every player hears them at full
    /// volume, wherever the event happened.
    /// </summary>
    public static class Announcements
    {
        /// <summary>
        /// The clip for this event and team (for flag events the team whose flag it is, for AboutToWin the team about
        /// to win), or null if the event has no announcement.
        /// </summary>
        public static string GetSound(eGameEvent eventType, eTeam team)
        {
            if (team != eTeam.Red && team != eTeam.Blue)
                return null;

            var red = team == eTeam.Red;

            return eventType switch
            {
                eGameEvent.FlagLost => red ? Const.SFX_MSG_RED_FLAG_DROPPED : Const.SFX_MSG_BLUE_FLAG_DROPPED,
                eGameEvent.FlagReturned => red ? Const.SFX_MSG_RED_FLAG_RETURNED : Const.SFX_MSG_BLUE_FLAG_RETURNED,
                eGameEvent.FlagCaptured => red ? Const.SFX_MSG_RED_FLAG_CAPTURED : Const.SFX_MSG_BLUE_FLAG_CAPTURED,
                eGameEvent.AboutToWin => red ? Const.SFX_MSG_RED_ABOUT_TO_WIN : Const.SFX_MSG_BLUE_ABOUT_TO_WIN,
                _ => null,
            };
        }

        /// <summary>
        /// The sound played on the results screen: the won fanfare if the player's own team won the level, otherwise
        /// the lost one.
        /// </summary>
        public static string GetLevelEndSound(eTeam winner, eTeam ownTeam)
        {
            return winner == ownTeam ? Const.SFX_LEVEL_WON : Const.SFX_LEVEL_LOST;
        }
    }
}
