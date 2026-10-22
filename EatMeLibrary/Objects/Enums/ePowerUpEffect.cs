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
        /// Orange: never starves while it lasts.
        /// </summary>
        VitaminC = 4,

        /// <summary>
        /// Apple: kills every ghost within PowerUpEffects.EFFECT_RADIUS while it lasts (PowerUpEffects.KillsGhostsInRange),
        /// and ghosts won't chase the pac and run from it (Player.RepelsGhosts).
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

        /// <summary>
        /// Pretzel: everyone else within PowerUpEffects.EFFECT_RADIUS moves at half speed (PowerUpEffects.GetTwisterSpeedScale).
        /// </summary>
        Twister = 10,

        /// <summary>
        /// Starman: PacRules.INVINCIBLE_SPEED_SCALE times as fast, can't be killed, and kills any enemy pac or ghost it
        /// touches (Player.IsInvincible). Ghosts run from it. Leaves a speed trail and blinks yellow.
        /// </summary>
        Invincible = 11,

        /// <summary>
        /// Reinforcements (fairy): while the pac lives, its team's dead pacs respawn at once (no respawn timer).
        /// </summary>
        Reinforcements = 13,

        /// <summary>
        /// Bomb: the action button drops one bomb (once per buff) on the pac's cell. It goes off
        /// WeaponRules.BOMB_FUSE_MS later, stunning every enemy pac and ghost within PowerUpEffects.EFFECT_RADIUS.
        /// </summary>
        Bomb = 14,

        /// <summary>
        /// Super leaf: the pac passes through enemy pacs, ghosts and walls (Player.PassesThroughWalls).
        /// </summary>
        RaccoonTail = 15,

        /// <summary>
        /// White sword: the action button throws a sword the way the pac faces (WeaponRules.MAX_SWORDS in flight).
        /// </summary>
        SwordSwing = 16,

        /// <summary>
        /// Holy water: the pac leaves a trail of holy flames that kill anything that isn't invincible.
        /// </summary>
        HolyFire = 17,

        /// <summary>
        /// Fire flower: the pac flashes white and the action button fires fireballs (one per WeaponRules.ACTION_COOLDOWN_MS).
        /// </summary>
        FireFlower = 18,

        /// <summary>
        /// Pear: the action button teleports the pac to a random dot cell nobody is in.
        /// </summary>
        Warp = 19,

        /// <summary>
        /// Banana: the action button drops a banana peel on the pac's cell (one peel per cell). It stays until something
        /// touches it, which is stunned.
        /// </summary>
        BananaPeel = 20,

        /// <summary>
        /// Tetromino: the action button fills the cell behind the pac with a block that acts as a wall until the effect ends.
        /// </summary>
        BlockDrop = 21,

        /// <summary>
        /// Hammer: a hammer swings in front of the pac, killing any pac or ghost it hits (unless invincible) and smashing
        /// walls (not the outer wall) into dot cells.
        /// </summary>
        HammerTime = 22,

        /// <summary>
        /// BFG 9000: the action button fires a slow ball that bursts on the first pac, ghost or wall it hits, killing
        /// everything that isn't invincible within PowerUpEffects.EFFECT_RADIUS.
        /// </summary>
        Bfg = 23,

        /// <summary>
        /// Mushroom: size PowerUpEffects.SUPER_SIZE, moving through walls and smashing them (not the outer wall) into
        /// dot cells; back to PacRules.START_SIZE when it ends.
        /// </summary>
        SuperSize = 24,

        /// <summary>
        /// Energy tank: PacRules.ENERGY_TANK_SPEED_SCALE speed, dots grow it to PowerUpEffects.GetMaxSize (twice PacMaxSize),
        /// and never starves; back down to PacMaxSize when it ends.
        /// </summary>
        EnergyTank = 25,

        /// <summary>
        /// Screw attack: the pac flashes white and spins, and kills any enemy pac or ghost it touches. It can't eat dots
        /// or power pills.
        /// </summary>
        ScrewAttack = 26,

        /// <summary>
        /// Pepper shaker: a ghost that touches the pac is stunned instead of killing it.
        /// </summary>
        Pepper = 27,

        /// <summary>
        /// Spread gun: the pac fires a stunning shot each way (up, down, left, right) every WeaponRules.SPREAD_SHOT_INTERVAL_MS.
        /// </summary>
        SpreadShot = 28,

        /// <summary>
        /// Triforce: when it starts every flag of the pac's team away from base goes home (a rescue for the pac each), and
        /// while it works the team's flags are sealed in a force field: enemies can't pick them up (GameFlag.IsSealed).
        /// </summary>
        ForceField = 29,

        /// <summary>
        /// POW block: each press of the action button stuns every ghost and every other pac within
        /// PowerUpEffects.EFFECT_RADIUS for POW_STUN_MS (GameServer.PowStun).
        /// </summary>
        Pow = 30,

        /// <summary>
        /// Heart container: when it starts the pac grows to PacMaxSize if smaller and its hunger resets, and it never
        /// starves while it lasts. Its size stays as it is when it ends.
        /// </summary>
        FullHeal = 31,

        /// <summary>
        /// Poke ball: each press of the action button (at most one per WeaponRules.POKE_BALL_COOLDOWN_MS) throws a poke
        /// ball that seeks the nearest ghost. Every ball still flying goes when the effect ends.
        /// </summary>
        Gotcha = 32,
    }
}
