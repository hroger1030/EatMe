namespace EatMeLibrary
{
    /// <summary>
    /// A map cell whose dot has been eaten. A struct so DotRegenQueue stores it inline and eating allocates nothing.
    /// </summary>
    public struct EatenCell
    {
        public ushort X;
        public ushort Y;

        public EatenCell(int x, int y)
        {
            X = (ushort)x;
            Y = (ushort)y;
        }
    }
}
