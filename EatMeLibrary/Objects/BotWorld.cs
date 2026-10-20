using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// What a bot can see when it decides where to go.
    /// </summary>
    public class BotWorld
    {
        public eGridDef[,] Map { get; set; }

        /// <summary>
        /// GameConfiguration.PacMaxSize: the most dots can make a pac, and the size a powered pac needs to eat ghosts.
        /// </summary>
        public float MaxSize { get; set; }

        /// <summary>
        /// Seeds how much each bot misjudges each other pac's size (PacBrain.GetPerceivedSize). The server uses the
        /// level's map seed, so the errors change each level.
        /// </summary>
        public int PerceptionSeed { get; set; }

        /// <summary>
        /// Living pacs, the bot itself included.
        /// </summary>
        public IReadOnlyList<Player> Pacs { get; set; } = [];

        public IReadOnlyList<Ghost> Ghosts { get; set; } = [];

        public IReadOnlyList<GameFlag> Flags { get; set; } = [];

        public IReadOnlyList<PowerUp> PowerUps { get; set; } = [];

        /// <summary>
        /// Weapon effects in the maze; bots run from holy flames (all but their own) like any other threat.
        /// </summary>
        public IReadOnlyList<WeaponEffect> WeaponEffects { get; set; } = [];

        /// <summary>
        /// A full PathMap from each flag base cell, so bots can head for a base from anywhere without searching the
        /// whole map. Made once per level, filled with the bot's own blocked cells (PacBrain.GetBlocked).
        /// </summary>
        public IReadOnlyDictionary<(int X, int Y), PathMap> FlagBasePaths { get; set; } = new Dictionary<(int X, int Y), PathMap>();
    }
}
