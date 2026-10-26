using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// The bar across the bottom of the game screen: what the server's scoring is set to, left to right.
    /// </summary>
    public static class ScoringBar
    {
        /// <summary>
        /// The items in the order they are drawn: the points for a flag capture, a flag return, eating an enemy pac and
        /// eating a ghost, each as its label, its sprite and its points ("capture [enemy flag] 20pt").
        /// </summary>
        public static List<ScoringBarItem> GetItems(GameConfiguration config)
        {
            return
            [
                new ScoringBarItem(Const.STRING_SCORING_CAPTURE, eScoringIcon.EnemyFlag, GetPointsText(config.FlagScore)),
                new ScoringBarItem(Const.STRING_SCORING_RETURN, eScoringIcon.OwnFlag, GetPointsText(config.FlagRescue)),
                new ScoringBarItem(Const.STRING_SCORING_EAT, eScoringIcon.EnemyPac, GetPointsText(config.PlayerKill)),
                new ScoringBarItem(Const.STRING_SCORING_EAT, eScoringIcon.Ghost, GetPointsText(config.GhostKill)),
            ];
        }

        /// <summary>
        /// The gap between neighbouring elements spread evenly across the bar: the first starts at left, the last ends at
        /// right, and the space left over is shared equally between them. Never below 0 (they then just run on).
        /// </summary>
        public static float GetEvenGap(float totalWidth, int count, float left, float right)
        {
            if (count < 2)
                return 0f;

            return Math.Max(0f, (right - left - totalWidth) / (count - 1));
        }

        /// <summary>
        /// The ping's box on the bar is as wide as the widest ping text up to this many ms, so the bar doesn't move as
        /// the ping changes.
        /// </summary>
        public const int PING_BOX_MAX_MS = 9999;

        /// <summary>
        /// Every ping text the ping's box must fit: unknown, and each digit repeated to PING_BOX_MAX_MS's length
        /// (digits aren't all the same width in every font).
        /// </summary>
        public static List<string> GetPingWidthSamples()
        {
            var samples = new List<string> { ClientGameState.FormatPingText(-1) };
            var digits = PING_BOX_MAX_MS.ToString().Length;

            for (var digit = '0'; digit <= '9'; digit++)
                samples.Add(ClientGameState.FormatPingText(int.Parse(new string(digit, digits))));

            return samples;
        }

        /// <summary>
        /// Points as the bar shows them, e.g. "20pt".
        /// </summary>
        public static string GetPointsText(int points)
        {
            return $"{points}{Const.STRING_SCORING_POINTS}";
        }

        /// <summary>
        /// The other team: the one whose flag and pacs the bar shows as enemies.
        /// </summary>
        public static eTeam GetEnemyTeam(eTeam team)
        {
            return team == eTeam.Red ? eTeam.Blue : eTeam.Red;
        }
    }
}
