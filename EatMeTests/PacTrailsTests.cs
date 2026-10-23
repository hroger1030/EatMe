using EatMeLibrary;

namespace EatMeTests
{
    public class PacTrailsTests
    {
        private static PacState Pac(int id, float x, float y, bool powered = true, bool alive = true, ePowerUp effect = ePowerUp.Solarian)
        {
            return new PacState(id, eTeam.Blue, x, y, 0f, eFacing.Right, alive, false, false, powerUpEffect: powered ? effect : ePowerUp.None);
        }

        [Test]
        public void Update_PowerPillPacHasNoTrail()
        {
            var trails = new PacTrails();
            trails.Update([Pac(1, 1.5f, 1.5f, effect: ePowerUp.PowerPill)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            Assert.That(trails.GetTrail(1), Is.Empty);
        }

        [Test]
        public void Update_WarpSpeedPacGetsTrailNewestFirst()
        {
            var trails = new PacTrails();

            for (var i = 0; i < 3; i++)
                trails.Update([Pac(1, 1.5f + i * 0.2f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            var trail = trails.GetTrail(1);
            Assert.That(trail.Count, Is.EqualTo(3));
            Assert.That(trail[0].X, Is.EqualTo(1.9f).Within(0.0001f));
            Assert.That(trail[2].X, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void Update_KeepsAtMostTrailLength()
        {
            var trails = new PacTrails();

            for (var i = 0; i < PacTrails.TRAIL_LENGTH + 3; i++)
                trails.Update([Pac(1, 1.5f + i * 0.1f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            Assert.That(trails.GetTrail(1).Count, Is.EqualTo(PacTrails.TRAIL_LENGTH));
        }

        [Test]
        public void Update_UnpoweredPacHasNoTrail()
        {
            var trails = new PacTrails();
            trails.Update([Pac(1, 1.5f, 1.5f, powered: false)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            Assert.That(trails.GetTrail(1), Is.Empty);
        }

        [Test]
        public void Update_LosingPowerOrDyingDropsTrail()
        {
            var trails = new PacTrails();
            trails.Update([Pac(1, 1.5f, 1.5f), Pac(2, 5.5f, 5.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            trails.Update([Pac(1, 1.6f, 1.5f, powered: false), Pac(2, 5.6f, 5.5f, alive: false)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            Assert.That(trails.GetTrail(1), Is.Empty);
            Assert.That(trails.GetTrail(2), Is.Empty);
        }

        [Test]
        public void Update_PacLeavingSnapshotDropsTrail()
        {
            var trails = new PacTrails();
            trails.Update([Pac(1, 1.5f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));
            trails.Update([], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            Assert.That(trails.GetTrail(1), Is.Empty);
        }

        [Test]
        public void Update_JumpStartsTrailAgain()
        {
            var trails = new PacTrails();
            trails.Update([Pac(1, 1.5f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));
            trails.Update([Pac(1, 1.7f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            // through a warp tunnel to the far side
            trails.Update([Pac(1, 30.5f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));

            var trail = trails.GetTrail(1);
            Assert.That(trail.Count, Is.EqualTo(1));
            Assert.That(trail[0].X, Is.EqualTo(30.5f));
        }

        [Test]
        public void Update_SamplesOnlyEverySampleInterval()
        {
            var trails = new PacTrails();
            trails.Update([Pac(1, 1.5f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS));
            trails.Update([Pac(1, 1.55f, 1.5f)], TimeSpan.FromMilliseconds(PacTrails.TRAIL_SAMPLE_MS * 0.25));

            Assert.That(trails.GetTrail(1).Count, Is.EqualTo(1));
        }

        [Test]
        public void GetAlpha_FadesWithAge()
        {
            for (var i = 1; i < PacTrails.TRAIL_LENGTH; i++)
                Assert.That(PacTrails.GetAlpha(i), Is.LessThan(PacTrails.GetAlpha(i - 1)));

            Assert.That(PacTrails.GetAlpha(0), Is.LessThanOrEqualTo(PacTrails.TRAIL_START_ALPHA));
            Assert.That(PacTrails.GetAlpha(PacTrails.TRAIL_LENGTH - 1), Is.GreaterThan(0f));
            Assert.That(PacTrails.GetAlpha(PacTrails.TRAIL_LENGTH), Is.EqualTo(0f));
        }

        [Test]
        public void IsJump_OnlyOverMaxStep()
        {
            Assert.That(PacTrails.IsJump(1f, 1f, 1.5f, 1f), Is.False);
            Assert.That(PacTrails.IsJump(1f, 1f, 1f, 3f), Is.True);
        }
    }
}
