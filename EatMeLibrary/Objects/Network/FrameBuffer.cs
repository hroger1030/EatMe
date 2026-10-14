using System;
using System.Buffers.Binary;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Collects bytes from a stream connection and hands back whole messages.
    /// TCP can split or join frames arbitrarily, so bytes are held until a full frame has arrived.
    /// </summary>
    public class FrameBuffer
    {
        private const int INITIAL_CAPACITY = 4096;

        private byte[] _Buffer = new byte[INITIAL_CAPACITY];
        private int _Count;

        /// <summary>
        /// Bytes received but not yet returned as a message.
        /// </summary>
        public int BufferedBytes => _Count;

        public void Append(byte[] data, int offset, int count)
        {
            if (_Count + count > _Buffer.Length)
                Array.Resize(ref _Buffer, Math.Max(_Buffer.Length * 2, _Count + count));

            Buffer.BlockCopy(data, offset, _Buffer, _Count, count);
            _Count += count;
        }

        /// <summary>
        /// Returns the next whole message, or false if more bytes are needed.
        /// Throws InvalidDataException if the stream is corrupt; the connection should then be dropped.
        /// </summary>
        public bool TryReadMessage(out INetworkMessage message)
        {
            message = null;

            if (_Count < NetworkConstants.LENGTH_PREFIX_SIZE)
                return false;

            var bodyLength = BinaryPrimitives.ReadInt32LittleEndian(_Buffer.AsSpan(0, NetworkConstants.LENGTH_PREFIX_SIZE));

            if (bodyLength < 1 || bodyLength > NetworkConstants.MAX_MESSAGE_SIZE)
                throw new InvalidDataException($"Invalid message length: {bodyLength}");

            var frameLength = NetworkConstants.LENGTH_PREFIX_SIZE + bodyLength;

            if (_Count < frameLength)
                return false;

            message = MessageSerializer.Deserialize(_Buffer, NetworkConstants.LENGTH_PREFIX_SIZE, bodyLength);

            // shift any following bytes to the front
            _Count -= frameLength;
            Buffer.BlockCopy(_Buffer, frameLength, _Buffer, 0, _Count);

            return true;
        }
    }
}
