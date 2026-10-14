using EatMeLibrary;

namespace EatMeTests
{
    public class CameraMathTests
    {
        [Test]
        public void GetScreenOffset_CentresCamera()
        {
            Assert.That(CameraMath.GetScreenOffset(100f, 800f), Is.EqualTo(300f));
        }

        [Test]
        public void GetScreenOffset_RoundsToWholePixels()
        {
            Assert.That(CameraMath.GetScreenOffset(100.3f, 800f), Is.EqualTo(300f));
            Assert.That(CameraMath.GetScreenOffset(100.7f, 800f), Is.EqualTo(299f));
            Assert.That(CameraMath.GetScreenOffset(100f, 801f), Is.EqualTo(300f));
        }
    }
}
