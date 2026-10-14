using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// The global sounds (announcer clips and the flag capture sting) waiting to play, oldest first, so they play one
    /// at a time instead of over each other. A clip already waiting isn't added again, and a clip that has waited
    /// longer than MAX_WAIT_MS is dropped rather than played late.
    /// </summary>
    public class AnnouncementQueue
    {
        public const long MAX_WAIT_MS = 3000;

        private struct PendingClip
        {
            public string Name;
            public long QueuedMs;
        }

        private readonly Queue<PendingClip> _Pending = new();

        public int Count => _Pending.Count;

        /// <summary>
        /// Adds a clip to the end of the queue, unless the same clip is already waiting.
        /// </summary>
        public void Enqueue(string name, long nowMs)
        {
            if (string.IsNullOrEmpty(name))
                return;

            foreach (var pending in _Pending)
            {
                if (pending.Name == name)
                    return;
            }

            _Pending.Enqueue(new PendingClip { Name = name, QueuedMs = nowMs });
        }

        /// <summary>
        /// Takes the oldest clip that hasn't waited longer than MAX_WAIT_MS, dropping any that have. False if none.
        /// </summary>
        public bool TryDequeue(long nowMs, out string name)
        {
            while (_Pending.Count > 0)
            {
                var pending = _Pending.Dequeue();

                if (nowMs - pending.QueuedMs <= MAX_WAIT_MS)
                {
                    name = pending.Name;
                    return true;
                }
            }

            name = null;
            return false;
        }

        public void Clear()
        {
            _Pending.Clear();
        }
    }
}
