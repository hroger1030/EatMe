using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace EatMeServer
{
    /// <summary>
    /// Writes server console lines colored by what they are about (eLogType). Safe to call from any thread, since
    /// network tasks log too; errors go to standard error. Once EnablePrompt is called the console's last line is
    /// always the typing prompt, "[up hh.mm.ss] " (GetPrompt), followed by the line being typed (ConsoleInput), which
    /// the server draws itself from the keys HandleKey is given. With ANSI on, a scroll region keeps the prompt on
    /// the window's bottom row and the log scrolls above it; without, the prompt line is wiped before each logged
    /// line and drawn again after it. Either way logged lines never break into what is being typed.
    /// </summary>
    public static class ServerLog
    {
        /// <summary>
        /// 24-bit ANSI color for help text (dark orange, RGB 204,85,0), which ConsoleColor has no match for.
        /// </summary>
        public const string ANSI_HELP_COLOR = "[38;2;204;85;0m";

        /// <summary>
        /// ANSI code that puts the text color back to the console's default.
        /// </summary>
        public const string ANSI_RESET_COLOR = "[39m";

        /// <summary>
        /// ANSI code that saves the cursor (where the next logged line goes while the prompt is pinned).
        /// </summary>
        public const string ANSI_SAVE_CURSOR = "\u001b7";

        /// <summary>
        /// ANSI code that puts the cursor back where ANSI_SAVE_CURSOR saved it.
        /// </summary>
        public const string ANSI_RESTORE_CURSOR = "\u001b8";

        /// <summary>
        /// ANSI code that clears the cursor's whole line.
        /// </summary>
        public const string ANSI_CLEAR_LINE = "\u001b[2K";

        /// <summary>
        /// ANSI code that puts the scroll region back to the whole window.
        /// </summary>
        public const string ANSI_RESET_SCROLL_REGION = "\u001b[r";

        /// <summary>
        /// The fewest window rows the prompt is pinned at, so the log keeps at least two rows of its own.
        /// </summary>
        public const int MIN_PINNED_HEIGHT = 3;

        private const string ANSI_CURSOR_UP = "\u001b[1A";

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        private static readonly object LOCK = new();

        private static bool _UseAnsi;

        // null while the prompt is off
        private static Stopwatch _Uptime;
        private static ConsoleInput _Input;

        // a scroll region keeps the prompt on the bottom row
        private static bool _IsPinned;

        // the window size the prompt was last drawn for, and the prompt text drawn
        private static int _Width;
        private static int _Height;
        private static string _ShownPrompt;

        /// <summary>
        /// The console color for a kind of line.
        /// </summary>
        public static ConsoleColor GetColor(eLogType type)
        {
            switch (type)
            {
                case eLogType.Level:
                    return ConsoleColor.Magenta;

                case eLogType.Join:
                    return ConsoleColor.Green;

                case eLogType.Leave:
                    return ConsoleColor.DarkYellow;

                case eLogType.Kill:
                    return ConsoleColor.DarkRed;

                case eLogType.Flag:
                    return ConsoleColor.Cyan;

                case eLogType.PowerUp:
                    return ConsoleColor.Blue;

                case eLogType.Command:
                    return ConsoleColor.White;

                case eLogType.Help:
                    // the closest console color, used when the console can't show ANSI_HELP_COLOR
                    return ConsoleColor.DarkYellow;

                case eLogType.Warning:
                    return ConsoleColor.Yellow;

                case eLogType.Error:
                    return ConsoleColor.Red;

                default:
                    return ConsoleColor.Gray;
            }
        }

        /// <summary>
        /// The 24-bit ANSI color code for a kind of line, or empty when GetColor's console color is used.
        /// </summary>
        public static string GetAnsiColor(eLogType type)
        {
            return type == eLogType.Help ? ANSI_HELP_COLOR : string.Empty;
        }

        /// <summary>
        /// Lets Write use ANSI color codes when the console shows them: on Windows it turns on virtual terminal
        /// processing for standard output; elsewhere terminals already show them. Returns whether they are on.
        /// </summary>
        public static bool EnableAnsiColors()
        {
            if (Console.IsOutputRedirected)
                return _UseAnsi = false;

            if (!OperatingSystem.IsWindows())
                return _UseAnsi = true;

            var handle = GetStdHandle(STD_OUTPUT_HANDLE);

            _UseAnsi = GetConsoleMode(handle, out var mode)
                && SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);

            return _UseAnsi;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr handle, uint mode);

        /// <summary>
        /// The typing prompt for how long the server has been up: whole hours, minutes and seconds, each at least two digits.
        /// </summary>
        public static string GetPrompt(TimeSpan uptime)
        {
            return $"[up {(int)uptime.TotalHours:00}.{uptime.Minutes:00}.{uptime.Seconds:00}] ";
        }

        /// <summary>
        /// ANSI code that keeps scrolling to rows 1 to height - 1, leaving the bottom row to the prompt. Row 1 is
        /// the region's top, so lines scrolling out of it still go into the console's scrollback.
        /// </summary>
        public static string GetScrollRegionCode(int height)
        {
            return $"\u001b[1;{height - 1}r";
        }

        /// <summary>
        /// ANSI code that moves the cursor to a row and column of the window, both counted from 1.
        /// </summary>
        public static string GetMoveCode(int row, int column)
        {
            return $"\u001b[{row};{column}H";
        }

        /// <summary>
        /// Starts showing the prompt, timed from uptime, with an empty line to type in, and shows it now. Only
        /// for a console someone types at: keys come in through HandleKey.
        /// </summary>
        public static void EnablePrompt(Stopwatch uptime)
        {
            lock (LOCK)
            {
                _Uptime = uptime;
                _Input = new ConsoleInput();
                _IsPinned = _UseAnsi && Console.WindowHeight >= MIN_PINNED_HEIGHT;

                if (_IsPinned)
                    StartPinning(keepLogCursor: true);

                DrawPrompt();
            }
        }

        /// <summary>
        /// Stops showing the prompt: wipes it and gives the window back its whole height.
        /// </summary>
        public static void DisablePrompt()
        {
            lock (LOCK)
            {
                if (_Uptime == null)
                    return;

                if (_IsPinned)
                    Console.Write(GetMoveCode(_Height, 1) + ANSI_CLEAR_LINE + ANSI_RESET_SCROLL_REGION + ANSI_RESTORE_CURSOR);
                else
                    WipePromptLine();

                _IsPinned = false;
                _Uptime = null;
                _Input = null;
            }
        }

        /// <summary>
        /// Applies a key typed at the console and draws the line again. On Enter returns true with the typed
        /// line, which is also logged after the prompt, as it would have been echoed.
        /// </summary>
        public static bool HandleKey(ConsoleKeyInfo key, out string line)
        {
            lock (LOCK)
            {
                line = null;

                if (_Input == null)
                    return false;

                if (!_Input.HandleKey(key, out line))
                {
                    DrawPrompt();
                    return false;
                }

                Write(eLogType.Info, GetPrompt(_Uptime.Elapsed) + line);
                return true;
            }
        }

        /// <summary>
        /// Draws the prompt again if the window has changed size or the uptime it shows has moved on. Called
        /// every tick.
        /// </summary>
        public static void Refresh()
        {
            lock (LOCK)
            {
                if (_Uptime == null)
                    return;

                try
                {
                    if (_IsPinned && Console.WindowHeight != _Height)
                    {
                        if (Console.WindowHeight < MIN_PINNED_HEIGHT)
                            return;

                        StartPinning(keepLogCursor: false);
                        DrawPrompt();
                    }
                    else if (Console.WindowWidth != _Width || GetPrompt(_Uptime.Elapsed) != _ShownPrompt)
                    {
                        DrawPrompt();
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        /// <summary>
        /// Clears the console, and draws the prompt again if it is on. Does nothing to the console when the output
        /// isn't one (redirected, or under tests).
        /// </summary>
        public static void Clear()
        {
            lock (LOCK)
            {
                try
                {
                    Console.Clear();
                }
                catch (IOException)
                {
                }

                if (_Uptime == null)
                    return;

                // the log starts again at the top
                if (_IsPinned)
                    Console.Write(GetScrollRegionCode(_Height) + GetMoveCode(1, 1) + ANSI_SAVE_CURSOR);

                DrawPrompt();
            }
        }

        public static void Write(eLogType type, string text)
        {
            lock (LOCK)
            {
                if (_IsPinned)
                    Console.Write(ANSI_RESTORE_CURSOR + ANSI_CLEAR_LINE);
                else if (_Uptime != null)
                    WipePromptLine();

                var old = Console.ForegroundColor;
                Console.ForegroundColor = GetColor(type);

                var ansi = _UseAnsi ? GetAnsiColor(type) : string.Empty;

                if (ansi.Length > 0)
                    Console.WriteLine(ansi + text + ANSI_RESET_COLOR);
                else if (type == eLogType.Error)
                    Console.Error.WriteLine(text);
                else
                    Console.WriteLine(text);

                Console.ForegroundColor = old;

                if (_IsPinned)
                    Console.Write(ANSI_SAVE_CURSOR);

                DrawPrompt();
            }
        }

        // sets the scroll region for the window's height and saves where the log goes on: where the cursor is
        // (moved up a row first if it is on the bottom row, which becomes the prompt's), or the region's last row
        private static void StartPinning(bool keepLogCursor)
        {
            _Height = Console.WindowHeight;

            // a line feed scrolls the window when the cursor is on the bottom row, then the cursor goes back up
            var logCursor = keepLogCursor
                ? "\n" + ANSI_CURSOR_UP
                : GetMoveCode(_Height - 1, 1);

            // setting the region homes the cursor, so it is saved first and put back after
            Console.Write(logCursor + ANSI_SAVE_CURSOR + GetScrollRegionCode(_Height) + ANSI_RESTORE_CURSOR);
        }

        // blanks the prompt's line and leaves the cursor at its start, for a console with no ANSI
        private static void WipePromptLine()
        {
            Console.Write("\r" + new string(' ', Math.Max(0, _Width - 1)) + "\r");
        }

        // draws the prompt and as much of the typed line as fits, with the cursor where typing goes
        private static void DrawPrompt()
        {
            if (_Uptime == null || _Input == null)
                return;

            _Width = Console.WindowWidth;
            _ShownPrompt = GetPrompt(_Uptime.Elapsed);

            // the last column is left empty, so the line never wraps
            var room = _Width - 1 - _ShownPrompt.Length;
            var text = _Input.Text;
            var start = ConsoleInput.GetViewStart(text.Length, _Input.Cursor, room);
            var shown = room > 0 ? text.Substring(start, Math.Min(room, text.Length - start)) : string.Empty;
            var beforeCursor = shown.Substring(0, Math.Min(shown.Length, _Input.Cursor - start));

            if (_IsPinned)
            {
                Console.Write(GetMoveCode(_Height, 1) + ANSI_CLEAR_LINE + _ShownPrompt + shown
                    + GetMoveCode(_Height, _ShownPrompt.Length + beforeCursor.Length + 1));
            }
            else
            {
                // padded to the end so nothing drawn before is left over, then rewritten up to the cursor
                var padding = new string(' ', Math.Max(0, room - shown.Length));
                Console.Write("\r" + _ShownPrompt + shown + padding + "\r" + _ShownPrompt + beforeCursor);
            }
        }
    }
}
