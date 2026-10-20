namespace EatMeLibrary
{
    /// <summary>
    /// A projectile or object a power up effect put in the maze: a thrown sword, a fireball, a holy flame, a blue
    /// shell or a poke ball. The server moves them (GameServer.UpdateWeaponEffects) and sends them in
    /// SnapshotMessage.WeaponEffects; the client draws Kind's row of the weapon effects sheet.
    /// </summary>
    public class WeaponEffect : GameEntity
    {
        public eWeaponEffect Kind { get; set; }

        /// <summary>
        /// Id of the pac that made it, who is credited with its kills.
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// False until a shot has stopped touching the pac that fired it, so it doesn't hit it as it leaves; after
        /// that it can.
        /// </summary>
        public bool OwnerClear { get; set; }

        /// <summary>
        /// Id of the ghost a poke ball holds, or NO_GHOST while it is still flying.
        /// </summary>
        public int CapturedGhostId { get; set; } = NO_GHOST;

        public const int NO_GHOST = -1;

        public bool IsHoldingGhost => CapturedGhostId != NO_GHOST;
    }
}
