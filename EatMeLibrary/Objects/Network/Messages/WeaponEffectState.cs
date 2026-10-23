using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One weapon effect (WeaponEffect) inside a Snapshot.
    /// </summary>
    public struct WeaponEffectState
    {
        public int Id { get; }
        public eWeaponEffect Kind { get; }
        public float X { get; }
        public float Y { get; }
        public eFacing Facing { get; }

        /// <summary>
        /// Id of the pac that made it; the client draws a decoy as a copy of this pac.
        /// </summary>
        public int OwnerId { get; }

        public WeaponEffectState(int id, eWeaponEffect kind, float x, float y, eFacing facing, int ownerId)
        {
            Id = id;
            Kind = kind;
            X = x;
            Y = y;
            Facing = facing;
            OwnerId = ownerId;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write((byte)Kind);
            writer.Write(X);
            writer.Write(Y);
            writer.Write((byte)Facing);
            writer.Write(OwnerId);
        }

        public static WeaponEffectState Read(BinaryReader reader)
        {
            return new WeaponEffectState(reader.ReadInt32(), (eWeaponEffect)reader.ReadByte(), reader.ReadSingle(), reader.ReadSingle(), (eFacing)reader.ReadByte(), reader.ReadInt32());
        }
    }
}
