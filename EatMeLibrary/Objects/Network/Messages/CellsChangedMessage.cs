using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: map cells whose contents changed, such as a dot eaten or regrown.
    /// </summary>
    public class CellsChangedMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.CellsChanged;

        public List<CellChange> Cells { get; set; } = [];

        /// <summary>
        /// True for the cells sent to a player joining part way through a level (GameServer.BuildEatenCells): they
        /// changed before the player was there, so the client shows no wall debris for them.
        /// </summary>
        public bool IsCatchUp { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(IsCatchUp);
            writer.Write(Cells.Count);
            foreach (var cell in Cells)
                cell.Write(writer);
        }

        public void Read(BinaryReader reader)
        {
            IsCatchUp = reader.ReadBoolean();
            var count = NetworkHelper.ReadCount(reader);
            Cells = [];
            for (var i = 0; i < count; i++)
                Cells.Add(CellChange.Read(reader));
        }
    }
}
