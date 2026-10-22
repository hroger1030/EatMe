using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Breaks text into lines that fit a width, measured by the caller (a sprite font on the client).
    /// </summary>
    public static class TextWrap
    {
        /// <summary>
        /// The lines of text, each paragraph (line break) wrapped at spaces so no line measures more than maxWidth,
        /// unless it is a single word too long to break. A blank paragraph is an empty line.
        /// </summary>
        public static List<string> Wrap(string text, float maxWidth, Func<string, float> measure)
        {
            var lines = new List<string>();

            foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (paragraph.Trim().Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var currentLine = string.Empty;

                foreach (var word in paragraph.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = currentLine.Length == 0 ? word : $"{currentLine} {word}";

                    if (currentLine.Length > 0 && measure(candidate) > maxWidth)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = candidate;
                    }
                }

                lines.Add(currentLine);
            }

            return lines;
        }
    }
}
