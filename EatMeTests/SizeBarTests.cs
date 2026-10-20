using EatMeLibrary;

namespace EatMeTests
{
    public class SizeBarTests
    {
        [TestCase(0f, 0f)]
        [TestCase(50f, 0.5f)]
        [TestCase(100f, 1f)]
        [TestCase(180f, 1f)]
        [TestCase(-5f, 0f)]
        public void GetFill_ZeroToFullAt100(float size, float expected)
        {
            Assert.That(SizeBar.GetFill(size), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0f, eSizeBarLevel.Normal)]
        [TestCase(100f, eSizeBarLevel.Normal)]
        [TestCase(101f, eSizeBarLevel.Big)]
        [TestCase(150f, eSizeBarLevel.Big)]
        [TestCase(151f, eSizeBarLevel.Huge)]
        public void GetLevel_OrangeOver100_RedOver150(float size, eSizeBarLevel expected)
        {
            Assert.That(SizeBar.GetLevel(size), Is.EqualTo(expected));
        }
    }
}
