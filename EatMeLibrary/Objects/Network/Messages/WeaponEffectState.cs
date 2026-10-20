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

        public WeaponEffectState(int id, eWeaponEffect kind, float x, float y, eFacing facing)
        {
            Id = id;
            Kind = kind;
            X = x;
            Y = y;
            Facing = facing;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write((byte)Kind);
            writer.Write(X);
            writer.Write(Y);
            writer.Write((byte)Facing);
        }

        public static WeaponEffectState Read(BinaryReader reader)
        {
            return new WeaponEffectState(reader.ReadInt32(), (eWeaponEffect)reader.ReadByte(), reader.ReadSingle(), reader.ReadSingle(), (eFacing)reader.ReadByte());
        }
    }
}
