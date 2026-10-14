using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// One power up's state inside a Snapshot.
    /// </summary>
    public struct PowerUpState
    {
        public int PowerUpId { get; }
        public ePowerUp Item { get; }
        public float X { get; }
        public float Y { get; }

        public PowerUpState(int powerUpId, ePowerUp item, float x, float y)
        {
            PowerUpId = powerUpId;
            Item = item;
            X = x;
            Y = y;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PowerUpId);
            writer.Write((long)Item);
            writer.Write(X);
            writer.Write(Y);
        }

        public static PowerUpState Read(BinaryReader reader)
        {
            return new PowerUpState(
                reader.ReadInt32(),
                (ePowerUp)reader.ReadInt64(),
                reader.ReadSingle(),
                reader.ReadSingle());
        }
    }
}
