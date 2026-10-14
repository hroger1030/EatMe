using EatMeLibrary;
using System;

namespace EatMeTests
{
    public class FlagPointerMathTests
    {
        private const float WIDTH = 800f;
        private const float HEIGHT = 600f;
        private const float RADIUS = 250f;
        private const float TOLERANCE = 0.001f;

        [Test]
        public void IsOnScreen_InsideBoard_IsTrue()
        {
            Assert.That(FlagPointerMath.IsOnScreen(400f, 300f, 24f, WIDTH, HEIGHT), Is.True);
        }

        [Test]
        public void IsOnScreen_PartlyOverEdge_IsTrue()
        {
            Assert.That(FlagPointerMath.IsOnScreen(-20f, 300f, 24f, WIDTH, HEIGHT), Is.True);
            Assert.That(FlagPointerMath.IsOnScreen(400f, HEIGHT + 20f, 24f, WIDTH, HEIGHT), Is.True);
        }

        [Test]
        public void IsOnScreen_WhollyOffBoard_IsFalse()
        {
            Assert.That(FlagPointerMath.IsOnScreen(-24f, 300f, 24f, WIDTH, HEIGHT), Is.False);
            Assert.That(FlagPointerMath.IsOnScreen(WIDTH + 24f, 300f, 24f, WIDTH, HEIGHT), Is.False);
            Assert.That(FlagPointerMath.IsOnScreen(400f, -24f, 24f, WIDTH, HEIGHT), Is.False);
            Assert.That(FlagPointerMath.IsOnScreen(400f, HEIGHT + 24f, 24f, WIDTH, HEIGHT), Is.False);
        }

        [Test]
        public void GetCirclePosition_StraightRight_SitsOnCircle()
        {
            var (x, y) = FlagPointerMath.GetCirclePosition(400f, 300f, 5000f, 300f, RADIUS);

            Assert.That(x, Is.EqualTo(400f + RADIUS).Within(TOLERANCE));
            Assert.That(y, Is.EqualTo(300f).Within(TOLERANCE));
        }

        [Test]
        public void GetCirclePosition_StraightUp_SitsOnCircle()
        {
            var (x, y) = FlagPointerMath.GetCirclePosition(400f, 300f, 400f, -5000f, RADIUS);

            Assert.That(x, Is.EqualTo(400f).Within(TOLERANCE));
            Assert.That(y, Is.EqualTo(300f - RADIUS).Within(TOLERANCE));
        }

        [Test]
        public void GetCirclePosition_Diagonal_StaysOnLineToTarget()
        {
            // 3000 right and 4000 down from (400, 300): a 3-4-5 triangle, so 3/5 and 4/5 of the radius
            var (x, y) = FlagPointerMath.GetCirclePosition(400f, 300f, 3400f, 4300f, RADIUS);

            Assert.That(x, Is.EqualTo(400f + RADIUS * 0.6f).Within(TOLERANCE));
            Assert.That(y, Is.EqualTo(300f + RADIUS * 0.8f).Within(TOLERANCE));
        }

        [Test]
        public void GetCirclePosition_TargetInsideCircle_StillSitsOnCircle()
        {
            var (x, y) = FlagPointerMath.GetCirclePosition(400f, 300f, 390f, 300f, RADIUS);

            Assert.That(x, Is.EqualTo(400f - RADIUS).Within(TOLERANCE));
            Assert.That(y, Is.EqualTo(300f).Within(TOLERANCE));
        }

        [Test]
        public void GetCirclePosition_TargetAtCentre_GivesCentre()
        {
            var (x, y) = FlagPointerMath.GetCirclePosition(400f, 300f, 400f, 300f, RADIUS);

            Assert.That(x, Is.EqualTo(400f));
            Assert.That(y, Is.EqualTo(300f));
        }

        [Test]
        public void GetDistance_IsStraightLineDistance()
        {
            Assert.That(FlagPointerMath.GetDistance(10f, 20f, 13f, 24f), Is.EqualTo(5f).Within(TOLERANCE));
            Assert.That(FlagPointerMath.GetDistance(13f, 24f, 10f, 20f), Is.EqualTo(5f).Within(TOLERANCE));
            Assert.That(FlagPointerMath.GetDistance(7f, 7f, 7f, 7f), Is.EqualTo(0f));
        }

        [Test]
        public void GetAngle_PointsAtTarget()
        {
            Assert.That(FlagPointerMath.GetAngle(0f, 0f, 10f, 0f), Is.EqualTo(0f).Within(TOLERANCE));
            Assert.That(FlagPointerMath.GetAngle(0f, 0f, 0f, 10f), Is.EqualTo(MathF.PI / 2f).Within(TOLERANCE));
            Assert.That(FlagPointerMath.GetAngle(0f, 0f, 0f, -10f), Is.EqualTo(-MathF.PI / 2f).Within(TOLERANCE));
            Assert.That(MathF.Abs(FlagPointerMath.GetAngle(0f, 0f, -10f, 0f)), Is.EqualTo(MathF.PI).Within(TOLERANCE));
        }
    }
}
