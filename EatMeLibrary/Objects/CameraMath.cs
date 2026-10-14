using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Camera sums for the game board.
    /// </summary>
    public static class CameraMath
    {
        /// <summary>
        /// What to add to a world position (one axis, in pixels) to get its screen position, for a camera centred on
        /// cameraPosition in a viewport viewportSize pixels across. It is rounded to whole pixels so the maze lines
        /// up with the screen's pixels and doesn't crawl as the camera follows a pac between cells.
        /// </summary>
        public static float GetScreenOffset(float cameraPosition, float viewportSize)
        {
            return MathF.Round(viewportSize * 0.5f - cameraPosition);
        }
    }
}
