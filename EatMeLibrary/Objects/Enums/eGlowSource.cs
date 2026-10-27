namespace EatMeLibrary
{
    /// <summary>
    /// The kinds of things on the game board that glow under the colour bloom (GlowSources.GetStrength).
    /// </summary>
    public enum eGlowSource : byte
    {
        WallTube,
        Dot,
        PowerPill,
        FlagBase,
        Flag,
        Pac,
        Ghost,
        PowerUp,
        WeaponEffect,
        Death,
        Debris,
    }
}
