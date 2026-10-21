using System;
using System.Text;

namespace EatMeLibrary
{
    /// <summary>
    /// Rules for editing the client's text boxes that don't need MonoGame, so they can be unit tested.
    /// </summary>
    public static class TextEditRules
    {
        /// <summary>
        /// Returns text with pasted text added on the end: only allowed characters are kept, in order, up to
        /// maxLength. Anything else (line breaks, spaces, characters the box doesn't take) is dropped.
        /// </summary>
        public static string ApplyPaste(string text, string pasted, int maxLength, Func<char, bool> allowCharacter)
        {
            text ??= string.Empty;

            if (string.IsNullOrEmpty(pasted) || text.Length >= maxLength)
                return text;

            var result = new StringBuilder(text);

            foreach (var c in pasted)
            {
                if (result.Length >= maxLength)
                    break;

                if (allowCharacter(c))
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}
