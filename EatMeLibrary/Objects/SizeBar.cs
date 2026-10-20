using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The size bar under "Size n" on the player state panel: it fills from 0 at size 0 to full at FULL_SIZE, and
    /// changes color as the pac grows past FULL_SIZE and HUGE_SIZE.
    /// </summary>
    public static class SizeBar
    {
        /// <summary>
        /// The size the bar is full at; above it the bar turns orange.
        /// </summary>
        public const float FULL_SIZE = 100f;

        /// <summary>
        /// Above this size the bar turns red.
        /// </summary>
        public const float HUGE_SIZE = 150f;

        /// <summary>
        /// How full the bar is, 0 to 1: size over FULL_SIZE.
        /// </summary>
        public static float GetFill(float size)
        {
            return Math.Clamp(size / FULL_SIZE, 0f, 1f);
        }

        public static eSizeBarLevel GetLevel(float size)
        {
            if (size > HUGE_SIZE)
                return eSizeBarLevel.Huge;

            return size > FULL_SIZE ? eSizeBarLevel.Big : eSizeBarLevel.Normal;
        }
    }
}
