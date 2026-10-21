using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The times recorded for one named piece of work over a reporting window: how many, the total, the slowest,
    /// and a histogram of PerfStats.BUCKET_MS wide buckets for percentiles. Adding a time allocates nothing.
    /// </summary>
    public class PerfSection
    {
        private readonly int[] _Buckets = new int[PerfStats.BUCKET_COUNT];

        public string Name { get; }

        public int Count { get; private set; }

        public double TotalMs { get; private set; }

        public double MaxMs { get; private set; }

        public double AverageMs => Count == 0 ? 0 : TotalMs / Count;

        public PerfSection(string name)
        {
            Name = name;
        }

        public void Add(double ms)
        {
            ms = Math.Max(0, ms);
            Count++;
            TotalMs += ms;
            MaxMs = Math.Max(MaxMs, ms);

            // anything slower than the histogram covers lands in the last bucket
            var bucket = Math.Min((int)(ms / PerfStats.BUCKET_MS), PerfStats.BUCKET_COUNT - 1);
            _Buckets[bucket]++;
        }

        /// <summary>
        /// The time that share (0 to 1) of the recorded times are at or under: the top of the bucket that share
        /// is reached in, never more than MaxMs. 0 with nothing recorded.
        /// </summary>
        public double GetPercentileMs(double share)
        {
            if (Count == 0)
                return 0;

            var needed = Math.Max(1, (int)Math.Ceiling(Count * Math.Clamp(share, 0, 1)));
            var seen = 0;

            for (var bucket = 0; bucket < PerfStats.BUCKET_COUNT; bucket++)
            {
                seen += _Buckets[bucket];

                // the last bucket holds everything slower, so its top is the slowest time
                if (seen >= needed)
                    return bucket == PerfStats.BUCKET_COUNT - 1 ? MaxMs : Math.Min((bucket + 1) * PerfStats.BUCKET_MS, MaxMs);
            }

            return MaxMs;
        }

        public void Reset()
        {
            Count = 0;
            TotalMs = 0;
            MaxMs = 0;
            Array.Clear(_Buckets);
        }
    }
}
