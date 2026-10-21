using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EatMeLibrary
{
    /// <summary>
    /// Times named pieces of work (PerfSection) with the Stopwatch's high resolution timestamps, for perf logging.
    /// Sections are indexed by number in the order their names were given. Recording allocates nothing, so it can
    /// run every tick.
    /// </summary>
    public class PerfStats
    {
        /// <summary>
        /// ms each histogram bucket covers.
        /// </summary>
        public const double BUCKET_MS = 0.05;

        /// <summary>
        /// Histogram buckets per section, so percentiles are exact to BUCKET_MS up to 50 ms.
        /// </summary>
        public const int BUCKET_COUNT = 1000;

        /// <summary>
        /// The percentile reports show, as a share.
        /// </summary>
        public const double REPORT_PERCENTILE = 0.99;

        private readonly List<PerfSection> _Sections = new();

        public IReadOnlyList<PerfSection> Sections => _Sections;

        public PerfStats(IEnumerable<string> names)
        {
            foreach (var name in names)
                _Sections.Add(new PerfSection(name));
        }

        public static long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// ms between two timestamps from GetTimestamp.
        /// </summary>
        public static double GetElapsedMs(long startTimestamp, long endTimestamp)
        {
            return (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        /// <summary>
        /// How many of something a second, counted over windowMs.
        /// </summary>
        public static double GetRate(double count, double windowMs)
        {
            return windowMs <= 0 ? 0 : count * 1000 / windowMs;
        }

        /// <summary>
        /// Records the time from startTimestamp to now in a section and returns now, so the next section can
        /// start from it.
        /// </summary>
        public long Record(int section, long startTimestamp)
        {
            var now = GetTimestamp();
            _Sections[section].Add(GetElapsedMs(startTimestamp, now));
            return now;
        }

        public void Record(int section, double ms)
        {
            _Sections[section].Add(ms);
        }

        public void Reset()
        {
            foreach (var section in _Sections)
                section.Reset();
        }
    }
}
