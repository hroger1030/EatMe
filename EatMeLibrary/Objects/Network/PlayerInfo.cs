namespace EatMeLibrary
{
    /// <summary>
    /// What a client knows about another player, from PlayerJoined.
    /// </summary>
    public class PlayerInfo
    {
        public int Id { get; }
        public string Name { get; }
        public eTeam Team { get; }

        public PlayerInfo(int id, string name, eTeam team)
        {
            Id = id;
            Name = name;
            Team = team;
        }
    }
}
