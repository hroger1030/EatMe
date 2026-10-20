using EatMeLibrary;

namespace EatMeTests
{
    public class LevelsTests
    {
        [Test]
        public void GetLevel_NumbersMatchTheirPosition()
        {
            for (var level = 1; level <= Levels.Count; level++)
                Assert.That(Levels.GetLevel(level).Level, Is.EqualTo(level));
        }

        [Test]
        public void GetLevel_PastTheLastLevel_StartsOver()
        {
            Assert.That(Levels.GetLevel(Levels.Count + 1), Is.EqualTo(Levels.GetLevel(1)));
            Assert.That(Levels.GetLevel(Levels.Count * 2 + 3), Is.EqualTo(Levels.GetLevel(3)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GetLevel_BelowOne_Throws(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Levels.GetLevel(level));
        }

        [Test]
        public void PickRandomLevel_IsInRangeAndNeverTheCurrentLevel()
        {
            var random = new Random(5);

            for (var current = 1; current <= Levels.Count; current++)
            {
                for (var i = 0; i < 200; i++)
                {
                    var level = Levels.PickRandomLevel(current, random);

                    Assert.That(level, Is.InRange(1, Levels.Count));
                    Assert.That(level, Is.Not.EqualTo(current));
                }
            }
        }

        [Test]
        public void PickRandomLevel_PastTheLastLevel_SkipsTheSameLevel()
        {
            var random = new Random(9);

            for (var i = 0; i < 200; i++)
                Assert.That(Levels.PickRandomLevel(Levels.Count + 3, random), Is.Not.EqualTo(3));
        }

        [Test]
        public void PickRandomLevel_ReachesEveryOtherLevel()
        {
            var random = new Random(11);
            var seen = new HashSet<int>();

            for (var i = 0; i < 5000; i++)
                seen.Add(Levels.PickRandomLevel(1, random));

            Assert.That(seen.Count, Is.EqualTo(Levels.Count - 1));
        }

        [Test]
        public void EveryLevel_HasName()
        {
            for (var level = 1; level <= Levels.Count; level++)
                Assert.That(Levels.GetLevel(level).Name, Is.Not.Empty, $"level {level}");
        }

        [Test]
        public void EveryLevel_HasDarkBackground()
        {
            for (var level = 1; level <= Levels.Count; level++)
            {
                var color = Levels.GetLevel(level).BackgroundColor;
                Assert.That(Math.Max(color.R, Math.Max(color.G, color.B)), Is.LessThanOrEqualTo(96), $"level {level}");
            }
        }

        [Test]
        public void OnlyPacParadise_HasBackgroundImage()
        {
            for (var level = 1; level <= Levels.Count; level++)
            {
                var image = Levels.GetLevel(level).BackgroundImage;
                Assert.That(image, Is.EqualTo(level == 32 ? Const.IMAGE_SPACE_FILL : null), $"level {level}");
            }
        }

        [Test]
        public void PacParadise_HasTransparentWallFill()
        {
            Assert.That(Levels.GetLevel(32).WallFillColor.A, Is.EqualTo(0));
        }

        [Test]
        public void GetSeed_AddsLevelToServerSeed()
        {
            Assert.That(Levels.GetSeed(0, 1), Is.EqualTo(1));
            Assert.That(Levels.GetSeed(1000, 3), Is.EqualTo(1003));
        }

        [Test]
        public void GetSeed_EveryLevelHasUniqueSeed()
        {
            var seeds = Enumerable.Range(1, Levels.Count).Select(level => Levels.GetSeed(12345, level));

            Assert.That(seeds, Is.Unique);
        }

        [Test]
        public void GetSeed_WrapsInsteadOfOverflowing()
        {
            Assert.That(Levels.GetSeed(int.MaxValue, 1), Is.EqualTo(int.MinValue));
        }

        [Test]
        public void EveryLevel_IntroducesExactlyOnePowerUp()
        {
            var seen = ePowerUp.None;

            for (var level = 1; level <= Levels.Count; level++)
            {
                var added = Levels.GetLevel(level).PowerUps & ~seen;

                Assert.That(added, Is.Not.EqualTo(ePowerUp.None), $"level {level}");
                Assert.That(added & (added - 1), Is.EqualTo(ePowerUp.None), $"level {level} adds more than one");

                seen |= added;
            }
        }

        [Test]
        public void EveryPowerUp_IsIntroducedBySomeLevel()
        {
            var seen = ePowerUp.None;

            for (var level = 1; level <= Levels.Count; level++)
                seen |= Levels.GetLevel(level).PowerUps;

            Assert.That(seen, Is.EqualTo(ePowerUp.Any));
        }

        [Test]
        public void Any_IsEveryPowerUp()
        {
            var all = ePowerUp.None;

            foreach (var item in Enum.GetValues<ePowerUp>())
            {
                if (item != ePowerUp.Any)
                    all |= item;
            }

            Assert.That(all, Is.EqualTo(ePowerUp.Any));
        }
    }
}
