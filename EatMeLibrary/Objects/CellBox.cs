namespace EatMeLibrary
{
    /// <summary>
    /// A rectangle of map cells: the top left cell and how many cells wide and high it is.
    /// </summary>
    public struct CellBox
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public CellBox(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
