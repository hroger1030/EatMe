namespace EatMeLibrary
{
    public class GameEntity
    {
        public int Id { get; set; }

        public FastG.Point2 Location { get; set; }

        public float Size { get; set; }

        public float Speed { get; set; }

        public eFacing Facing { get; set; }

        public eTeam Team { get; set; }

        public bool Alive { get; set; }

        /// <summary>
        /// ms left before a dead entity comes back.
        /// </summary>
        public int RespawnTimer { get; set; }

        /// <summary>
        /// ms left of a stun (the POW block). A stunned entity can't move; nothing else about it changes.
        /// </summary>
        public int StunMs { get; set; }

        public bool IsStunned => StunMs > 0;
    }
}
