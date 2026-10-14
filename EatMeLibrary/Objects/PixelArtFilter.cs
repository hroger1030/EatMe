using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The sharp bilinear filter the client's PixelArt shader uses on the game board: each texel is drawn as a solid
    /// block, and only the last half screen pixel either side of a texel edge blends with its neighbour, so scaled,
    /// rotated and sub-pixel sprites get smooth edges without going blurry. GetSampleTexel mirrors the shader for
    /// one axis so it can be tested.
    /// </summary>
    public static class PixelArtFilter
    {
        /// <summary>
        /// Keeps the ramp from dividing by zero when a texel covers a huge number of pixels.
        /// </summary>
        public const float MIN_RAMP = 0.0001f;

        /// <summary>
        /// Where to sample, in texels, for a point at texel coordinate texel when one screen pixel covers
        /// texelsPerPixel texels. Points well inside a texel move to its centre; points within half a pixel of an edge
        /// move linearly from the centre out to the edge, so a linear sampler blends there. At one texel per pixel or
        /// more (no magnification) the point is left alone, which is plain bilinear.
        /// </summary>
        public static float GetSampleTexel(float texel, float texelsPerPixel)
        {
            var region = Math.Clamp(0.5f - 0.5f * texelsPerPixel, 0f, 0.5f);
            var texelFloor = MathF.Floor(texel);
            var fromCentre = texel - texelFloor - 0.5f;
            var ramp = MathF.Max(0.5f - region, MIN_RAMP);
            var offset = (fromCentre - Math.Clamp(fromCentre, -region, region)) * 0.5f / ramp;

            return texelFloor + 0.5f + offset;
        }
    }
}
