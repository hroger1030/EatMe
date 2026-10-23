using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The frozen phases at either end of a level: a countdown before play starts and a victory pause after a team
    /// wins, before the results. Nothing in the game moves or acts during either.
    /// </summary>
    public static class LevelPhases
    {
        private const int MS_PER_SECOND = 1000;

        /// <summary>
        /// How long the 3, 2, 1 countdown lasts before a level's play starts.
        /// </summary>
        public const int START_COUNTDOWN_MS = 3000;

        /// <summary>
        /// How long "Victory!" shows over the frozen board once a team wins, before the results.
        /// </summary>
        public const int VICTORY_MS = 3000;

        /// <summary>
        /// How long "Victory!" stays on, then off, as it flashes.
        /// </summary>
        public const int VICTORY_FLASH_MS = 250;

        /// <summary>
        /// The countdown number to show with msLeft of the countdown to go: the seconds left rounded up, so 3, 2 and 1
        /// each show for a second, and 0 once it has run out.
        /// </summary>
        public static int GetCountdownNumber(long msLeft)
        {
            if (msLeft <= 0)
                return 0;

            return (int)((msLeft + MS_PER_SECOND - 1) / MS_PER_SECOND);
        }

        /// <summary>
        /// The start countdown's text for a countdown number: "Starting in... 3" (Const.STRING_STARTING_IN), or empty
        /// once it has run out.
        /// </summary>
        public static string GetCountdownText(int number)
        {
            if (number <= 0)
                return string.Empty;

            return $"{Const.STRING_STARTING_IN} {number}";
        }

        /// <summary>
        /// True while the flashing "Victory!" is on, a time after it first showed: on for VICTORY_FLASH_MS, then off
        /// as long, starting on.
        /// </summary>
        public static bool IsVictoryTextOn(TimeSpan time)
        {
            var phase = Math.Max(0, time.TotalMilliseconds) % (VICTORY_FLASH_MS * 2);
            return phase < VICTORY_FLASH_MS;
        }
    }
}
