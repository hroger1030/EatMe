namespace EatMeLibrary
{
    /// <summary>
    /// Icons the mini map draws. Each is one cell of the mini map icon sheet (Const.IMAGE_MINI_MAP_ICONS, MiniMapSprites),
    /// in this order. The own pac's four come in eFacing order.
    /// </summary>
    public enum eMiniMapIcon : byte
    {
        /// <summary>A living pac other than the player's own: a filled circle.</summary>
        Dot,

        /// <summary>The player's own pac, a Pac-Man facing up.</summary>
        OwnPacUp,

        /// <summary>The player's own pac, a Pac-Man facing down.</summary>
        OwnPacDown,

        /// <summary>The player's own pac, a Pac-Man facing left.</summary>
        OwnPacLeft,

        /// <summary>The player's own pac, a Pac-Man facing right.</summary>
        OwnPacRight,

        /// <summary>A living ghost.</summary>
        Ghost,

        /// <summary>A power up: a cherry, two berries on a forked stem.</summary>
        PowerUp,

        /// <summary>A flag not being carried: a swallowtail banner on a pole, like the board's flag.</summary>
        Flag,

        /// <summary>A warp door: an arch, an upside down U.</summary>
        WarpArch,

        /// <summary>A flag base: a ring.</summary>
        FlagBase,
    }
}
