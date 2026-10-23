using EatMeLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace EatMeServer
{
    /// <summary>
    /// Perf logging for the server. The main loop and GameServer.Update time every part of each tick into Stats
    /// (one PerfSection per ePerfSection) all the time; every REPORT_INTERVAL_MS Update ends the window and, while
    /// IsLogging (the perf command), returns a report of it: ticks a second, tick times, how much of the tick
    /// budget was used, each step's times, garbage collections, memory and network traffic.
    /// </summary>
    public class ServerPerf
    {
        /// <summary>
        /// ms each report covers.
        /// </summary>
        public const int REPORT_INTERVAL_MS = 5000;

        /// <summary>
        /// Steps listed on each line of a report.
        /// </summary>
        public const int STEPS_PER_LINE = 6;

        private const double BYTES_PER_KB = 1024.0;
        private const double BYTES_PER_MB = 1024.0 * 1024.0;

        private long _WindowStart;
        private readonly int[] _Collections = new int[GC.MaxGeneration + 1];
        private long _Allocated;
        private long _Sent;
        private long _Received;

        public PerfStats Stats { get; } = new(GetSectionNames());

        public bool IsLogging { get; private set; }

        public ServerPerf()
        {
            StartWindow();
        }

        /// <summary>
        /// The report label for each section, in ePerfSection order: its name in lower case.
        /// </summary>
        public static List<string> GetSectionNames()
        {
            var names = new List<string>();

            foreach (var name in Enum.GetNames<ePerfSection>())
                names.Add(name.ToLowerInvariant());

            return names;
        }

        /// <summary>
        /// Records the time from startTimestamp to now in a section and returns now (PerfStats.Record).
        /// </summary>
        public long Record(ePerfSection section, long startTimestamp)
        {
            return Stats.Record((int)section, startTimestamp);
        }

        /// <summary>
        /// Turns reporting on or off. Either way a new window starts, so the first report covers a whole one.
        /// </summary>
        public void SetLogging(bool on)
        {
            IsLogging = on;
            StartWindow();
        }

        /// <summary>
        /// Call once a tick. Once the window has run REPORT_INTERVAL_MS it ends: the report of it is returned while
        /// logging (null otherwise) and a new window starts.
        /// </summary>
        public string Update(GameServer game)
        {
            var now = PerfStats.GetTimestamp();
            var windowMs = PerfStats.GetElapsedMs(_WindowStart, now);

            if (windowMs < REPORT_INTERVAL_MS)
                return null;

            var report = IsLogging ? BuildReport(game, windowMs) : null;
            StartWindow();
            return report;
        }

        /// <summary>
        /// Share (0 to 100) of the window spent working rather than sleeping between ticks. Over 100 the server
        /// can't keep up with its TickRate.
        /// </summary>
        public static double GetLoadPercent(double busyMs, double windowMs)
        {
            return windowMs <= 0 ? 0 : busyMs * 100 / windowMs;
        }

        /// <summary>
        /// How many of something a second.
        /// </summary>
        public static double GetRate(double count, double windowMs)
        {
            return PerfStats.GetRate(count, windowMs);
        }

        /// <summary>
        /// One line per STEPS_PER_LINE sections from ePerfSection.Lag on that ran in the window, each as
        /// "name avg/max" in ms; sections that didn't run are left out.
        /// </summary>
        public static List<string> FormatSteps(PerfStats stats)
        {
            var lines = new List<string>();
            var line = new StringBuilder();
            var onLine = 0;

            for (var i = (int)ePerfSection.Lag; i < stats.Sections.Count; i++)
            {
                var section = stats.Sections[i];

                if (section.Count == 0)
                    continue;

                if (onLine == STEPS_PER_LINE)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    onLine = 0;
                }

                if (onLine > 0)
                    line.Append("  ");

                line.Append($"{section.Name} {section.AverageMs:0.00}/{section.MaxMs:0.00}");
                onLine++;
            }

            if (onLine > 0)
                lines.Add(line.ToString());

            return lines;
        }

        private string BuildReport(GameServer game, double windowMs)
        {
            var tick = Stats.Sections[(int)ePerfSection.Tick];
            var state = game.IsPaused ? "paused" : game.IsIntermission ? "intermission" : game.IsVictory ? "victory" : game.IsCountdown ? "countdown" : "playing";
            var text = new StringBuilder();

            text.Append($"\n Perf ({state}, {game.PlayerCount} players, {game.BotCount} bots): {GetRate(tick.Count, windowMs):0.0} ticks/s, ");
            text.Append($"tick avg {tick.AverageMs:0.00} p99 {tick.GetPercentileMs(PerfStats.REPORT_PERCENTILE):0.00} max {tick.MaxMs:0.00} ms, ");
            text.Append($"load {GetLoadPercent(tick.TotalMs, windowMs):0.0}%");

            var collections = new List<string>();

            for (var generation = 0; generation < _Collections.Length; generation++)
                collections.Add((GC.CollectionCount(generation) - _Collections[generation]).ToString());

            var allocated = GC.GetTotalAllocatedBytes() - _Allocated;
            var sent = MessageConnection.TotalBytesSent - _Sent;
            var received = MessageConnection.TotalBytesReceived - _Received;

            text.Append($"\n  gc {string.Join("/", collections)}, allocated {GetRate(allocated, windowMs) / BYTES_PER_MB:0.00} MB/s, ");
            text.Append($"heap {GC.GetTotalMemory(false) / BYTES_PER_MB:0.0} MB, ");
            text.Append($"sent {GetRate(sent, windowMs) / BYTES_PER_KB:0.0} KB/s, received {GetRate(received, windowMs) / BYTES_PER_KB:0.0} KB/s");

            var network = Stats.Sections[(int)ePerfSection.Network];
            var commands = Stats.Sections[(int)ePerfSection.Commands];
            text.Append($"\n  steps avg/max ms: network {network.AverageMs:0.00}/{network.MaxMs:0.00}  commands {commands.AverageMs:0.00}/{commands.MaxMs:0.00}");

            foreach (var line in FormatSteps(Stats))
                text.Append($"\n  {line}");

            return text.ToString();
        }

        private void StartWindow()
        {
            Stats.Reset();
            _WindowStart = PerfStats.GetTimestamp();

            for (var generation = 0; generation < _Collections.Length; generation++)
                _Collections[generation] = GC.CollectionCount(generation);

            _Allocated = GC.GetTotalAllocatedBytes();
            _Sent = MessageConnection.TotalBytesSent;
            _Received = MessageConnection.TotalBytesReceived;
        }
    }
}
