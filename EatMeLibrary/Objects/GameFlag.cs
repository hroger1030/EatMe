
namespace EatMeLibrary
{
    /// <summary>
    /// A team's flag: where its base is, where the flag is now, and who is carrying it.
    /// Positions are in map cells.
    /// </summary>
    public class GameFlag
    {
        public int Id { get; }
        public eTeam Team { get; }
        public float BaseX { get; }
        public float BaseY { get; }
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// Id of the player carrying the flag, or FlagState.NO_CARRIER.
        /// </summary>
        public int CarrierId { get; set; } = FlagState.NO_CARRIER;

        public bool IsCarried => CarrierId != FlagState.NO_CARRIER;

        public bool IsAtBase => !IsCarried && X == BaseX && Y == BaseY;

        /// <summary>
        /// True while a living pac of the flag's team has Force Field (triforce) working: enemies can't pick it up.
        /// </summary>
        public bool IsSealed { get; set; }

        /// <summary>
        /// True if a pac of the given team may pick this flag up: an enemy flag nobody carries that isn't sealed.
        /// </summary>
        public bool CanBePickedUpBy(eTeam team)
        {
            return team != Team && !IsCarried && !IsSealed;
        }

        public GameFlag(int id, eTeam team, float baseX, float baseY)
        {
            Id = id;
            Team = team;
            BaseX = baseX;
            BaseY = baseY;
            X = baseX;
            Y = baseY;
        }

        public void ReturnToBase()
        {
            CarrierId = FlagState.NO_CARRIER;
            X = BaseX;
            Y = BaseY;
        }

        public FlagState ToState()
        {
            return new FlagState(Id, Team, X, Y, CarrierId, IsSealed);
        }
    }
}
