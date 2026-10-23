using System.Collections.Generic;

namespace EatMeLibrary
{
    public class Ghost : GameEntity
    {
        /// <summary>
        /// GrudgeId when the ghost holds no grudge.
        /// </summary>
        public const int NO_GRUDGE = -1;

        public string Name { get; set; }

        public int PlayerKills { get; set; }

        public FastColor SpriteColor { get; set; }

        public Player CurrentTarget { get; set; }

        /// <summary>
        /// How it behaves (GhostBrain), picked for the level by MapMaker.PickGhostLineup.
        /// </summary>
        public GhostPersonality Personality { get; set; } = GhostPersonalities.NORM;

        /// <summary>
        /// The cell it spawns on; the Guard never strays more than GhostBrain.LEASH_CELLS from it.
        /// </summary>
        public (int X, int Y) SpawnCell { get; set; }

        /// <summary>
        /// The Ditherer's corner, which it runs back to (GhostBrain.FindHomeCorner).
        /// </summary>
        public (int X, int Y) HomeCell { get; set; }

        /// <summary>
        /// The Ditherer is on its way back to HomeCell.
        /// </summary>
        public bool IsScattering { get; set; }

        /// <summary>
        /// Id of the pac that last killed it, which the Grudge hunts, or NO_GRUDGE.
        /// </summary>
        public int GrudgeId { get; set; } = NO_GRUDGE;

        /// <summary>
        /// ms since it last spawned; the Sleeper naps on this clock and the SlowThinker stamps its history with it.
        /// </summary>
        public int LifeMs { get; set; }

        /// <summary>
        /// ms the Sprinter has spent on its current target.
        /// </summary>
        public int ChaseMs { get; set; }

        /// <summary>
        /// ms the Sprinter has left to rest before it chases again.
        /// </summary>
        public int RestMs { get; set; }

        /// <summary>
        /// The junction the Lurker waits on, or null when it has to find one.
        /// </summary>
        public (int X, int Y)? LurkCell { get; set; }

        /// <summary>
        /// The cell the Roamer is wandering to, or null when it has to pick one (GhostBrain.PickWanderCell).
        /// </summary>
        public (int X, int Y)? WanderCell { get; set; }

        /// <summary>
        /// Which flag base the FlagHound is patrolling toward (an index into the level's flags).
        /// </summary>
        public int PatrolIndex { get; set; }

        /// <summary>
        /// The SlowThinker's record of its target's cells, oldest first, stamped with LifeMs (GhostBrain.GetLaggedCell).
        /// </summary>
        public Queue<(int Ms, int X, int Y)> TargetHistory { get; } = new();

        /// <summary>
        /// True while a poke ball holds the ghost: it isn't alive and doesn't count down to respawn; it comes back
        /// where the ball is when something touches the ball.
        /// </summary>
        public bool IsCaptured { get; set; }
    }
}
