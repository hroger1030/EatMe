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
        public void GetBandColor_RunsAlongTheDiagonalAndRepeatsEverySeven()
        {
            Assert.That(Rainbow.GetBandColor(0, 0), Is.EqualTo(Rainbow.COLORS[0]));
            Assert.That(Rainbow.GetBandColor(1, 0), Is.EqualTo(Rainbow.COLORS[1]));
            Assert.That(Rainbow.GetBandColor(0, 1), Is.EqualTo(Rainbow.COLORS[1]));
            Assert.That(Rainbow.GetBandColor(3, 3), Is.EqualTo(Rainbow.COLORS[6]));
            Assert.That(Rainbow.GetBandColor(4, 3), Is.EqualTo(Rainbow.COLORS[0]));
        }

        [Test]
        public void BuildDotColors_UsesBandColors()
        {
            var colors = Rainbow.BuildDotColors(18, 18);

            for (var x = 0; x < 18; x++)
                for (var y = 0; y < 18; y++)
                    Assert.That(colors[x, y], Is.EqualTo(Rainbow.GetBandColor(x, y)));
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
        public void GetWallColor_CellCentreMatchesDotColor()
        {
            // the top right and bottom left quarters sit on the cell's own diagonal
            Assert.That(Rainbow.GetWallColor(2, 1, 1), Is.EqualTo(Rainbow.GetBandColor(2, 1)));
            Assert.That(Rainbow.GetWallColor(2, 1, 3), Is.EqualTo(Rainbow.GetBandColor(2, 1)));
        }

        [Test]
        public void GetWallColor_FollowsTheDiagonal()
        {
            // the bottom right quarter of (0, 0) and the top left quarter of (1, 0) are the same distance along it
            Assert.That(Rainbow.GetWallColor(0, 0, 2), Is.EqualTo(Rainbow.GetWallColor(1, 0, 0)));
            Assert.That(Rainbow.GetWallColor(5, 2, 1), Is.EqualTo(Rainbow.GetWallColor(2, 5, 1)));
        }

        [Test]
        public void GetScrollPhase_ChangesAsTheGhostMovesAndWraps()
        {
            Assert.That(Rainbow.GetScrollPhase(0.5f, 0.5f, 16), Is.EqualTo(8));
            Assert.That(Rainbow.GetScrollPhase(0.75f, 0.5f, 16), Is.EqualTo(10));
            Assert.That(Rainbow.GetScrollPhase(0.5f + Rainbow.GHOST_SCROLL_CELLS, 0.5f, 16), Is.EqualTo(8));
            Assert.That(Rainbow.GetScrollPhase(-0.25f, 0f, 16), Is.InRange(0, 15));
        }

        [Test]
        public void ReplaceWithGradient_ColorsBodyAcrossEachFrame()
        {
            var white = GhostPalette.BODY_COLOR.PackedValue;
            var black = FastColor.FromRgb(0, 0, 0).PackedValue;

            // two frames, each seven pixels wide, one row
            var pixels = new[] { white, white, white, white, white, white, black, white, white, white, white, white, white, black };

            var result = Rainbow.ReplaceWithGradient(pixels, 14, 7, GhostPalette.BODY_COLOR, 0f);

            Assert.That(result[6], Is.EqualTo(black));
            Assert.That(result[13], Is.EqualTo(black));
            Assert.That(result[0], Is.EqualTo(Rainbow.GetGradientColor(0.5f / 7f).PackedValue));
            Assert.That(result[7], Is.EqualTo(result[0]));
            Assert.That(result[1], Is.Not.EqualTo(result[0]));
        }
    }
}
