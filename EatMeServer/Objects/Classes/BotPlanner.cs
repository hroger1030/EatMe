using EatMeLibrary;
using System;

namespace EatMeServer
{
    /// <summary>
    /// How many bots the server runs and which teams they join or leave.
    /// </summary>
    public static class BotPlanner
    {
        /// <summary>
        /// Bots needed to bring the humans up to minPlayers, never taking the total past maxPlayers.
        /// </summary>
        public static int GetBotCount(int humanCount, int minPlayers, int maxPlayers)
        {
            return Math.Max(0, Math.Min(minPlayers, maxPlayers) - humanCount);
        }

        /// <summary>
        /// Bots each team needs so both teams are the same size: as big as the bigger team's humans, and together at
        /// least minPlayers (never more than maxPlayers, rounded up to an even number). Only humans count toward
        /// maxPlayers, so uneven humans are always evened up, however many bots that takes.
        /// </summary>
        public static void GetBalancedBotCounts(int redHumans, int blueHumans, int minPlayers, int maxPlayers,
            out int redBots, out int blueBots)
        {
            var teamSize = Math.Max(Math.Max(redHumans, blueHumans), (Math.Min(minPlayers, maxPlayers) + 1) / 2);

            redBots = teamSize - redHumans;
            blueBots = teamSize - blueHumans;
        }

        /// <summary>
        /// The team a bot leaves from to make room for a human. The human's preferred team if it has a bot, so they
        /// can join it; otherwise (or for Either) the bigger team that has a bot. Null if there are no bots.
        /// </summary>
        public static eTeam? PickTeamToRemoveFrom(eTeamPreference preferred, int redBots, int blueBots, int redCount, int blueCount)
        {
            if (preferred == eTeamPreference.Red && redBots > 0)
                return eTeam.Red;

            if (preferred == eTeamPreference.Blue && blueBots > 0)
                return eTeam.Blue;

            if (redBots > 0 && (blueBots == 0 || redCount >= blueCount))
                return eTeam.Red;

            if (blueBots > 0)
                return eTeam.Blue;

            return null;
        }
    }
}
