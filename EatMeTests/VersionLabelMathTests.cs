using EatMeLibrary;
using System.Globalization;

namespace EatMeTests
{
    public class VersionLabelMathTests
    {
        [Test]
        public void GetVersionText_PrefixesV()
        {
            Assert.That(VersionLabelMath.GetVersionText(0.9f), Is.EqualTo("v0.9"));
        }

        [Test]
        public void GetVersionText_UsesDotInCommaLocale()
        {
            var previous = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(VersionLabelMath.GetVersionText(1.25f), Is.EqualTo("v1.25"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void GetTextX_RightAlignsInsideMargin()
        {
            Assert.That(VersionLabelMath.GetTextX(1000f, 96f, 16f), Is.EqualTo(888));
        }
    }
}
