using EatMeLibrary;

namespace EatMeTests
{
    /// <summary>
    /// The attract screen's timing and order, item names and text wrapping.
    /// </summary>
    public class AttractTests
    {
        [Test]
        public void GetFadeAlpha_GoesFromClearToBlackOverTheFade()
        {
            Assert.That(AttractMath.GetFadeAlpha(TimeSpan.FromSeconds(-1)), Is.Zero);
            Assert.That(AttractMath.GetFadeAlpha(TimeSpan.Zero), Is.Zero);
            Assert.That(AttractMath.GetFadeAlpha(TimeSpan.FromMilliseconds(AttractMath.FADE_MS / 2)), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(AttractMath.GetFadeAlpha(TimeSpan.FromMilliseconds(AttractMath.FADE_MS)), Is.EqualTo(1f));
            Assert.That(AttractMath.GetFadeAlpha(TimeSpan.FromMilliseconds(AttractMath.FADE_MS * 3)), Is.EqualTo(1f));
        }

        [Test]
        public void GetScrollOffset_ScrollsUntilTheBottomIsOnScreen()
        {
            Assert.That(AttractMath.GetScrollOffset(TimeSpan.Zero, 3000f, 1000f, 100f), Is.Zero);
            Assert.That(AttractMath.GetScrollOffset(TimeSpan.FromSeconds(5), 3000f, 1000f, 100f), Is.EqualTo(500));
            Assert.That(AttractMath.GetScrollOffset(TimeSpan.FromSeconds(60), 3000f, 1000f, 100f), Is.EqualTo(2000), "stops with the bottom at the bottom of the screen");
            Assert.That(AttractMath.GetScrollOffset(TimeSpan.FromSeconds(60), 800f, 1000f, 100f), Is.Zero, "content shorter than the screen never scrolls");
        }

        [Test]
        public void IsFinished_AfterScrollingAndHolding()
        {
            var scrollTime = AttractMath.GetScrollTime(3000f, 1000f, 100f);
            var hold = TimeSpan.FromMilliseconds(AttractMath.HOLD_MS);
            Assert.That(scrollTime, Is.EqualTo(TimeSpan.FromSeconds(20)));

            Assert.That(AttractMath.IsFinished(scrollTime, 3000f, 1000f, 100f), Is.False);
            Assert.That(AttractMath.IsFinished(scrollTime + hold - TimeSpan.FromMilliseconds(10), 3000f, 1000f, 100f), Is.False);
            Assert.That(AttractMath.IsFinished(scrollTime + hold, 3000f, 1000f, 100f), Is.True);
            Assert.That(AttractMath.IsFinished(hold, 500f, 1000f, 100f), Is.True, "nothing to scroll, just the hold");
        }

        [Test]
        public void GetGhostOrder_IsEveryGhostAlphabetically()
        {
            var order = AttractMath.GetGhostOrder();

            Assert.That(order, Has.Count.EqualTo(GhostPersonalities.ALL.Count));
            Assert.That(order.Select(ghost => ghost.Name), Is.Ordered.Using((IComparer<string>)StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public void GetPowerUpOrder_IsEverySpawnableItemAlphabetically_WithoutThePowerPill()
        {
            var order = AttractMath.GetPowerUpOrder();

            Assert.That(order, Does.Not.Contain(ePowerUp.PowerPill));
            Assert.That(order, Has.Count.EqualTo(31));
            Assert.That(order, Is.EquivalentTo(PowerUpRules.SPAWNABLE));
            Assert.That(order.Select(PowerUpNames.GetName), Is.Ordered.Using((IComparer<string>)StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public void PowerUpNames_EveryItemHasAUniquePrintableName()
        {
            var names = new HashSet<string>();

            foreach (var item in Enum.GetValues<ePowerUp>())
            {
                if (item == ePowerUp.None)
                    continue;

                var name = PowerUpNames.GetName(item);
                Assert.That(name, Is.Not.Empty);
                Assert.That(name.All(c => c >= ' ' && c <= '~'), Is.True, name);
                Assert.That(names.Add(name), Is.True, $"{name} is used twice");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => PowerUpNames.GetName((ePowerUp)255));
        }

        [Test]
        public void AttractBlurbs_EveryGhostHasAUniquePrintableBlurb()
        {
            var blurbs = new HashSet<string>();

            foreach (var ghost in GhostPersonalities.ALL)
            {
                var blurb = AttractBlurbs.GetGhostBlurb(ghost.Behavior);
                Assert.That(blurb, Is.Not.Empty);
                Assert.That(blurb.All(c => c >= ' ' && c <= '~'), Is.True, blurb);
                Assert.That(blurbs.Add(blurb), Is.True, $"{ghost.Name} shares a blurb");
            }
        }

        [Test]
        public void AttractBlurbs_EveryItemHasAUniquePrintableBlurb()
        {
            var blurbs = new HashSet<string>();

            foreach (var item in Enum.GetValues<ePowerUp>())
            {
                if (item == ePowerUp.None)
                    continue;

                var blurb = AttractBlurbs.GetPowerUpBlurb(item);
                Assert.That(blurb, Is.Not.Empty);
                Assert.That(blurb.All(c => c >= ' ' && c <= '~'), Is.True, blurb);
                Assert.That(blurbs.Add(blurb), Is.True, $"{item} shares a blurb");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => AttractBlurbs.GetPowerUpBlurb((ePowerUp)255));
        }

        [Test]
        public void TextWrap_BreaksAtSpacesToFit_KeepingBlankLines()
        {
            // every character is one unit wide
            var lines = TextWrap.Wrap("one two three four\r\n\r\nfive", 9f, text => text.Length);

            Assert.That(lines, Is.EqualTo(new[] { "one two", "three", "four", "", "five" }));
        }

        [Test]
        public void TextWrap_LeavesAWordTooLongToBreakOnItsOwnLine()
        {
            var lines = TextWrap.Wrap("a extraordinarily b", 5f, text => text.Length);

            Assert.That(lines, Is.EqualTo(new[] { "a", "extraordinarily", "b" }));
        }
    }
}
