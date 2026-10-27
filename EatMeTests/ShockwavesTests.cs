using EatMeLibrary;

namespace EatMeTests
{
    public class ShockwavesTests
    {
        [Test]
        public void GetRadiusCells_GrowsToTheStandardRadius()
        {
            Assert.That(Shockwaves.GetRadiusCells(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(Shockwaves.GetRadiusCells(TimeSpan.FromMilliseconds(Shockwaves.DURATION_MS / 2)), Is.GreaterThan(PowerUpEffects.EFFECT_RADIUS * 0.5f));
            Assert.That(Shockwaves.GetRadiusCells(TimeSpan.FromMilliseconds(Shockwaves.DURATION_MS)), Is.EqualTo(PowerUpEffects.EFFECT_RADIUS));
        }

        [Test]
        public void GetAmplitude_FadesToNothing()
        {
            Assert.That(Shockwaves.GetAmplitude(TimeSpan.Zero), Is.EqualTo(Shockwaves.AMPLITUDE_PIXELS));
            Assert.That(Shockwaves.GetAmplitude(TimeSpan.FromMilliseconds(Shockwaves.DURATION_MS)), Is.EqualTo(0f));
        }

        [Test]
        public void TravelsWithTheVisibleRing()
        {
            Assert.That(Shockwaves.DURATION_MS, Is.EqualTo(ShockRings.DURATION_MS));

            for (var ms = 0; ms <= Shockwaves.DURATION_MS; ms += 50)
            {
                var since = TimeSpan.FromMilliseconds(ms);
                Assert.That(Shockwaves.GetRadiusCells(since), Is.EqualTo(ShockRings.GetRadiusCells(since)), ms.ToString());
            }
        }

        [Test]
        public void GetOffset_OnlyMovesPixelsInTheRing()
        {
            Assert.That(Shockwaves.GetOffset(100f, 100f, 40f, 10f), Is.EqualTo(0f).Within(0.001f), "the ring's middle line");
            Assert.That(Shockwaves.GetOffset(130f, 100f, 40f, 10f), Is.EqualTo(0f), "outside the ring");
            Assert.That(Shockwaves.GetOffset(70f, 100f, 40f, 10f), Is.EqualTo(0f), "inside the ring");
            Assert.That(Shockwaves.GetOffset(110f, 100f, 40f, 10f), Is.GreaterThan(0f), "ahead of the middle, pushed outward");
            Assert.That(Shockwaves.GetOffset(90f, 100f, 40f, 10f), Is.LessThan(0f), "behind it, pulled in");
        }

        [Test]
        public void Add_DropsFinishedRingsAndKeepsAtMostTheMax()
        {
            var waves = new Shockwaves();

            for (var i = 0; i < Shockwaves.MAX_SHOCKWAVES + 2; i++)
                waves.Add(i * 10f, 0f, i * Shockwaves.MIN_GAP_MS / 10);

            Assert.That(waves.Waves.Count, Is.EqualTo(Shockwaves.MAX_SHOCKWAVES));
            Assert.That(waves.Waves[0].X, Is.EqualTo(20f), "the oldest made way");

            waves.Update(10000);
            Assert.That(waves.Waves, Is.Empty);
        }

        [Test]
        public void Add_LeavesOutARingRightNextToANewOne()
        {
            var waves = new Shockwaves();
            waves.Add(5f, 5f, 0);
            waves.Add(5.5f, 5f, Shockwaves.MIN_GAP_MS - 1);
            Assert.That(waves.Waves.Count, Is.EqualTo(1));

            waves.Add(5.5f, 5f, Shockwaves.MIN_GAP_MS);
            Assert.That(waves.Waves.Count, Is.EqualTo(2));
        }
    }
}
