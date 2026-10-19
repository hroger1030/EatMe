using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public class Camera
    {
        private const float VISIBILITY_PADDING = 1.1f;

        public Vector2 Position { get; set; }

        /// <summary>
        /// Whole pixels added to every screen position while the board shakes (ScreenShake); zero otherwise.
        /// </summary>
        public Vector2 ShakeOffset { get; set; }

        public Camera() { }

        /// <summary>
        /// Where a world position is on screen. The camera's offset is rounded to whole pixels, so the maze lines up
        /// with the screen's pixels and doesn't crawl as the camera follows a pac.
        /// </summary>
        public Vector2 ToScreenPosition(Vector2 worldPosition, Viewport viewport)
        {
            var offset = new Vector2(CameraMath.GetScreenOffset(Position.X, viewport.Width), CameraMath.GetScreenOffset(Position.Y, viewport.Height));
            return worldPosition + offset + ShakeOffset;
        }

        public Vector2 ToWorldPosition(Vector2 screenPosition, Viewport viewport)
        {
            var screenCenter = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
            return screenPosition - Position + screenCenter;
        }

        /// <summary>
        /// Checks whether a world-space bounding box intersects the camera's viewport, padded out
        /// by VISIBILITY_PADDING so sprites aren't clipped right at the edge.
        /// </summary>
        public bool IsVisible(FastG.AARectangle boundingBox, Viewport viewport)
        {
            var paddedWidth = viewport.Width * VISIBILITY_PADDING;
            var paddedHeight = viewport.Height * VISIBILITY_PADDING;

            var visibleWorldArea = new FastG.AARectangle(Position.ToGeometryPoint(), paddedWidth, paddedHeight);

            return boundingBox.Intersects(visibleWorldArea);
        }
    }
}
