namespace EatMeLibrary
{
    public class Player : GameEntity
    {
        public string Name { get; set; }

        /// <summary>
        /// The pac sprite the player picked, sent to every client in PlayerJoined. Bots keep the default.
        /// </summary>
        public ePacShape PacShape { get; set; } = PacShapes.DEFAULT;

        public bool HasFlag { get; set; }

        public int FlagCaps { get; set; }

        public int PlayerKills { get; set; }

        public int GhostKills { get; set; }

        public int FlagRescues { get; set; }

        /// <summary>
        /// Power ups this player has eaten since connecting. Never reset.
        /// </summary>
        public int PowerUpsEaten { get; set; }

        public int DotsEaten { get; set; }

        /// <summary>
        /// Points this player has won for their team this level. Ranks players on the results screen.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Every point this player has won since connecting. Never reset; it is what goes on the high score list.
        /// </summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// ms since the pac last ate a dot or power pill. It starves (shrinks) once this passes MsUntilStarvation.
        /// </summary>
        public int MsSinceEaten { get; set; }

        /// <summary>
        /// Whether the last cell the pac entered had a dot or power pill in it. Clients play the chomp while it's set.
        /// </summary>
        public bool Eating { get; set; }

        /// <summary>
        /// Whether the pac is growing from standing on its own team's spawn (below PacMaxSize). Clients play the charge loop while it's set.
        /// </summary>
        public bool SpawnFeeding { get; set; }

        /// <summary>
        /// The cell the pac was in last tick, so Eating only changes when it enters a new one. -1 when it has none yet.
        /// </summary>
        public int LastCellX { get; set; } = -1;

        public int LastCellY { get; set; } = -1;

        /// <summary>
        /// The power up effect working on the pac, None when it has none. Only one works at a time.
        /// </summary>
        public ePowerUpEffect PowerUpEffect { get; set; }

        /// <summary>
        /// ms left on PowerUpEffect. 0 when it has none working.
        /// </summary>
        public int PowerUpMs { get; set; }

        /// <summary>
        /// True while a power pill is working: ghosts run from the pac and it pulses yellow.
        /// </summary>
        public bool IsPowered => PowerUpEffect == ePowerUpEffect.PowerPill && PowerUpMs > 0;

        /// <summary>
        /// True while Invincible (starman) is working: the pac can't be killed and kills any enemy pac or ghost it touches.
        /// </summary>
        public bool IsInvincible => PowerUpEffect == ePowerUpEffect.Invincible && PowerUpMs > 0;

        /// <summary>
        /// True while ghosts won't chase the pac and run from it when it is within their vision: a power pill,
        /// Doctor Away (apple) or Invincible (starman) is working. Doctor Away doesn't let it kill them.
        /// </summary>
        public bool RepelsGhosts => IsPowered || IsInvincible || (PowerUpEffect == ePowerUpEffect.DoctorAway && PowerUpMs > 0);

        /// <summary>
        /// True while Raccoon Tail is working: the pac passes through enemy pacs and ghosts.
        /// </summary>
        public bool PassesThrough => PowerUpEffects.PassesThrough(PowerUpEffect) && PowerUpMs > 0;

        /// <summary>
        /// True while an effect that destroys what it touches is working (PowerUpEffects.KillsOnContact: Invincible,
        /// Screw Attack, Super Size): the pac kills any enemy pac or ghost it touches, and ghosts can't kill it.
        /// </summary>
        public bool KillsOnContact => PowerUpEffects.KillsOnContact(PowerUpEffect) && PowerUpMs > 0;

        /// <summary>
        /// True while Raccoon Tail or Super Size is working: the pac moves through walls.
        /// </summary>
        public bool PassesThroughWalls => PowerUpEffects.PassesThroughWalls(PowerUpEffect) && PowerUpMs > 0;

        /// <summary>
        /// ms before the action button does anything again (WeaponRules.ACTION_COOLDOWN_MS after every press that did something).
        /// </summary>
        public int ActionCooldownMs { get; set; }

        /// <summary>
        /// ms before a pac with Spread Shot fires its next four shots (WeaponRules.SPREAD_SHOT_INTERVAL_MS).
        /// </summary>
        public int SpreadShotMs { get; set; }

        /// <summary>
        /// The cell a pac with Holy Fire was in last tick, so a flame goes down in each cell it leaves.
        /// </summary>
        public (int X, int Y)? HolyFireCell { get; set; }

        /// <summary>
        /// True once a pac with Bomb has dropped its one bomb; a new Bomb buff gives it another.
        /// </summary>
        public bool BombDropped { get; set; }
    }
}
