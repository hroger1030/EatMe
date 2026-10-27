using System;

namespace EatMeLibrary
{
    /// <summary>
    /// A chromatic aberration kick on the game board, for explosions (a Blast, a BFG burst, smashed walls): red, green
    /// and blue split apart from the board's centre by a few pixels and snap back together over a moment. Start one
    /// with Start, advance it every frame with Update and give GetPixels to the board's shader. Like ScreenShake.
    /// </summary>
    public class ChromaticKick
    {
        /// <summary>
        /// How far red and blue split from green at the board's edge when a Blast, a BFG burst or smashed walls kick.
        /// </summary>
        public const float BLAST_PIXELS = 6f;
        public const int BLAST_DURATION_MS = 220;

        /// <summary>
        /// How far the running kick split at the start.
        /// </summary>
        public float PixelsAtStart { get; private set; }

        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// How long since the running kick started.
        /// </summary>
        public TimeSpan Elapsed { get; private set; }

        public bool IsKicking => Elapsed < Duration;

        /// <summary>
        /// Starts a kick. One already running carries on instead if it is still split wider than the new one would
        /// start, so a small kick never cuts a big one short.
        /// </summary>
        public void Start(float pixels, TimeSpan duration)
        {
            if (pixels <= 0f || duration <= TimeSpan.Zero)
                return;

            if (IsKicking && GetPixels(Elapsed, PixelsAtStart, Duration) > pixels)
                return;

            PixelsAtStart = pixels;
            Duration = duration;
            Elapsed = TimeSpan.Zero;
        }

        public void Update(TimeSpan elapsed)
        {
            if (IsKicking)
                Elapsed += elapsed;
        }

        public void Stop()
        {
            PixelsAtStart = 0f;
            Duration = TimeSpan.Zero;
            Elapsed = TimeSpan.Zero;
        }

        /// <summary>
        /// How far apart the channels are now, in pixels at the board's edge; 0 when nothing is kicking.
        /// </summary>
        public float GetPixels()
        {
            return GetPixels(Elapsed, PixelsAtStart, Duration);
        }

        /// <summary>
        /// How far apart the channels are a time after a kick of the given size and length started: the full split at
        /// the start, closing in a straight line to 0 at the end, and 0 before it starts and once it is over.
        /// </summary>
        public static float GetPixels(TimeSpan sinceStart, float pixels, TimeSpan duration)
        {
            if (sinceStart < TimeSpan.Zero || sinceStart >= duration)
                return 0f;

            return pixels * (1f - (float)(sinceStart.TotalMilliseconds / duration.TotalMilliseconds));
        }
    }
}
