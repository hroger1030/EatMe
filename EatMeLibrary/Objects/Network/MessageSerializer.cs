using System;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Turns messages into bytes and back.
    /// A frame on the wire is [int32 body length][body], and a body is [message type byte][message fields].
    /// </summary>
    public static class MessageSerializer
    {
        /// <summary>
        /// Returns the complete frame for a message, length prefix included, ready to send.
        /// </summary>
        public static byte[] Serialize(INetworkMessage message)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // placeholder for the length, filled in once the body is written
            writer.Write(0);
            writer.Write((byte)message.MessageType);
            message.Write(writer);
            writer.Flush();

            var bodyLength = (int)stream.Length - NetworkConstants.LENGTH_PREFIX_SIZE;

            if (bodyLength > NetworkConstants.MAX_MESSAGE_SIZE)
                throw new InvalidOperationException($"{message.MessageType} message is {bodyLength} bytes, over the {NetworkConstants.MAX_MESSAGE_SIZE} byte limit");

            stream.Position = 0;
            writer.Write(bodyLength);
            writer.Flush();

            return stream.ToArray();
        }

        /// <summary>
        /// Rebuilds a message from a frame body (no length prefix). Throws InvalidDataException if the body is malformed.
        /// </summary>
        public static INetworkMessage Deserialize(byte[] buffer, int offset, int count)
        {
            if (count < 1)
                throw new InvalidDataException("Empty message body");

            using var stream = new MemoryStream(buffer, offset, count, writable: false);
            using var reader = new BinaryReader(stream);

            var message = CreateMessage((eMessageType)reader.ReadByte());

            try
            {
                message.Read(reader);
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException($"{message.MessageType} message is truncated", ex);
            }

            if (stream.Position != stream.Length)
                throw new InvalidDataException($"{message.MessageType} message has {stream.Length - stream.Position} extra bytes");

            return message;
        }

        public static INetworkMessage Deserialize(byte[] body)
        {
            return Deserialize(body, 0, body.Length);
        }

        /// <summary>
        /// Returns an empty message of the given type, ready to Read into.
        /// </summary>
        public static INetworkMessage CreateMessage(eMessageType messageType)
        {
            return messageType switch
            {
                eMessageType.Hello => new HelloMessage(),
                eMessageType.Input => new InputMessage(),
                eMessageType.Ping => new PingMessage(),
                eMessageType.Leave => new LeaveMessage(),
                eMessageType.ServerPong => new ServerPongMessage(),
                eMessageType.Welcome => new WelcomeMessage(),
                eMessageType.Reject => new RejectMessage(),
                eMessageType.LevelStart => new LevelStartMessage(),
                eMessageType.Snapshot => new SnapshotMessage(),
                eMessageType.CellsChanged => new CellsChangedMessage(),
                eMessageType.PlayerJoined => new PlayerJoinedMessage(),
                eMessageType.PlayerLeft => new PlayerLeftMessage(),
                eMessageType.GameEvent => new GameEventMessage(),
                eMessageType.LevelWon => new LevelWonMessage(),
                eMessageType.Pong => new PongMessage(),
                eMessageType.HighScores => new HighScoresMessage(),
                eMessageType.ServerPing => new ServerPingMessage(),
                eMessageType.DiscoveryRequest => new DiscoveryRequestMessage(),
                eMessageType.DiscoveryResponse => new DiscoveryResponseMessage(),
                _ => throw new InvalidDataException($"Unknown message type: {(byte)messageType}"),
            };
        }
    }
}
