using EatMeLibrary;

namespace EatMeTests
{
    public class WeaponEffectSpritesTests
    {
        [Test]
        public void EveryEffect_HasARowOnTheSheet()
        {
            var effects = Enum.GetValues<eWeaponEffect>().Where(e => !WeaponEffectSprites.IsDrawnAsPac(e)).ToArray();

            Assert.That(effects, Has.Length.EqualTo(WeaponEffectSprites.ROWS));

            foreach (var effect in effects)
                Assert.That(WeaponEffectSprites.GetCellY(effect), Is.LessThan(WeaponEffectSprites.ROWS * WeaponEffectSprites.CELL_PIXELS), effect.ToString());
        }

        [Test]
        public void GetCell_RowPerEffect_FramesLeftToRight()
        {
            Assert.That(WeaponEffectSprites.GetCellY(eWeaponEffect.BfgShot), Is.EqualTo(0));
            Assert.That(WeaponEffectSprites.GetCellY(eWeaponEffect.SpreadShot), Is.EqualTo(9 * 60));
            Assert.That(WeaponEffectSprites.GetCellX(0), Is.EqualTo(0));
            Assert.That(WeaponEffectSprites.GetCellX(1), Is.EqualTo(60));
            Assert.Throws<ArgumentOutOfRangeException>(() => WeaponEffectSprites.GetCellX(WeaponEffectSprites.FRAME_COUNT));
        }

        [Test]
        public void GetFrame_Alternates()
        {
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.Zero), Is.EqualTo(0));
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.FromMilliseconds(WeaponEffectSprites.FRAME_MS * 1.5)), Is.EqualTo(1));
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.FromMilliseconds(WeaponEffectSprites.FRAME_MS * 2.5)), Is.EqualTo(0));
            Assert.That(WeaponEffectSprites.GetFrame(TimeSpan.FromSeconds(-1)), Is.EqualTo(0));
        }
    }
}
