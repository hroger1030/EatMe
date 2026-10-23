using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One flag's state inside a Snapshot. While carried, X and Y follow the carrier.
    /// </summary>
    public struct FlagState
    {
        public const int NO_CARRIER = -1;

        public int FlagId { get; }
        public eTeam Team { get; }
        public float X { get; }
        public float Y { get; }
        public int CarrierId { get; }

        public bool IsCarried => CarrierId != NO_CARRIER;

        /// <summary>
        /// Whether the flag is sealed in a Force Field (triangle force): enemies can't pick it up, and the client draws the field.
        /// </summary>
        public bool IsSealed { get; }

        public FlagState(int flagId, eTeam team, float x, float y, int carrierId, bool isSealed = false)
        {
            FlagId = flagId;
            Team = team;
            X = x;
            Y = y;
            CarrierId = carrierId;
            IsSealed = isSealed;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(FlagId);
            writer.Write((byte)Team);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(CarrierId);
            writer.Write(IsSealed);
        }

        public static FlagState Read(BinaryReader reader)
        {
            return new FlagState(
                reader.ReadInt32(),
                (eTeam)reader.ReadByte(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadInt32(),
                reader.ReadBoolean());
        }
    }
}
