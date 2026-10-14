using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// First in, first out list of eaten dots. Once it holds Threshold cells (DotRegenPercent of the level's dots),
    /// every dot eaten puts the oldest eaten one back, so that share of the dots stays eaten. The queue's buffer is
    /// sized at Reset, so adding never allocates.
    /// </summary>
    public class DotRegenQueue
    {
        public const int MIN_PERCENT = 0;
        public const int MAX_PERCENT = 100;

        private const int PERCENT_SCALE = 100;

        private readonly Queue<EatenCell> _Cells = new();

        /// <summary>
        /// Eaten dots held before the oldest starts coming back.
        /// </summary>
        public int Threshold { get; private set; }

        public int Count => _Cells.Count;

        /// <summary>
        /// Empties the queue for a level with dotCount dots, percent (clamped to 0-100) of which stay eaten.
        /// </summary>
        public void Reset(int dotCount, int percent)
        {
            _Cells.Clear();
            Threshold = GetThreshold(dotCount, percent);

            // one over the dot count, since Add enqueues before it dequeues
            _Cells.EnsureCapacity(Math.Max(dotCount, 0) + 1);
        }

        /// <summary>
        /// Number of eaten dots held before they start coming back: percent (clamped to 0-100) of dotCount.
        /// </summary>
        public static int GetThreshold(int dotCount, int percent)
        {
            return Math.Max(dotCount, 0) * Math.Clamp(percent, MIN_PERCENT, MAX_PERCENT) / PERCENT_SCALE;
        }

        /// <summary>
        /// Records the dot eaten at (x, y). Returns true, with the oldest eaten cell in restored, when the queue has
        /// gone over Threshold and that dot should be put back.
        /// </summary>
        public bool Add(int x, int y, out EatenCell restored)
        {
            _Cells.Enqueue(new EatenCell(x, y));

            if (_Cells.Count > Threshold)
            {
                restored = _Cells.Dequeue();
                return true;
            }

            restored = default;
            return false;
        }

        /// <summary>
        /// The eaten cells, oldest first. Queue's enumerator is a struct, so foreach doesn't allocate.
        /// </summary>
        public Queue<EatenCell>.Enumerator GetEnumerator()
        {
            return _Cells.GetEnumerator();
        }
    }
}
