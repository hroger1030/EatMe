using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Picks the loading screen tip shown on the intermission screen.
    /// </summary>
    public static class TipRules
    {
        /// <summary>
        /// One tip picked at random, or empty if there are no tips.
        /// </summary>
        public static string PickTip(List<string> tips, Random random)
        {
            if (tips == null || tips.Count == 0)
                return string.Empty;

            return tips[random.Next(tips.Count)];
        }
    }
}
