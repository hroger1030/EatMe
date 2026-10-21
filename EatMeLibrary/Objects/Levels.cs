using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The fixed list of levels. Server and client both look levels up here, so only the level number needs agreeing on.
    /// </summary>
    public static class Levels
    {
        /// <summary>
        /// Level data in play order. Level 1 is the first entry.
        /// </summary>
        private static readonly LevelInfo[] LEVELS =
        [
            new(1, "Blue Maze", FastColor.FromRgb(33, 33, 222), FastColor.FromRgb(10, 10, 90), FastColor.FromRgb(0, 0, 0)),
            new(2, "Cherry Lane", FastColor.FromRgb(230, 20, 70), FastColor.FromRgb(110, 0, 34), FastColor.FromRgb(8, 34, 14)),
            new(3, "Lime Grove", FastColor.FromRgb(33, 200, 66), FastColor.FromRgb(20, 90, 30), FastColor.FromRgb(48, 0, 64)),
            new(4, "Amber Alley", FastColor.FromRgb(255, 170, 0), FastColor.FromRgb(140, 80, 0), FastColor.FromRgb(72, 40, 0)),
            new(5, "Violet Vault", FastColor.FromRgb(160, 60, 220), FastColor.FromRgb(80, 24, 120), FastColor.FromRgb(40, 0, 72)),
            new(6, "Cyan Circuit", FastColor.FromRgb(0, 210, 220), FastColor.FromRgb(0, 70, 80), FastColor.FromRgb(8, 8, 12)),
            new(7, "Pink Palace", FastColor.FromRgb(255, 105, 180), FastColor.FromRgb(140, 40, 90), FastColor.FromRgb(34, 6, 38)),
            new(8, "Mint Meadow", FastColor.FromRgb(152, 255, 152), FastColor.FromRgb(60, 130, 70), FastColor.FromRgb(0, 56, 24)),
            new(9, "Copper Canal", FastColor.FromRgb(200, 120, 50), FastColor.FromRgb(100, 55, 20), FastColor.FromRgb(0, 24, 72)),
            new(10, "Gold Rush", FastColor.FromRgb(255, 215, 0), FastColor.FromRgb(150, 110, 0), FastColor.FromRgb(56, 44, 0)),
            new(11, "Teal Tunnels", FastColor.FromRgb(0, 170, 160), FastColor.FromRgb(0, 90, 84), FastColor.FromRgb(0, 48, 44)),
            new(12, "Ruby Ruins", FastColor.FromRgb(200, 20, 60), FastColor.FromRgb(100, 8, 30), FastColor.FromRgb(24, 0, 6)),
            new(13, "Ice Cavern", FastColor.FromRgb(170, 230, 255), FastColor.FromRgb(70, 120, 160), FastColor.FromRgb(0, 16, 56)),
            new(14, "Lava Lanes", FastColor.FromRgb(255, 80, 20), FastColor.FromRgb(150, 30, 0), FastColor.FromRgb(48, 8, 0)),
            new(15, "Jade Temple", FastColor.FromRgb(0, 168, 107), FastColor.FromRgb(0, 90, 58), FastColor.FromRgb(10, 74, 15)),
            new(16, "Neon Nights", FastColor.FromRgb(57, 255, 20), FastColor.FromRgb(15, 60, 10), FastColor.FromRgb(0, 0, 0)),
            new(17, "Rose Garden", FastColor.FromRgb(255, 0, 127), FastColor.FromRgb(130, 0, 64), FastColor.FromRgb(64, 0, 36)),
            new(18, "Sky Harbor", FastColor.FromRgb(135, 206, 235), FastColor.FromRgb(50, 100, 140), FastColor.FromRgb(60, 30, 0)),
            new(19, "Rust Belt", FastColor.FromRgb(183, 65, 14), FastColor.FromRgb(100, 36, 10), FastColor.FromRgb(0, 44, 56)),
            new(20, "Plum Pit", FastColor.FromRgb(170, 80, 160), FastColor.FromRgb(90, 36, 85), FastColor.FromRgb(48, 0, 48)),
            new(21, "Sand Dunes", FastColor.FromRgb(237, 201, 175), FastColor.FromRgb(160, 130, 95), FastColor.FromRgb(56, 40, 24)),
            new(22, "Coral Reef", FastColor.FromRgb(255, 127, 80), FastColor.FromRgb(150, 65, 40), FastColor.FromRgb(0, 40, 80)),
            new(23, "Indigo Abyss", FastColor.FromRgb(90, 40, 200), FastColor.FromRgb(40, 16, 100), FastColor.FromRgb(12, 0, 32)),
            new(24, "Silver Spire", FastColor.FromRgb(192, 192, 192), FastColor.FromRgb(100, 104, 112), FastColor.FromRgb(32, 36, 48)),
            new(25, "Toxic Swamp", FastColor.FromRgb(170, 255, 0), FastColor.FromRgb(80, 120, 0), FastColor.FromRgb(24, 48, 0)),
            new(26, "Magma Core", FastColor.FromRgb(255, 40, 0), FastColor.FromRgb(140, 16, 0), FastColor.FromRgb(32, 0, 0)),
            new(27, "Frost Bite", FastColor.FromRgb(220, 240, 255), FastColor.FromRgb(110, 150, 190), FastColor.FromRgb(0, 32, 80)),
            new(28, "Peach Pier", FastColor.FromRgb(255, 200, 150), FastColor.FromRgb(170, 120, 85), FastColor.FromRgb(0, 52, 64)),
            new(29, "Olive Oasis", FastColor.FromRgb(150, 180, 0), FastColor.FromRgb(85, 100, 0), FastColor.FromRgb(40, 48, 0)),
            new(30, "Ghost Town", FastColor.FromRgb(150, 150, 170), FastColor.FromRgb(60, 60, 72), FastColor.FromRgb(10, 10, 16)),
            new(31, "Galaxy Gate", FastColor.FromRgb(110, 80, 255), FastColor.FromRgb(50, 30, 130), FastColor.FromRgb(20, 0, 40)),
            new(32, "Pac Paradise", FastColor.FromRgb(255, 255, 0), FastColor.TRANSPARENT, FastColor.FromRgb(0, 0, 0), isRainbow: true,
                backgroundImage: Const.IMAGE_SPACE_FILL),
        ];

        /// <summary>
        /// Number of distinct levels before the list repeats.
        /// </summary>
        public static int Count => LEVELS.Length;

        /// <summary>
        /// Returns the data for a level, starting at 1. Past the last level the list starts over,
        /// so level Count + 1 is the same as level 1.
        /// </summary>
        public static LevelInfo GetLevel(int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), level, "Levels start at 1");

            return LEVELS[(level - 1) % LEVELS.Length];
        }

        /// <summary>
        /// A random level from 1 to Count other than currentLevel (the same level counts however many times the list
        /// has started over), so the next map is always a different one. With only one level, that level.
        /// </summary>
        public static int PickRandomLevel(int currentLevel, Random random)
        {
            if (LEVELS.Length < 2)
                return 1;

            var current = currentLevel < 1 ? 0 : (currentLevel - 1) % LEVELS.Length + 1;

            if (current == 0)
                return random.Next(LEVELS.Length) + 1;

            // pick from the other Count - 1 levels, skipping over the current one
            var level = random.Next(LEVELS.Length - 1) + 1;

            return level >= current ? level + 1 : level;
        }

        /// <summary>
        /// The seed a level's map is built from: the server's seed plus the level number.
        /// </summary>
        public static int GetSeed(int serverSeed, int level)
        {
            return unchecked(serverSeed + level);
        }
    }
}
