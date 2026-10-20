using System;
using System.Collections.Generic;
using System.Text;

namespace EatMeServer
{
    /// <summary>
    /// The line being typed at the server console: its text, the cursor and the lines typed before (Up and Down).
    /// Drawing it is ServerLog's job; this only edits it, so it can be tested without a console.
    /// </summary>
    public class ConsoleInput
    {
        /// <summary>
        /// The most typed lines kept for Up and Down.
        /// </summary>
        public const int MAX_HISTORY = 100;

        private readonly StringBuilder _Text = new();
        private readonly List<string> _History = new();

        // where Up and Down are in the history; _History.Count means the line being typed, not a past one
        private int _HistoryIndex;

        // the line being typed when Up first left it, so Down can bring it back
        private string _Draft = string.Empty;

        public string Text => _Text.ToString();

        /// <summary>
        /// Where the next typed character goes, 0 to Text's length.
        /// </summary>
        public int Cursor { get; private set; }

        public IReadOnlyList<string> History => _History;

        /// <summary>
        /// Applies one key press. Returns true with the typed line when it was Enter, else false and null.
        /// </summary>
        public bool HandleKey(ConsoleKeyInfo key, out string line)
        {
            line = null;

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    line = Submit();
                    return true;

                case ConsoleKey.Backspace:
                    Backspace();
                    break;

                case ConsoleKey.Delete:
                    Delete();
                    break;

                case ConsoleKey.LeftArrow:
                    MoveLeft();
                    break;

                case ConsoleKey.RightArrow:
                    MoveRight();
                    break;

                case ConsoleKey.Home:
                    Home();
                    break;

                case ConsoleKey.End:
                    End();
                    break;

                case ConsoleKey.UpArrow:
                    HistoryUp();
                    break;

                case ConsoleKey.DownArrow:
                    HistoryDown();
                    break;

                case ConsoleKey.Escape:
                    Clear();
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                        Insert(key.KeyChar);
                    break;
            }

            return false;
        }

        public void Insert(char c)
        {
            _Text.Insert(Cursor, c);
            Cursor++;
        }

        /// <summary>
        /// Removes the character before the cursor.
        /// </summary>
        public void Backspace()
        {
            if (Cursor == 0)
                return;

            Cursor--;
            _Text.Remove(Cursor, 1);
        }

        /// <summary>
        /// Removes the character under the cursor.
        /// </summary>
        public void Delete()
        {
            if (Cursor < _Text.Length)
                _Text.Remove(Cursor, 1);
        }

        public void MoveLeft()
        {
            Cursor = Math.Max(0, Cursor - 1);
        }

        public void MoveRight()
        {
            Cursor = Math.Min(_Text.Length, Cursor + 1);
        }

        public void Home()
        {
            Cursor = 0;
        }

        public void End()
        {
            Cursor = _Text.Length;
        }

        /// <summary>
        /// Empties the line, leaving the history where it is.
        /// </summary>
        public void Clear()
        {
            SetText(string.Empty);
            _HistoryIndex = _History.Count;
        }

        /// <summary>
        /// Shows the line typed before the one shown. Does nothing at the oldest.
        /// </summary>
        public void HistoryUp()
        {
            if (_HistoryIndex == 0)
                return;

            if (_HistoryIndex == _History.Count)
                _Draft = Text;

            _HistoryIndex--;
            SetText(_History[_HistoryIndex]);
        }

        /// <summary>
        /// Shows the line typed after the one shown, or the line that was being typed after the newest.
        /// </summary>
        public void HistoryDown()
        {
            if (_HistoryIndex >= _History.Count)
                return;

            _HistoryIndex++;
            SetText(_HistoryIndex == _History.Count ? _Draft : _History[_HistoryIndex]);
        }

        /// <summary>
        /// Ends the line: returns it, empties the input and adds it to the history, unless it is blank or the
        /// same as the newest line there. At most MAX_HISTORY lines are kept.
        /// </summary>
        public string Submit()
        {
            var line = Text;

            if (line.Trim().Length > 0 && (_History.Count == 0 || _History[^1] != line))
            {
                _History.Add(line);

                if (_History.Count > MAX_HISTORY)
                    _History.RemoveAt(0);
            }

            _Draft = string.Empty;
            Clear();
            return line;
        }

        /// <summary>
        /// The first character of the text to show when only room characters fit, so the cursor stays in view:
        /// 0 while text and cursor fit, else far enough along that the cursor sits in the last column.
        /// </summary>
        public static int GetViewStart(int textLength, int cursor, int room)
        {
            if (room <= 0)
                return cursor;

            // the cursor needs a column of its own past the end of the text
            if (Math.Max(textLength, cursor + 1) <= room)
                return 0;

            return Math.Max(0, cursor + 1 - room);
        }

        private void SetText(string text)
        {
            _Text.Clear();
            _Text.Append(text);
            Cursor = _Text.Length;
        }
    }
}
