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
        public const float DEFAULT_DURATION_SECONDS = 0.6f;

        /// <summary>
        /// The POW block's shockwave.
        /// </summary>
        public const float POW_AMPLITUDE_PIXELS = 18f;
        public const float POW_DURATION_SECONDS = 0.6f;

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
        public float DurationSeconds { get; private set; }

        /// <summary>
        /// Seconds since the running shake started.
        /// </summary>
        public float ElapsedSeconds { get; private set; }

        public bool IsShaking => ElapsedSeconds < DurationSeconds;

        /// <summary>
        /// Starts a shake. A shake already running carries on instead if it is still bouncing higher than the new one
        /// would start, so a small jolt never cuts a big one short.
        /// </summary>
        public void Start(float amplitudePixels = DEFAULT_AMPLITUDE_PIXELS, float durationSeconds = DEFAULT_DURATION_SECONDS)
        {
            if (amplitudePixels <= 0f || durationSeconds <= 0f)
                return;

            if (IsShaking && GetHeight(ElapsedSeconds, AmplitudePixels, DurationSeconds) > amplitudePixels)
                return;

            AmplitudePixels = amplitudePixels;
            DurationSeconds = durationSeconds;
            ElapsedSeconds = 0f;
        }

        public void Update(float deltaSeconds)
        {
            if (IsShaking)
                ElapsedSeconds += deltaSeconds;
        }

        public void Stop()
        {
            AmplitudePixels = 0f;
            DurationSeconds = 0f;
            ElapsedSeconds = 0f;
        }

        /// <summary>
        /// The board's offset in whole pixels now; (0, 0) when nothing is shaking.
        /// </summary>
        public (float X, float Y) GetOffset()
        {
            return GetOffset(ElapsedSeconds, AmplitudePixels, DurationSeconds);
        }

        /// <summary>
        /// The board's offset in whole pixels a time in seconds after a shake of the given height and length started;
        /// (0, 0) before it starts and once it is over.
        /// </summary>
        public static (float X, float Y) GetOffset(float secondsSinceStart, float amplitudePixels, float durationSeconds)
        {
            if (secondsSinceStart < 0f || secondsSinceStart >= durationSeconds)
                return (0f, 0f);

            var height = GetHeight(secondsSinceStart, amplitudePixels, durationSeconds);
            var angle = 2f * MathF.PI * BOUNCES_PER_SECOND * secondsSinceStart;

            return (MathF.Round(height * SIDEWAYS_SHARE * MathF.Sin(angle * 1.3f)), MathF.Round(height * MathF.Cos(angle)));
        }

        /// <summary>
        /// How high the bounce is a time in seconds into a shake, dying away by DECAY_SHARE of its duration; 0 once
        /// it is over.
        /// </summary>
        public static float GetHeight(float secondsSinceStart, float amplitudePixels, float durationSeconds)
        {
            if (secondsSinceStart < 0f || secondsSinceStart >= durationSeconds)
                return 0f;

            return amplitudePixels * MathF.Exp(-secondsSinceStart / (durationSeconds * DECAY_SHARE));
        }
    }
}
