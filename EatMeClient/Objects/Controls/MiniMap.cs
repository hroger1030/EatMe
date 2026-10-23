using System;
using System.Collections.Generic;
using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// A square overview of the whole map: an outlined box in its team's color around every spawn room and a yellow one
    /// around the ghost generator, a grey arch on every warp door, a ring in its team's color on every flag base, a dot in
    /// its team's color for every living pac (a Pac-Man facing its way for the player's own; flickering like the flag
    /// while carrying one), a white ghost for every living ghost, a purple cherry for every power up and a blinking flag for every flag not being carried, inside a
    /// yellow border with rounded corners. The maze itself isn't drawn.
    /// </summary>
    public class MiniMap
    {
        public const int BORDER_PIXELS = 2;
        public const int CORNER_RADIUS = 12;
        private const int SPAWN_BOX_THICKNESS = 2;
        private const int SPAWN_BOX_MIN_PIXELS = 16;
        private const float SPAWN_BOX_SCALE = 2f;
        private const int SPAWN_BOX_CORNER_RADIUS = 5;

        private Texture2D _pixel;
        private Texture2D _icons;
        private eGridDef[,] _basesMap;
        private List<GameFlag> _bases = [];
        private List<FastG.Point2> _warpCells = [];
        private List<CellBox> _redSpawns = [];
        private List<CellBox> _blueSpawns = [];
        private List<CellBox> _ghostSpawns = [];

        /// <summary>
        /// Draws the mini-map filling bounds (border included). Spawn boxes, warp arches and base rings go under everything, then power
        /// ups, ghosts, pacs and flags.
        /// Carried flags aren't drawn.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, SnapshotMessage snapshot, eGridDef[,] map, int ownPlayerId, TimeSpan animationTime)
        {
            if (_pixel == null)
                _pixel = TextureHelper.GetPixel(spriteBatch.GraphicsDevice);

            if (_icons == null)
                _icons = AssetCache.GetTexture(Const.IMAGE_MINI_MAP_ICONS);

            // border, then the panel inside it
            TextureHelper.FillRounded(spriteBatch, _pixel, bounds, CORNER_RADIUS, ClientColors.COLOR_PAC_YELLOW);

            var inner = new Rectangle(bounds.X + BORDER_PIXELS, bounds.Y + BORDER_PIXELS, bounds.Width - BORDER_PIXELS * 2, bounds.Height - BORDER_PIXELS * 2);
            TextureHelper.FillRounded(spriteBatch, _pixel, inner, CORNER_RADIUS - BORDER_PIXELS, Color.Black);

            if (snapshot == null || map == null || map.GetLength(0) <= 0)
                return;

            var mapSize = map.GetLength(0);

            // a new map instance means a new level, so find its bases, spawns and warp doors again
            if (map != _basesMap)
            {
                _bases = MiniMapMath.GetFlagBases(map);
                _warpCells = MiniMapMath.GetWarpCells(map);
                _redSpawns = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointRed);
                _blueSpawns = MiniMapMath.GetCellBoxes(map, eGridDef.SpawnPointBlue);
                _ghostSpawns = MiniMapMath.GetCellBoxes(map, eGridDef.GhostGenerator);
                _basesMap = map;
            }

            foreach (var box in _redSpawns)
                DrawBox(spriteBatch, _pixel, inner, mapSize, box, ClientColors.GetTeamColor(eTeam.Red));

            foreach (var box in _blueSpawns)
                DrawBox(spriteBatch, _pixel, inner, mapSize, box, ClientColors.GetTeamColor(eTeam.Blue));

            foreach (var box in _ghostSpawns)
                DrawBox(spriteBatch, _pixel, inner, mapSize, box, ClientColors.COLOR_PAC_YELLOW);

            foreach (var warp in _warpCells)
                DrawIcon(spriteBatch, _icons, eMiniMapIcon.WarpArch, inner, mapSize, warp.X, warp.Y, ClientColors.COLOR_MINI_MAP_WARP);

            foreach (var flagBase in _bases)
                DrawIcon(spriteBatch, _icons, eMiniMapIcon.FlagBase, inner, mapSize, flagBase.BaseX, flagBase.BaseY, ClientColors.GetTeamColor(flagBase.Team));

            foreach (var powerUp in snapshot.PowerUps)
                DrawIcon(spriteBatch, _icons, eMiniMapIcon.PowerUp, inner, mapSize, powerUp.X, powerUp.Y, ClientColors.COLOR_MINI_MAP_POWER_UP);

            // under the pacs, so a pac on a ghost still shows; dead and captured ghosts aren't alive
            foreach (var ghost in snapshot.Ghosts)
            {
                if (ghost.Alive)
                    DrawIcon(spriteBatch, _icons, eMiniMapIcon.Ghost, inner, mapSize, ghost.X, ghost.Y, ClientColors.COLOR_MINI_MAP_GHOST);
            }

            DrawEnemyDecoys(spriteBatch, inner, mapSize, snapshot, ownPlayerId, animationTime);

            // a pac carrying a flag is drawn as that flag instead (below), except the player's own, which stays so it can
            // be found, with the flag over it
            foreach (var pac in snapshot.Pacs)
            {
                var isOwn = pac.PlayerId == ownPlayerId;

                if (!pac.Alive || (!isOwn && IsCarrier(pac, snapshot.Flags)))
                    continue;

                // the player's own pac is a bigger Pac-Man facing its way, so it's easy to find
                var icon = isOwn ? MiniMapSprites.GetOwnPacIcon(pac.Facing) : eMiniMapIcon.Dot;
                DrawIcon(spriteBatch, _icons, icon, inner, mapSize, pac.X, pac.Y, ClientColors.GetPacTint(pac, isOwn, animationTime));
            }

            // every flag once, carried or not: a carried one sits on its carrier
            foreach (var flag in snapshot.Flags)
            {
                var (x, y) = GetFlagPosition(flag, snapshot.Pacs);

                // a flag sealed in a force field (triforce) blinks gold
                var color = flag.IsSealed && ForceField.IsMiniMapGold(animationTime) ? ClientColors.COLOR_FORCE_FIELD : ClientColors.GetFlagColor(flag.Team, animationTime);
                DrawIcon(spriteBatch, _icons, eMiniMapIcon.Flag, inner, mapSize, x, y, color);
            }
        }

        /// <summary>
        /// True if the pac is carrying a flag.
        /// </summary>
        private static bool IsCarrier(PacState pac, List<FlagState> flags)
        {
            foreach (var flag in flags)
            {
                if (flag.CarrierId == pac.PlayerId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Where a flag is drawn: on its carrier while carried, otherwise where it lies.
        /// </summary>
        private static (float X, float Y) GetFlagPosition(FlagState flag, List<PacState> pacs)
        {
            if (flag.IsCarried)
            {
                foreach (var pac in pacs)
                {
                    if (pac.PlayerId == flag.CarrierId)
                        return (pac.X, pac.Y);
                }
            }

            return (flag.X, flag.Y);
        }

        /// <summary>
        /// A dot for every Cherry Twin decoy whose owner is on the other team from the player, so to enemies it looks
        /// like one more pac. The owner's team doesn't see it.
        /// </summary>
        private void DrawEnemyDecoys(SpriteBatch spriteBatch, Rectangle inner, int mapSize, SnapshotMessage snapshot, int ownPlayerId, TimeSpan animationTime)
        {
            eTeam? ownTeam = null;

            foreach (var pac in snapshot.Pacs)
                if (pac.PlayerId == ownPlayerId)
                    ownTeam = pac.Team;

            foreach (var effect in snapshot.WeaponEffects)
            {
                if (!WeaponEffectSprites.IsDrawnAsPac(effect.Kind))
                    continue;

                foreach (var pac in snapshot.Pacs)
                    if (pac.PlayerId == effect.OwnerId && pac.Alive && pac.Team != ownTeam)
                        DrawIcon(spriteBatch, _icons, eMiniMapIcon.Dot, inner, mapSize, effect.X, effect.Y, ClientColors.GetPacTint(pac, false, animationTime));
            }
        }

        /// <summary>
        /// An icon's cell from the mini map icon sheet, drawn at its own size and centred on the map position, kept inside
        /// the panel.
        /// </summary>
        private static void DrawIcon(SpriteBatch spriteBatch, Texture2D icons, eMiniMapIcon icon, Rectangle inner, int mapSize, float cellX, float cellY, Color color)
        {
            const int SIZE = MiniMapSprites.CELL_PIXELS;

            var centreX = inner.X + MiniMapMath.CellToMiniMap(cellX, mapSize, inner.Width);
            var centreY = inner.Y + MiniMapMath.CellToMiniMap(cellY, mapSize, inner.Height);
            var x = MathHelper.Clamp((int)(centreX - SIZE * 0.5f), inner.Left, inner.Right - SIZE);
            var y = MathHelper.Clamp((int)(centreY - SIZE * 0.5f), inner.Top, inner.Bottom - SIZE);
            var source = new Rectangle(MiniMapSprites.GetCellX(icon), MiniMapSprites.GetCellY(icon), SIZE, SIZE);

            spriteBatch.Draw(icons, new Rectangle(x, y, SIZE, SIZE), source, color);
        }

        /// <summary>
        /// The outline of a box of cells with rounded corners (SPAWN_BOX_CORNER_RADIUS), SPAWN_BOX_THICKNESS pixels wide, scaled to the panel and then SPAWN_BOX_SCALE times
        /// bigger; grown around its centre to at least SPAWN_BOX_MIN_PIXELS a side so a small room still shows as a box.
        /// </summary>
        private static void DrawBox(SpriteBatch spriteBatch, Texture2D pixel, Rectangle inner, int mapSize, CellBox box, Color color)
        {
            var left = inner.X + MiniMapMath.CellToMiniMap(box.X, mapSize, inner.Width);
            var right = inner.X + MiniMapMath.CellToMiniMap(box.X + box.Width, mapSize, inner.Width);
            var top = inner.Y + MiniMapMath.CellToMiniMap(box.Y, mapSize, inner.Height);
            var bottom = inner.Y + MiniMapMath.CellToMiniMap(box.Y + box.Height, mapSize, inner.Height);
            var width = Math.Max(SPAWN_BOX_MIN_PIXELS, (int)MathF.Round((right - left) * SPAWN_BOX_SCALE));
            var height = Math.Max(SPAWN_BOX_MIN_PIXELS, (int)MathF.Round((bottom - top) * SPAWN_BOX_SCALE));
            var x = MathHelper.Clamp((int)MathF.Round(((left + right) * 0.5f) - (width * 0.5f)), inner.Left, inner.Right - width);
            var y = MathHelper.Clamp((int)MathF.Round(((top + bottom) * 0.5f) - (height * 0.5f)), inner.Top, inner.Bottom - height);

            var radius = Math.Min(SPAWN_BOX_CORNER_RADIUS, Math.Min(width, height) / 2);
            var edgeRows = Math.Max(radius, SPAWN_BOX_THICKNESS);

            // the rounded rows at the top and bottom, one pixel row at a time
            for (var row = 0; row < edgeRows; row++)
            {
                var outer = RoundedCorners.GetRowInset(radius, row);
                var hollow = RoundedCorners.GetOutlineInnerInset(radius, SPAWN_BOX_THICKNESS, row);

                foreach (var rowY in new[] { y + row, y + height - 1 - row })
                {
                    if (hollow < 0)
                    {
                        spriteBatch.Draw(pixel, new Rectangle(x + outer, rowY, width - outer * 2, 1), color);
                    }
                    else
                    {
                        var span = Math.Max(0, hollow - outer);
                        spriteBatch.Draw(pixel, new Rectangle(x + outer, rowY, span, 1), color);
                        spriteBatch.Draw(pixel, new Rectangle(x + width - hollow, rowY, span, 1), color);
                    }
                }
            }

            // the straight sides between them
            spriteBatch.Draw(pixel, new Rectangle(x, y + edgeRows, SPAWN_BOX_THICKNESS, height - edgeRows * 2), color);
            spriteBatch.Draw(pixel, new Rectangle(x + width - SPAWN_BOX_THICKNESS, y + edgeRows, SPAWN_BOX_THICKNESS, height - edgeRows * 2), color);
        }
    }
}
