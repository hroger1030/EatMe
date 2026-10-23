using EatMeLibrary;

namespace EatMeTests
{
    public class RainbowTests
    {
        [Test]
        public void Level32_IsTheOnlyRainbowLevel()
        {
            for (var level = 1; level <= Levels.Count; level++)
                Assert.That(Levels.GetLevel(level).IsRainbow, Is.EqualTo(level == 32), $"Level {level}");
        }

        [Test]
        public void GetGradientColor_HitsEachColorAndWraps()
        {
            for (var i = 0; i < Rainbow.COLORS.Length; i++)
                Assert.That(Rainbow.GetGradientColor(i / 7f), Is.EqualTo(Rainbow.COLORS[i]));

            Assert.That(Rainbow.GetGradientColor(1f), Is.EqualTo(Rainbow.COLORS[0]));
            Assert.That(Rainbow.GetGradientColor(-1f / 7f), Is.EqualTo(Rainbow.COLORS[6]));
        }

        [Test]
        public void GetGradientColor_BlendsBetweenColors()
        {
            // halfway from red (255, 0, 0) to orange (255, 127, 0)
            var color = Rainbow.GetGradientColor(0.5f / 7f);

            Assert.That(color.R, Is.EqualTo(255));
            Assert.That(color.G, Is.EqualTo(64).Within(1));
            Assert.That(color.B, Is.EqualTo(0));
        }

        [Test]
        public void GetFlowPhase_StartsAtZeroAndWraps()
        {
            var oneSecond = TimeSpan.FromSeconds(1);

            Assert.That(Rainbow.GetFlowPhase(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(Rainbow.GetFlowPhase(oneSecond), Is.EqualTo(Rainbow.FLOW_CELLS_PER_SECOND / Rainbow.CYCLE_CELLS).Within(0.0001f));

            var oneRound = TimeSpan.FromSeconds(Rainbow.CYCLE_CELLS / Rainbow.FLOW_CELLS_PER_SECOND);
            Assert.That(Rainbow.GetFlowPhase(oneRound + oneSecond), Is.EqualTo(Rainbow.GetFlowPhase(oneSecond)).Within(0.0001f));
            Assert.That(Rainbow.GetFlowPhase(TimeSpan.FromSeconds(1234.5)), Is.InRange(0f, 1f));
        }

        [Test]
        public void GetShade_FollowsTheDiagonal()
        {
            Assert.That(Rainbow.GetShade(0f, 0f, TimeSpan.Zero), Is.EqualTo(Rainbow.COLORS[0]));
            Assert.That(Rainbow.GetShade(1f, 0f, TimeSpan.Zero), Is.EqualTo(Rainbow.COLORS[1]));
            Assert.That(Rainbow.GetShade(0f, 1f, TimeSpan.Zero), Is.EqualTo(Rainbow.COLORS[1]));
            Assert.That(Rainbow.GetShade(5.5f, 2f, TimeSpan.Zero), Is.EqualTo(Rainbow.GetShade(2f, 5.5f, TimeSpan.Zero)));
            Assert.That(Rainbow.GetShade(3f + Rainbow.CYCLE_CELLS, 0f, TimeSpan.Zero), Is.EqualTo(Rainbow.COLORS[3]));
        }

        [Test]
        public void GetShade_FlowsTowardsTheBottomRight()
        {
            // after one second each shade has moved FLOW_CELLS_PER_SECOND further along the diagonal
            var later = Rainbow.GetShade(Rainbow.FLOW_CELLS_PER_SECOND, 0f, TimeSpan.FromSeconds(1));

            Assert.That(later, Is.EqualTo(Rainbow.GetShade(0f, 0f, TimeSpan.Zero)));
            Assert.That(Rainbow.GetShade(0f, 0f, TimeSpan.FromSeconds(1)), Is.Not.EqualTo(Rainbow.GetShade(0f, 0f, TimeSpan.Zero)));
        }
    }
}
