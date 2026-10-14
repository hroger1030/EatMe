namespace EatMeLibrary
{
    public class GameData
    {
        public Player[] Player { get; set; }

        public int CurrentLevel { get; set; }

        public int MaxScore { get; set; }

        public int RedTeamScore { get; set; }

        public int BlueTeamScore { get; set; }

        public Ghost[] Ghost { get; set; }
    }
}
