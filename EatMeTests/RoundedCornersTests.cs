using EatMeLibrary;

namespace EatMeTests
{
    public class RoundedCornersTests
    {
        [Test]
        public void GetRowInset_EdgeRowIsCutInMost()
        {
            Assert.That(RoundedCorners.GetRowInset(12, 0), Is.EqualTo(9));
        }

        [Test]
        public void GetRowInset_ShrinksTowardsTheMiddle()
        {
            for (var row = 1; row < 12; row++)
                Assert.That(RoundedCorners.GetRowInset(12, row), Is.LessThanOrEqualTo(RoundedCorners.GetRowInset(12, row - 1)));
        }

        [Test]
        public void GetRowInset_LastCurvedRowIsNotCutIn()
        {
            Assert.That(RoundedCorners.GetRowInset(12, 11), Is.EqualTo(0));
        }

        [Test]
        public void GetRowInset_RowsPastTheRadiusAreNotCutIn()
        {
            Assert.That(RoundedCorners.GetRowInset(12, 12), Is.EqualTo(0));
            Assert.That(RoundedCorners.GetRowInset(12, 50), Is.EqualTo(0));
        }

        [Test]
        public void GetRowInset_ZeroRadiusHasNoInset()
        {
            Assert.That(RoundedCorners.GetRowInset(0, 0), Is.EqualTo(0));
        }
    }
}
