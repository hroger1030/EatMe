using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class ServerRulesTests
    {
        [TestCase(eTeamPreference.Red, 0, 0, eTeam.Red)]
        [TestCase(eTeamPreference.Blue, 0, 0, eTeam.Blue)]
        [TestCase(eTeamPreference.Red, 3, 2, eTeam.Blue)]
        [TestCase(eTeamPreference.Blue, 2, 3, eTeam.Red)]
        [TestCase(eTeamPreference.Red, 2, 3, eTeam.Red)]
        [TestCase(eTeamPreference.Either, 1, 1, eTeam.Red)]
        [TestCase(eTeamPreference.Either, 2, 1, eTeam.Blue)]
        public void AssignTeam(eTeamPreference preferred, int redCount, int blueCount, eTeam expected)
        {
            Assert.That(TeamBalancer.AssignTeam(preferred, redCount, blueCount), Is.EqualTo(expected));
        }

        [TestCase("A", true)]
        [TestCase("Pacman2026", true)]
        [TestCase("TwentyCharacterName1", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("TwentyOneCharacterNam", false)]
        [TestCase("has space", false)]
        [TestCase("dash-name", true)]
        [TestCase("Mr.Pac", true)]
        [TestCase("under_score", true)]
        [TestCase("bang!", false)]
        [TestCase("café", false)]
        public void PlayerName_IsValid(string name, bool expected)
        {
            Assert.That(PlayerNameRules.IsValid(name), Is.EqualTo(expected));
        }

        [Test]
        public void LoadingScreenTips_Parse_SkipsBlankLinesAndTrims()
        {
            var messages = LoadingScreenTipsManager.Parse("  Welcome!  \r\n\r\nHave fun\n   \n");

            Assert.That(messages, Is.EqualTo(new[] { "Welcome!", "Have fun" }));
        }

        [Test]
        public void LoadingScreenTips_Parse_KeepsEveryLine()
        {
            var text = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line {i}"));

            var messages = LoadingScreenTipsManager.Parse(text);

            Assert.That(messages.Count, Is.EqualTo(200));
            Assert.That(messages[0], Is.EqualTo("Line 1"));
            Assert.That(messages[199], Is.EqualTo("Line 200"));
        }

        [Test]
        public void TipRules_PickTip_AnyLineCanBeChosen()
        {
            var tips = Enumerable.Range(1, 20).Select(i => $"Line {i}").ToList();
            var random = new Random(1);
            var seen = new HashSet<string>();

            for (var i = 0; i < 1000; i++)
                seen.Add(TipRules.PickTip(tips, random));

            Assert.That(seen, Is.EquivalentTo(tips));
            Assert.That(TipRules.PickTip([], random), Is.Empty);
            Assert.That(TipRules.PickTip(null, random), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void MessageOfTheDay_EmptyOrMissing_UsesDefault(string configured)
        {
            Assert.That(ConfigManager.GetMessageOfTheDay(configured), Is.EqualTo(Const.MOTD_DEFAULT));
        }

        [Test]
        public void MessageOfTheDay_Configured_IsKeptTrimmed()
        {
            Assert.That(ConfigManager.GetMessageOfTheDay("  Have fun  "), Is.EqualTo("Have fun"));
        }

        [TestCase(-5, 1)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(12, 12)]
        [TestCase(500, 500)]
        public void ClampStartingLevel(int configured, int expected)
        {
            Assert.That(ConfigManager.ClampStartingLevel(configured), Is.EqualTo(expected));
        }

        [Test]
        public void GameServer_StartsOnGivenLevel()
        {
            var settings = new ServerSettings { ServerName = "Start", TargetFps = 30, AutoBalanceTeams = false };
            var server = new GameServer(settings, [], level: 7);

            Assert.That(server.CurrentLevel.Level, Is.EqualTo(7));
        }

        [Test]
        public void LoadingScreenTips_Parse_EmptyFile_NoMessages()
        {
            Assert.That(LoadingScreenTipsManager.Parse(""), Is.Empty);
        }

        [Test]
        public void HighScores_Parse_ReadsNameScoreAndDate()
        {
            var scores = HighScoreRules.Parse("  LotusVenom , 1522400 , 2024-04-12  \r\n");

            Assert.That(scores.Count, Is.EqualTo(1));
            Assert.That(scores[0].Name, Is.EqualTo("LotusVenom"));
            Assert.That(scores[0].Score, Is.EqualTo(1522400));
            Assert.That(scores[0].Date, Is.EqualTo("2024-04-12"));
        }

        [Test]
        public void HighScores_Parse_NameMayHoldCommas()
        {
            var scores = HighScoreRules.Parse("Smith, John,500,2025-01-01");

            Assert.That(scores[0].Name, Is.EqualTo("Smith, John"));
            Assert.That(scores[0].Score, Is.EqualTo(500));
        }

        [Test]
        public void HighScores_Parse_RanksHighestFirstKeepingFileOrderForTies()
        {
            var scores = HighScoreRules.Parse("Dios,200,2026-01-09\nTitar,98750,2026-03-30\nSpartan,200,2025-12-25\n");

            Assert.That(scores.Select(s => s.Name), Is.EqualTo(new[] { "Titar", "Dios", "Spartan" }));
        }

        [Test]
        public void HighScores_Parse_SkipsBlankAndBadLines()
        {
            var badLines = new List<string>();

            var scores = HighScoreRules.Parse("\n   \nGood,10,2025-01-01\nNoFields\nOnly,10\nBadScore,ten,2025-01-01\nNegative,-5,2025-01-01\n,10,2025-01-01\n", badLines);

            Assert.That(scores.Select(s => s.Name), Is.EqualTo(new[] { "Good" }));
            Assert.That(badLines, Is.EqualTo(new[] { "NoFields", "Only,10", "BadScore,ten,2025-01-01", "Negative,-5,2025-01-01", ",10,2025-01-01" }));
        }

        [Test]
        public void HighScores_Parse_EmptyFile_NoScores()
        {
            Assert.That(HighScoreRules.Parse(""), Is.Empty);
        }

        [Test]
        public void IsDiscoveryRequest_RecognisesRequest()
        {
            Assert.That(DiscoveryResponder.IsDiscoveryRequest(MessageSerializer.Serialize(new DiscoveryRequestMessage())), Is.True);
        }

        [Test]
        public void IsDiscoveryRequest_RejectsOtherMessagesAndJunk()
        {
            Assert.That(DiscoveryResponder.IsDiscoveryRequest(MessageSerializer.Serialize(new PingMessage())), Is.False);
            Assert.That(DiscoveryResponder.IsDiscoveryRequest([1, 2, 3]), Is.False);
            Assert.That(DiscoveryResponder.IsDiscoveryRequest([255, 255, 255, 127, 0]), Is.False);
        }
    }
}
