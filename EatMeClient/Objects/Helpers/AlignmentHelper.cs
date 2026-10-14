using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public static class AlignmentHelper
    {
        /// <summary>
        /// Computes the top-left screen position for an element of the given size within
        /// a viewport, based on the requested vertical/horizontal alignment flags.
        ///
        /// Missing flags will default to VerticalTop and HorizontalLeft values.
        /// </summary>
        public static Vector2 GetPosition(Viewport viewport, Vector2 size, eElementAlignment alignment)
        {
            const eElementAlignment VerticalMask = eElementAlignment.VerticalTop | eElementAlignment.VerticalMiddle | eElementAlignment.VerticalBottom;
            const eElementAlignment HorizontalMask = eElementAlignment.HorizontalLeft | eElementAlignment.HorizontalCenter | eElementAlignment.HorizontalRight;

            var output = Vector2.Zero;

            switch (alignment & VerticalMask)
            {
                case eElementAlignment.VerticalTop:
                    output.Y = 0f;
                    break;
                case eElementAlignment.VerticalMiddle:
                    output.Y = (viewport.Height - size.Y) * 0.5f;
                    break;
                case eElementAlignment.VerticalBottom:
                    output.Y = viewport.Height - size.Y;
                    break;
            }

            switch (alignment & HorizontalMask)
            {
                case eElementAlignment.HorizontalLeft:
                    output.X = 0f;
                    break;
                case eElementAlignment.HorizontalCenter:
                    output.X = (viewport.Width - size.X) * 0.5f;
                    break;
                case eElementAlignment.HorizontalRight:
                    output.X = viewport.Width - size.X;
                    break;
            }

            return output;
        }
    }
}