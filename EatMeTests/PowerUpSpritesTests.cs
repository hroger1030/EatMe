using EatMeLibrary;

namespace EatMeTests
{
    public class PowerUpSpritesTests
    {
        private static IEnumerable<ePowerUp> GetItems()
        {
            return PowerUpRules.ALL;
        }

        [Test]
        public void GetCellIndex_FollowsEnumOrder()
        {
            Assert.That(PowerUpSprites.GetCellIndex(ePowerUp.Cherry), Is.EqualTo(0));
            Assert.That(PowerUpSprites.GetCellIndex(ePowerUp.Key), Is.EqualTo(7));
            Assert.That(PowerUpSprites.GetCellIndex(ePowerUp.Pretzel), Is.EqualTo(8));
            Assert.That(PowerUpSprites.GetCellIndex(ePowerUp.PokeBall), Is.EqualTo(31));
        }

        [Test]
        public void EveryItem_HasItsOwnCellOnTheSheet()
        {
            var cells = GetItems().Select(PowerUpSprites.GetCellIndex).ToList();

            Assert.That(cells, Is.Unique);
            Assert.That(cells, Has.All.InRange(0, PowerUpSprites.COLUMNS * PowerUpSprites.ROWS - 1));
            Assert.That(cells, Has.Count.EqualTo(PowerUpSprites.COLUMNS * PowerUpSprites.ROWS));
        }

        [Test]
        public void GetCellPosition_GoesAcrossThenDown()
        {
            Assert.That(PowerUpSprites.GetCellX(ePowerUp.Cherry), Is.EqualTo(0));
            Assert.That(PowerUpSprites.GetCellY(ePowerUp.Cherry), Is.EqualTo(0));
            Assert.That(PowerUpSprites.GetCellX(ePowerUp.Key), Is.EqualTo(420));
            Assert.That(PowerUpSprites.GetCellY(ePowerUp.Key), Is.EqualTo(0));
            Assert.That(PowerUpSprites.GetCellX(ePowerUp.Pretzel), Is.EqualTo(0));
            Assert.That(PowerUpSprites.GetCellY(ePowerUp.Pretzel), Is.EqualTo(60));
            Assert.That(PowerUpSprites.GetCellX(ePowerUp.PokeBall), Is.EqualTo(420));
            Assert.That(PowerUpSprites.GetCellY(ePowerUp.PokeBall), Is.EqualTo(180));
        }

        [Test]
        public void GetCellIndex_NotAnItem_Throws()
        {
            Assert.Throws<ArgumentException>(() => PowerUpSprites.GetCellIndex(ePowerUp.None));
            Assert.Throws<ArgumentException>(() => PowerUpSprites.GetCellIndex((ePowerUp)33));
            Assert.Throws<ArgumentException>(() => PowerUpSprites.GetCellIndex((ePowerUp)255));
        }
    }
}
