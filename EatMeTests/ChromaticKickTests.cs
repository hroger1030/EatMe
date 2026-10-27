using EatMeLibrary;

namespace EatMeTests
{
    public class ChromaticKickTests
    {
        private static readonly TimeSpan DURATION = TimeSpan.FromMilliseconds(ChromaticKick.BLAST_DURATION_MS);

        [Test]
        public void GetPixels_StartsFullAndClosesToNothing()
        {
            Assert.That(ChromaticKick.GetPixels(TimeSpan.Zero, 6f, DURATION), Is.EqualTo(6f));
            Assert.That(ChromaticKick.GetPixels(DURATION / 2, 6f, DURATION), Is.EqualTo(3f).Within(0.001f));
            Assert.That(ChromaticKick.GetPixels(DURATION, 6f, DURATION), Is.EqualTo(0f));
            Assert.That(ChromaticKick.GetPixels(TimeSpan.FromMilliseconds(-1), 6f, DURATION), Is.EqualTo(0f));
        }

        [Test]
        public void Kick_RunsOutAfterItsDuration()
        {
            var kick = new ChromaticKick();
            kick.Start(ChromaticKick.BLAST_PIXELS, DURATION);
            Assert.That(kick.GetPixels(), Is.EqualTo(ChromaticKick.BLAST_PIXELS));

            kick.Update(DURATION);
            Assert.That(kick.IsKicking, Is.False);
            Assert.That(kick.GetPixels(), Is.EqualTo(0f));
        }

        [Test]
        public void Start_ASmallerKickDoesNotCutABigOneShort()
        {
            var kick = new ChromaticKick();
            kick.Start(10f, DURATION);
            kick.Start(2f, DURATION);
            Assert.That(kick.GetPixels(), Is.EqualTo(10f));

            kick.Start(12f, DURATION);
            Assert.That(kick.GetPixels(), Is.EqualTo(12f));
        }

        [Test]
        public void Stop_EndsTheKick()
        {
            var kick = new ChromaticKick();
            kick.Start(10f, DURATION);
            kick.Stop();
            Assert.That(kick.GetPixels(), Is.EqualTo(0f));
        }
    }
}
