using EatMeLibrary;

namespace EatMeTests
{
    public class LevelPhasesTests
    {
        [Test]
        public void GetCountdownNumber_RoundsTheSecondsLeftUp()
        {
            Assert.That(LevelPhases.GetCountdownNumber(LevelPhases.START_COUNTDOWN_MS), Is.EqualTo(3));
            Assert.That(LevelPhases.GetCountdownNumber(2001), Is.EqualTo(3));
            Assert.That(LevelPhases.GetCountdownNumber(2000), Is.EqualTo(2));
            Assert.That(LevelPhases.GetCountdownNumber(1000), Is.EqualTo(1));
            Assert.That(LevelPhases.GetCountdownNumber(1), Is.EqualTo(1));
            Assert.That(LevelPhases.GetCountdownNumber(0), Is.EqualTo(0));
            Assert.That(LevelPhases.GetCountdownNumber(-500), Is.EqualTo(0));
        }

        [Test]
        public void GetCountdownText_SaysStartingInWithTheNumber()
        {
            Assert.That(LevelPhases.GetCountdownText(3), Is.EqualTo("Starting in... 3"));
            Assert.That(LevelPhases.GetCountdownText(1), Is.EqualTo("Starting in... 1"));
            Assert.That(LevelPhases.GetCountdownText(0), Is.Empty);
        }

        [Test]
        public void IsVictoryTextOn_FlashesOnThenOff()
        {
            var flash = TimeSpan.FromMilliseconds(LevelPhases.VICTORY_FLASH_MS);

            Assert.That(LevelPhases.IsVictoryTextOn(TimeSpan.Zero), Is.True, "starts on");
            Assert.That(LevelPhases.IsVictoryTextOn(flash * 0.5), Is.True);
            Assert.That(LevelPhases.IsVictoryTextOn(flash * 1.5), Is.False);
            Assert.That(LevelPhases.IsVictoryTextOn(flash * 2.5), Is.True);
            Assert.That(LevelPhases.IsVictoryTextOn(TimeSpan.FromSeconds(-1)), Is.True);
        }

        [Test]
        public void ClientState_LevelStart_StartsTheCountdown_AndFreezesUntilItRunsOut()
        {
            var state = new ClientGameState();
            state.Apply(new LevelStartMessage { Level = 1, CountdownMs = LevelPhases.START_COUNTDOWN_MS }, 1000);

            Assert.That(state.GetCountdownNumber(1000), Is.EqualTo(3));
            Assert.That(state.GetCountdownNumber(2500), Is.EqualTo(2));
            Assert.That(state.GetCountdownNumber(3999), Is.EqualTo(1));
            Assert.That(state.IsFrozen(3999), Is.True);
            Assert.That(state.GetCountdownNumber(4000), Is.EqualTo(0));
            Assert.That(state.IsFrozen(4000), Is.False);
        }

        [Test]
        public void ClientState_Victory_ShowsUntilTheResults()
        {
            var state = new ClientGameState();
            state.Apply(new LevelStartMessage { Level = 1 }, 0);
            state.Apply(new VictoryMessage { WinningTeam = eTeam.Blue, MsRemaining = LevelPhases.VICTORY_MS }, 1000);

            Assert.That(state.IsVictory, Is.True);
            Assert.That(state.VictoryTeam, Is.EqualTo(eTeam.Blue));
            Assert.That(state.IsFrozen(1000), Is.True);
            Assert.That(state.IsIntermission, Is.False, "the results come later");
            Assert.That(state.IsVictoryTextOn(1000), Is.True);
            Assert.That(state.IsVictoryTextOn(1000 + (long)(LevelPhases.VICTORY_FLASH_MS * 1.5)), Is.False, "it flashes");

            state.Apply(new LevelWonMessage { WinningTeam = eTeam.Blue, MsRemaining = 10000 }, 4000);

            Assert.That(state.IsVictory, Is.False);
            Assert.That(state.IsVictoryTextOn(4000), Is.False);
            Assert.That(state.IsIntermission, Is.True);
        }

        [Test]
        public void ClientState_LevelStart_ClearsVictory()
        {
            var state = new ClientGameState();
            state.Apply(new VictoryMessage { WinningTeam = eTeam.Red, MsRemaining = 1000 }, 0);
            state.Apply(new LevelStartMessage { Level = 2 }, 100);

            Assert.That(state.IsVictory, Is.False);
        }
    }
}
