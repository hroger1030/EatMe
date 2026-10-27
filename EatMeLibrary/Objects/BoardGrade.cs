namespace EatMeLibrary
{
    /// <summary>
    /// The color grade the game board is drawn with (StateGrade.GetGrade), passed to the client's board shaders.
    /// </summary>
    public struct BoardGrade
    {
        /// <summary>
        /// 1 leaves colors as they are, 0 is greyscale, above 1 is more colorful.
        /// </summary>
        public float Saturation { get; set; }

        /// <summary>
        /// Each channel is multiplied by its tint; 1 leaves it as it is.
        /// </summary>
        public float TintR { get; set; }
        public float TintG { get; set; }
        public float TintB { get; set; }

        /// <summary>
        /// How far every color is pushed to white, 0 to 1.
        /// </summary>
        public float Flash { get; set; }

        /// <summary>
        /// How strongly the board's edges are washed red (starving), 0 to 1.
        /// </summary>
        public float EdgeStrength { get; set; }

        /// <summary>
        /// How far the board wobbles sideways (stunned), in pixels.
        /// </summary>
        public float WobblePixels { get; set; }

        /// <summary>
        /// The board as it is.
        /// </summary>
        public static BoardGrade None => new() { Saturation = 1f, TintR = 1f, TintG = 1f, TintB = 1f };

        /// <summary>
        /// True when the grade changes nothing.
        /// </summary>
        public bool IsNone => Saturation == 1f && TintR == 1f && TintG == 1f && TintB == 1f && Flash == 0f && EdgeStrength == 0f && WobblePixels == 0f;
    }
}
