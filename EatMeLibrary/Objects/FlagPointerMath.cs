using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Sums for the flag pointers: arrows on a circle around the player pointing at flags that are off screen. All
    /// positions are in pixels relative to the board's top left corner.
    /// </summary>
    public static class FlagPointerMath
    {
        /// <summary>
        /// Whether any of a square halfSize pixels from its centre (x, y) to each side overlaps a board width by
        /// height pixels.
        /// </summary>
        public static bool IsOnScreen(float x, float y, float halfSize, float width, float height)
        {
            return x + halfSize > 0f && x - halfSize < width && y + halfSize > 0f && y - halfSize < height;
        }

        /// <summary>
        /// Where a pointer goes for a target at (targetX, targetY): on the circle of the given radius around
        /// (centreX, centreY), where the line from the centre to the target crosses it. A target at the centre gives
        /// the centre.
        /// </summary>
        public static (float X, float Y) GetCirclePosition(float centreX, float centreY, float targetX, float targetY, float radius)
        {
            var dx = targetX - centreX;
            var dy = targetY - centreY;
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance == 0f)
                return (centreX, centreY);

            var scale = radius / distance;
            return (centreX + dx * scale, centreY + dy * scale);
        }

        /// <summary>
        /// The straight line distance, in pixels, between (x1, y1) and (x2, y2).
        /// </summary>
        public static float GetDistance(float x1, float y1, float x2, float y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// The clockwise turn, in radians, that makes a sprite drawn pointing right point from (fromX, fromY) at
        /// (toX, toY), with y growing downwards as on screen.
        /// </summary>
        public static float GetAngle(float fromX, float fromY, float toX, float toY)
        {
            return MathF.Atan2(toY - fromY, toX - fromX);
        }
    }
}
