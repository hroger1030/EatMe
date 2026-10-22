namespace EatMeLibrary
{
    /// <summary>
    /// What a bot is after (PacBrain.PickGoal), highest priority first. A bot keeps its goal for a while (BotMemory)
    /// rather than picking again at every cell centre.
    /// </summary>
    public enum eBotGoal
    {
        /// <summary>No goal yet, or the last one was dropped; the next cell centre picks one.</summary>
        None,

        /// <summary>Carrying the enemy flag: take it home, getting its own flag back first when that is close.</summary>
        CarryHome,

        /// <summary>Return its own dropped flag.</summary>
        Rescue,

        /// <summary>Chase a smaller enemy carrying its flag.</summary>
        HuntCarrier,

        /// <summary>Pick up a dropped enemy flag nearby (any bot free to pick one up).</summary>
        LooseFlag,

        /// <summary>Eat a nearby power pill.</summary>
        PowerPill,

        /// <summary>Eat a nearby power up.</summary>
        PowerUp,

        /// <summary>Chase a smaller enemy (any enemy while invincible).</summary>
        Hunt,

        /// <summary>Chase a ghost it can kill.</summary>
        HuntGhost,

        /// <summary>Go for the enemy flag (attackers only).</summary>
        EnemyFlag,

        /// <summary>Eat the nearest dot.</summary>
        Dot,
    }
}
