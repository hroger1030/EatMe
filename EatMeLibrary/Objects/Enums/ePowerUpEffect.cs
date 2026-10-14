namespace EatMeLibrary
{
    /// <summary>
    /// The power up effect working on a pac. A pac has at most one; a new one replaces it and restarts the timer.
    /// Every effect lasts GameConfiguration.PowerUpDurationMs.
    /// </summary>
    public enum ePowerUpEffect : byte
    {
        None = 0,
        PowerPill = 1,

        /// <summary>
        /// Cherry: moves PacRules.SUGAR_RUSH_SPEED_SCALE times as fast and leaves a speed trail.
        /// </summary>
        SugarRush = 2,

        /// <summary>
        /// Strawberry: dots give PacRules.SWEET_TOOTH_DOT_GAIN_SCALE times as much growth.
        /// </summary>
        SweetTooth = 3,

        /// <summary>
        /// Orange: never starves, and lasts until the pac dies or another effect replaces it (PowerUpEffects.IsPermanent).
        /// </summary>
        VitaminC = 4,

        /// <summary>
        /// Apple: ghosts won't chase the pac and run from it (Player.RepelsGhosts). Permanent (PowerUpEffects.IsPermanent).
        /// </summary>
        DoctorAway = 5,

        /// <summary>
        /// Melon: PacRules.MELON_BELLY_SIZE_GAIN bigger while it works, and never hungry.
        /// </summary>
        MelonBelly = 6,

        /// <summary>
        /// Galaxian: points scored are worth PowerUpEffects.ROCKET_SCIENCE_SCORE_SCALE times as much.
        /// </summary>
        RocketScience = 7,

        /// <summary>
        /// Bell: every ghost in the level moves PowerUpEffects.DINNER_BELL_GHOST_SPEED_SCALE times as fast.
        /// </summary>
        DinnerBell = 8,

        /// <summary>
        /// Key: dead enemy pacs can't respawn while it works (PowerUpEffects.LocksSpawn).
        /// </summary>
        Lockdown = 9,
    }
}
