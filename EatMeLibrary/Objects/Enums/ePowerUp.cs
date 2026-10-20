using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Bonus items that can appear in a level: every bonus item from the Pac-Man and Ms. Pac-Man arcade games,
    /// plus power ups from other classic games. Each level introduces exactly one item (see Levels).
    /// Power pills are on every level, so they aren't listed here.
    /// </summary>
    [Flags]
    public enum ePowerUp : uint
    {
        None = 0,

        Cherry = 1u << 0,
        Strawberry = 1u << 1,
        Orange = 1u << 2,
        Apple = 1u << 3,
        Melon = 1u << 4,
        Galaxian = 1u << 5,
        Bell = 1u << 6,
        Key = 1u << 7,
        Pretzel = 1u << 8,
        Pear = 1u << 9,
        Banana = 1u << 10,
        Mushroom = 1u << 11,
        OneUpMushroom = 1u << 12,
        FireFlower = 1u << 13,
        Starman = 1u << 14,
        PowBlock = 1u << 15,
        SuperLeaf = 1u << 16,
        Hammer = 1u << 17,
        Triforce = 1u << 18,
        HeartContainer = 1u << 19,
        WhiteSword = 1u << 20,
        EnergyTank = 1u << 21,
        MorphBall = 1u << 22,
        ScrewAttack = 1u << 23,
        HolyWater = 1u << 24,
        SpreadGun = 1u << 25,
        BlueShell = 1u << 26,
        Tetromino = 1u << 27,
        BFG = 1u << 28,
        MegaManOneUp = 1u << 29,
        PepperShaker = 1u << 30,
        PokeBall = 1u << 31,

        // Every bonus item
        Any = Cherry | Strawberry | Orange | Apple | Melon | Galaxian | Bell | Key | Pretzel | Pear | Banana
            | Mushroom | OneUpMushroom | FireFlower | Starman | PowBlock | SuperLeaf | Hammer
            | Triforce | HeartContainer | WhiteSword | EnergyTank | MorphBall | ScrewAttack
            | HolyWater | SpreadGun | BlueShell | Tetromino | BFG | MegaManOneUp | PepperShaker | PokeBall,
    }
}
