using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Where the text of a message scroller sits as it slides across the screen from right to left.
    /// </summary>
    public static class ScrollerMath
    {
        /// <summary>
        /// The text's left edge, in whole pixels, after elapsedSeconds. It starts just off the right edge of a screen
        /// screenWidth wide, moves left at pixelsPerSecond, and once it has fully left the screen starts over from the right.
        /// </summary>
        public static int GetTextX(float screenWidth, float textWidth, double elapsedSeconds, float pixelsPerSecond)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pixelsPerSecond, 0f);

            var cycle = (double)screenWidth + textWidth;
            if (cycle <= 0)
                return 0;

            var scrolled = Math.Max(0, elapsedSeconds) * pixelsPerSecond % cycle;
            return (int)Math.Floor(screenWidth - scrolled);
        }
    }
}
