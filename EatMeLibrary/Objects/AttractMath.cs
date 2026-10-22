using System;
using System.Collections.Generic;
using System.Linq;

namespace EatMeLibrary
{
    /// <summary>
    /// Timing and order for the attract screen: the title menu fades to black after IDLE_SECONDS with no input, then
    /// the attract screen scrolls its content up at SCROLL_PIXELS_PER_SECOND until the bottom of the content reaches
    /// the bottom of the screen, holds there for HOLD_SECONDS and goes back to the title.
    /// </summary>
    public static class AttractMath
    {
        /// <summary>
        /// Seconds the title menu waits with no input before fading to the attract screen.
        /// </summary>
        public const float IDLE_SECONDS = 10f;

        /// <summary>
        /// Seconds the title menu takes to fade to black.
        /// </summary>
        public const float FADE_SECONDS = 1f;

        /// <summary>
        /// Seconds the attract screen holds at the bottom before going back to the title.
        /// </summary>
        public const float HOLD_SECONDS = 5f;

        /// <summary>
        /// How fast the attract screen's content scrolls up, in pixels a second.
        /// </summary>
        public const float SCROLL_PIXELS_PER_SECOND = 60f;

        /// <summary>
        /// How black the fade is after fadeSeconds of fading: 0 (none) to 1 (fully black) over FADE_SECONDS.
        /// </summary>
        public static float GetFadeAlpha(float fadeSeconds)
        {
            return Math.Clamp(fadeSeconds / FADE_SECONDS, 0f, 1f);
        }

        /// <summary>
        /// How far, in whole pixels, the content has scrolled up after seconds: it moves at pixelsPerSecond and stops
        /// once its bottom meets the bottom of a view viewHeight tall. Content no taller than the view never scrolls.
        /// </summary>
        public static int GetScrollOffset(float seconds, float contentHeight, float viewHeight, float pixelsPerSecond = SCROLL_PIXELS_PER_SECOND)
        {
            var maxScroll = Math.Max(0f, contentHeight - viewHeight);
            return (int)Math.Min(Math.Max(0f, seconds) * pixelsPerSecond, maxScroll);
        }

        /// <summary>
        /// Seconds until the content's bottom reaches the bottom of the view.
        /// </summary>
        public static float GetScrollSeconds(float contentHeight, float viewHeight, float pixelsPerSecond = SCROLL_PIXELS_PER_SECOND)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pixelsPerSecond, 0f);
            return Math.Max(0f, contentHeight - viewHeight) / pixelsPerSecond;
        }

        /// <summary>
        /// True once the content has scrolled to the bottom and held there for HOLD_SECONDS.
        /// </summary>
        public static bool IsFinished(float seconds, float contentHeight, float viewHeight, float pixelsPerSecond = SCROLL_PIXELS_PER_SECOND)
        {
            return seconds >= GetScrollSeconds(contentHeight, viewHeight, pixelsPerSecond) + HOLD_SECONDS;
        }

        /// <summary>
        /// Every ghost personality, alphabetically by name, ignoring case.
        /// </summary>
        public static List<GhostPersonality> GetGhostOrder()
        {
            return GhostPersonalities.ALL.OrderBy(personality => personality.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Every item that can appear on the board (PowerUpRules.SPAWNABLE), alphabetically by PowerUpNames.GetName,
        /// ignoring case.
        /// </summary>
        public static List<ePowerUp> GetPowerUpOrder()
        {
            return Enum.GetValues<ePowerUp>()
                .Where(item => item != ePowerUp.None && item != ePowerUp.Any && PowerUpRules.SPAWNABLE.HasFlag(item))
                .OrderBy(PowerUpNames.GetName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
