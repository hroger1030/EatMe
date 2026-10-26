using EatMeLibrary;

namespace EatMeTests
{
    public class ScoringBarTests
    {
        [Test]
        public void GetItems_ShowsEveryScoringValue_InOrder()
        {
            var config = new GameConfiguration { FlagScore = 25, FlagRescue = 4, PlayerKill = 2, GhostKill = 3 };

            var items = ScoringBar.GetItems(config);

            Assert.That(items.Select(i => i.Label), Is.EqualTo(new[]
            {
                Const.STRING_SCORING_CAPTURE, Const.STRING_SCORING_RETURN, Const.STRING_SCORING_EAT, Const.STRING_SCORING_EAT,
            }));
            Assert.That(items.Select(i => i.Icon), Is.EqualTo(new[]
            {
                eScoringIcon.EnemyFlag, eScoringIcon.OwnFlag, eScoringIcon.EnemyPac, eScoringIcon.Ghost,
            }));
            Assert.That(items.Select(i => i.Value), Is.EqualTo(new[] { "25pt", "4pt", "2pt", "3pt" }));
        }

        [Test]
        public void GetPointsText_AddsPt()
        {
            Assert.That(ScoringBar.GetPointsText(20), Is.EqualTo("20pt"));
        }

        [TestCase(500f, 5, 20f, 1900f, 345f)]
        [TestCase(100f, 2, 0f, 300f, 200f)]
        [TestCase(2000f, 5, 20f, 1900f, 0f)]
        [TestCase(100f, 1, 0f, 300f, 0f)]
        public void GetEvenGap_SharesTheSpareWidthBetweenNeighbours(float totalWidth, int count, float left, float right, float expected)
        {
            Assert.That(ScoringBar.GetEvenGap(totalWidth, count, left, right), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void GetPingWidthSamples_CoverUnknownAndEveryDigitAtFullLength()
        {
            var samples = ScoringBar.GetPingWidthSamples();

            Assert.That(samples, Does.Contain(ClientGameState.FormatPingText(-1)));
            Assert.That(samples, Does.Contain(ClientGameState.FormatPingText(ScoringBar.PING_BOX_MAX_MS)));
            Assert.That(samples, Does.Contain(ClientGameState.FormatPingText(8888)));
            Assert.That(samples, Has.Count.EqualTo(11));
        }

        [TestCase(eTeam.Red, eTeam.Blue)]
        [TestCase(eTeam.Blue, eTeam.Red)]
        public void GetEnemyTeam_IsTheOtherTeam(eTeam team, eTeam expected)
        {
            Assert.That(ScoringBar.GetEnemyTeam(team), Is.EqualTo(expected));
        }
    }
}
