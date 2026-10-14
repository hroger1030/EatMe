using EatMeLibrary;

namespace EatMeServer
{
    /// <summary>
    /// A power pill that was eaten, waiting to grow back in its cell. Dots use DotRegenQueue instead.
    /// </summary>
    public class CellRegen
    {
        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// What grows back: eGridDef.PowerPill.
        /// </summary>
        public eGridDef Item { get; }

        /// <summary>
        /// ms left before it grows back.
        /// </summary>
        public int MsLeft { get; set; }

        public CellRegen(int x, int y, eGridDef item, int msLeft)
        {
            X = x;
            Y = y;
            Item = item;
            MsLeft = msLeft;
        }
    }
}
