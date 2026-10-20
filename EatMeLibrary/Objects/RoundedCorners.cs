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

        /// <summary>
        /// For the outline of a rounded rectangle thickness pixels wide, how many whole pixels in from each side the hollow
        /// inside of a row starts, or -1 when the row is inside the top or bottom edge and so solid all the way across.
        /// Row 0 is the outermost row; the inside's corners have radius minus thickness.
        /// </summary>
        public static int GetOutlineInnerInset(int radius, int thickness, int row)
        {
            if (row < thickness)
                return -1;

            return thickness + GetRowInset(radius - thickness, row - thickness);
        }
    }
}
