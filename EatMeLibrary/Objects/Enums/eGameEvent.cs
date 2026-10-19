namespace EatMeLibrary
{
    /// <summary>
    /// One-off happenings the client plays sounds and animations for.
    /// </summary>
    public enum eGameEvent : byte
    {
        PacEaten,
        GhostKilled,
        FlagCaptured,

        /// <summary>
        /// A carried flag was dropped (its carrier died, left or stepped onto a spawn tile).
        /// </summary>
        FlagLost,

        /// <summary>
        /// A dropped flag was taken back to its base by its own team.
        /// </summary>
        FlagReturned,

        /// <summary>
        /// A team's score passed GameServer.ABOUT_TO_WIN_PERCENT of MaxScore; sent once per team per level.
        /// </summary>
        AboutToWin,

        /// <summary>
        /// A pac (ActorId) ate a power up (TargetId) at X, Y.
        /// </summary>
        PowerUpEaten,

        /// <summary>
        /// A pac (ActorId) set off a POW block at X, Y, stunning everything within PowerUpEffects.EFFECT_RADIUS but itself.
        /// </summary>
        Pow,
    }
}
