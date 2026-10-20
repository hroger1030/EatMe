using System;

namespace EatMeLibrary
{
    [Flags]
    public enum eGridDef
    {
        /// <summary>
        /// Solid wall cell (the outer wall can never be broken; the Hammer and Super Size smash the rest). Any cell without this flag is open.
        /// </summary>
        Wall = 1,

        Food = 16,
        PowerPill = 32,
        FlagBaseRed = 64,
        FlagBaseBlue = 128,

        SpawnPointRed = 512,
        SpawnPointBlue = 256,

        GhostGenerator = 1024,

        /// <summary>
        /// Where power ups appear (GameServer.UpdatePowerUps). Otherwise a normal cell, with a dot.
        /// </summary>
        PowerUpSpawn = 2048,

        /// <summary>
        /// A Tetromino block, always together with Wall: it acts as a wall until it is taken away again. The client draws
        /// the block sprite (a weapon effect) instead of a wall tube, and the walls round it don't join onto it.
        /// </summary>
        TetrisBlock = 4096,
    }
}
