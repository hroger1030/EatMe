using System;

namespace EatMeLibrary
{
    [Flags]
    public enum eGridDef
    {
        /// <summary>
        /// Solid, indestructible wall cell. Any cell without this flag is open.
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
    }
}
