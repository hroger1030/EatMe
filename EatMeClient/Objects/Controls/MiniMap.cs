using System;
using System.Collections.Generic;
using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// A square overview of the whole map: a grey arch on every warp door, a ring in its team's color on every flag base,
    /// a dot in its team's color for every living pac (a star for the player's own; flickering like the flag while
    /// carrying one), a purple X for every power up and a blinking triangle for every flag not being carried, inside a
    /// yellow border with rounded corners. The maze itself isn't drawn.
    /// </summary>
    public class MiniMap
    {
        public const int BORDER_PIXELS = 2;
        public const int CORNER_RADIUS = 12;
        private const int PLAYER_DOT_PIXELS = 10;
        private const int OWN_STAR_PIXELS = 15;
        private const int FLAG_TRIANGLE_PIXELS = 10;
        private const int POWER_UP_CROSS_PIXELS = 10;
        private const float POWER_UP_CROSS_THICKNESS = 2f;
        private const int WARP_ARCH_PIXELS = 10;
        private const float WARP_ARCH_THICKNESS = 2f;
        private const int BASE_RING_PIXELS = 16;
        private const float BASE_RING_THICKNESS = 2f;

        private Texture2D _pixel;
        private Texture2D _ring;
        private Texture2D _dot;
        private Texture2D _ownStar;
        private Texture2D _triangle;
        private Texture2D _cross;
        private Texture2D _arch;
        private eGridDef[,] _basesMap;
        private List<GameFlag> _bases = [];
        private List<FastG.Point2> _warpCells = [];

        /// <summary>
        /// Draws the mini-map filling bounds (border included). Warp arches and base rings go under everything, then pacs,
        /// power ups and flags.
        /// Carried flags aren't drawn.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, SnapshotMessage snapshot, eGridDef[,] map, int ownPlayerId, float animationTime)
        {
            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            if (_ring == null)
                _ring = CreateRing(spriteBatch.GraphicsDevice, BASE_RING_PIXELS, BASE_RING_THICKNESS);

            // a ring as thick as its radius is a filled circle
            if (_dot == null)
                _dot = CreateRing(spriteBatch.GraphicsDevice, PLAYER_DOT_PIXELS, PLAYER_DOT_PIXELS * 0.5f);

            if (_ownStar == null)
                _ownStar = CreateShape(spriteBatch.GraphicsDevice, OWN_STAR_PIXELS, (x, y) => MiniMapMath.IsOnStar(x, y, OWN_STAR_PIXELS));

            if (_triangle == null)
                _triangle = CreateShape(spriteBatch.GraphicsDevice, FLAG_TRIANGLE_PIXELS, (x, y) => MiniMapMath.IsOnTriangle(x, y, FLAG_TRIANGLE_PIXELS));

            if (_cross == null)
                _cross = CreateShape(spriteBatch.GraphicsDevice, POWER_UP_CROSS_PIXELS, (x, y) => MiniMapMath.IsOnCross(x, y, POWER_UP_CROSS_PIXELS, POWER_UP_CROSS_THICKNESS));

            if (_arch == null)
                _arch = CreateShape(spriteBatch.GraphicsDevice, WARP_ARCH_PIXELS, (x, y) => MiniMapMath.IsOnArch(x, y, WARP_ARCH_PIXELS, WARP_ARCH_THICKNESS));

            // border, then the panel inside it
            TextureHelper.FillRounded(spriteBatch, _pixel, bounds, CORNER_RADIUS, ClientColors.COLOR_PAC_YELLOW);

            var inner = new Rectangle(bounds.X + BORDER_PIXELS, bounds.Y + BORDER_PIXELS, bounds.Width - BORDER_PIXELS * 2, bounds.Height - BORDER_PIXELS * 2);
            TextureHelper.FillRounded(spriteBatch, _pixel, inner, CORNER_RADIUS - BORDER_PIXELS, Color.Black);

            if (snapshot == null || map == null || map.GetLength(0) <= 0)
                return;

            var mapSize = map.GetLength(0);

            // a new map instance means a new level, so find its bases and warp doors again
            if (map != _basesMap)
            {
                _bases = MiniMapMath.GetFlagBases(map);
                _warpCells = MiniMapMath.GetWarpCells(map);
                _basesMap = map;
            }

            foreach (var warp in _warpCells)
                DrawCentred(spriteBatch, _arch, inner, mapSize, warp.X, warp.Y, WARP_ARCH_PIXELS, ClientColors.COLOR_MINI_MAP_WARP);

            foreach (var flagBase in _bases)
                DrawCentred(spriteBatch, _ring, inner, mapSize, flagBase.BaseX, flagBase.BaseY, BASE_RING_PIXELS, ClientColors.GetTeamColor(flagBase.Team));

            foreach (var pac in snapshot.Pacs)
            {
                if (!pac.Alive)
                    continue;

                // the player's own pac is a bigger star, so it's easy to find
                var isOwn = pac.PlayerId == ownPlayerId;
                var dot = isOwn ? _ownStar : _dot;
                var dotPixels = isOwn ? OWN_STAR_PIXELS : PLAYER_DOT_PIXELS;
                DrawCentred(spriteBatch, dot, inner, mapSize, pac.X, pac.Y, dotPixels, GetPacColor(pac, snapshot.Flags, isOwn, animationTime));
            }

            foreach (var powerUp in snapshot.PowerUps)
                DrawCentred(spriteBatch, _cross, inner, mapSize, powerUp.X, powerUp.Y, POWER_UP_CROSS_PIXELS, ClientColors.COLOR_MINI_MAP_POWER_UP);

            // a carried flag isn't drawn; its carrier's dot shows where it is
            foreach (var flag in snapshot.Flags)
            {
                if (flag.IsCarried)
                    continue;

                // a flag sealed in a force field (triforce) blinks gold
                var color = flag.IsSealed && ForceField.IsMiniMapGold(animationTime) ? ClientColors.COLOR_FORCE_FIELD : ClientColors.GetFlagColor(flag.Team, animationTime);
                DrawCentred(spriteBatch, _triangle, inner, mapSize, flag.X, flag.Y, FLAG_TRIANGLE_PIXELS, color);
            }
        }

        /// <summary>
        /// A pac's color (the lighter shade for the player's own pac), or, while it carries a flag, that flag's flicker so
        /// the carried flag can be seen.
        /// </summary>
        private static Color GetPacColor(PacState pac, List<FlagState> flags, bool isOwn, float animationTime)
        {
            foreach (var flag in flags)
            {
                if (flag.CarrierId == pac.PlayerId)
                    return ClientColors.GetFlagColor(flag.Team, animationTime);
            }

            return ClientColors.GetPacColor(pac.Team, isOwn);
        }

        /// <summary>
        /// A square texture drawn size pixels wide, centred on the map position and kept inside the panel.
        /// </summary>
        private static void DrawCentred(SpriteBatch spriteBatch, Texture2D texture, Rectangle inner, int mapSize, float cellX, float cellY, int size, Color color)
        {
            var centreX = inner.X + MiniMapMath.CellToMiniMap(cellX, mapSize, inner.Width);
            var centreY = inner.Y + MiniMapMath.CellToMiniMap(cellY, mapSize, inner.Height);
            var x = MathHelper.Clamp((int)(centreX - size * 0.5f), inner.Left, inner.Right - size);
            var y = MathHelper.Clamp((int)(centreY - size * 0.5f), inner.Top, inner.Bottom - size);

            spriteBatch.Draw(texture, new Rectangle(x, y, size, size), color);
        }

        /// <summary>
        /// A white ring diameter pixels across and thickness pixels wide on a transparent background, tinted when drawn.
        /// </summary>
        private static Texture2D CreateRing(GraphicsDevice graphicsDevice, int diameter, float thickness)
        {
            var data = new Color[diameter * diameter];

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                    data[y * diameter + x] = MiniMapMath.IsOnRing(x, y, diameter, thickness) ? Color.White : Color.Transparent;
            }

            var ring = new Texture2D(graphicsDevice, diameter, diameter);
            ring.SetData(data);

            return ring;
        }

        /// <summary>
        /// A white shape size pixels across on a transparent background, tinted when drawn; isOn says which pixels are set.
        /// </summary>
        private static Texture2D CreateShape(GraphicsDevice graphicsDevice, int size, Func<int, int, bool> isOn)
        {
            var data = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                    data[y * size + x] = isOn(x, y) ? Color.White : Color.Transparent;
            }

            var shape = new Texture2D(graphicsDevice, size, size);
            shape.SetData(data);

            return shape;
        }
    }
}
