using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The shape of a rectangle's rounded corners, one pixel row at a time.
    /// </summary>
    public static class RoundedCorners
    {
        /// <summary>
        /// How many whole pixels in from each side a row of a rounded rectangle starts, for corners of the given radius.
        /// Row 0 is the outermost row (the top or bottom edge); rows at or past the radius aren't cut in at all.
        /// </summary>
        public static int GetRowInset(int radius, int row)
        {
            if (radius <= 0 || row < 0 || row >= radius)
                return 0;

            var dy = radius - row - 0.5;
            return (int)Math.Round(radius - Math.Sqrt(radius * radius - dy * dy));
        }
    }
}
