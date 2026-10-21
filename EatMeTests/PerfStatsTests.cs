using EatMeLibrary;
using EatMeServer;

namespace EatMeTests
{
    public class PerfStatsTests
    {
        [Test]
        public void SectionWithNothingRecordedIsZero()
        {
            var section = new PerfSection("empty");

            Assert.That(section.Count, Is.EqualTo(0));
            Assert.That(section.AverageMs, Is.EqualTo(0));
            Assert.That(section.MaxMs, Is.EqualTo(0));
            Assert.That(section.GetPercentileMs(PerfStats.REPORT_PERCENTILE), Is.EqualTo(0));
        }

        [Test]
        public void SectionKeepsCountTotalAverageAndMax()
        {
            var section = new PerfSection("step");
            section.Add(1);
            section.Add(2);
            section.Add(6);

            Assert.That(section.Count, Is.EqualTo(3));
            Assert.That(section.TotalMs, Is.EqualTo(9).Within(1e-9));
            Assert.That(section.AverageMs, Is.EqualTo(3).Within(1e-9));
            Assert.That(section.MaxMs, Is.EqualTo(6).Within(1e-9));
        }

        [Test]
        public void PercentileIsTheTopOfItsBucket()
        {
            var section = new PerfSection("step");

            // 99 quick times and one slow one: the 99th percentile is still quick
            for (var i = 0; i < 99; i++)
                section.Add(0.12);

            section.Add(20);

            Assert.That(section.GetPercentileMs(0.99), Is.EqualTo(0.15).Within(1e-9));
            Assert.That(section.GetPercentileMs(1), Is.EqualTo(20).Within(1e-9));
        }

        [Test]
        public void PercentileNeverPassesMax()
        {
            var section = new PerfSection("step");
            section.Add(0.01);

            Assert.That(section.GetPercentileMs(0.99), Is.EqualTo(0.01).Within(1e-9));
        }

        [Test]
        public void TimesPastTheHistogramUseMax()
        {
            var section = new PerfSection("step");
            section.Add(500);

            Assert.That(section.GetPercentileMs(0.5), Is.EqualTo(500).Within(1e-9));
        }

        [Test]
        public void ResetClearsEverySection()
        {
            var stats = new PerfStats(["a", "b"]);
            stats.Record(0, 3.0);
            stats.Record(1, 4.0);
            stats.Reset();

            Assert.That(stats.Sections[0].Count, Is.EqualTo(0));
            Assert.That(stats.Sections[1].MaxMs, Is.EqualTo(0));
            Assert.That(stats.Sections[1].GetPercentileMs(1), Is.EqualTo(0));
        }

        [Test]
        public void RecordFromTimestampReturnsNow()
        {
            var stats = new PerfStats(["a"]);
            var start = PerfStats.GetTimestamp();
            var end = stats.Record(0, start);

            Assert.That(end, Is.GreaterThanOrEqualTo(start));
            Assert.That(stats.Sections[0].Count, Is.EqualTo(1));
            Assert.That(stats.Sections[0].TotalMs, Is.EqualTo(PerfStats.GetElapsedMs(start, end)).Within(1e-9));
        }

        [Test]
        public void ServerSectionsAreNamedInLowerCase()
        {
            var names = ServerPerf.GetSectionNames();

            Assert.That(names, Has.Count.EqualTo(Enum.GetValues<ePerfSection>().Length));
            Assert.That(names[(int)ePerfSection.PowerUps], Is.EqualTo("powerups"));
        }

        [TestCase(50, 1000, 5)]
        [TestCase(2000, 1000, 200)]
        [TestCase(10, 0, 0)]
        public void LoadIsBusyShareOfWindow(double busyMs, double windowMs, double expected)
        {
            Assert.That(ServerPerf.GetLoadPercent(busyMs, windowMs), Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase(300, 5000, 60)]
        [TestCase(5, 0, 0)]
        public void RateIsPerSecond(double count, double windowMs, double expected)
        {
            Assert.That(ServerPerf.GetRate(count, windowMs), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void StepsSkipSectionsThatDidNotRunAndWrap()
        {
            var perf = new ServerPerf();

            // the loop's own sections aren't steps
            perf.Stats.Record((int)ePerfSection.Tick, 9.0);
            perf.Stats.Record((int)ePerfSection.Network, 9.0);

            for (var i = (int)ePerfSection.Respawns; i <= (int)ePerfSection.Snapshot; i++)
                perf.Stats.Record(i, 0.5);

            var lines = ServerPerf.FormatSteps(perf.Stats);
            var steps = (int)ePerfSection.Snapshot - (int)ePerfSection.Respawns + 1;

            Assert.That(lines, Has.Count.EqualTo((steps + ServerPerf.STEPS_PER_LINE - 1) / ServerPerf.STEPS_PER_LINE));
            Assert.That(lines[0], Does.StartWith("respawns 0.50/0.50"));
            Assert.That(string.Join(" ", lines), Does.Not.Contain("intermission").And.Not.Contain("tick").And.Not.Contain("network"));
        }

        [Test]
        public void ServerUpdateTimesItsSteps()
        {
            var settings = new ServerSettings { ServerName = "Perf", TargetFps = 30, AutoBalanceTeams = false, GameConfig = new GameConfiguration { MinPlayers = 2, MaxPlayers = 8 } };
            var server = new GameServer(settings, []);
            var connection = new FakeConnection(1);
            server.HandleEvent(new NetworkEvent(eNetworkEventType.Connected, connection));
            server.HandleMessage(connection, new HelloMessage { PlayerName = "Timer" });
            server.Perf.Stats.Reset();

            server.Update(16);

            Assert.That(server.Perf.Stats.Sections[(int)ePerfSection.Bots].Count, Is.EqualTo(1));
            Assert.That(server.Perf.Stats.Sections[(int)ePerfSection.Win].Count, Is.EqualTo(1));
            Assert.That(server.Perf.Stats.Sections[(int)ePerfSection.Intermission].Count, Is.EqualTo(0));
        }

        [Test]
        public void PerfCommandTurnsLoggingOnAndOff()
        {
            var server = new GameServer(new ServerSettings { ServerName = "Perf", GameConfig = new GameConfiguration() }, []);

            Assert.That(ServerCommands.Execute(server, "perf", out _), Is.EqualTo("Perf logging off"));
            Assert.That(ServerCommands.Execute(server, "perf on", out _), Is.EqualTo("Perf logging on"));
            Assert.That(server.Perf.IsLogging, Is.True);
            Assert.That(ServerCommands.Execute(server, "perf off", out _), Is.EqualTo("Perf logging off"));
            Assert.That(ServerCommands.Execute(server, "perf maybe", out _), Is.EqualTo("Usage: perf on|off"));
        }

        [Test]
        public void NoReportBeforeTheWindowEnds()
        {
            var server = new GameServer(new ServerSettings { ServerName = "Perf", GameConfig = new GameConfiguration() }, []);
            server.Perf.SetLogging(true);

            Assert.That(server.Perf.Update(server), Is.Null);
        }
    }
}
