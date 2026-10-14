using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// New contents of one map cell inside a CellsChanged message.
    /// </summary>
    public struct CellChange
    {
        public ushort X { get; }
        public ushort Y { get; }
        public eGridDef Cell { get; }

        public CellChange(ushort x, ushort y, eGridDef cell)
        {
            X = x;
            Y = y;
            Cell = cell;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write((int)Cell);
        }

        public static CellChange Read(BinaryReader reader)
        {
            return new CellChange(reader.ReadUInt16(), reader.ReadUInt16(), (eGridDef)reader.ReadInt32());
        }
    }
}
