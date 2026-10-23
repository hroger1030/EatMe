using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// The speed trails behind pacs with an effect that leaves one (PowerUpEffects.HasSpeedTrail): the last few
    /// positions (in map cells) of each such pac, newest first, sampled every TRAIL_SAMPLE_MS. A pac's trail is
    /// dropped when it loses the effect, dies, leaves the snapshot or jumps further than MAX_TRAIL_STEP in one sample (a warp tunnel or a
    /// respawn), so no streak is drawn across the map.
    /// </summary>
    public class PacTrails
    {
        public const int TRAIL_SAMPLE_MS = 40;

        /// <summary>
        /// Positions kept per pac, so copies drawn behind it.
        /// </summary>
        public const int TRAIL_LENGTH = 10;

        /// <summary>
        /// Further than this (in cells) between two samples is a jump, not movement.
        /// </summary>
        public const float MAX_TRAIL_STEP = 1f;

        /// <summary>
        /// The newest copy's opacity before GetAlpha scales it down.
        /// </summary>
        public const float TRAIL_START_ALPHA = 0.5f;

        private readonly Dictionary<int, List<(float X, float Y)>> _Trails = new();
        private readonly HashSet<int> _Seen = new();
        private readonly List<int> _Stale = new();
        private TimeSpan _SampleTimer;

        /// <summary>
        /// Takes this frame's pacs and how long the frame took.
        /// </summary>
        public void Update(IEnumerable<PacState> pacs, TimeSpan elapsed)
        {
            _SampleTimer += elapsed;
            var sample = _SampleTimer.TotalMilliseconds >= TRAIL_SAMPLE_MS;

            if (sample)
                _SampleTimer = TimeSpan.FromTicks(_SampleTimer.Ticks % (TRAIL_SAMPLE_MS * TimeSpan.TicksPerMillisecond));

            _Seen.Clear();

            if (pacs != null)
            {
                foreach (var pac in pacs)
                {
                    if (!pac.Alive || !PowerUpEffects.HasSpeedTrail(pac.PowerUpEffect))
                        continue;

                    _Seen.Add(pac.PlayerId);

                    if (!_Trails.TryGetValue(pac.PlayerId, out var trail))
                    {
                        trail = new List<(float X, float Y)>(TRAIL_LENGTH + 1);
                        _Trails[pac.PlayerId] = trail;
                    }

                    if (trail.Count > 0 && IsJump(trail[0].X, trail[0].Y, pac.X, pac.Y))
                        trail.Clear();

                    if (!sample && trail.Count > 0)
                        continue;

                    trail.Insert(0, (pac.X, pac.Y));

                    if (trail.Count > TRAIL_LENGTH)
                        trail.RemoveAt(trail.Count - 1);
                }
            }

            _Stale.Clear();
            foreach (var id in _Trails.Keys)
            {
                if (!_Seen.Contains(id))
                    _Stale.Add(id);
            }

            foreach (var id in _Stale)
                _Trails.Remove(id);
        }

        /// <summary>
        /// A pac's trail, newest position first, or an empty list if it has none.
        /// </summary>
        public IReadOnlyList<(float X, float Y)> GetTrail(int playerId)
        {
            return _Trails.TryGetValue(playerId, out var trail) ? trail : Array.Empty<(float X, float Y)>();
        }

        public void Clear()
        {
            _Trails.Clear();
            _SampleTimer = TimeSpan.Zero;
        }

        /// <summary>
        /// How opaque a trail copy is, from 0 (newest) to TRAIL_LENGTH - 1 (oldest): fading from TRAIL_START_ALPHA
        /// down towards nothing.
        /// </summary>
        public static float GetAlpha(int index)
        {
            if (index < 0 || index >= TRAIL_LENGTH)
                return 0f;

            return TRAIL_START_ALPHA * (TRAIL_LENGTH - index) / (TRAIL_LENGTH + 1);
        }

        /// <summary>
        /// True when two positions are too far apart to be one sample of movement.
        /// </summary>
        public static bool IsJump(float fromX, float fromY, float toX, float toY)
        {
            return MathF.Abs(toX - fromX) > MAX_TRAIL_STEP || MathF.Abs(toY - fromY) > MAX_TRAIL_STEP;
        }
    }
}
