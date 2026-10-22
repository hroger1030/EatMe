using EatMeLibrary;

namespace EatMeTests
{
    /// <summary>
    /// The pac shapes a player can pick: names, sheet rows and the default.
    /// </summary>
    public class PacShapeTests
    {
        [Test]
        public void Default_IsFang()
        {
            Assert.That(PacShapes.DEFAULT, Is.EqualTo(ePacShape.Fang));
        }

        [Test]
        public void All_HasNineShapesInSheetOrder()
        {
            Assert.That(PacShapes.ALL, Has.Length.EqualTo(9));

            for (var i = 0; i < PacShapes.ALL.Length; i++)
                Assert.That(PacShapes.GetCellY(PacShapes.ALL[i]), Is.EqualTo(i));
        }

        [Test]
        public void GetName_GivesEachShapesName()
        {
            var names = PacShapes.ALL.Select(PacShapes.GetName);

            Assert.That(names, Is.EqualTo(new[] { "O.G.", "Fang", "Hex", "Boxie", "Trey", "Doc", "Gears", "LadyDi", "Go" }));
        }

        [Test]
        public void Clamp_KeepsShapes_AndTurnsAnythingElseIntoTheDefault()
        {
            Assert.That(PacShapes.Clamp(ePacShape.Go), Is.EqualTo(ePacShape.Go));
            Assert.That(PacShapes.Clamp((ePacShape)9), Is.EqualTo(PacShapes.DEFAULT));
            Assert.That(PacShapes.Clamp((ePacShape)(-1)), Is.EqualTo(PacShapes.DEFAULT));
            Assert.That(PacShapes.GetCellY((ePacShape)42), Is.EqualTo((int)PacShapes.DEFAULT));
        }
    }
}
