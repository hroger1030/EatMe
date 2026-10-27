using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// One visible shock ring on the game board: where it went off, in map cells, what set it off, and when, on the
    /// client's clock.
    /// </summary>
    public struct ShockRing
    {
        public float X { get; }
        public float Y { get; }
        public eGameEvent EventType { get; }
        public long StartMs { get; }

        public ShockRing(float x, float y, eGameEvent eventType, long startMs)
        {
            X = x;
            Y = y;
            EventType = eventType;
            StartMs = startMs;
        }
    }

    /// <summary>
    /// The visible shock rings on the game board: a bright ring that races out from a blast (the POW block, a Stun
    /// Bomb) or a BFG burst to the standard radius (PowerUpEffects.EFFECT_RADIUS) in DURATION_MS, thinning and fading
    /// as it goes, drawn by the client's ShockRing shader as a bright core with a soft glow either side. GetCore and
    /// GetGlow mirror its sums. The pixel-bending ripple under it is Shockwaves.
    /// </summary>
    public class ShockRings
    {
        /// <summary>
        /// The most rings on the board at once; a new one replaces the oldest.
        /// </summary>
        public const int MAX_RINGS = 6;

        /// <summary>
        /// How long a ring takes to reach the standard radius, by when it has faded away.
        /// </summary>
        public const int DURATION_MS = 350;

        /// <summary>
        /// The ring's core width in screen pixels as it starts and as it reaches the radius.
        /// </summary>
        public const float START_WIDTH_PIXELS = 12f;
        public const float END_WIDTH_PIXELS = 5f;

        /// <summary>
        /// How far the soft glow reaches either side of the ring's middle line, in half core widths.
        /// </summary>
        public const float GLOW_SPREAD = 3f;

        private readonly List<ShockRing> _Rings = new(MAX_RINGS);

        /// <summary>
        /// The rings still going, oldest first.
        /// </summary>
        public IReadOnlyList<ShockRing> Rings => _Rings;

        /// <summary>
        /// True for the events that send out a visible ring: a Blast (the POW block, a Stun Bomb) and a BFG burst.
        /// </summary>
        public static bool IsSource(eGameEvent eventType)
        {
            return eventType == eGameEvent.Blast || eventType == eGameEvent.BfgBurst;
        }

        /// <summary>
        /// Starts a ring at a map position. Rings that have finished are dropped first, and with MAX_RINGS still going
        /// the oldest makes way.
        /// </summary>
        public void Add(float x, float y, eGameEvent eventType, long nowMs)
        {
            Update(nowMs);

            if (_Rings.Count >= MAX_RINGS)
                _Rings.RemoveAt(0);

            _Rings.Add(new ShockRing(x, y, eventType, nowMs));
        }

        /// <summary>
        /// Drops the rings that have reached the radius.
        /// </summary>
        public void Update(long nowMs)
        {
            for (var i = _Rings.Count - 1; i > -1; i--)
                if (nowMs - _Rings[i].StartMs >= DURATION_MS)
                    _Rings.RemoveAt(i);
        }

        public void Clear()
        {
            _Rings.Clear();
        }

        /// <summary>
        /// How far out a ring is a time after it started, in cells: very fast at first, slowing as it reaches
        /// PowerUpEffects.EFFECT_RADIUS at DURATION_MS.
        /// </summary>
        public static float GetRadiusCells(TimeSpan sinceStart)
        {
            var left = 1f - GetProgress(sinceStart);
            return PowerUpEffects.EFFECT_RADIUS * (1f - left * left * left);
        }

        /// <summary>
        /// The ring's core width in screen pixels a time after it started: START_WIDTH_PIXELS thinning in a straight
        /// line to END_WIDTH_PIXELS at DURATION_MS.
        /// </summary>
        public static float GetWidthPixels(TimeSpan sinceStart)
        {
            return START_WIDTH_PIXELS + (END_WIDTH_PIXELS - START_WIDTH_PIXELS) * GetProgress(sinceStart);
        }

        /// <summary>
        /// How bright the ring is a time after it started: 1 at the start, staying bright then fading to 0 at
        /// DURATION_MS, and 0 outside that time.
        /// </summary>
        public static float GetAlpha(TimeSpan sinceStart)
        {
            if (sinceStart < TimeSpan.Zero || sinceStart.TotalMilliseconds >= DURATION_MS)
                return 0f;

            var t = GetProgress(sinceStart);
            return 1f - t * t;
        }

        /// <summary>
        /// How far a pixel's distance from the centre is from the ring's middle line, in half core widths, as the shader
        /// works it out: 0 on the line, 1 at the core's edge.
        /// </summary>
        public static float GetBand(float distancePixels, float radiusPixels, float widthPixels)
        {
            if (widthPixels <= 0f)
                return float.MaxValue;

            return MathF.Abs(distancePixels - radiusPixels) / (widthPixels * 0.5f);
        }

        /// <summary>
        /// The bright core's strength at a band (GetBand): 1 on the ring's middle line, falling in a straight line to 0
        /// at the core's edge.
        /// </summary>
        public static float GetCore(float band)
        {
            return Math.Clamp(1f - band, 0f, 1f);
        }

        /// <summary>
        /// The soft glow's strength at a band (GetBand): 1 on the ring's middle line, falling off smoothly to 0 at
        /// GLOW_SPREAD.
        /// </summary>
        public static float GetGlow(float band)
        {
            var share = Math.Clamp(1f - band / GLOW_SPREAD, 0f, 1f);
            return share * share;
        }

        /// <summary>
        /// How far from its centre a ring's glow reaches, in screen pixels, for one drawn at radiusPixels and
        /// widthPixels wide: the size of the square drawn for it is twice this.
        /// </summary>
        public static float GetReachPixels(float radiusPixels, float widthPixels)
        {
            return radiusPixels + widthPixels * 0.5f * GLOW_SPREAD;
        }

        private static float GetProgress(TimeSpan sinceStart)
        {
            return Math.Clamp((float)(sinceStart.TotalMilliseconds / DURATION_MS), 0f, 1f);
        }
    }
}
