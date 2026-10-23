namespace EatMeLibrary
{
    /// <summary>
    /// A bonus item, and the power up effect it gives the pac that eats it. A pac has at most one effect; a new one
    /// replaces it and restarts the timer. Every effect lasts GameConfiguration.PowerUpDurationMs. The value less one
    /// is the item's cell on the power up sheet (PowerUpSprites).
    /// </summary>
    public enum ePowerUp : byte
    {
        None = 0,

        /// <summary>
        /// Cherry Twin: the action button drops a decoy twin of the pac that ghosts and enemy bots go after instead.
        /// </summary>
        Cherry = 1,

        /// <summary>
        /// Sweet Tooth: dots give PacRules.SWEET_TOOTH_DOT_GAIN_SCALE times as much growth.
        /// </summary>
        Strawberry = 2,

        /// <summary>
        /// Vitamin C: never starves while it lasts.
        /// </summary>
        Orange = 3,

        /// <summary>
        /// Doctor Away: kills every ghost within PowerUpEffects.EFFECT_RADIUS while it lasts (PowerUpEffects.KillsGhostsInRange),
        /// and ghosts won't chase the pac and run from it (Player.RepelsGhosts).
        /// </summary>
        Apple = 4,

        /// <summary>
        /// Melon Roll: the action button sends the pac rolling straight on (MelonRoll), out of control, smashing walls and
        /// stunning anyone it touches until it reaches something it can't smash or the effect ends.
        /// </summary>
        Melon = 5,

        /// <summary>
        /// Warp Speed: moves PacRules.WARP_SPEED_SCALE times as fast and leaves a speed trail.
        /// </summary>
        Galaxian = 6,

        /// <summary>
        /// Dinner Bell: every ghost in the level moves PowerUpEffects.DINNER_BELL_GHOST_SPEED_SCALE times as fast.
        /// </summary>
        Bell = 7,

        /// <summary>
        /// Lockdown: dead enemy pacs can't respawn while it works (PowerUpEffects.LocksSpawn).
        /// </summary>
        Key = 8,

        /// <summary>
        /// Twister: everyone else within PowerUpEffects.EFFECT_RADIUS moves at half speed (PowerUpEffects.GetTwisterSpeedScale).
        /// </summary>
        Pretzel = 9,

        /// <summary>
        /// Warp: the action button teleports the pac to a random dot cell nobody is in.
        /// </summary>
        Pear = 10,

        /// <summary>
        /// Banana Peel: the action button drops a banana peel on the pac's cell (one peel per cell). It stays until
        /// something touches it, which is stunned.
        /// </summary>
        Banana = 11,

        /// <summary>
        /// Super Size: size PowerUpEffects.SUPER_SIZE, moving through walls and smashing them (not the outer wall) into
        /// dot cells; back to PacRules.START_SIZE when it ends.
        /// </summary>
        Mushroom = 12,

        /// <summary>
        /// A power pill that moves like the other items; eating one (or a maze power pill) gives this effect.
        /// </summary>
        PowerPill = 13,

        /// <summary>
        /// Fireball: the pac flashes white and the action button fires fireballs (one per WeaponRules.ACTION_COOLDOWN_MS).
        /// </summary>
        FireFlower = 14,

        /// <summary>
        /// Invincible: PacRules.INVINCIBLE_SPEED_SCALE times as fast, can't be killed, and kills any enemy pac or ghost
        /// it touches (Player.IsInvincible). Ghosts run from it. Leaves a speed trail and blinks yellow.
        /// </summary>
        Starman = 15,

        /// <summary>
        /// POW: each press of the action button stuns every ghost and every other pac within
        /// PowerUpEffects.EFFECT_RADIUS for POW_STUN_MS (GameServer.PowStun).
        /// </summary>
        PowBlock = 16,

        /// <summary>
        /// Raccoon Tail: the pac passes through enemy pacs, ghosts and walls (Player.PassesThroughWalls).
        /// </summary>
        SuperLeaf = 17,

        /// <summary>
        /// Hammer Time: a hammer swings in front of the pac, killing any pac or ghost it hits (unless invincible) and
        /// smashing walls (not the outer wall) into dot cells.
        /// </summary>
        Hammer = 18,

        /// <summary>
        /// Force Field: when it starts every flag of the pac's team away from base goes home (a rescue for the pac each),
        /// and while it works the team's flags are sealed in a force field: enemies can't pick them up (GameFlag.IsSealed).
        /// </summary>
        Triforce = 19,

        /// <summary>
        /// Full Heal: when it starts the pac grows to PacMaxSize if smaller and its hunger resets, and it never starves
        /// while it lasts. Its size stays as it is when it ends.
        /// </summary>
        HeartContainer = 20,

        /// <summary>
        /// Sword Swing: the action button throws a sword the way the pac faces (WeaponRules.MAX_SWORDS in flight).
        /// </summary>
        WhiteSword = 21,

        /// <summary>
        /// Energy Tank: PacRules.ENERGY_TANK_SPEED_SCALE speed, dots grow it to PowerUpEffects.GetMaxSize (twice
        /// PacMaxSize), and never starves; back down to PacMaxSize when it ends.
        /// </summary>
        EnergyTank = 22,

        /// <summary>
        /// Reinforcements (fairy): while the pac lives, its team's dead pacs respawn at once (no respawn timer).
        /// </summary>
        Reinforcements = 23,

        /// <summary>
        /// Screw Attack: the pac flashes white and spins, and kills any enemy pac or ghost it touches. It can't eat dots
        /// or power pills.
        /// </summary>
        ScrewAttack = 24,

        /// <summary>
        /// Holy Fire: the pac leaves a trail of holy flames that kill anything that isn't invincible.
        /// </summary>
        HolyWater = 25,

        /// <summary>
        /// Spread Shot: the pac fires a stunning shot each way (up, down, left, right) every WeaponRules.SPREAD_SHOT_INTERVAL_MS.
        /// </summary>
        SpreadGun = 26,

        /// <summary>
        /// Blue Shell: the first press of the action button (once per Blue Shell eaten) fires a shell that follows the
        /// maze to the enemy flag carrier, or else the enemy with the highest score, and kills it.
        /// </summary>
        BlueShell = 27,

        /// <summary>
        /// Block Drop: each plain open cell the pac leaves fills with a block that acts as a wall until the effect ends.
        /// </summary>
        Tetromino = 28,

        /// <summary>
        /// BFG 9000: the action button fires a slow ball that bursts on the first pac, ghost or wall it hits, killing
        /// everything that isn't invincible within PowerUpEffects.EFFECT_RADIUS.
        /// </summary>
        BFG = 29,

        /// <summary>
        /// Bomb: the action button drops one bomb (once per buff) on the pac's cell. It goes off WeaponRules.BOMB_FUSE_MS
        /// later, stunning every enemy pac and ghost within PowerUpEffects.EFFECT_RADIUS.
        /// </summary>
        Bomb = 30,

        /// <summary>
        /// Pepper: a ghost that touches the pac is stunned instead of killing it.
        /// </summary>
        PepperShaker = 31,

        /// <summary>
        /// Gotcha: each press of the action button (at most one per WeaponRules.ACTION_COOLDOWN_MS) throws a poke
        /// ball that seeks the nearest ghost. Every ball still flying goes when the effect ends.
        /// </summary>
        PokeBall = 32,
    }
}
