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

        [Test]
        public void GetOutlineInnerInset_RowsInsideTheEdgeAreSolid()
        {
            Assert.That(RoundedCorners.GetOutlineInnerInset(5, 2, 0), Is.EqualTo(-1));
            Assert.That(RoundedCorners.GetOutlineInnerInset(5, 2, 1), Is.EqualTo(-1));
        }

        [Test]
        public void GetOutlineInnerInset_HollowStartsPastTheOuterInset()
        {
            for (var row = 2; row < 5; row++)
                Assert.That(RoundedCorners.GetOutlineInnerInset(5, 2, row), Is.GreaterThan(RoundedCorners.GetRowInset(5, row)));
        }

        [Test]
        public void GetOutlineInnerInset_StraightSidesAreThicknessWide()
        {
            Assert.That(RoundedCorners.GetOutlineInnerInset(5, 2, 5), Is.EqualTo(2));
            Assert.That(RoundedCorners.GetOutlineInnerInset(5, 2, 40), Is.EqualTo(2));
        }
    }
}
