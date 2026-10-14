using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: something happened that needs a sound or animation.
    /// </summary>
    public class GameEventMessage : INetworkMessage
    {
        public const int NO_ID = -1;

        public eMessageType MessageType => eMessageType.GameEvent;

        public eGameEvent EventType { get; set; }

        /// <summary>
        /// Player or ghost that caused the event, or NO_ID.
        /// </summary>
        public int ActorId { get; set; } = NO_ID;

        /// <summary>
        /// Player or ghost the event happened to, or NO_ID.
        /// </summary>
        public int TargetId { get; set; } = NO_ID;
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// For flag events the team whose flag it is; for AboutToWin the team about to win. Unused otherwise.
        /// </summary>
        public eTeam Team { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)EventType);
            writer.Write(ActorId);
            writer.Write(TargetId);
            writer.Write(X);
            writer.Write(Y);
            writer.Write((byte)Team);
        }

        public void Read(BinaryReader reader)
        {
            EventType = (eGameEvent)reader.ReadByte();
            ActorId = reader.ReadInt32();
            TargetId = reader.ReadInt32();
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Team = (eTeam)reader.ReadByte();
        }
    }
}
