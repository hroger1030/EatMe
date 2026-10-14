using EatMeLibrary;

namespace EatMeTests
{
    public class ScrollerMathTests
    {
        [Test]
        public void GetTextX_StartsAtRightEdge()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, 0, 100f), Is.EqualTo(1000));
        }

        [Test]
        public void GetTextX_MovesLeftOverTime()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, 2.5, 100f), Is.EqualTo(750));
        }

        [Test]
        public void GetTextX_FullyOffLeftJustBeforeWrapping()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, 13.99, 100f), Is.EqualTo(-399));
        }

        [Test]
        public void GetTextX_WrapsBackToRightEdge()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, 14, 100f), Is.EqualTo(1000));
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, 16.5, 100f), Is.EqualTo(750));
        }

        [Test]
        public void GetTextX_WholePixels()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, 0.015, 100f), Is.EqualTo(998));
        }
    }
}
