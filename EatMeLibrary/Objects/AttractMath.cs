using System;
using System.Collections.Generic;
using System.Linq;

namespace EatMeLibrary
{
    /// <summary>
    /// Timing and order for the attract screen: the title menu fades to black after IDLE_MS with no input, then
    /// the attract screen scrolls its content up at SCROLL_PIXELS_PER_SECOND until the bottom of the content reaches
    /// the bottom of the screen, holds there for HOLD_MS and goes back to the title.
    /// </summary>
    public static class AttractMath
    {
        /// <summary>
        /// How long the title menu waits with no input before fading to the attract screen.
        /// </summary>
        public const int IDLE_MS = 10000;

        /// <summary>
        /// How long the title menu takes to fade to black.
        /// </summary>
        public const int FADE_MS = 1000;

        /// <summary>
        /// How long the attract screen holds at the bottom before going back to the title.
        /// </summary>
        public const int HOLD_MS = 5000;

        /// <summary>
        /// How fast the attract screen's content scrolls up, in pixels a second.
        /// </summary>
        public const float SCROLL_PIXELS_PER_SECOND = 120f;

        /// <summary>
        /// How black the fade is after fading for a time: 0 (none) to 1 (fully black) over FADE_MS.
        /// </summary>
        public static float GetFadeAlpha(TimeSpan fading)
        {
            return (float)Math.Clamp(fading.TotalMilliseconds / FADE_MS, 0, 1);
        }

        /// <summary>
        /// How far, in whole pixels, the content has scrolled up after a time: it moves at pixelsPerSecond and stops
        /// once its bottom meets the bottom of a view viewHeight tall. Content no taller than the view never scrolls.
        /// </summary>
        public static int GetScrollOffset(TimeSpan time, float contentHeight, float viewHeight, float pixelsPerSecond = SCROLL_PIXELS_PER_SECOND)
        {
            var maxScroll = Math.Max(0f, contentHeight - viewHeight);
            return (int)Math.Min(Math.Max(0, time.TotalSeconds) * pixelsPerSecond, maxScroll);
        }

        /// <summary>
        /// How long until the content's bottom reaches the bottom of the view.
        /// </summary>
        public static TimeSpan GetScrollTime(float contentHeight, float viewHeight, float pixelsPerSecond = SCROLL_PIXELS_PER_SECOND)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pixelsPerSecond, 0f);
            return TimeSpan.FromSeconds(Math.Max(0f, contentHeight - viewHeight) / pixelsPerSecond);
        }

        /// <summary>
        /// True once the content has scrolled to the bottom and held there for HOLD_MS.
        /// </summary>
        public static bool IsFinished(TimeSpan time, float contentHeight, float viewHeight, float pixelsPerSecond = SCROLL_PIXELS_PER_SECOND)
        {
            return time >= GetScrollTime(contentHeight, viewHeight, pixelsPerSecond) + TimeSpan.FromMilliseconds(HOLD_MS);
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
            return PowerUpRules.SPAWNABLE
                .OrderBy(PowerUpNames.GetName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
