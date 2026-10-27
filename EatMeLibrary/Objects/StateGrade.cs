using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Grades the game board (not the HUD) by how the player's own pac is doing: a cool, colorful pulse with a power
    /// pill, greyed and wobbling while stunned, red edges pulsing as it starves, and at the end of a level a white flash
    /// then a warm grade for a win or a cold one for a loss. The client's board shaders apply it; ApplyGrade and
    /// GetEdgeWeight mirror their sums.
    /// </summary>
    public static class StateGrade
    {
        /// <summary>
        /// A power pill: at the top of each pulse the board is this colorful and this cool.
        /// </summary>
        public const float POWER_SATURATION = 1.4f;
        public const float POWER_TINT_R = 0.85f;
        public const float POWER_TINT_G = 0.95f;
        public const float POWER_TINT_B = 1.2f;

        /// <summary>
        /// Stunned: the board is this grey and wobbles this far sideways, a wave down the board every WOBBLE_CYCLE_MS.
        /// </summary>
        public const float STUN_SATURATION = 0.2f;
        public const float STUN_WOBBLE_PIXELS = 3f;
        public const int WOBBLE_CYCLE_MS = 1200;

        /// <summary>
        /// How tightly the stun wobble's waves are packed down the board, in radians per pixel.
        /// </summary>
        public const float WOBBLE_WAVES_PER_PIXEL = 0.02f;

        /// <summary>
        /// Starving: the red edges start once the hunger meter is past STARVE_START_FILL, pulse every STARVE_PULSE_MS
        /// and reach STARVE_EDGE_MAX when it is full.
        /// </summary>
        public const float STARVE_START_FILL = 0.5f;
        public const int STARVE_PULSE_MS = 700;
        public const float STARVE_EDGE_MAX = 0.75f;

        /// <summary>
        /// How far into the board the red edges reach: higher keeps them nearer the edge.
        /// </summary>
        public const float EDGE_POWER = 3f;

        /// <summary>
        /// The end of a level: a white flash fading over FLASH_MS, while the win or loss grade fades in over FADE_MS.
        /// </summary>
        public const int FLASH_MS = 250;
        public const int FADE_MS = 600;

        public const float VICTORY_SATURATION = 1.2f;
        public const float VICTORY_TINT_R = 1.15f;
        public const float VICTORY_TINT_G = 1.0f;
        public const float VICTORY_TINT_B = 0.8f;

        public const float DEFEAT_SATURATION = 0.55f;
        public const float DEFEAT_TINT_R = 0.75f;
        public const float DEFEAT_TINT_G = 0.85f;
        public const float DEFEAT_TINT_B = 1.15f;

        /// <summary>
        /// The board's grade. powered, stunned and hungerFill are the own living pac's (false, false and 0 when it is
        /// dead); victoryTeam is the level's winner while "Victory!" shows (sinceVictory since then), else null; time is
        /// the animation clock and powerPulseMs the power pill pulse's period, so the grade pulses with the pac.
        /// </summary>
        public static BoardGrade GetGrade(bool powered, bool stunned, float hungerFill, eTeam? victoryTeam, eTeam ownTeam,
            TimeSpan sinceVictory, TimeSpan time, int powerPulseMs)
        {
            var grade = BoardGrade.None;

            if (victoryTeam.HasValue)
            {
                var won = victoryTeam.Value == ownTeam;
                var fade = Math.Clamp((float)(sinceVictory.TotalMilliseconds / FADE_MS), 0f, 1f);

                grade.Flash = 1f - Math.Clamp((float)(sinceVictory.TotalMilliseconds / FLASH_MS), 0f, 1f);
                grade.Saturation = Lerp(1f, won ? VICTORY_SATURATION : DEFEAT_SATURATION, fade);
                grade.TintR = Lerp(1f, won ? VICTORY_TINT_R : DEFEAT_TINT_R, fade);
                grade.TintG = Lerp(1f, won ? VICTORY_TINT_G : DEFEAT_TINT_G, fade);
                grade.TintB = Lerp(1f, won ? VICTORY_TINT_B : DEFEAT_TINT_B, fade);
                return grade;
            }

            if (powered && powerPulseMs > 0)
            {
                var pulse = GetPulse(time, powerPulseMs);
                grade.Saturation = Lerp(1f, POWER_SATURATION, pulse);
                grade.TintR = Lerp(1f, POWER_TINT_R, pulse);
                grade.TintG = Lerp(1f, POWER_TINT_G, pulse);
                grade.TintB = Lerp(1f, POWER_TINT_B, pulse);
            }

            if (stunned)
            {
                grade.Saturation = STUN_SATURATION;
                grade.WobblePixels = STUN_WOBBLE_PIXELS;
            }

            grade.EdgeStrength = GetStarveEdge(hungerFill, time);
            return grade;
        }

        /// <summary>
        /// How strongly the edges are washed red for a hunger meter's fill: nothing up to STARVE_START_FILL, then a
        /// pulse every STARVE_PULSE_MS whose peak grows to STARVE_EDGE_MAX as the meter fills.
        /// </summary>
        public static float GetStarveEdge(float hungerFill, TimeSpan time)
        {
            var share = Math.Clamp((hungerFill - STARVE_START_FILL) / (1f - STARVE_START_FILL), 0f, 1f);
            if (share <= 0f)
                return 0f;

            return share * STARVE_EDGE_MAX * GetPulse(time, STARVE_PULSE_MS);
        }

        /// <summary>
        /// A smooth pulse from 0 up to 1 and back every periodMs, starting at 0, the same as the power pill pulse on
        /// the pac (ClientColors.GetPacTint).
        /// </summary>
        public static float GetPulse(TimeSpan time, int periodMs)
        {
            if (periodMs <= 0)
                return 0f;

            return 0.5f - 0.5f * MathF.Cos((float)(time.TotalMilliseconds / periodMs) * 2f * MathF.PI);
        }

        /// <summary>
        /// How far round the stun wobble's wave has gone after a time, in radians, 0 to 2 pi.
        /// </summary>
        public static float GetWobblePhase(TimeSpan time)
        {
            var cycles = time.TotalMilliseconds / WOBBLE_CYCLE_MS;
            return (float)((cycles - Math.Floor(cycles)) * 2.0 * Math.PI);
        }

        /// <summary>
        /// One color (channels 0 to 1) graded, as the shader does: saturation, then tint, then the flash to white.
        /// The edges are separate (GetEdgeWeight).
        /// </summary>
        public static (float R, float G, float B) ApplyGrade(float r, float g, float b, BoardGrade grade)
        {
            var luma = r * 0.299f + g * 0.587f + b * 0.114f;

            r = (luma + (r - luma) * grade.Saturation) * grade.TintR;
            g = (luma + (g - luma) * grade.Saturation) * grade.TintG;
            b = (luma + (b - luma) * grade.Saturation) * grade.TintB;

            return (r + (1f - r) * grade.Flash, g + (1f - g) * grade.Flash, b + (1f - b) * grade.Flash);
        }

        /// <summary>
        /// How much of the red edge a board position (0 to 1 each way) gets, 0 in the middle to 1 at the edge.
        /// </summary>
        public static float GetEdgeWeight(float u, float v)
        {
            var edge = MathF.Max(MathF.Abs(u * 2f - 1f), MathF.Abs(v * 2f - 1f));
            return MathF.Pow(Math.Clamp(edge, 0f, 1f), EDGE_POWER);
        }

        private static float Lerp(float from, float to, float amount)
        {
            return from + (to - from) * amount;
        }
    }
}
