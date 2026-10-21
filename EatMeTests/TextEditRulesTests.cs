using EatMeLibrary;

namespace EatMeTests
{
    public class TextEditRulesTests
    {
        private static bool IsDigit(char c) => char.IsAsciiDigit(c);

        [Test]
        public void ApplyPaste_AppendsToExistingText()
        {
            Assert.That(TextEditRules.ApplyPaste("12", "34", 10, IsDigit), Is.EqualTo("1234"));
        }

        [Test]
        public void ApplyPaste_DropsCharactersNotAllowed()
        {
            Assert.That(TextEditRules.ApplyPaste("", "1 a2\r\n3", 10, IsDigit), Is.EqualTo("123"));
        }

        [Test]
        public void ApplyPaste_StopsAtMaxLength()
        {
            Assert.That(TextEditRules.ApplyPaste("12", "345678", 5, IsDigit), Is.EqualTo("12345"));
        }

        [Test]
        public void ApplyPaste_FullTextIsUnchanged()
        {
            Assert.That(TextEditRules.ApplyPaste("12345", "6", 5, IsDigit), Is.EqualTo("12345"));
        }

        [Test]
        public void ApplyPaste_EmptyOrNullPasteIsUnchanged()
        {
            Assert.That(TextEditRules.ApplyPaste("12", "", 5, IsDigit), Is.EqualTo("12"));
            Assert.That(TextEditRules.ApplyPaste("12", null, 5, IsDigit), Is.EqualTo("12"));
        }

        [Test]
        public void ApplyPaste_KeepsAddressPunctuation()
        {
            Assert.That(TextEditRules.ApplyPaste("", " 203.0.113.5:7777\n", 40, c => char.IsAsciiLetterOrDigit(c) || c == '.' || c == ':'),
                Is.EqualTo("203.0.113.5:7777"));
        }
    }
}
