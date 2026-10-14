using EatMeLibrary;

namespace EatMeServer
{
    /// <summary>
    /// Puts players on their preferred team unless that would make the teams uneven.
    /// </summary>
    public static class TeamBalancer
    {
        /// <summary>
        /// Returns the preferred team if it has no more players than the other one, otherwise the smaller team.
        /// Either also gets the smaller team (red on a tie).
        /// </summary>
        public static eTeam AssignTeam(eTeamPreference preferred, int redCount, int blueCount)
        {
            if (preferred == eTeamPreference.Red && redCount <= blueCount)
                return eTeam.Red;

            if (preferred == eTeamPreference.Blue && blueCount <= redCount)
                return eTeam.Blue;

            return redCount <= blueCount ? eTeam.Red : eTeam.Blue;
        }
    }
}
