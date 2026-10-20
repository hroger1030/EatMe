using System;
using System.Diagnostics;

namespace EatMeServer
{
    /// <summary>
    /// Writes server console lines colored by what they are about (eLogType). Safe to call from any thread, since
    /// network tasks log too; errors go to standard error. Once EnablePrompt is called the console's last line is
    /// always the typing prompt, "[Up: h:mm] " (GetPrompt): it is written again after every line logged.
    /// </summary>
    public static class ServerLog
    {
        private static readonly object LOCK = new();

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

                case eLogType.Warning:
                    return ConsoleColor.Yellow;

                case eLogType.Error:
                    return ConsoleColor.Red;

                default:
                    return ConsoleColor.Gray;
            }
        }

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

        public static void Write(eLogType type, string text)
        {
            lock (LOCK)
            {
                EndPromptLine();

                var old = Console.ForegroundColor;
                Console.ForegroundColor = GetColor(type);

                if (type == eLogType.Error)
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
