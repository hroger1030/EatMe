namespace EatMeLibrary
{
    /// <summary>
    /// How a ghost picks who to chase and how it moves. One per GhostPersonality; GhostBrain has the rules.
    /// </summary>
    public enum eGhostBehavior
    {
        /// <summary>Norm: chases the nearest pac in vision, flag carriers first, until it dies or a better one turns up.</summary>
        Normal,

        /// <summary>Fang: like Normal, but never swaps its target for a closer pac, only for a flag carrier.</summary>
        Hunter,

        /// <summary>Sly: heads for the cell GhostBrain.AMBUSH_STEPS ahead of its target.</summary>
        Ambusher,

        /// <summary>Dither: chases until within SCATTER_DISTANCE of its target, then runs back to its home corner and tries again.</summary>
        Ditherer,

        /// <summary>Snitch: sees flag carriers from FLAG_HOUND_VISION_SCALE times as far, ignores other pacs unless very close, and otherwise patrols between flag bases.</summary>
        FlagHound,

        /// <summary>Warden: never goes more than LEASH_CELLS from its spawn cell.</summary>
        Guard,

        /// <summary>Drift: slow and short sighted (its personality's scales), otherwise Normal.</summary>
        Wanderer,

        /// <summary>Zip: fast, but drops its target after ATTENTION_MS and rests REST_MS before picking another.</summary>
        Sprinter,

        /// <summary>Murk: waits still on a junction until a pac comes in sight, then charges LURK_CHARGE_SCALE times as fast.</summary>
        Lurker,

        /// <summary>Brute: always chases the smallest pac in sight.</summary>
        Bully,

        /// <summary>Glutton: always chases the biggest pac in sight.</summary>
        Glutton,

        /// <summary>Spite: hunts the pac that last killed it from GRUDGE_VISION_SCALE times as far, until it gets them.</summary>
        Grudge,

        /// <summary>Frenzy: speeds up from FRENZY_MIN_SPEED_SCALE to FRENZY_MAX_SPEED_SCALE as the leading team nears MaxScore.</summary>
        Elroy,

        /// <summary>Dopey: heads for where its target was LAG_MS ago.</summary>
        SlowThinker,

        /// <summary>Bonk: doesn't path find; takes the open way pointing most straight at its target.</summary>
        StraightLiner,

        /// <summary>Boo: freezes while its target faces it down a clear straight line.</summary>
        Shy,

        /// <summary>Mimic: copies the direction of the nearest pac in sight.</summary>
        Copycat,

        /// <summary>Snooze: stops dead for NAP_MS out of every NAP_CYCLE_MS.</summary>
        Sleeper,

        /// <summary>Wobble: picks a new random pac in sight at every junction.</summary>
        Confused,

        /// <summary>Panic: runs from every pac in sight, powered or not, and chases nobody.</summary>
        Scaredy,

        /// <summary>Tagalong: follows the nearest other ghost, and joins in when it chases.</summary>
        Sidekick,
    }
}
