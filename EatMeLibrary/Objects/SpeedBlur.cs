namespace EatMeLibrary
{
    /// <summary>
    /// The radial speed blur on the game board while the player's own pac goes fast (Warp Speed, Melon Roll while it
    /// rolls, Rolling Thunder): the client's BoardGrade shader averages TAPS samples along the line to the own pac,
    /// so everything streaks away from it. GetSampleScale mirrors the shader.
    /// </summary>
    public static class SpeedBlur
    {
        /// <summary>
        /// Samples along each streak, the shader's loop count.
        /// </summary>
        public const int TAPS = 6;

        /// <summary>
        /// How long the streaks are, as a share of the distance to the own pac.
        /// </summary>
        public const float WARP_SPEED_STRENGTH = 0.05f;
        public const float ROLLING_THUNDER_STRENGTH = 0.06f;
        public const float MELON_ROLL_STRENGTH = 0.08f;

        /// <summary>
        /// The streaks' strength for the own pac's effect and whether it is rolling; 0 for no blur.
        /// </summary>
        public static float GetStrength(ePowerUp effect, bool rolling)
        {
            if (rolling)
                return MELON_ROLL_STRENGTH;

            return effect switch
            {
                ePowerUp.Solarian => WARP_SPEED_STRENGTH,
                ePowerUp.RollingThunder => ROLLING_THUNDER_STRENGTH,
                _ => 0f,
            };
        }

        /// <summary>
        /// Where a sample along a streak is, as a share of the way from the own pac to the pixel: 1 for the pixel
        /// itself, down to 1 - strength for the last tap.
        /// </summary>
        public static float GetSampleScale(int tap, float strength)
        {
            return 1f - strength * tap / (TAPS - 1);
        }
    }
}
