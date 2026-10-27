namespace EatMeLibrary
{
    /// <summary>
    /// How strongly each kind of thing on the game board glows under the colour bloom. The client draws the glowing
    /// things a second time into the glow target, each tinted by its strength, and only that target is blurred into
    /// the bloom, so the background, wall fills, floors, names and HUD never glow.
    /// </summary>
    public static class GlowSources
    {
        /// <summary>
        /// The glow tint's multiplier for a kind of glowing thing, 0 (no glow) to 1 (full glow).
        /// </summary>
        public static float GetStrength(eGlowSource source)
        {
            return source switch
            {
                eGlowSource.WallTube => 1f,
                eGlowSource.PowerPill => 1f,
                eGlowSource.Flag => 1f,
                eGlowSource.WeaponEffect => 1f,
                eGlowSource.Death => 1f,
                eGlowSource.Debris => 1f,
                eGlowSource.Pac => 0.6f,
                eGlowSource.FlagBase => 0.6f,
                eGlowSource.Dot => 0.5f,
                eGlowSource.Ghost => 0.5f,
                eGlowSource.PowerUp => 0.5f,
                _ => 0f,
            };
        }
    }
}
