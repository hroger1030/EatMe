using EatMeLibrary;

namespace EatMeTests
{
    public class FrameBufferTests
    {
        private static byte[] MakeFrame(int playerId)
        {
            return MessageSerializer.Serialize(new PlayerLeftMessage { PlayerId = playerId });
        }

        [Test]
        public void TryReadMessage_WholeFrame_ReturnsMessage()
        {
            var buffer = new FrameBuffer();
            var frame = MakeFrame(42);
            buffer.Append(frame, 0, frame.Length);

            Assert.That(buffer.TryReadMessage(out var message), Is.True);
            Assert.That(((PlayerLeftMessage)message).PlayerId, Is.EqualTo(42));
            Assert.That(buffer.BufferedBytes, Is.Zero);
            Assert.That(buffer.TryReadMessage(out _), Is.False);
        }

        [Test]
        public void TryReadMessage_EmptyBuffer_ReturnsFalse()
        {
            Assert.That(new FrameBuffer().TryReadMessage(out var message), Is.False);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void TryReadMessage_OneByteAtATime_WaitsForWholeFrame()
        {
            var buffer = new FrameBuffer();
            var frame = MakeFrame(7);

            for (var i = 0; i < frame.Length - 1; i++)
            {
                buffer.Append(frame, i, 1);
                Assert.That(buffer.TryReadMessage(out _), Is.False, $"after {i + 1} bytes");
            }

            buffer.Append(frame, frame.Length - 1, 1);

            Assert.That(buffer.TryReadMessage(out var message), Is.True);
            Assert.That(((PlayerLeftMessage)message).PlayerId, Is.EqualTo(7));
        }

        [Test]
        public void TryReadMessage_SeveralFramesInOneAppend_ReturnsEachInOrder()
        {
            var buffer = new FrameBuffer();
            byte[] data = [.. MakeFrame(1), .. MakeFrame(2), .. MakeFrame(3)];
            buffer.Append(data, 0, data.Length);

            for (var expectedId = 1; expectedId <= 3; expectedId++)
            {
                Assert.That(buffer.TryReadMessage(out var message), Is.True);
                Assert.That(((PlayerLeftMessage)message).PlayerId, Is.EqualTo(expectedId));
            }

            Assert.That(buffer.TryReadMessage(out _), Is.False);
        }

        [Test]
        public void TryReadMessage_FrameSplitAcrossAppends_KeepsLeftover()
        {
            var buffer = new FrameBuffer();
            byte[] data = [.. MakeFrame(1), .. MakeFrame(2)];
            var split = data.Length - 3;

            buffer.Append(data, 0, split);
            Assert.That(buffer.TryReadMessage(out _), Is.True);
            Assert.That(buffer.TryReadMessage(out _), Is.False);

            buffer.Append(data, split, data.Length - split);
            Assert.That(buffer.TryReadMessage(out var message), Is.True);
            Assert.That(((PlayerLeftMessage)message).PlayerId, Is.EqualTo(2));
        }

        [Test]
        public void Append_LargerThanInitialCapacity_Grows()
        {
            var cells = Enumerable.Range(0, 2000).Select(i => new CellChange((ushort)i, 0, eGridDef.Food)).ToList();
            var frame = MessageSerializer.Serialize(new CellsChangedMessage { Cells = cells });
            var buffer = new FrameBuffer();
            buffer.Append(frame, 0, frame.Length);

            Assert.That(buffer.TryReadMessage(out var message), Is.True);
            Assert.That(((CellsChangedMessage)message).Cells, Is.EqualTo(cells));
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(NetworkConstants.MAX_MESSAGE_SIZE + 1)]
        public void TryReadMessage_InvalidLength_Throws(int length)
        {
            var buffer = new FrameBuffer();
            var prefix = BitConverter.GetBytes(length);
            buffer.Append(prefix, 0, prefix.Length);

            Assert.Throws<InvalidDataException>(() => buffer.TryReadMessage(out _));
        }
    }
}
