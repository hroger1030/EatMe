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
            Assert.That(AttractMath.GetFadeAlpha(-1f), Is.Zero);
            Assert.That(AttractMath.GetFadeAlpha(0f), Is.Zero);
            Assert.That(AttractMath.GetFadeAlpha(AttractMath.FADE_SECONDS / 2f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(AttractMath.GetFadeAlpha(AttractMath.FADE_SECONDS), Is.EqualTo(1f));
            Assert.That(AttractMath.GetFadeAlpha(AttractMath.FADE_SECONDS * 3f), Is.EqualTo(1f));
        }

        [Test]
        public void GetScrollOffset_ScrollsUntilTheBottomIsOnScreen()
        {
            Assert.That(AttractMath.GetScrollOffset(0f, 3000f, 1000f, 100f), Is.Zero);
            Assert.That(AttractMath.GetScrollOffset(5f, 3000f, 1000f, 100f), Is.EqualTo(500));
            Assert.That(AttractMath.GetScrollOffset(60f, 3000f, 1000f, 100f), Is.EqualTo(2000), "stops with the bottom at the bottom of the screen");
            Assert.That(AttractMath.GetScrollOffset(60f, 800f, 1000f, 100f), Is.Zero, "content shorter than the screen never scrolls");
        }

        [Test]
        public void IsFinished_AfterScrollingAndHolding()
        {
            var scrollSeconds = AttractMath.GetScrollSeconds(3000f, 1000f, 100f);
            Assert.That(scrollSeconds, Is.EqualTo(20f));

            Assert.That(AttractMath.IsFinished(scrollSeconds, 3000f, 1000f, 100f), Is.False);
            Assert.That(AttractMath.IsFinished(scrollSeconds + AttractMath.HOLD_SECONDS - 0.01f, 3000f, 1000f, 100f), Is.False);
            Assert.That(AttractMath.IsFinished(scrollSeconds + AttractMath.HOLD_SECONDS, 3000f, 1000f, 100f), Is.True);
            Assert.That(AttractMath.IsFinished(AttractMath.HOLD_SECONDS, 500f, 1000f, 100f), Is.True, "nothing to scroll, just the hold");
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
            Assert.That(order.Aggregate(ePowerUp.None, (all, item) => all | item), Is.EqualTo(PowerUpRules.SPAWNABLE));
            Assert.That(order.Select(PowerUpNames.GetName), Is.Ordered.Using((IComparer<string>)StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public void PowerUpNames_EveryItemHasAUniquePrintableName()
        {
            var names = new HashSet<string>();

            foreach (var item in Enum.GetValues<ePowerUp>())
            {
                if (item == ePowerUp.None || item == ePowerUp.Any)
                    continue;

                var name = PowerUpNames.GetName(item);
                Assert.That(name, Is.Not.Empty);
                Assert.That(name.All(c => c >= ' ' && c <= '~'), Is.True, name);
                Assert.That(names.Add(name), Is.True, $"{name} is used twice");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => PowerUpNames.GetName(ePowerUp.Any));
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
                if (item == ePowerUp.None || item == ePowerUp.Any)
                    continue;

                var blurb = AttractBlurbs.GetPowerUpBlurb(item);
                Assert.That(blurb, Is.Not.Empty);
                Assert.That(blurb.All(c => c >= ' ' && c <= '~'), Is.True, blurb);
                Assert.That(blurbs.Add(blurb), Is.True, $"{item} shares a blurb");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => AttractBlurbs.GetPowerUpBlurb(ePowerUp.Any));
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
