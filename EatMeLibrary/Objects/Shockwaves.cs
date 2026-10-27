using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// One shockwave ring on the game board: where it went off, in map cells, and when, on the client's clock.
    /// </summary>
    public struct Shockwave
    {
        public float X { get; }
        public float Y { get; }
        public long StartMs { get; }

        public Shockwave(float x, float y, long startMs)
        {
            X = x;
            Y = y;
            StartMs = startMs;
        }
    }

    /// <summary>
    /// The shockwave rings on the game board: a ring that bends the board's pixels as it races out from a blast (the
    /// POW block, a Stun Bomb) or a BFG burst to the standard radius, travelling with the visible ring (ShockRings: the
    /// same timing and radius) and fading as it does. The client's BoardDistort shader draws up to MAX_SHOCKWAVES of
    /// them; GetOffset mirrors its sums.
    /// </summary>
    public class Shockwaves
    {
        /// <summary>
        /// The most rings on the board at once (the shader's array size); a new one replaces the oldest.
        /// </summary>
        public const int MAX_SHOCKWAVES = 6;

        /// <summary>
        /// How long a ring takes to reach the standard radius, fading as it goes: the visible ring's time, so they
        /// travel together.
        /// </summary>
        public const int DURATION_MS = ShockRings.DURATION_MS;

        /// <summary>
        /// How far the ring pushes pixels at the start.
        /// </summary>
        public const float AMPLITUDE_PIXELS = 32f;

        /// <summary>
        /// How wide the ring is, in cells, from its inside edge to its outside edge.
        /// </summary>
        public const float WIDTH_CELLS = 3f;

        /// <summary>
        /// A new ring this soon after another one within MERGE_CELLS of it is left out, so blasts going off together in
        /// one spot make one ripple.
        /// </summary>
        public const int MIN_GAP_MS = 200;
        public const float MERGE_CELLS = 2f;

        private readonly List<Shockwave> _Waves = new(MAX_SHOCKWAVES);

        /// <summary>
        /// The rings still going, oldest first.
        /// </summary>
        public IReadOnlyList<Shockwave> Waves => _Waves;

        /// <summary>
        /// Starts a ring at a map position, unless one started under MIN_GAP_MS ago within MERGE_CELLS of it. Rings that
        /// have finished are dropped first, and with MAX_SHOCKWAVES still going the oldest makes way.
        /// </summary>
        public void Add(float x, float y, long nowMs)
        {
            Update(nowMs);

            foreach (var wave in _Waves)
                if (nowMs - wave.StartMs < MIN_GAP_MS && (wave.X - x) * (wave.X - x) + (wave.Y - y) * (wave.Y - y) <= MERGE_CELLS * MERGE_CELLS)
                    return;

            if (_Waves.Count >= MAX_SHOCKWAVES)
                _Waves.RemoveAt(0);

            _Waves.Add(new Shockwave(x, y, nowMs));
        }

        /// <summary>
        /// Drops the rings that have finished.
        /// </summary>
        public void Update(long nowMs)
        {
            for (var i = _Waves.Count - 1; i > -1; i--)
                if (nowMs - _Waves[i].StartMs >= DURATION_MS)
                    _Waves.RemoveAt(i);
        }

        public void Clear()
        {
            _Waves.Clear();
        }

        /// <summary>
        /// How far out a ring is a time after it started, in cells: exactly where the visible ring is
        /// (ShockRings.GetRadiusCells), reaching PowerUpEffects.EFFECT_RADIUS at DURATION_MS.
        /// </summary>
        public static float GetRadiusCells(TimeSpan sinceStart)
        {
            return ShockRings.GetRadiusCells(sinceStart);
        }

        /// <summary>
        /// How hard a ring pushes a time after it started, in pixels: AMPLITUDE_PIXELS at the start, holding up and
        /// then fading to 0 at DURATION_MS, as the visible ring's brightness does (ShockRings.GetAlpha).
        /// </summary>
        public static float GetAmplitude(TimeSpan sinceStart)
        {
            if (sinceStart < TimeSpan.Zero || sinceStart.TotalMilliseconds >= DURATION_MS)
                return 0f;

            return AMPLITUDE_PIXELS * ShockRings.GetAlpha(sinceStart);
        }

        /// <summary>
        /// How far a ring moves the pixel a distance from its centre, along the line from the centre (outward when
        /// positive), as the shader does: a sine wave over the ring's width, zero on the ring's middle line and away
        /// from it, and fading to the ring's edges.
        /// </summary>
        public static float GetOffset(float distancePixels, float radiusPixels, float widthPixels, float amplitude)
        {
            if (widthPixels <= 0f)
                return 0f;

            var band = (distancePixels - radiusPixels) / (widthPixels * 0.5f);
            if (MathF.Abs(band) >= 1f)
                return 0f;

            return MathF.Sin(band * MathF.PI) * amplitude * (1f - MathF.Abs(band));
        }
    }
}
