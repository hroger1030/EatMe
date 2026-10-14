using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Scrolling maths for a list taller than the area it is shown in. Offsets are in pixels from the top.
    /// </summary>
    public static class ScrollMath
    {
        /// <summary>
        /// How far the content can scroll: 0 when it fits.
        /// </summary>
        public static float GetMaxScroll(float contentHeight, float viewHeight)
        {
            return Math.Max(0f, contentHeight - viewHeight);
        }

        /// <summary>
        /// The offset pulled back into 0 to GetMaxScroll.
        /// </summary>
        public static float ClampOffset(float offset, float contentHeight, float viewHeight)
        {
            return Math.Clamp(offset, 0f, GetMaxScroll(contentHeight, viewHeight));
        }

        /// <summary>
        /// The scrollbar thumb's height for a track viewHeight tall: the share of the content on show, at least minHeight
        /// (and never more than the track).
        /// </summary>
        public static float GetThumbHeight(float contentHeight, float viewHeight, float minHeight)
        {
            if (contentHeight <= viewHeight || contentHeight <= 0f)
                return viewHeight;

            return Math.Min(viewHeight, Math.Max(minHeight, viewHeight * viewHeight / contentHeight));
        }

        /// <summary>
        /// The thumb's top, measured from the track's top.
        /// </summary>
        public static float GetThumbOffset(float offset, float contentHeight, float viewHeight, float thumbHeight)
        {
            var maxScroll = GetMaxScroll(contentHeight, viewHeight);
            return maxScroll > 0f ? (viewHeight - thumbHeight) * Math.Clamp(offset / maxScroll, 0f, 1f) : 0f;
        }

        /// <summary>
        /// The offset for a scrollbar dragged so the thumb's centre is at y (measured from the track's top).
        /// </summary>
        public static float GetOffsetFromThumb(float y, float contentHeight, float viewHeight, float thumbHeight)
        {
            var travel = viewHeight - thumbHeight;
            var fraction = travel > 0f ? Math.Clamp((y - thumbHeight / 2f) / travel, 0f, 1f) : 0f;
            return fraction * GetMaxScroll(contentHeight, viewHeight);
        }
    }
}
