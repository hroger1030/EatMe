using EatMeLibrary;

namespace EatMeTests
{
    public class PixelArtFilterTests
    {
        private const float TOLERANCE = 0.0001f;

        [Test]
        public void GetSampleTexel_InsideTexelSnapsToCentre()
        {
            // 4 pixels per texel: the inner 3 pixels' worth of each texel is solid
            Assert.That(PixelArtFilter.GetSampleTexel(2.3f, 0.25f), Is.EqualTo(2.5f).Within(TOLERANCE));
            Assert.That(PixelArtFilter.GetSampleTexel(2.5f, 0.25f), Is.EqualTo(2.5f).Within(TOLERANCE));
            Assert.That(PixelArtFilter.GetSampleTexel(2.8f, 0.25f), Is.EqualTo(2.5f).Within(TOLERANCE));
        }

        [Test]
        public void GetSampleTexel_EdgeBlendsHalfway()
        {
            // right on a texel edge the sample stays on the edge, so the linear sampler mixes both texels evenly
            Assert.That(PixelArtFilter.GetSampleTexel(3f, 0.25f), Is.EqualTo(3f).Within(TOLERANCE));
        }

        [Test]
        public void GetSampleTexel_RampsAcrossLastHalfPixel()
        {
            // region is 0.375, so a point 0.4375 from the centre is half way along the ramp
            Assert.That(PixelArtFilter.GetSampleTexel(2.9375f, 0.25f), Is.EqualTo(2.75f).Within(TOLERANCE));
        }

        [Test]
        public void GetSampleTexel_NoMagnificationIsPlainBilinear()
        {
            Assert.That(PixelArtFilter.GetSampleTexel(2.3f, 1f), Is.EqualTo(2.3f).Within(TOLERANCE));
            Assert.That(PixelArtFilter.GetSampleTexel(2.3f, 2f), Is.EqualTo(2.3f).Within(TOLERANCE));
        }
    }
}
