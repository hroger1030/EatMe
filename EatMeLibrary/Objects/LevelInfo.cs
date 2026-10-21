
namespace EatMeLibrary
{
    /// <summary>
    /// Everything that sets one level apart from another.
    /// </summary>
    public class LevelInfo
    {
        /// <summary>
        /// Level number, starting at 1.
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// Shown to players when the level starts.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Tint for every wall in the level.
        /// </summary>
        public FastColor WallColor { get; }

        /// <summary>
        /// Color the inside of every wall is filled with. Transparent (alpha 0) means hollow walls, and nothing is drawn for the fill.
        /// </summary>
        public FastColor WallFillColor { get; }

        /// <summary>
        /// Color the board is filled with behind the maze. Dark, and a complement of the wall color.
        /// </summary>
        public FastColor BackgroundColor { get; }

        /// <summary>
        /// Image tiled over the background color behind the maze, or null for none.
        /// </summary>
        public string BackgroundImage { get; }

        /// <summary>
        /// A rainbow level: rainbow dots and walls (Rainbow), and ghosts twice the size (PacRules.GetGhostRadius).
        /// </summary>
        public bool IsRainbow { get; }

        public LevelInfo(int level, string name, FastColor wallColor, FastColor wallFillColor, FastColor backgroundColor,
            bool isRainbow = false, string backgroundImage = null)
        {
            IsRainbow = isRainbow;
            Level = level;
            Name = name;
            WallColor = wallColor;
            WallFillColor = wallFillColor;
            BackgroundColor = backgroundColor;
            BackgroundImage = backgroundImage;
        }
    }
}
