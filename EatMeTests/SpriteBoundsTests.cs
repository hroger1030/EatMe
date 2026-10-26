using EatMeLibrary;

namespace EatMeTests
{
    public class SpriteBoundsTests
    {
        private const uint CLEAR = 0x00FF00FF;
        private const uint SOLID = 0xFFFFFFFF;

        [Test]
        public void GetOpaqueBounds_IsTheBoxAroundTheDrawnPixels()
        {
            // 4x3 texture, drawn pixels at (1, 1) and (2, 2)
            var pixels = new uint[]
            {
                CLEAR, CLEAR, CLEAR, CLEAR,
                CLEAR, SOLID, CLEAR, CLEAR,
                CLEAR, CLEAR, SOLID, CLEAR,
            };

            var bounds = SpriteBounds.GetOpaqueBounds(pixels, 4, new CellBox(0, 0, 4, 3));

            Assert.That((bounds.X, bounds.Y, bounds.Width, bounds.Height), Is.EqualTo((1, 1, 2, 2)));
        }

        [Test]
        public void GetOpaqueBounds_LooksOnlyInsideTheCell()
        {
            // two 2x2 cells side by side; the right one has one drawn pixel at (3, 0)
            var pixels = new uint[]
            {
                SOLID, CLEAR, CLEAR, SOLID,
                SOLID, CLEAR, CLEAR, CLEAR,
            };

            var bounds = SpriteBounds.GetOpaqueBounds(pixels, 4, new CellBox(2, 0, 2, 2));

            Assert.That((bounds.X, bounds.Y, bounds.Width, bounds.Height), Is.EqualTo((3, 0, 1, 1)));
        }

        [Test]
        public void GetOpaqueBounds_EmptyCell_IsTheWholeCell()
        {
            var pixels = new uint[] { CLEAR, CLEAR, CLEAR, CLEAR };

            var bounds = SpriteBounds.GetOpaqueBounds(pixels, 2, new CellBox(0, 0, 2, 2));

            Assert.That((bounds.X, bounds.Y, bounds.Width, bounds.Height), Is.EqualTo((0, 0, 2, 2)));
        }
    }
}
