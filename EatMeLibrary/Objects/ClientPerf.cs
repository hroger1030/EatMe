using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// The client's performance overlay figures. The client times every Update and Draw into Stats (one PerfSection per
    /// eClientPerfSection) all the time; every WINDOW_MS Update ends the window and rebuilds Lines from it: frames a
    /// second, update and draw times, garbage collections and memory, and in a game snapshots a second, network
    /// traffic and ping. IsVisible (F1) only decides whether the overlay is drawn, so it shows figures straight away.
    /// </summary>
    public class ClientPerf
    {
        /// <summary>
        /// ms each set of figures covers.
        /// </summary>
        public const int WINDOW_MS = 1000;

        private const double BYTES_PER_KB = 1024.0;
        private const double BYTES_PER_MB = 1024.0 * 1024.0;

        private long _WindowStart;
        private long _LastDrawStart;
        private readonly int[] _Collections = new int[GC.MaxGeneration + 1];
        private long _Allocated;
        private long _Sent;
        private long _Received;
        private long _Snapshots;

        public PerfStats Stats { get; } = new(Enum.GetNames<eClientPerfSection>());

        public bool IsVisible { get; private set; }

        /// <summary>
        /// The overlay's text, one entry per line, from the last whole window. Empty until the first one ends.
        /// </summary>
        public List<string> Lines { get; } = [];

        public ClientPerf()
        {
            StartWindow(PerfStats.GetTimestamp(), 0);
        }

        public void ToggleVisible()
        {
            IsVisible = !IsVisible;
        }

        /// <summary>
        /// Records the time from startTimestamp to now in a section and returns now (PerfStats.Record).
        /// </summary>
        public long Record(eClientPerfSection section, long startTimestamp)
        {
            return Stats.Record((int)section, startTimestamp);
        }

        /// <summary>
        /// Call as each Draw starts: records the time since the last one started as a Frame.
        /// </summary>
        public void StartFrame(long drawStartTimestamp)
        {
            if (_LastDrawStart != 0)
                Stats.Record((int)eClientPerfSection.Frame, PerfStats.GetElapsedMs(_LastDrawStart, drawStartTimestamp));

            _LastDrawStart = drawStartTimestamp;
        }

        /// <summary>
        /// Call once a frame. Once the window has run WINDOW_MS it ends: Lines are rebuilt from it and true is returned.
        /// inGame adds the network lines, from the game's running count of snapshots received and its ping text.
        /// </summary>
        public bool Update(long nowTimestamp, bool inGame, long snapshotsReceived, string pingText)
        {
            var windowMs = PerfStats.GetElapsedMs(_WindowStart, nowTimestamp);

            if (windowMs < WINDOW_MS)
                return false;

            BuildLines(windowMs, inGame, snapshotsReceived, pingText);
            StartWindow(nowTimestamp, snapshotsReceived);
            return true;
        }

        /// <summary>
        /// How many a running count went up by since it was start. A count lower than start was restarted (a new game),
        /// so all of it is new.
        /// </summary>
        public static long GetCountSince(long start, long count)
        {
            return count >= start ? count - start : count;
        }

        private void BuildLines(double windowMs, bool inGame, long snapshotsReceived, string pingText)
        {
            var update = Stats.Sections[(int)eClientPerfSection.Update];
            var draw = Stats.Sections[(int)eClientPerfSection.Draw];
            var frame = Stats.Sections[(int)eClientPerfSection.Frame];

            var collections = new List<string>();

            for (var generation = 0; generation < _Collections.Length; generation++)
                collections.Add((GC.CollectionCount(generation) - _Collections[generation]).ToString());

            var allocated = GC.GetTotalAllocatedBytes() - _Allocated;

            Lines.Clear();
            Lines.Add(string.Format(Const.STRING_PERF_FPS, PerfStats.GetRate(draw.Count, windowMs), frame.AverageMs, frame.MaxMs));
            Lines.Add(string.Format(Const.STRING_PERF_UPDATE, update.AverageMs, update.GetPercentileMs(PerfStats.REPORT_PERCENTILE), update.MaxMs));
            Lines.Add(string.Format(Const.STRING_PERF_DRAW, draw.AverageMs, draw.GetPercentileMs(PerfStats.REPORT_PERCENTILE), draw.MaxMs));
            Lines.Add(string.Format(Const.STRING_PERF_MEMORY, string.Join("/", collections), PerfStats.GetRate(allocated, windowMs) / BYTES_PER_MB, GC.GetTotalMemory(false) / BYTES_PER_MB));

            if (!inGame)
                return;

            var snapshots = GetCountSince(_Snapshots, snapshotsReceived);
            var received = MessageConnection.TotalBytesReceived - _Received;
            var sent = MessageConnection.TotalBytesSent - _Sent;

            Lines.Add(string.Format(Const.STRING_PERF_NETWORK, PerfStats.GetRate(snapshots, windowMs), PerfStats.GetRate(received, windowMs) / BYTES_PER_KB, PerfStats.GetRate(sent, windowMs) / BYTES_PER_KB));
            Lines.Add(pingText);
        }

        private void StartWindow(long nowTimestamp, long snapshotsReceived)
        {
            Stats.Reset();
            _WindowStart = nowTimestamp;

            for (var generation = 0; generation < _Collections.Length; generation++)
                _Collections[generation] = GC.CollectionCount(generation);

            _Allocated = GC.GetTotalAllocatedBytes();
            _Sent = MessageConnection.TotalBytesSent;
            _Received = MessageConnection.TotalBytesReceived;
            _Snapshots = snapshotsReceived;
        }
    }
}
