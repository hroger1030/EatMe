namespace EatMeLibrary
{
    /// <summary>
    /// Projectiles and objects power up effects put in the maze. Each is one row of the weapon effects sheet
    /// (Const.IMAGE_WEAPON_EFFECTS, WeaponEffectSprites), in this order.
    /// </summary>
    public enum eWeaponEffect : byte
    {
        /// <summary>BFG 9000's plasma ball.</summary>
        BfgShot,

        /// <summary>Banana's peel, left lying in the maze.</summary>
        BananaPeel,

        /// <summary>White sword's thrown sword, pointing right.</summary>
        Sword,

        /// <summary>Holy water's blue flames.</summary>
        HolyFlame,

        /// <summary>Blue shell, flying.</summary>
        BlueShell,

        /// <summary>Tetromino's block, a wall for a while.</summary>
        TetrisBlock,

        /// <summary>Poke ball.</summary>
        PokeBall,

        /// <summary>Hammer, upright then swung a quarter turn.</summary>
        Hammer,

        /// <summary>Fire flower's fireball.</summary>
        Fireball,

        /// <summary>Spread gun's shot.</summary>
        SpreadShot,

        /// <summary>Bomb, upright, its fuse burning.</summary>
        Bomb,
    }
}
