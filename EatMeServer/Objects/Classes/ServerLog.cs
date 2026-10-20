using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace EatMeServer
{
    /// <summary>
    /// Writes server console lines colored by what they are about (eLogType). Safe to call from any thread, since
    /// network tasks log too; errors go to standard error. Once EnablePrompt is called the console's last line is
    /// always the typing prompt, "[Up:h:mm] " (GetPrompt): it is written again after every line logged.
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

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        private static readonly object LOCK = new();

        private static bool _UseAnsi;

        private static Stopwatch _Uptime;
        private static bool _IsPromptShowing;

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
        /// The typing prompt for how long the server has been up: whole hours, then minutes as two digits.
        /// </summary>
        public static string GetPrompt(TimeSpan uptime)
        {
            return $"[Up:{(int)uptime.TotalHours}:{uptime.Minutes:00}] ";
        }

        /// <summary>
        /// Starts showing the prompt, timed from uptime, and shows it now.
        /// </summary>
        public static void EnablePrompt(Stopwatch uptime)
        {
            lock (LOCK)
            {
                _Uptime = uptime;
                WritePrompt();
            }
        }

        /// <summary>
        /// Stops showing the prompt, ending the line it is on.
        /// </summary>
        public static void DisablePrompt()
        {
            lock (LOCK)
            {
                EndPromptLine();
                _Uptime = null;
            }
        }

        /// <summary>
        /// Shows the prompt again, e.g. after a command that printed nothing.
        /// </summary>
        public static void ShowPrompt()
        {
            lock (LOCK)
            {
                EndPromptLine();
                WritePrompt();
            }
        }

        /// <summary>
        /// Called when a typed line has been read: Enter already ended the prompt's line.
        /// </summary>
        public static void OnLineTyped()
        {
            lock (LOCK)
                _IsPromptShowing = false;
        }

        /// <summary>
        /// Clears the console. The prompt goes with it; ShowPrompt brings it back. Does nothing when the output
        /// isn't a console (redirected, or under tests).
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

                _IsPromptShowing = false;
            }
        }

        public static void Write(eLogType type, string text)
        {
            lock (LOCK)
            {
                EndPromptLine();

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
                WritePrompt();
            }
        }

        private static void EndPromptLine()
        {
            if (_IsPromptShowing)
                Console.WriteLine();

            _IsPromptShowing = false;
        }

        private static void WritePrompt()
        {
            if (_Uptime == null)
                return;

            Console.Write(GetPrompt(_Uptime.Elapsed));
            _IsPromptShowing = true;
        }
    }
}
