using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One ghost's state inside a Snapshot.
    /// </summary>
    public struct GhostState
    {
        public int GhostId { get; }
        public float X { get; }
        public float Y { get; }
        public eFacing Facing { get; }
        public bool Alive { get; }
        /// <summary>
        /// Whether the ghost is stunned (can't move); the client spins it in place.
        /// </summary>
        public bool Stunned { get; }

        public GhostState(int ghostId, float x, float y, eFacing facing, bool alive, bool stunned = false)
        {
            GhostId = ghostId;
            X = x;
            Y = y;
            Facing = facing;
            Alive = alive;
            Stunned = stunned;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(GhostId);
            writer.Write(X);
            writer.Write(Y);
            writer.Write((byte)Facing);
            writer.Write(Alive);
            writer.Write(Stunned);
        }

        public static GhostState Read(BinaryReader reader)
        {
            return new GhostState(
                reader.ReadInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                (eFacing)reader.ReadByte(),
                reader.ReadBoolean(),
                reader.ReadBoolean());
        }
    }
}
