using System;
using System.Linq;
using EatMeLibrary;

namespace EatMeTests
{
    public class DisplayAdjustTests
    {
        private const float TOLERANCE = 0.0001f;

        private static float[] Adjust(float r, float g, float b, float brightness)
        {
            var (red, green, blue) = DisplayAdjust.AdjustColor(r, g, b, brightness);
            return new[] { red, green, blue };
        }

        [Test]
        public void ClampBrightness_KeepsValuesInRange()
        {
            Assert.That(DisplayAdjust.ClampBrightness(75f), Is.EqualTo(75f));
            Assert.That(DisplayAdjust.ClampBrightness(150f), Is.EqualTo(DisplayAdjust.BRIGHTNESS_MAX));
            Assert.That(DisplayAdjust.ClampBrightness(10f), Is.EqualTo(DisplayAdjust.BRIGHTNESS_MIN));
        }

        [Test]
        public void ClampBrightness_NotANumberIsDefault()
        {
            Assert.That(DisplayAdjust.ClampBrightness(float.NaN), Is.EqualTo(DisplayAdjust.BRIGHTNESS_DEFAULT));
            Assert.That(DisplayAdjust.ClampBrightness(float.PositiveInfinity), Is.EqualTo(DisplayAdjust.BRIGHTNESS_DEFAULT));
        }

        [Test]
        public void GetMultiplier_IsPercentAsFraction()
        {
            Assert.That(DisplayAdjust.GetMultiplier(100f), Is.EqualTo(1f).Within(TOLERANCE));
            Assert.That(DisplayAdjust.GetMultiplier(50f), Is.EqualTo(0.5f).Within(TOLERANCE));
        }

        [Test]
        public void AdjustColor_DefaultLeavesColorUnchanged()
        {
            Assert.That(Adjust(0.2f, 0.6f, 0.9f, DisplayAdjust.BRIGHTNESS_DEFAULT), Is.EqualTo(new[] { 0.2f, 0.6f, 0.9f }).Within(TOLERANCE));
        }

        [Test]
        public void AdjustColor_DimsColorsAndKeepsBlack()
        {
            Assert.That(Adjust(1f, 1f, 0f, 50f), Is.EqualTo(new[] { 0.5f, 0.5f, 0f }).Within(TOLERANCE));
            Assert.That(Adjust(0f, 0f, 0f, 50f), Is.EqualTo(new[] { 0f, 0f, 0f }).Within(TOLERANCE));
        }

        [Test]
        public void ClampBloom_KeepsValuesInRange()
        {
            Assert.That(DisplayAdjust.ClampBloom(40f), Is.EqualTo(40f));
            Assert.That(DisplayAdjust.ClampBloom(150f), Is.EqualTo(DisplayAdjust.BLOOM_MAX));
            Assert.That(DisplayAdjust.ClampBloom(-10f), Is.EqualTo(DisplayAdjust.BLOOM_MIN));
            Assert.That(DisplayAdjust.ClampBloom(float.NaN), Is.EqualTo(DisplayAdjust.BLOOM_DEFAULT));
        }

        [Test]
        public void GetBloomIntensity_IsPercentAsFraction()
        {
            Assert.That(DisplayAdjust.GetBloomIntensity(0f), Is.EqualTo(0f));
            Assert.That(DisplayAdjust.GetBloomIntensity(50f), Is.EqualTo(0.5f).Within(TOLERANCE));
            Assert.That(DisplayAdjust.GetBloomIntensity(100f), Is.EqualTo(1f).Within(TOLERANCE));
        }

        [Test]
        public void BloomDefault_IsOff()
        {
            Assert.That(DisplayAdjust.GetBloomIntensity(DisplayAdjust.BLOOM_DEFAULT), Is.EqualTo(0f));
        }

        [Test]
        public void BloomGains_AreOffAtZeroAndAddUpToTheMaxGain()
        {
            Assert.That(DisplayAdjust.GetBloomTightGain(0f), Is.EqualTo(0f));
            Assert.That(DisplayAdjust.GetBloomWideGain(0f), Is.EqualTo(0f));
            Assert.That(DisplayAdjust.GetBloomTightGain(100f) + DisplayAdjust.GetBloomWideGain(100f), Is.EqualTo(DisplayAdjust.BLOOM_MAX_GAIN).Within(TOLERANCE));
            Assert.That(DisplayAdjust.GetBloomWideGain(50f), Is.EqualTo(DisplayAdjust.GetBloomWideGain(100f) * 0.5f).Within(TOLERANCE));
        }

        [Test]
        public void GlowSources_EveryStrengthIsInRangeAndGlows()
        {
            foreach (eGlowSource source in Enum.GetValues(typeof(eGlowSource)))
                Assert.That(GlowSources.GetStrength(source), Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), source.ToString());
        }

        [Test]
        public void GetBlurWeights_AddUpToOneAndFallOff()
        {
            var weights = DisplayAdjust.GetBlurWeights();
            Assert.That(weights.Length, Is.EqualTo(DisplayAdjust.BLOOM_BLUR_TAPS));
            Assert.That(weights[0] + 2f * weights.Skip(1).Sum(), Is.EqualTo(1f).Within(TOLERANCE));

            for (var i = 1; i < weights.Length; i++)
                Assert.That(weights[i], Is.LessThan(weights[i - 1]));
        }

        [Test]
        public void GetWindowedSize_IsAShareOfTheDesktop()
        {
            var (width, height) = DisplayAdjust.GetWindowedSize(1920, 1080);
            Assert.That(width, Is.EqualTo(1536));
            Assert.That(height, Is.EqualTo(864));
        }

        [Test]
        public void GetWindowedSize_KeepsTheAspectRatio()
        {
            var (width, height) = DisplayAdjust.GetWindowedSize(2560, 1440);
            Assert.That(width * 9, Is.EqualTo(height * 16));
        }

        [Test]
        public void GetWindowedSize_NeverZero()
        {
            var (width, height) = DisplayAdjust.GetWindowedSize(0, 0);
            Assert.That(width, Is.EqualTo(1));
            Assert.That(height, Is.EqualTo(1));
        }
    }
}
