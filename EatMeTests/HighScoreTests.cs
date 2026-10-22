using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class HighScoreTests
    {
        private static HighScoreEntry Entry(string name, int score)
        {
            return new HighScoreEntry { Name = name, Score = score, Date = "2026-01-01" };
        }

        private static List<HighScoreEntry> FullList()
        {
            return Enumerable.Range(1, HighScoreRules.MAX_HIGH_SCORES).Select(i => Entry($"P{i}", i * 100)).OrderByDescending(e => e.Score).ToList();
        }

        private static GameServer MakeServer(List<HighScoreEntry> highScores, Action<List<HighScoreEntry>> onChanged, int minPlayers = 0, int maxScore = 100)
        {
            var settings = new ServerSettings
            {
                ServerName = "Scores",
                ClientUpdatesPerSecond = 30,
                AutoBalanceTeams = false,
                GameConfig = new GameConfiguration { MinPlayers = minPlayers, MaxScore = maxScore, IntermissionTimeMs = 0 },
            };

            return new GameServer(settings, [], highScores: highScores, onHighScoresChanged: onChanged);
        }

        private static ServerPlayer Join(GameServer server, int id, eTeamPreference team)
        {
            var connection = new FakeConnection(id);
            server.HandleMessage(connection, new HelloMessage { PlayerName = $"P{id}", PreferredTeam = team });
            return server.Players.Single(p => p.Connection == connection);
        }

        [Test]
        public void Submit_ShortList_AddsAndRanks()
        {
            var scores = new List<HighScoreEntry> { Entry("A", 500), Entry("B", 100) };
            var entry = Entry("C", 300);

            Assert.That(HighScoreRules.Submit(scores, entry), Is.True);
            Assert.That(scores.Select(s => s.Name), Is.EqualTo(new[] { "A", "C", "B" }));
        }

        [Test]
        public void Submit_FullList_CutsToTen()
        {
            var scores = FullList();

            Assert.That(HighScoreRules.Submit(scores, Entry("New", 550)), Is.True);
            Assert.That(scores.Count, Is.EqualTo(HighScoreRules.MAX_HIGH_SCORES));
            Assert.That(scores.Any(s => s.Name == "P1"), Is.False, "lowest drops off");
        }

        [Test]
        public void Submit_TooLow_StaysOff()
        {
            var scores = FullList();
            var entry = Entry("Low", 50);

            Assert.That(HighScoreRules.Submit(scores, entry), Is.False);
            Assert.That(scores.Contains(entry), Is.False);
            Assert.That(scores.Count, Is.EqualTo(HighScoreRules.MAX_HIGH_SCORES));
        }

        [Test]
        public void Submit_TieWithLast_StaysOff()
        {
            var scores = FullList();

            Assert.That(HighScoreRules.Submit(scores, Entry("Tie", 100)), Is.False, "earlier scores keep their place on ties");
        }

        [Test]
        public void Submit_ExistingEntry_MovesUpWithoutDuplicate()
        {
            var entry = Entry("Me", 150);
            var scores = new List<HighScoreEntry> { Entry("A", 500), Entry("B", 200), entry };

            entry.Score = 600;

            Assert.That(HighScoreRules.Submit(scores, entry), Is.True);
            Assert.That(scores.Select(s => s.Name), Is.EqualTo(new[] { "Me", "A", "B" }));
        }

        [Test]
        public void Format_RoundTripsThroughParse()
        {
            var scores = new List<HighScoreEntry> { Entry("Smith, John", 900), Entry("Ann", 40) };

            var parsed = HighScoreRules.Parse(HighScoreRules.Format(scores));

            Assert.That(parsed.Select(s => (s.Name, s.Score, s.Date)), Is.EqualTo(scores.Select(s => (s.Name, s.Score, s.Date))));
        }

        [Test]
        public void GetDate_IsYearMonthDay()
        {
            Assert.That(HighScoreManager.GetDate(new DateTime(2026, 3, 7, 23, 59, 0)), Is.EqualTo("2026-03-07"));
        }

        [Test]
        public void UpdateHighScores_AddsHumansWithPointsButNotBots()
        {
            var scores = new List<HighScoreEntry>();
            var server = MakeServer(scores, null, minPlayers: 4);
            var scorer = Join(server, 1, eTeamPreference.Red);
            var idle = Join(server, 2, eTeamPreference.Blue);
            var bot = server.Players.First(p => p.IsBot);
            scorer.Player.TotalScore = 30;
            bot.Player.TotalScore = 99;

            Assert.That(server.UpdateHighScores(new DateTime(2026, 10, 15)), Is.True);
            Assert.That(scores.Count, Is.EqualTo(1));
            Assert.That(scores[0].Name, Is.EqualTo(scorer.Player.Name));
            Assert.That(scores[0].Score, Is.EqualTo(30));
            Assert.That(scores[0].Date, Is.EqualTo("2026-10-15"));
            Assert.That(idle.HighScore, Is.Null, "no points, no entry");
        }

        [Test]
        public void HighScores_SentOnJoinAndBroadcastWhenChanged()
        {
            var scores = new List<HighScoreEntry> { Entry("Old", 500) };
            var server = MakeServer(scores, null);
            var connection = new FakeConnection(1);
            server.HandleMessage(connection, new HelloMessage { PlayerName = "P1", PreferredTeam = eTeamPreference.Red });

            var onJoin = connection.Received.OfType<HighScoresMessage>().Single();
            Assert.That(onJoin.Scores.Select(e => e.Name), Is.EqualTo(new[] { "Old" }));

            server.Players.Single(p => p.Connection == connection).Player.TotalScore = 900;
            server.UpdateHighScores(DateTime.Now);

            var changed = connection.Received.OfType<HighScoresMessage>().Last();
            Assert.That(changed.Scores.Select(e => e.Name), Is.EqualTo(new[] { "P1", "Old" }));

            var count = connection.Received.OfType<HighScoresMessage>().Count();
            server.UpdateHighScores(DateTime.Now);
            Assert.That(connection.Received.OfType<HighScoresMessage>().Count(), Is.EqualTo(count), "no change, nothing sent");
        }

        [Test]
        public void ClientGameState_HighScores_EmptyUntilServerSendsThem()
        {
            var state = new ClientGameState();
            Assert.That(state.HighScores, Is.Empty);

            state.Apply(new HighScoresMessage { Scores = [Entry("A", 500), Entry("B", 100)] }, 0);

            Assert.That(state.HighScores.Select(e => e.Score), Is.EqualTo(new[] { 500, 100 }));
        }

        [Test]
        public void UpdateHighScores_OneEntryPerConnectionUpdatedEachLevel()
        {
            var scores = new List<HighScoreEntry>();
            var saves = 0;
            var server = MakeServer(scores, _ => saves++);
            var player = Join(server, 1, eTeamPreference.Red);

            player.Player.TotalScore = 30;
            server.UpdateHighScores(DateTime.Now);
            player.Player.TotalScore = 75;
            server.UpdateHighScores(DateTime.Now);

            Assert.That(scores.Count, Is.EqualTo(1));
            Assert.That(scores[0].Score, Is.EqualTo(75));
            Assert.That(saves, Is.EqualTo(2));

            Assert.That(server.UpdateHighScores(DateTime.Now), Is.False, "no change, no save");
            Assert.That(saves, Is.EqualTo(2));
        }

        [Test]
        public void TotalScore_KeepsGrowingAcrossLevelsAndReachesHighScores()
        {
            var scores = new List<HighScoreEntry>();
            var server = MakeServer(scores, null, maxScore: 1);
            var red = Join(server, 1, eTeamPreference.Red).Player;
            var blue = Join(server, 2, eTeamPreference.Blue).Player;

            for (var level = 0; level < 2; level++)
            {
                blue.Location = red.Location;
                red.Size = 60f;
                blue.Size = 0f;
                blue.Alive = true;
                server.UpdatePacFights();
                Assert.That(server.CheckLevelWon(), Is.True);
            }

            Assert.That(red.Score, Is.Zero, "level score resets");
            Assert.That(red.TotalScore, Is.EqualTo(2), "running total doesn't");
            Assert.That(scores.Single().Score, Is.EqualTo(2));
            Assert.That(server.BuildSnapshot().Pacs.Single(p => p.PlayerId == red.Id).TotalScore, Is.EqualTo(2));
        }

        [Test]
        public void Merge_AddsNewScoresAsCopiesRankedHighestFirst()
        {
            var stored = new List<HighScoreEntry> { Entry("A", 500) };
            var incoming = new List<HighScoreEntry> { Entry("B", 900), Entry("C", 100) };

            Assert.That(HighScoreRules.Merge(stored, incoming), Is.True);
            Assert.That(stored.Select(e => e.Name), Is.EqualTo(new[] { "B", "A", "C" }));
            Assert.That(stored, Does.Not.Contain(incoming[0]), "the client keeps its own copies");
        }

        [Test]
        public void Merge_SameNameAndDate_OnlyRaisesTheScore()
        {
            var stored = new List<HighScoreEntry> { Entry("A", 500) };

            Assert.That(HighScoreRules.Merge(stored, [Entry("A", 300)]), Is.False, "a lower score changes nothing");
            Assert.That(HighScoreRules.Merge(stored, [Entry("A", 700)]), Is.True);
            Assert.That(stored.Count, Is.EqualTo(1));
            Assert.That(stored[0].Score, Is.EqualTo(700));
        }

        [Test]
        public void Merge_SameNameOtherDate_IsASecondEntry()
        {
            var stored = new List<HighScoreEntry> { Entry("A", 500) };

            HighScoreRules.Merge(stored, [new HighScoreEntry { Name = "A", Score = 400, Date = "2026-02-02" }]);

            Assert.That(stored.Count, Is.EqualTo(2));
        }

        [Test]
        public void Merge_KeepsScoresFromEveryServerAndOnlyHigherOnesPushOthersOff()
        {
            var stored = FullList();
            var lowest = stored.Last().Score;

            Assert.That(HighScoreRules.Merge(stored, [Entry("Low", lowest - 1)]), Is.False, "too low to make the list");
            Assert.That(HighScoreRules.Merge(stored, [Entry("High", lowest + 1)]), Is.True);
            Assert.That(stored.Count, Is.EqualTo(HighScoreRules.MAX_HIGH_SCORES));
            Assert.That(stored.Last().Name, Is.EqualTo("High"));
        }

        [Test]
        public void Merge_EmptyOrBadIncoming_ChangesNothing()
        {
            var stored = new List<HighScoreEntry> { Entry("A", 500) };

            Assert.That(HighScoreRules.Merge(stored, []), Is.False);
            Assert.That(HighScoreRules.Merge(stored, null), Is.False);
            Assert.That(HighScoreRules.Merge(stored, [Entry(" ", 900), Entry("Neg", -1)]), Is.False);
            Assert.That(stored.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetRankText_UsesParenthesis()
        {
            Assert.That(HighScoreRules.GetRankText(0), Is.EqualTo("1)"));
            Assert.That(HighScoreRules.GetRankText(99), Is.EqualTo("100)"));
        }

        [Test]
        public void TableLayout_RunsLeftToRightThenDown()
        {
            Assert.That(HighScoreRules.GetRowCount(0, 2), Is.EqualTo(0));
            Assert.That(HighScoreRules.GetRowCount(5, 2), Is.EqualTo(3));
            Assert.That(HighScoreRules.GetRowCount(100, 2), Is.EqualTo(50));
            Assert.That(HighScoreRules.GetEntryIndex(0, 1, 2), Is.EqualTo(1));
            Assert.That(HighScoreRules.GetEntryIndex(2, 0, 2), Is.EqualTo(4));
        }

        [Test]
        public void ScrollMath_ClampsAndSizesTheThumb()
        {
            Assert.That(ScrollMath.GetMaxScroll(100f, 300f), Is.EqualTo(0f), "content that fits doesn't scroll");
            Assert.That(ScrollMath.GetMaxScroll(1000f, 300f), Is.EqualTo(700f));
            Assert.That(ScrollMath.ClampOffset(-5f, 1000f, 300f), Is.EqualTo(0f));
            Assert.That(ScrollMath.ClampOffset(5000f, 1000f, 300f), Is.EqualTo(700f));

            Assert.That(ScrollMath.GetThumbHeight(1200f, 300f, 30f), Is.EqualTo(75f));
            Assert.That(ScrollMath.GetThumbHeight(100000f, 300f, 30f), Is.EqualTo(30f));
            Assert.That(ScrollMath.GetThumbOffset(900f, 1200f, 300f, 75f), Is.EqualTo(225f), "fully scrolled puts the thumb at the bottom");
            Assert.That(ScrollMath.GetOffsetFromThumb(300f, 1200f, 300f, 75f), Is.EqualTo(900f));
            Assert.That(ScrollMath.GetOffsetFromThumb(0f, 1200f, 300f, 75f), Is.EqualTo(0f));
        }
    }
}
