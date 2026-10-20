namespace EatMeLibrary
{
    /// <summary>
    /// Which color the player state panel's size bar is (SizeBar.GetLevel).
    /// </summary>
    public enum eSizeBarLevel : byte
    {
        /// <summary>Up to SizeBar.FULL_SIZE: green.</summary>
        Normal,

        /// <summary>Above SizeBar.FULL_SIZE: bright orange.</summary>
        Big,

        /// <summary>Above SizeBar.HUGE_SIZE: bright red.</summary>
        Huge,
    }
}
