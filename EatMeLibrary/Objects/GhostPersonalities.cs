using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// Every ghost personality. Each level's ghosts are picked from here by MapMaker.PickGhostLineup.
    /// </summary>
    public static class GhostPersonalities
    {
        /// <summary>
        /// On even levels Norm is this many times as likely to be drawn as any other ghost (odd levels always have one).
        /// </summary>
        public const int NORM_WEIGHT = 3;

        /// <summary>
        /// Every personality, Id matching its index. Colors keep clear of the red and blue team and spawn colors.
        /// </summary>
        public static readonly IReadOnlyList<GhostPersonality> ALL =
        [
            new(0, "Norm", FastColor.FromRgb(225, 190, 130), eGhostBehavior.Normal),
            new(1, "Fang", FastColor.FromRgb(120, 255, 40), eGhostBehavior.Hunter, visionScale: 1.2f),
            new(2, "Sly", FastColor.FromRgb(255, 80, 200), eGhostBehavior.Ambusher),
            new(3, "Dither", FastColor.FromRgb(255, 140, 0), eGhostBehavior.Ditherer),
            new(4, "Snitch", FastColor.FromRgb(255, 215, 0), eGhostBehavior.FlagHound),
            new(5, "Warden", FastColor.FromRgb(200, 205, 220), eGhostBehavior.Guard),
            new(6, "Drift", FastColor.FromRgb(100, 255, 180), eGhostBehavior.Wanderer, speedScale: 0.8f, visionScale: 0.5f),
            new(7, "Zip", FastColor.FromRgb(210, 255, 0), eGhostBehavior.Sprinter, speedScale: 1.25f),
            new(8, "Murk", FastColor.FromRgb(140, 60, 255), eGhostBehavior.Lurker),
            new(9, "Brute", FastColor.FromRgb(0, 220, 200), eGhostBehavior.Bully),
            new(10, "Glutton", FastColor.FromRgb(190, 60, 230), eGhostBehavior.Glutton),
            new(11, "Spite", FastColor.FromRgb(255, 110, 70), eGhostBehavior.Grudge),
            new(12, "Frenzy", FastColor.FromRgb(255, 245, 200), eGhostBehavior.Elroy),
            new(13, "Dopey", FastColor.FromRgb(255, 175, 215), eGhostBehavior.SlowThinker),
            new(14, "Bonk", FastColor.FromRgb(200, 130, 60), eGhostBehavior.StraightLiner),
            new(15, "Boo", FastColor.FromRgb(200, 160, 255), eGhostBehavior.Shy),
            new(16, "Mimic", FastColor.FromRgb(255, 250, 90), eGhostBehavior.Copycat),
            new(17, "Snooze", FastColor.FromRgb(170, 170, 40), eGhostBehavior.Sleeper),
            new(18, "Wobble", FastColor.FromRgb(40, 200, 60), eGhostBehavior.Confused),
            new(19, "Panic", FastColor.FromRgb(255, 200, 160), eGhostBehavior.Scaredy),
            new(20, "Tagalong", FastColor.FromRgb(170, 80, 140), eGhostBehavior.Sidekick),
        ];

        /// <summary>
        /// The normal ghost. Fills every slot once each personality has been used.
        /// </summary>
        public static readonly GhostPersonality NORM = ALL[0];
    }
}
