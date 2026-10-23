using EatMeLibrary;

namespace EatMeTests
{
    public class ScreenShakeTests
    {
        private const float AMPLITUDE = 18f;
        private static readonly TimeSpan DURATION = TimeSpan.FromMilliseconds(600);

        [Test]
        public void GetOffset_StartsWithAFullBounce()
        {
            var (x, y) = ScreenShake.GetOffset(TimeSpan.Zero, AMPLITUDE, DURATION);

            Assert.That(x, Is.EqualTo(0f));
            Assert.That(y, Is.EqualTo(AMPLITUDE));
        }

        [Test]
        public void GetOffset_IsZeroBeforeStartAndAfterDuration()
        {
            Assert.That(ScreenShake.GetOffset(TimeSpan.FromMilliseconds(-100), AMPLITUDE, DURATION), Is.EqualTo((0f, 0f)));
            Assert.That(ScreenShake.GetOffset(DURATION, AMPLITUDE, DURATION), Is.EqualTo((0f, 0f)));
            Assert.That(ScreenShake.GetOffset(TimeSpan.MaxValue, AMPLITUDE, DURATION), Is.EqualTo((0f, 0f)));
        }

        [Test]
        public void GetOffset_DiesAway_InWholePixels()
        {
            var previousPeak = float.MaxValue;
            var period = TimeSpan.FromSeconds(1.0 / ScreenShake.BOUNCES_PER_SECOND);

            // the peaks of each bounce get smaller
            for (var t = TimeSpan.Zero; t < DURATION; t += period)
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
            var oneSecond = TimeSpan.FromSeconds(1);
            var twoSeconds = TimeSpan.FromSeconds(2);

            Assert.That(ScreenShake.GetHeight(TimeSpan.Zero, 10f, twoSeconds), Is.EqualTo(10f));
            Assert.That(ScreenShake.GetHeight(TimeSpan.FromMilliseconds(500), 10f, twoSeconds), Is.EqualTo(ScreenShake.GetHeight(TimeSpan.FromMilliseconds(250), 10f, oneSecond)).Within(0.0001f), "twice as long, twice as slow");
            Assert.That(ScreenShake.GetHeight(twoSeconds, 10f, twoSeconds), Is.EqualTo(0f));
        }

        [Test]
        public void Shake_StartsUpdatesAndEnds()
        {
            var shake = new ScreenShake();
            var blast = TimeSpan.FromMilliseconds(ScreenShake.BLAST_DURATION_MS);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(shake.GetOffset(), Is.EqualTo((0f, 0f)));

            shake.Start(ScreenShake.BLAST_AMPLITUDE_PIXELS, blast);
            Assert.That(shake.IsShaking, Is.True);
            Assert.That(shake.GetOffset().Y, Is.EqualTo(ScreenShake.BLAST_AMPLITUDE_PIXELS));

            shake.Update(blast);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(shake.GetOffset(), Is.EqualTo((0f, 0f)));
        }

        [Test]
        public void Start_SmallerShakeDoesntCutOffABiggerOne()
        {
            var shake = new ScreenShake();
            var oneSecond = TimeSpan.FromSeconds(1);
            shake.Start(30f, oneSecond);

            shake.Start(5f, oneSecond);
            Assert.That(shake.AmplitudePixels, Is.EqualTo(30f), "the big one is still bouncing higher");

            shake.Update(TimeSpan.FromMilliseconds(900));
            shake.Start(5f, oneSecond);
            Assert.That(shake.AmplitudePixels, Is.EqualTo(5f), "the big one has died down below it");
            Assert.That(shake.Elapsed, Is.EqualTo(TimeSpan.Zero));
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
