using EatMeLibrary;

namespace EatMeTests
{
    public class DotRegenQueueTests
    {
        [Test]
        public void GetThreshold_IsPercentOfDots()
        {
            Assert.That(DotRegenQueue.GetThreshold(600, 50), Is.EqualTo(300));
            Assert.That(DotRegenQueue.GetThreshold(10, 25), Is.EqualTo(2));
        }

        [Test]
        public void GetThreshold_ClampsPercentAndDots()
        {
            Assert.That(DotRegenQueue.GetThreshold(600, -5), Is.Zero);
            Assert.That(DotRegenQueue.GetThreshold(600, 150), Is.EqualTo(600));
            Assert.That(DotRegenQueue.GetThreshold(-1, 50), Is.Zero);
        }

        [Test]
        public void Add_BelowThreshold_RestoresNothing()
        {
            var queue = new DotRegenQueue();
            queue.Reset(10, 30);

            for (var i = 0; i < 3; i++)
                Assert.That(queue.Add(i, 0, out _), Is.False);

            Assert.That(queue.Count, Is.EqualTo(3));
        }

        [Test]
        public void Add_OverThreshold_RestoresOldestFirst()
        {
            var queue = new DotRegenQueue();
            queue.Reset(10, 20);
            queue.Add(1, 1, out _);
            queue.Add(2, 2, out _);

            Assert.That(queue.Add(3, 3, out var first), Is.True);
            Assert.That((first.X, first.Y), Is.EqualTo(((ushort)1, (ushort)1)));

            Assert.That(queue.Add(4, 4, out var second), Is.True);
            Assert.That((second.X, second.Y), Is.EqualTo(((ushort)2, (ushort)2)));

            Assert.That(queue.Count, Is.EqualTo(2));
        }

        [Test]
        public void Add_ZeroPercent_RestoresAtOnce()
        {
            var queue = new DotRegenQueue();
            queue.Reset(10, 0);

            Assert.That(queue.Add(5, 6, out var restored), Is.True);
            Assert.That((restored.X, restored.Y), Is.EqualTo(((ushort)5, (ushort)6)));
            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void Add_HundredPercent_NeverRestores()
        {
            var queue = new DotRegenQueue();
            queue.Reset(4, 100);

            for (var i = 0; i < 4; i++)
                Assert.That(queue.Add(i, 0, out _), Is.False);
        }

        [Test]
        public void Reset_EmptiesQueue()
        {
            var queue = new DotRegenQueue();
            queue.Reset(10, 50);
            queue.Add(1, 1, out _);

            queue.Reset(20, 10);

            Assert.That(queue.Count, Is.Zero);
            Assert.That(queue.Threshold, Is.EqualTo(2));
        }

        [Test]
        public void Enumerates_OldestFirst()
        {
            var queue = new DotRegenQueue();
            queue.Reset(10, 100);
            queue.Add(3, 0, out _);
            queue.Add(1, 0, out _);
            queue.Add(2, 0, out _);

            var xs = new List<int>();
            foreach (var cell in queue)
                xs.Add(cell.X);

            Assert.That(xs, Is.EqualTo(new[] { 3, 1, 2 }));
        }
    }
}
