namespace EatMeServer
{
    /// <summary>
    /// The pieces of the server's tick that perf logging times (ServerPerf), in the order the report lists them.
    /// Tick is the whole tick but its sleep; the rest are parts of it.
    /// </summary>
    public enum ePerfSection
    {
        Tick,
        Network,
        Commands,
        Lag,
        Intermission,
        Respawns,
        Stuns,
        Bots,
        Move,
        Eating,
        Fights,
        Ghosts,
        Weapons,
        PowerUps,
        Flags,
        Regens,
        Cells,
        Win,
        Snapshot,
    }
}
