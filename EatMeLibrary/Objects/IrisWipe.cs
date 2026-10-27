using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The iris wipe on the game board: a circle centred on the player's own pac opens from black as the start
    /// countdown begins and closes back to black at the end of the victory pause, before the results. The client's
    /// BoardGrade shader blacks out everything outside the circle (GetCoverage mirrors it).
    /// </summary>
    public static class IrisWipe
    {
        /// <summary>
        /// How long the circle takes to open or close.
        /// </summary>
        public const int IRIS_MS = 700;

        /// <summary>
        /// The circle's soft edge, in pixels.
        /// </summary>
        public const float EDGE_PIXELS = 4f;

        /// <summary>
        /// How far open the circle is, 0 (closed, all black) to 1 (open). nowMs is the client's clock, countdownEndMs
        /// when the start countdown ends (ClientGameState.CountdownEndMs) and victoryStartMs when "Victory!" showed,
        /// while isVictory. It opens over the first IRIS_MS of the countdown and closes over the last IRIS_MS of
        /// LevelPhases.VICTORY_MS, easing in and out.
        /// </summary>
        public static float GetOpenFraction(long nowMs, long countdownEndMs, bool isVictory, long victoryStartMs)
        {
            if (isVictory)
            {
                var closeStartMs = victoryStartMs + LevelPhases.VICTORY_MS - IRIS_MS;
                return 1f - Ease((float)(nowMs - closeStartMs) / IRIS_MS);
            }

            var countdownStartMs = countdownEndMs - LevelPhases.START_COUNTDOWN_MS;
            return Ease((float)(nowMs - countdownStartMs) / IRIS_MS);
        }

        /// <summary>
        /// The circle's radius in pixels when open by openFraction, round a centre in a board width by height pixels:
        /// open is far enough to reach the board's farthest corner.
        /// </summary>
        public static float GetRadius(float openFraction, float centreX, float centreY, float width, float height)
        {
            var farX = MathF.Max(centreX, width - centreX);
            var farY = MathF.Max(centreY, height - centreY);
            return Math.Clamp(openFraction, 0f, 1f) * (MathF.Sqrt(farX * farX + farY * farY) + EDGE_PIXELS);
        }

        /// <summary>
        /// How much of a pixel a distance from the centre still shows, 1 inside the circle to 0 outside, across the
        /// soft edge.
        /// </summary>
        public static float GetCoverage(float distancePixels, float radiusPixels)
        {
            return Math.Clamp((radiusPixels - distancePixels) / EDGE_PIXELS, 0f, 1f);
        }

        private static float Ease(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
