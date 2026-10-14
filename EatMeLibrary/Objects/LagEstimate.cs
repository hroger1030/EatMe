using System;

namespace EatMeLibrary
{
    /// <summary>
    /// A running average of one player's round trip time, measured by the server with ServerPing/ServerPong.
    /// Each sample moves the average part of the way, so one spike doesn't swing it.
    /// </summary>
    public class LagEstimate
    {
        /// <summary>
        /// Share of the gap between the average and a new sample that the average moves by.
        /// </summary>
        public const float SMOOTHING = 0.2f;

        /// <summary>
        /// ms between the server's pings to each player.
        /// </summary>
        public const int PING_INTERVAL_MS = 1000;

        /// <summary>
        /// The smoothed round trip in ms, or -1 before the first sample.
        /// </summary>
        public int SmoothedMs { get; private set; } = -1;

        /// <summary>
        /// Adds one measured round trip. The first is taken as it is; negative samples are ignored.
        /// </summary>
        public void AddSample(int roundTripMs)
        {
            if (roundTripMs < 0)
                return;

            SmoothedMs = SmoothedMs < 0 ? roundTripMs : SmoothedMs + (int)Math.Round((roundTripMs - SmoothedMs) * SMOOTHING);
        }
    }
}
