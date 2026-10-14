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
        /// Each level introduces exactly one bonus item no earlier level has (listed first); later levels also bring back earlier ones.
        /// </summary>
        private static readonly LevelInfo[] LEVELS =
        [
            new(1, "Blue Maze", FastColor.FromRgb(33, 33, 222), FastColor.FromRgb(10, 10, 90), FastColor.FromRgb(0, 0, 0), ePowerUp.Cherry),
            new(2, "Cherry Lane", FastColor.FromRgb(230, 20, 70), FastColor.FromRgb(110, 0, 34), FastColor.FromRgb(8, 34, 14), ePowerUp.Strawberry),
            new(3, "Lime Grove", FastColor.FromRgb(33, 200, 66), FastColor.FromRgb(20, 90, 30), FastColor.FromRgb(48, 0, 64), ePowerUp.Orange),
            new(4, "Amber Alley", FastColor.FromRgb(255, 170, 0), FastColor.FromRgb(140, 80, 0), FastColor.FromRgb(72, 40, 0), ePowerUp.Apple),
            new(5, "Violet Vault", FastColor.FromRgb(160, 60, 220), FastColor.FromRgb(80, 24, 120), FastColor.FromRgb(40, 0, 72), ePowerUp.Melon),
            new(6, "Cyan Circuit", FastColor.FromRgb(0, 210, 220), FastColor.FromRgb(0, 70, 80), FastColor.FromRgb(8, 8, 12), ePowerUp.Galaxian),
            new(7, "Pink Palace", FastColor.FromRgb(255, 105, 180), FastColor.FromRgb(140, 40, 90), FastColor.FromRgb(34, 6, 38), ePowerUp.Bell),
            new(8, "Mint Meadow", FastColor.FromRgb(152, 255, 152), FastColor.FromRgb(60, 130, 70), FastColor.FromRgb(0, 56, 24), ePowerUp.Key | ePowerUp.Cherry),
            new(9, "Copper Canal", FastColor.FromRgb(200, 120, 50), FastColor.FromRgb(100, 55, 20), FastColor.FromRgb(0, 24, 72), ePowerUp.Pretzel | ePowerUp.Orange),
            new(10, "Gold Rush", FastColor.FromRgb(255, 215, 0), FastColor.FromRgb(150, 110, 0), FastColor.FromRgb(56, 44, 0), ePowerUp.Pear | ePowerUp.Bell),
            new(11, "Teal Tunnels", FastColor.FromRgb(0, 170, 160), FastColor.FromRgb(0, 90, 84), FastColor.FromRgb(0, 48, 44), ePowerUp.Banana | ePowerUp.Strawberry),
            new(12, "Ruby Ruins", FastColor.FromRgb(200, 20, 60), FastColor.FromRgb(100, 8, 30), FastColor.FromRgb(24, 0, 6), ePowerUp.Triforce | ePowerUp.Apple),
            new(13, "Ice Cavern", FastColor.FromRgb(170, 230, 255), FastColor.FromRgb(70, 120, 160), FastColor.FromRgb(0, 16, 56), ePowerUp.PowBlock | ePowerUp.Melon),
            new(14, "Lava Lanes", FastColor.FromRgb(255, 80, 20), FastColor.FromRgb(150, 30, 0), FastColor.FromRgb(48, 8, 0), ePowerUp.FireFlower | ePowerUp.Galaxian),
            new(15, "Jade Temple", FastColor.FromRgb(0, 168, 107), FastColor.FromRgb(0, 90, 58), FastColor.FromRgb(10, 74, 15), ePowerUp.HeartContainer | ePowerUp.Key),
            new(16, "Neon Nights", FastColor.FromRgb(57, 255, 20), FastColor.FromRgb(15, 60, 10), FastColor.FromRgb(0, 0, 0), ePowerUp.Tetromino | ePowerUp.Galaxian | ePowerUp.Pretzel),
            new(17, "Rose Garden", FastColor.FromRgb(255, 0, 127), FastColor.FromRgb(130, 0, 64), FastColor.FromRgb(64, 0, 36), ePowerUp.SuperLeaf | ePowerUp.Cherry | ePowerUp.Strawberry),
            new(18, "Sky Harbor", FastColor.FromRgb(135, 206, 235), FastColor.FromRgb(50, 100, 140), FastColor.FromRgb(60, 30, 0), ePowerUp.WhiteSword | ePowerUp.Pear | ePowerUp.Banana),
            new(19, "Rust Belt", FastColor.FromRgb(183, 65, 14), FastColor.FromRgb(100, 36, 10), FastColor.FromRgb(0, 44, 56), ePowerUp.Hammer | ePowerUp.Bell | ePowerUp.Key),
            new(20, "Plum Pit", FastColor.FromRgb(170, 80, 160), FastColor.FromRgb(90, 36, 85), FastColor.FromRgb(48, 0, 48), ePowerUp.MegaManOneUp | ePowerUp.Triforce | ePowerUp.Pear),
            new(21, "Sand Dunes", FastColor.FromRgb(237, 201, 175), FastColor.FromRgb(160, 130, 95), FastColor.FromRgb(56, 40, 24), ePowerUp.BFG | ePowerUp.Orange | ePowerUp.Banana),
            new(22, "Coral Reef", FastColor.FromRgb(255, 127, 80), FastColor.FromRgb(150, 65, 40), FastColor.FromRgb(0, 40, 80), ePowerUp.PokeBall | ePowerUp.Strawberry | ePowerUp.PowBlock),
            new(23, "Indigo Abyss", FastColor.FromRgb(90, 40, 200), FastColor.FromRgb(40, 16, 100), FastColor.FromRgb(12, 0, 32), ePowerUp.MorphBall | ePowerUp.Melon | ePowerUp.HeartContainer),
            new(24, "Silver Spire", FastColor.FromRgb(192, 192, 192), FastColor.FromRgb(100, 104, 112), FastColor.FromRgb(32, 36, 48), ePowerUp.HolyWater | ePowerUp.Bell | ePowerUp.Key | ePowerUp.WhiteSword),
            new(25, "Toxic Swamp", FastColor.FromRgb(170, 255, 0), FastColor.FromRgb(80, 120, 0), FastColor.FromRgb(24, 48, 0), ePowerUp.Mushroom | ePowerUp.FireFlower | ePowerUp.PowBlock | ePowerUp.Pretzel),
            new(26, "Magma Core", FastColor.FromRgb(255, 40, 0), FastColor.FromRgb(140, 16, 0), FastColor.FromRgb(32, 0, 0), ePowerUp.EnergyTank | ePowerUp.FireFlower | ePowerUp.Hammer | ePowerUp.Apple),
            new(27, "Frost Bite", FastColor.FromRgb(220, 240, 255), FastColor.FromRgb(110, 150, 190), FastColor.FromRgb(0, 32, 80), ePowerUp.ScrewAttack | ePowerUp.MorphBall | ePowerUp.EnergyTank | ePowerUp.Cherry),
            new(28, "Peach Pier", FastColor.FromRgb(255, 200, 150), FastColor.FromRgb(170, 120, 85), FastColor.FromRgb(0, 52, 64), ePowerUp.PepperShaker | ePowerUp.Mushroom | ePowerUp.SuperLeaf | ePowerUp.Orange),
            new(29, "Olive Oasis", FastColor.FromRgb(150, 180, 0), FastColor.FromRgb(85, 100, 0), FastColor.FromRgb(40, 48, 0), ePowerUp.SpreadGun | ePowerUp.BFG | ePowerUp.MegaManOneUp | ePowerUp.Apple),
            new(30, "Ghost Town", FastColor.FromRgb(150, 150, 170), FastColor.FromRgb(60, 60, 72), FastColor.FromRgb(10, 10, 16), ePowerUp.OneUpMushroom | ePowerUp.HolyWater | ePowerUp.Triforce | ePowerUp.PokeBall | ePowerUp.Melon),
            new(31, "Galaxy Gate", FastColor.FromRgb(110, 80, 255), FastColor.FromRgb(50, 30, 130), FastColor.FromRgb(20, 0, 40), ePowerUp.BlueShell | ePowerUp.Galaxian | ePowerUp.Tetromino | ePowerUp.ScrewAttack | ePowerUp.SpreadGun | ePowerUp.HeartContainer),
            new(32, "Pac Paradise", FastColor.FromRgb(255, 255, 0), FastColor.TRANSPARENT, FastColor.FromRgb(0, 0, 0), ePowerUp.Any, isRainbow: true,
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
        /// The seed a level's map is built from: the server's seed plus the level number.
        /// </summary>
        public static int GetSeed(int serverSeed, int level)
        {
            return unchecked(serverSeed + level);
        }
    }
}
