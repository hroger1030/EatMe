using System;

namespace EatMeLibrary
{
    /// <summary>
    /// An invincible (Starman) pac is drawn in the rainbow, one color at a time: the client's StarRainbow shader colors
    /// the whole white pac sprite with the Rainbow gradient at GetPhase, running round the colors every CYCLE_MS.
    /// GetColor mirrors its sums; the mini map uses it too.
    /// </summary>
    public static class StarRainbow
    {
        /// <summary>
        /// How long the rainbow takes to run once round the seven colors.
        /// </summary>
        public const int CYCLE_MS = 600;

        /// <summary>
        /// True if a pac with this effect is drawn in the rainbow: Invincible.
        /// </summary>
        public static bool IsRainbow(ePowerUp effect)
        {
            return effect == ePowerUp.Starman;
        }

        /// <summary>
        /// How far round the colors the rainbow has run after a time, 0 to 1. The shader takes it as its Phase.
        /// </summary>
        public static float GetPhase(TimeSpan time)
        {
            var cycles = (float)(time.TotalMilliseconds / CYCLE_MS);
            return cycles - MathF.Floor(cycles);
        }

        /// <summary>
        /// The one color the whole pac is after a time, as the shader works it out.
        /// </summary>
        public static FastColor GetColor(TimeSpan time)
        {
            return Rainbow.GetGradientColor(GetPhase(time));
        }
    }
}
