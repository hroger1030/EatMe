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
        /// An explosion at X, Y (ActorId the pac that set it off, Team its team): a POW block press or a bomb going off.
        /// Every client plays the boom in-world, and the board shakes for everyone inside PowerUpEffects.EFFECT_RADIUS.
        /// The server sends it with GameServer.BroadcastBlast; anything that blows up can use it.
        /// </summary>
        Blast,

        /// <summary>
        /// Sent only to the pac (TargetId) that just respawned at once thanks to a teammate's Reinforcements; it hears
        /// the angelic chorus.
        /// </summary>
        AngelicRespawn,

        /// <summary>
        /// A monster ball caught a ghost (TargetId) at X, Y.
        /// </summary>
        MonsterBallCapture,

        /// <summary>
        /// Something touched a monster ball holding a ghost (TargetId), which got out at X, Y.
        /// </summary>
        MonsterBallRelease,

        /// <summary>
        /// A pac with Super Size (mushroom) smashed one or more walls this tick; X, Y is where it is. Everyone hears
        /// the boom and every board shakes.
        /// </summary>
        WallSmash,

        /// <summary>
        /// A BFG ball (owner ActorId) burst at X, Y, killing everything that isn't invincible within
        /// PowerUpEffects.EFFECT_RADIUS.
        /// </summary>
        BfgBurst,

        /// <summary>
        /// An enemy pac or a ghost touched a Cherry Twin decoy (owner ActorId) at X, Y, which popped. Nobody is hurt.
        /// </summary>
        DecoyPopped,
    }
}
