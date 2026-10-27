using EatMeLibrary;

namespace EatMeTests
{
    public class StateGradeTests
    {
        private const float TOLERANCE = 0.0001f;
        private const int PULSE_MS = 1000;

        private static BoardGrade Grade(bool powered = false, bool stunned = false, float hunger = 0f, eTeam? winner = null,
            int sinceVictoryMs = 0, int timeMs = 0)
        {
            return StateGrade.GetGrade(powered, stunned, hunger, winner, eTeam.Blue, TimeSpan.FromMilliseconds(sinceVictoryMs),
                TimeSpan.FromMilliseconds(timeMs), PULSE_MS);
        }

        [Test]
        public void Calm_ChangesNothing()
        {
            Assert.That(Grade().IsNone, Is.True);
            var (r, g, b) = StateGrade.ApplyGrade(0.2f, 0.5f, 0.9f, BoardGrade.None);
            Assert.That(r, Is.EqualTo(0.2f).Within(TOLERANCE));
            Assert.That(g, Is.EqualTo(0.5f).Within(TOLERANCE));
            Assert.That(b, Is.EqualTo(0.9f).Within(TOLERANCE));
        }

        [Test]
        public void PowerPill_PulsesCoolAndColorful()
        {
            Assert.That(Grade(powered: true).IsNone, Is.True, "the pulse starts at 0, like the pac's");

            var peak = Grade(powered: true, timeMs: PULSE_MS / 2);
            Assert.That(peak.Saturation, Is.EqualTo(StateGrade.POWER_SATURATION).Within(TOLERANCE));
            Assert.That(peak.TintB, Is.GreaterThan(peak.TintR));
        }

        [Test]
        public void Stunned_IsGreyAndWobbles()
        {
            var grade = Grade(stunned: true);
            Assert.That(grade.Saturation, Is.EqualTo(StateGrade.STUN_SATURATION));
            Assert.That(grade.WobblePixels, Is.EqualTo(StateGrade.STUN_WOBBLE_PIXELS));
        }

        [Test]
        public void Starving_RedEdgesGrowAsTheMeterFills()
        {
            var peak = TimeSpan.FromMilliseconds(StateGrade.STARVE_PULSE_MS / 2);
            Assert.That(StateGrade.GetStarveEdge(StateGrade.STARVE_START_FILL, peak), Is.EqualTo(0f));
            Assert.That(StateGrade.GetStarveEdge(0.75f, peak), Is.GreaterThan(0f).And.LessThan(StateGrade.STARVE_EDGE_MAX));
            Assert.That(StateGrade.GetStarveEdge(1f, peak), Is.EqualTo(StateGrade.STARVE_EDGE_MAX).Within(TOLERANCE));
            Assert.That(StateGrade.GetStarveEdge(1f, TimeSpan.Zero), Is.EqualTo(0f), "the pulse's low point");
        }

        [Test]
        public void Victory_FlashesThenWarms_DefeatCools()
        {
            Assert.That(Grade(winner: eTeam.Blue).Flash, Is.EqualTo(1f));

            var won = Grade(winner: eTeam.Blue, sinceVictoryMs: StateGrade.FADE_MS);
            Assert.That(won.Flash, Is.EqualTo(0f));
            Assert.That(won.TintR, Is.EqualTo(StateGrade.VICTORY_TINT_R).Within(TOLERANCE));

            var lost = Grade(winner: eTeam.Red, sinceVictoryMs: StateGrade.FADE_MS);
            Assert.That(lost.TintB, Is.EqualTo(StateGrade.DEFEAT_TINT_B).Within(TOLERANCE));
            Assert.That(lost.Saturation, Is.EqualTo(StateGrade.DEFEAT_SATURATION).Within(TOLERANCE));
        }

        [Test]
        public void ApplyGrade_GreyscaleAndFlash()
        {
            var grey = BoardGrade.None;
            grey.Saturation = 0f;
            var (r, g, b) = StateGrade.ApplyGrade(1f, 0f, 0f, grey);
            Assert.That(r, Is.EqualTo(0.299f).Within(TOLERANCE));
            Assert.That(g, Is.EqualTo(0.299f).Within(TOLERANCE));
            Assert.That(b, Is.EqualTo(0.299f).Within(TOLERANCE));

            var flash = BoardGrade.None;
            flash.Flash = 1f;
            Assert.That(StateGrade.ApplyGrade(0.1f, 0.2f, 0.3f, flash), Is.EqualTo((1f, 1f, 1f)));
        }

        [Test]
        public void GetEdgeWeight_NothingInTheMiddleAllAtTheEdge()
        {
            Assert.That(StateGrade.GetEdgeWeight(0.5f, 0.5f), Is.EqualTo(0f));
            Assert.That(StateGrade.GetEdgeWeight(0f, 0.5f), Is.EqualTo(1f));
            Assert.That(StateGrade.GetEdgeWeight(0.5f, 1f), Is.EqualTo(1f));
        }
    }
}
