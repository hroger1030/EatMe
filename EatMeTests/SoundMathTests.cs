using EatMeLibrary;

namespace EatMeTests
{
    public class SoundMathTests
    {
        private const float WIDTH = 800f;
        private const float HEIGHT = 600f;
        private const float TOLERANCE = 0.001f;

        [TestCase(eGameEvent.FlagCaptured, true)]
        [TestCase(eGameEvent.FlagLost, true)]
        [TestCase(eGameEvent.FlagReturned, true)]
        [TestCase(eGameEvent.AboutToWin, true)]
        [TestCase(eGameEvent.PacEaten, false)]
        [TestCase(eGameEvent.GhostKilled, false)]
        public void IsGlobalEvent_FlagAndWinEventsOnly(eGameEvent eventType, bool expected)
        {
            Assert.That(SoundMath.IsGlobalEvent(eventType), Is.EqualTo(expected));
        }

        [Test]
        public void GetOnScreenVolume_AtCentre_IsFull()
        {
            Assert.That(SoundMath.GetOnScreenVolume(0f, 0f, WIDTH, HEIGHT), Is.EqualTo(1f).Within(TOLERANCE));
        }

        [Test]
        public void GetOnScreenVolume_HalfwayToEdge_IsHalf()
        {
            Assert.That(SoundMath.GetOnScreenVolume(200f, 0f, WIDTH, HEIGHT), Is.EqualTo(0.5f).Within(TOLERANCE));
            Assert.That(SoundMath.GetOnScreenVolume(0f, -150f, WIDTH, HEIGHT), Is.EqualTo(0.5f).Within(TOLERANCE));
        }

        [Test]
        public void GetOnScreenVolume_NearerEdgeWins()
        {
            // 100 of 400 across, 225 of 300 down: three quarters of the way to the bottom edge
            Assert.That(SoundMath.GetOnScreenVolume(100f, 225f, WIDTH, HEIGHT), Is.EqualTo(0.25f).Within(TOLERANCE));
        }

        [Test]
        public void GetOnScreenVolume_AtOrPastEdge_IsSilent()
        {
            Assert.That(SoundMath.GetOnScreenVolume(400f, 0f, WIDTH, HEIGHT), Is.EqualTo(0f));
            Assert.That(SoundMath.GetOnScreenVolume(-5000f, 0f, WIDTH, HEIGHT), Is.EqualTo(0f));
            Assert.That(SoundMath.GetOnScreenVolume(0f, 301f, WIDTH, HEIGHT), Is.EqualTo(0f));
        }

        [Test]
        public void GetOnScreenVolume_NoBoard_IsSilent()
        {
            Assert.That(SoundMath.GetOnScreenVolume(0f, 0f, 0f, HEIGHT), Is.EqualTo(0f));
        }

        [Test]
        public void GetEventVolume_GlobalEventOffScreen_IsFull()
        {
            Assert.That(SoundMath.GetEventVolume(eGameEvent.FlagCaptured, -5000f, 5000f, WIDTH, HEIGHT), Is.EqualTo(1f));
        }

        [Test]
        public void GetEventVolume_LocalEventOffScreen_IsSilent()
        {
            Assert.That(SoundMath.GetEventVolume(eGameEvent.PacEaten, -5000f, 0f, WIDTH, HEIGHT), Is.EqualTo(0f));
        }

        [Test]
        public void GetEventVolume_LocalEventOnScreen_Fades()
        {
            Assert.That(SoundMath.GetEventVolume(eGameEvent.GhostKilled, 200f, 0f, WIDTH, HEIGHT), Is.EqualTo(0.5f).Within(TOLERANCE));
        }
    }
}
