using EatMeLibrary;

namespace EatMeTests
{
    public class ScrollerMathTests
    {
        [Test]
        public void GetTextX_StartsAtRightEdge()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, TimeSpan.Zero, 100f), Is.EqualTo(1000));
        }

        [Test]
        public void GetTextX_MovesLeftOverTime()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, TimeSpan.FromMilliseconds(2500), 100f), Is.EqualTo(750));
        }

        [Test]
        public void GetTextX_FullyOffLeftJustBeforeWrapping()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, TimeSpan.FromMilliseconds(13990), 100f), Is.EqualTo(-399));
        }

        [Test]
        public void GetTextX_WrapsBackToRightEdge()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, TimeSpan.FromSeconds(14), 100f), Is.EqualTo(1000));
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, TimeSpan.FromMilliseconds(16500), 100f), Is.EqualTo(750));
        }

        [Test]
        public void GetTextX_WholePixels()
        {
            Assert.That(ScrollerMath.GetTextX(1000f, 400f, TimeSpan.FromMilliseconds(15), 100f), Is.EqualTo(998));
        }
    }
}
