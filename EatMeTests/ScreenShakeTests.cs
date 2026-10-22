using EatMeLibrary;

namespace EatMeTests
{
    public class ScreenShakeTests
    {
        private const float AMPLITUDE = 18f;
        private const float DURATION = 0.6f;

        [Test]
        public void GetOffset_StartsWithAFullBounce()
        {
            var (x, y) = ScreenShake.GetOffset(0f, AMPLITUDE, DURATION);

            Assert.That(x, Is.EqualTo(0f));
            Assert.That(y, Is.EqualTo(AMPLITUDE));
        }

        [Test]
        public void GetOffset_IsZeroBeforeStartAndAfterDuration()
        {
            Assert.That(ScreenShake.GetOffset(-0.1f, AMPLITUDE, DURATION), Is.EqualTo((0f, 0f)));
            Assert.That(ScreenShake.GetOffset(DURATION, AMPLITUDE, DURATION), Is.EqualTo((0f, 0f)));
            Assert.That(ScreenShake.GetOffset(float.MaxValue, AMPLITUDE, DURATION), Is.EqualTo((0f, 0f)));
        }

        [Test]
        public void GetOffset_DiesAway_InWholePixels()
        {
            var previousPeak = float.MaxValue;
            var period = 1f / ScreenShake.BOUNCES_PER_SECOND;

            // the peaks of each bounce get smaller
            for (var t = 0f; t < DURATION; t += period)
            {
                var (x, y) = ScreenShake.GetOffset(t, AMPLITUDE, DURATION);
                Assert.That(MathF.Abs(y), Is.LessThanOrEqualTo(previousPeak));
                Assert.That(x, Is.EqualTo(MathF.Round(x)));
                Assert.That(y, Is.EqualTo(MathF.Round(y)));
                previousPeak = MathF.Abs(y);
            }

            Assert.That(previousPeak, Is.LessThan(AMPLITUDE * 0.2f));
        }

        [Test]
        public void GetHeight_DiesAwayInProportionToDuration()
        {
            Assert.That(ScreenShake.GetHeight(0f, 10f, 2f), Is.EqualTo(10f));
            Assert.That(ScreenShake.GetHeight(0.5f, 10f, 2f), Is.EqualTo(ScreenShake.GetHeight(0.25f, 10f, 1f)).Within(0.0001f), "twice as long, twice as slow");
            Assert.That(ScreenShake.GetHeight(2f, 10f, 2f), Is.EqualTo(0f));
        }

        [Test]
        public void Shake_StartsUpdatesAndEnds()
        {
            var shake = new ScreenShake();
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(shake.GetOffset(), Is.EqualTo((0f, 0f)));

            shake.Start(ScreenShake.BLAST_AMPLITUDE_PIXELS, ScreenShake.BLAST_DURATION_SECONDS);
            Assert.That(shake.IsShaking, Is.True);
            Assert.That(shake.GetOffset().Y, Is.EqualTo(ScreenShake.BLAST_AMPLITUDE_PIXELS));

            shake.Update(ScreenShake.BLAST_DURATION_SECONDS);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(shake.GetOffset(), Is.EqualTo((0f, 0f)));
        }

        [Test]
        public void Start_SmallerShakeDoesntCutOffABiggerOne()
        {
            var shake = new ScreenShake();
            shake.Start(30f, 1f);

            shake.Start(5f, 1f);
            Assert.That(shake.AmplitudePixels, Is.EqualTo(30f), "the big one is still bouncing higher");

            shake.Update(0.9f);
            shake.Start(5f, 1f);
            Assert.That(shake.AmplitudePixels, Is.EqualTo(5f), "the big one has died down below it");
            Assert.That(shake.ElapsedSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void Stop_EndsTheShake()
        {
            var shake = new ScreenShake();
            shake.Start();
            shake.Stop();

            Assert.That(shake.IsShaking, Is.False);
            Assert.That(shake.GetOffset(), Is.EqualTo((0f, 0f)));
        }
    }
}
