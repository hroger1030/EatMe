using EatMeLibrary;

namespace EatMeTests
{
    public class AnnouncementQueueTests
    {
        [Test]
        public void TryDequeue_Empty_False()
        {
            var queue = new AnnouncementQueue();

            Assert.That(queue.TryDequeue(0, out var name), Is.False);
            Assert.That(name, Is.Null);
        }

        [Test]
        public void TryDequeue_OldestFirst()
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(Const.SFX_MSG_RED_FLAG_DROPPED, 0);
            queue.Enqueue(Const.SFX_MSG_RED_FLAG_RETURNED, 10);

            Assert.That(queue.TryDequeue(20, out var first), Is.True);
            Assert.That(first, Is.EqualTo(Const.SFX_MSG_RED_FLAG_DROPPED));
            Assert.That(queue.TryDequeue(20, out var second), Is.True);
            Assert.That(second, Is.EqualTo(Const.SFX_MSG_RED_FLAG_RETURNED));
            Assert.That(queue.TryDequeue(20, out _), Is.False);
        }

        [Test]
        public void Enqueue_SameClipWaiting_NotAddedAgain()
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(Const.SFX_MSG_BLUE_FLAG_DROPPED, 0);
            queue.Enqueue(Const.SFX_MSG_BLUE_FLAG_DROPPED, 10);

            Assert.That(queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void Enqueue_SameClipAfterItPlayed_Added()
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(Const.SFX_MSG_BLUE_FLAG_DROPPED, 0);
            queue.TryDequeue(0, out _);
            queue.Enqueue(Const.SFX_MSG_BLUE_FLAG_DROPPED, 10);

            Assert.That(queue.Count, Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Enqueue_NoName_Ignored(string name)
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(name, 0);

            Assert.That(queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryDequeue_StaleClipsDropped()
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(Const.SFX_MSG_RED_FLAG_DROPPED, 0);
            queue.Enqueue(Const.SFX_MSG_RED_FLAG_RETURNED, 1000);

            Assert.That(queue.TryDequeue(1000 + AnnouncementQueue.MAX_WAIT_MS, out var name), Is.True);
            Assert.That(name, Is.EqualTo(Const.SFX_MSG_RED_FLAG_RETURNED));
            Assert.That(queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryDequeue_AllStale_False()
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(Const.SFX_MSG_RED_FLAG_DROPPED, 0);

            Assert.That(queue.TryDequeue(AnnouncementQueue.MAX_WAIT_MS + 1, out _), Is.False);
            Assert.That(queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_Empties()
        {
            var queue = new AnnouncementQueue();
            queue.Enqueue(Const.SFX_MSG_RED_FLAG_DROPPED, 0);
            queue.Enqueue(Const.SFX_MSG_BLUE_ABOUT_TO_WIN, 0);
            queue.Clear();

            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.TryDequeue(0, out _), Is.False);
        }
    }
}
