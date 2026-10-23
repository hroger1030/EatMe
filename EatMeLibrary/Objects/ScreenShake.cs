using System;

namespace EatMeLibrary
{
    /// <summary>
    /// A shake of the game board, for any effect that should jolt the player's view (the POW block's shockwave is
    /// one): a damped bounce, mostly up and down with a little sideways, that dies away over the shake's duration.
    /// Start one with Start, advance it every frame with Update, and add GetOffset to everything drawn on the board.
    /// </summary>
    public class ScreenShake
    {
        /// <summary>
        /// The bounce's height in pixels at the start when Start is given none.
        /// </summary>
        public const float DEFAULT_AMPLITUDE_PIXELS = 18f;

        /// <summary>
        /// How long a shake lasts when Start is given no duration.
        /// </summary>
        public const int DEFAULT_DURATION_MS = 600;

        /// <summary>
        /// A Blast event (the POW block, a bomb), and smashed walls.
        /// </summary>
        public const float BLAST_AMPLITUDE_PIXELS = 18f;
        public const int BLAST_DURATION_MS = 600;

        /// <summary>
        /// Up and down bounces a second.
        /// </summary>
        public const float BOUNCES_PER_SECOND = 12f;

        /// <summary>
        /// The sideways sway's share of the bounce's height.
        /// </summary>
        public const float SIDEWAYS_SHARE = 0.35f;

        /// <summary>
        /// The bounce dies down to about a third every this share of the duration.
        /// </summary>
        public const float DECAY_SHARE = 0.5f;

        /// <summary>
        /// Height in pixels the running shake started at.
        /// </summary>
        public float AmplitudePixels { get; private set; }

        /// <summary>
        /// How long the running shake lasts.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// How long since the running shake started.
        /// </summary>
        public TimeSpan Elapsed { get; private set; }

        public bool IsShaking => Elapsed < Duration;

        /// <summary>
        /// Starts a shake of DEFAULT_AMPLITUDE_PIXELS for DEFAULT_DURATION_MS.
        /// </summary>
        public void Start()
        {
            Start(DEFAULT_AMPLITUDE_PIXELS, TimeSpan.FromMilliseconds(DEFAULT_DURATION_MS));
        }

        /// <summary>
        /// Starts a shake. A shake already running carries on instead if it is still bouncing higher than the new one
        /// would start, so a small jolt never cuts a big one short.
        /// </summary>
        public void Start(float amplitudePixels, TimeSpan duration)
        {
            if (amplitudePixels <= 0f || duration <= TimeSpan.Zero)
                return;

            if (IsShaking && GetHeight(Elapsed, AmplitudePixels, Duration) > amplitudePixels)
                return;

            AmplitudePixels = amplitudePixels;
            Duration = duration;
            Elapsed = TimeSpan.Zero;
        }

        public void Update(TimeSpan elapsed)
        {
            if (IsShaking)
                Elapsed += elapsed;
        }

        public void Stop()
        {
            AmplitudePixels = 0f;
            Duration = TimeSpan.Zero;
            Elapsed = TimeSpan.Zero;
        }

        /// <summary>
        /// The board's offset in whole pixels now; (0, 0) when nothing is shaking.
        /// </summary>
        public (float X, float Y) GetOffset()
        {
            return GetOffset(Elapsed, AmplitudePixels, Duration);
        }

        /// <summary>
        /// The board's offset in whole pixels a time after a shake of the given height and length started; (0, 0)
        /// before it starts and once it is over.
        /// </summary>
        public static (float X, float Y) GetOffset(TimeSpan sinceStart, float amplitudePixels, TimeSpan duration)
        {
            if (sinceStart < TimeSpan.Zero || sinceStart >= duration)
                return (0f, 0f);

            var height = GetHeight(sinceStart, amplitudePixels, duration);
            var angle = 2f * MathF.PI * BOUNCES_PER_SECOND * (float)sinceStart.TotalSeconds;

            return (MathF.Round(height * SIDEWAYS_SHARE * MathF.Sin(angle * 1.3f)), MathF.Round(height * MathF.Cos(angle)));
        }

        /// <summary>
        /// How high the bounce is a time into a shake, dying away by DECAY_SHARE of its duration; 0 once it is over.
        /// </summary>
        public static float GetHeight(TimeSpan sinceStart, float amplitudePixels, TimeSpan duration)
        {
            if (sinceStart < TimeSpan.Zero || sinceStart >= duration)
                return 0f;

            return amplitudePixels * MathF.Exp((float)(-sinceStart.TotalMilliseconds / (duration.TotalMilliseconds * DECAY_SHARE)));
        }
    }
}
