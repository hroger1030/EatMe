using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Bonus items that can appear in a level: every bonus item from the Pac-Man and Ms. Pac-Man arcade games,
    /// plus power ups from other classic games. Each level introduces exactly one item (see Levels).
    /// Power pills are on every level, so they aren't listed here.
    /// </summary>
    [Flags]
    public enum ePowerUp : long
    {
        None = 0,

        // Pac-Man and Ms. Pac-Man
        Cherry = 1L << 0,
        Strawberry = 1L << 1,
        Orange = 1L << 2,
        Apple = 1L << 3,

        // Pac-Man only
        Melon = 1L << 4,
        Galaxian = 1L << 5,
        Bell = 1L << 6,
        Key = 1L << 7,

        // Ms. Pac-Man only
        Pretzel = 1L << 8,
        Pear = 1L << 9,
        Banana = 1L << 10,

        // Super Mario Bros. (1985), Mario Bros. (1983) and Super Mario Bros. 3 (1988)
        Mushroom = 1L << 11,
        OneUpMushroom = 1L << 12,
        FireFlower = 1L << 13,
        Starman = 1L << 14,
        PowBlock = 1L << 15,
        SuperLeaf = 1L << 16,

        // Donkey Kong (1981)
        Hammer = 1L << 17,

        // The Legend of Zelda (1986)
        Triforce = 1L << 18,
        HeartContainer = 1L << 19,
        WhiteSword = 1L << 20,

        // Metroid (1986)
        EnergyTank = 1L << 21,
        MorphBall = 1L << 22,
        ScrewAttack = 1L << 23,

        // Castlevania (1986)
        HolyWater = 1L << 24,

        // Contra (1987)
        SpreadGun = 1L << 25,

        // Mario Kart 64 (1996)
        BlueShell = 1L << 26,

        // Tetris (1984)
        Tetromino = 1L << 27,

        // Doom (1993)
        BFG = 1L << 28,

        // Mega Man (1987)
        MegaManOneUp = 1L << 29,

        // BurgerTime (1982)
        PepperShaker = 1L << 30,

        // Pokemon (1996)
        PokeBall = 1L << 31,

        // Every bonus item
        Any = Cherry | Strawberry | Orange | Apple | Melon | Galaxian | Bell | Key | Pretzel | Pear | Banana
            | Mushroom | OneUpMushroom | FireFlower | Starman | PowBlock | SuperLeaf | Hammer
            | Triforce | HeartContainer | WhiteSword | EnergyTank | MorphBall | ScrewAttack
            | HolyWater | SpreadGun | BlueShell | Tetromino | BFG | MegaManOneUp | PepperShaker | PokeBall,
    }
}
