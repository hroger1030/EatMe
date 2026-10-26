namespace EatMeLibrary
{
    /// <summary>
    /// Finds the part of a sprite's cell that is actually drawn, so a sprite can be placed by what shows rather than by
    /// its cell's transparent margin.
    /// </summary>
    public static class SpriteBounds
    {
        /// <summary>
        /// The smallest box inside the cell holding every pixel that isn't fully transparent, in texture pixels.
        /// Pixels are packed 0xAABBGGRR (MonoGame's Color layout), row by row, textureWidth to a row. A cell with
        /// nothing in it gives the whole cell back.
        /// </summary>
        public static CellBox GetOpaqueBounds(uint[] pixels, int textureWidth, CellBox cell)
        {
            var left = int.MaxValue;
            var top = int.MaxValue;
            var right = int.MinValue;
            var bottom = int.MinValue;

            for (var y = cell.Y; y < cell.Y + cell.Height; y++)
            {
                for (var x = cell.X; x < cell.X + cell.Width; x++)
                {
                    if (pixels[y * textureWidth + x] >> 24 == 0)
                        continue;

                    if (x < left)
                        left = x;
                    if (x > right)
                        right = x;
                    if (y < top)
                        top = y;
                    if (y > bottom)
                        bottom = y;
                }
            }

            if (right < left)
                return cell;

            return new CellBox(left, top, right - left + 1, bottom - top + 1);
        }
    }
}
