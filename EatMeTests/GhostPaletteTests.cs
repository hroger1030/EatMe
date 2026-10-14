using EatMeLibrary;

namespace EatMeTests
{
    public class GhostPaletteTests
    {
        private static readonly FastColor RED = FastColor.FromRgb(255, 0, 0);
        private static readonly FastColor BLACK = FastColor.FromRgb(0, 0, 0);
        private static readonly FastColor EYE_GREEN = FastColor.FromRgb(0, 255, 0);

        [Test]
        public void ReplaceColor_ReplacesBodyColor()
        {
            var pixels = new[] { GhostPalette.BODY_COLOR.PackedValue, GhostPalette.BODY_COLOR.PackedValue };

            var result = GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, RED);

            Assert.That(result, Is.EqualTo(new[] { RED.PackedValue, RED.PackedValue }));
        }

        [Test]
        public void ReplaceColor_LeavesOtherColorsAlone()
        {
            var nearWhite = FastColor.FromRgb(254, 255, 255);
            var halfWhite = FastColor.FromArgb(128, 255, 255, 255);
            var pixels = new[] { BLACK.PackedValue, EYE_GREEN.PackedValue, nearWhite.PackedValue, halfWhite.PackedValue, 0u };

            var result = GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, RED);

            Assert.That(result, Is.EqualTo(pixels));
        }

        [Test]
        public void ReplaceColor_DoesNotChangeInput()
        {
            var pixels = new[] { GhostPalette.BODY_COLOR.PackedValue, BLACK.PackedValue };
            var original = (uint[])pixels.Clone();

            var result = GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, RED);

            Assert.That(pixels, Is.EqualTo(original));
            Assert.That(result, Is.Not.SameAs(pixels));
        }

        [Test]
        public void GetRandomColor_StaysRich()
        {
            var random = new Random(1234);

            for (var i = 0; i < 1000; i++)
            {
                var color = GhostPalette.GetRandomColor(random);
                var max = Math.Max(color.R, Math.Max(color.G, color.B));
                var min = Math.Min(color.R, Math.Min(color.G, color.B));

                Assert.That(color.A, Is.EqualTo(255));
                Assert.That(max, Is.GreaterThanOrEqualTo((int)(GhostPalette.MIN_VALUE * 255)));
                Assert.That((max - min) / (float)max, Is.GreaterThanOrEqualTo(GhostPalette.MIN_SATURATION - 0.01f));
            }
        }

        [Test]
        public void GetColor_SameSeedAndGhostGiveSameColor()
        {
            var first = GhostPalette.GetColor(42, 3);
            var second = GhostPalette.GetColor(42, 3);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void GetColor_GhostsInALevelDiffer()
        {
            var colors = Enumerable.Range(0, 4).Select(id => GhostPalette.GetColor(42, id)).Distinct().Count();

            Assert.That(colors, Is.EqualTo(4));
        }

        [Test]
        public void GetColorSeed_NextLevelDoesNotReuseSeeds()
        {
            const int SERVER_SEED = 1000;
            var seeds = new HashSet<int>();

            for (var level = 1; level <= 20; level++)
                for (var id = 0; id < 8; id++)
                    seeds.Add(GhostPalette.GetColorSeed(Levels.GetSeed(SERVER_SEED, level), id));

            Assert.That(seeds.Count, Is.EqualTo(20 * 8));
        }
    }
}
