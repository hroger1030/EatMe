using System.Diagnostics;
using EatMeLibrary;

namespace EatMeTests
{
    public class ClientPerfTests
    {
        private static long After(long timestamp, double ms)
        {
            return timestamp + (long)(ms * Stopwatch.Frequency / 1000);
        }

        [TestCase(300, 5000, 60)]
        [TestCase(5, 0, 0)]
        public void RateIsPerSecond(double count, double windowMs, double expected)
        {
            Assert.That(PerfStats.GetRate(count, windowMs), Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase(10, 40, 30)]
        [TestCase(10, 10, 0)]
        [TestCase(500, 25, 25)]
        public void CountSinceStartsOverWhenTheCountRestarts(long start, long count, long expected)
        {
            Assert.That(ClientPerf.GetCountSince(start, count), Is.EqualTo(expected));
        }

        [Test]
        public void StartsHidden()
        {
            var perf = new ClientPerf();

            Assert.That(perf.IsVisible, Is.False);
            Assert.That(perf.Lines, Is.Empty);
        }

        [Test]
        public void ToggleShowsAndHides()
        {
            var perf = new ClientPerf();

            perf.ToggleVisible();
            Assert.That(perf.IsVisible, Is.True);

            perf.ToggleVisible();
            Assert.That(perf.IsVisible, Is.False);
        }

        [Test]
        public void FirstDrawIsNotAFrame()
        {
            var perf = new ClientPerf();
            var start = PerfStats.GetTimestamp();

            perf.StartFrame(start);
            Assert.That(perf.Stats.Sections[(int)eClientPerfSection.Frame].Count, Is.EqualTo(0));

            perf.StartFrame(After(start, 16));
            var frame = perf.Stats.Sections[(int)eClientPerfSection.Frame];
            Assert.That(frame.Count, Is.EqualTo(1));
            Assert.That(frame.MaxMs, Is.EqualTo(16).Within(0.01));
        }

        [Test]
        public void LinesWaitForAWholeWindow()
        {
            var perf = new ClientPerf();
            var now = PerfStats.GetTimestamp();

            Assert.That(perf.Update(now, false, 0, ""), Is.False);
            Assert.That(perf.Lines, Is.Empty);
        }

        [Test]
        public void MenusShowFrameAndMemoryLines()
        {
            var perf = new ClientPerf();
            var now = After(PerfStats.GetTimestamp(), ClientPerf.WINDOW_MS + 1);

            Assert.That(perf.Update(now, false, 0, "Ping 42 ms"), Is.True);
            Assert.That(perf.Lines, Has.Count.EqualTo(4));
            Assert.That(perf.Lines[0], Does.StartWith("FPS"));
            Assert.That(perf.Lines, Has.None.EqualTo("Ping 42 ms"));
        }

        [Test]
        public void GamesAddNetworkAndPingLines()
        {
            var perf = new ClientPerf();
            var now = After(PerfStats.GetTimestamp(), ClientPerf.WINDOW_MS + 1);

            Assert.That(perf.Update(now, true, 0, "Ping 42 ms"), Is.True);
            Assert.That(perf.Lines, Has.Count.EqualTo(6));
            Assert.That(perf.Lines[4], Does.StartWith("Snapshots"));
            Assert.That(perf.Lines[5], Is.EqualTo("Ping 42 ms"));
        }

        [Test]
        public void EndingAWindowStartsANewOne()
        {
            var perf = new ClientPerf();
            var start = PerfStats.GetTimestamp();
            perf.Record(eClientPerfSection.Update, start);

            var now = After(start, ClientPerf.WINDOW_MS + 1);
            perf.Update(now, false, 0, "");

            Assert.That(perf.Stats.Sections[(int)eClientPerfSection.Update].Count, Is.EqualTo(0));
            Assert.That(perf.Update(After(now, 10), false, 0, ""), Is.False);
        }

        [Test]
        public void ClientStateCountsEverySnapshot()
        {
            var state = new ClientGameState();

            state.Apply(new SnapshotMessage { Tick = 5 }, 0);
            state.Apply(new SnapshotMessage { Tick = 3 }, 0);

            Assert.That(state.SnapshotsReceived, Is.EqualTo(2));
        }
    }
}
