namespace EatMeLibrary
{
    /// <summary>
    /// A bonus item drifting from a power up spawn point to a warp tunnel exit (GameServer.UpdatePowerUps).
    /// </summary>
    public class PowerUp : GameEntity
    {
        public ePowerUp Item { get; set; }

        /// <summary>
        /// The warp tunnel cell it is heading for; it leaves the level on reaching that cell's centre.
        /// </summary>
        public (int X, int Y) Exit { get; set; }

        public PowerUpState ToState()
        {
            return new PowerUpState(Id, Item, Location.X, Location.Y);
        }
    }
}
