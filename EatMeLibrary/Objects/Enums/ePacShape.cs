namespace EatMeLibrary
{
    /// <summary>
    /// The pac sprite a player picks, one row of the pac sheet each, in sheet order. The numbers are saved in the
    /// client settings and sent as a byte, so keep them.
    /// </summary>
    public enum ePacShape
    {
        OG = 0,
        Fang = 1,
        Hex = 2,
        Boxie = 3,
        Trey = 4,
        Doc = 5,
        Gears = 6,
        LadyDi = 7,
        Go = 8,
    }
}
