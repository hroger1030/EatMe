using EatMeLibrary;

namespace EatMeTests
{
    public class ColorRampTests
    {
        private static int[] Rgb(FastColor color)
        {
            return new[] { (int)color.R, color.G, color.B };
        }

        [Test]
        public void GetHeatColor_StopsAreExact()
        {
            Assert.That(Rgb(ColorRamp.GetHeatColor(0f)), Is.EqualTo(new[] { 0, 255, 0 }));
            Assert.That(Rgb(ColorRamp.GetHeatColor(1f / 3f)), Is.EqualTo(new[] { 255, 255, 0 }));
            Assert.That(Rgb(ColorRamp.GetHeatColor(2f / 3f)), Is.EqualTo(new[] { 255, 165, 0 }));
            Assert.That(Rgb(ColorRamp.GetHeatColor(1f)), Is.EqualTo(new[] { 255, 0, 0 }));
        }

        [Test]
        public void GetHeatColor_BlendsBetweenStops()
        {
            Assert.That(Rgb(ColorRamp.GetHeatColor(1f / 6f)), Is.EqualTo(new[] { 128, 255, 0 }));
        }

        [Test]
        public void GetHeatColor_ClampsOutOfRange()
        {
            Assert.That(Rgb(ColorRamp.GetHeatColor(-1f)), Is.EqualTo(Rgb(ColorRamp.GetHeatColor(0f))));
            Assert.That(Rgb(ColorRamp.GetHeatColor(2f)), Is.EqualTo(Rgb(ColorRamp.GetHeatColor(1f))));
        }

        [Test]
        public void GetHeatColor_IsSmooth()
        {
            const int STEPS = 300;
            var previous = ColorRamp.GetHeatColor(0f);
            for (var i = 1; i <= STEPS; i++)
            {
                var color = ColorRamp.GetHeatColor(i / (float)STEPS);
                Assert.That(Math.Abs(color.R - previous.R), Is.LessThanOrEqualTo(3));
                Assert.That(Math.Abs(color.G - previous.G), Is.LessThanOrEqualTo(3));
                Assert.That(Math.Abs(color.B - previous.B), Is.LessThanOrEqualTo(3));
                previous = color;
            }
        }

        [Test]
        public void GetPingColor_GoodPingIsGreen()
        {
            Assert.That(Rgb(ColorRamp.GetPingColor(0)), Is.EqualTo(new[] { 0, 255, 0 }));
            Assert.That(Rgb(ColorRamp.GetPingColor(ColorRamp.PING_GOOD_MS)), Is.EqualTo(new[] { 0, 255, 0 }));
        }

        [Test]
        public void GetPingColor_BadPingIsRed()
        {
            Assert.That(Rgb(ColorRamp.GetPingColor(ColorRamp.PING_BAD_MS)), Is.EqualTo(new[] { 255, 0, 0 }));
            Assert.That(Rgb(ColorRamp.GetPingColor(5000)), Is.EqualTo(new[] { 255, 0, 0 }));
        }

        [Test]
        public void GetPingColor_MapsRangeOntoHeatRamp()
        {
            Assert.That(Rgb(ColorRamp.GetPingColor(500)), Is.EqualTo(Rgb(ColorRamp.GetHeatColor(0.5f))));
        }
    }
}
