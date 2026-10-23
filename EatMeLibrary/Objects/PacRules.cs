using FastG;
using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Size and contact rules for pacs, shared so the client draws pacs the size the server treats them as.
    /// </summary>
    public static class PacRules
    {
        /// <summary>
        /// Width in map cells of a pac at size 0.
        /// </summary>
        public const float MIN_DIAMETER = 1f;

        /// <summary>
        /// Each this many points of size makes a pac one map cell wider.
        /// </summary>
        public const float SIZE_PER_EXTRA_CELL = 100f;

        /// <summary>
        /// Map cells of extra width per point of size (the inverse of SIZE_PER_EXTRA_CELL, so callers multiply).
        /// </summary>
        public const float CELLS_PER_SIZE = 1f / SIZE_PER_EXTRA_CELL;

        /// <summary>
        /// A pac at the max size moves this fraction faster than one at size 0.
        /// </summary>
        public const float MAX_SIZE_SPEED_BONUS = 0.2f;

        /// <summary>
        /// A pac with Warp Speed (galaxian) moves this many times as fast.
        /// </summary>
        public const float WARP_SPEED_SCALE = 1.5f;

        /// <summary>
        /// A pac with Sweet Tooth (strawberry) gets this many times as much growth from each dot.
        /// </summary>
        public const float SWEET_TOOTH_DOT_GAIN_SCALE = 3f;

        /// <summary>
        /// A pac with Invincible (starman) moves this many times as fast.
        /// </summary>
        public const float INVINCIBLE_SPEED_SCALE = 1.25f;

        /// <summary>
        /// A pac with Energy Tank moves this many times as fast.
        /// </summary>
        public const float ENERGY_TANK_SPEED_SCALE = 1.3f;

        /// <summary>
        /// A pac charging on its own spawn (PacState.SpawnFeeding) blinks purple, showing it for this long out of every two.
        /// </summary>
        public const int SPAWN_CHARGE_BLINK_MS = 150;

        /// <summary>
        /// True if a pac charging on its spawn is drawn purple at a time (otherwise its own color): purple for the first
        /// SPAWN_CHARGE_BLINK_MS of every two.
        /// </summary>
        public static bool IsSpawnChargeBlinkPurple(TimeSpan time)
        {
            return time.TotalMilliseconds % (SPAWN_CHARGE_BLINK_MS * 2) < SPAWN_CHARGE_BLINK_MS;
        }

        /// <summary>
        /// A flag counts as a circle half a cell across, centred on its position.
        /// </summary>
        public const float FLAG_RADIUS = 0.5f;

        /// <summary>
        /// A ghost counts as a circle one cell across.
        /// </summary>
        public const float GHOST_RADIUS = 0.5f;

        /// <summary>
        /// Size after eating one dot that adds gain. Dots never take a pac past maxSize, and a pac already past it
        /// (from a power pill) stays as it is.
        /// </summary>
        public static float GrowFromDot(float size, float gain, float maxSize)
        {
            if (size >= maxSize)
                return size;

            return MathF.Min(maxSize, size + gain);
        }

        /// <summary>
        /// Growth for a pac standing on its own team's spawn for the elapsed time: the same rate as running over a line of
        /// dots, one dot (dotGain) for every cell it would move at speed (cells a second).
        /// </summary>
        public static float GetSpawnFeedGain(float dotGain, float speed, TimeSpan elapsed)
        {
            return dotGain * speed * (float)elapsed.TotalSeconds;
        }

        /// <summary>
        /// What a power up effect multiplies a dot's growth (DotSizeGain) by: SWEET_TOOTH_DOT_GAIN_SCALE for Sweet Tooth, otherwise 1.
        /// </summary>
        public static float GetDotGainScale(ePowerUp effect)
        {
            return effect == ePowerUp.Strawberry ? SWEET_TOOTH_DOT_GAIN_SCALE : 1f;
        }

        /// <summary>
        /// Size an effect adds when it starts and takes away when it ends: powerPillSizeGain for a power pill,
        /// otherwise 0.
        /// </summary>
        public static float GetEffectSizeGain(ePowerUp effect, float powerPillSizeGain)
        {
            return effect switch
            {
                ePowerUp.PowerPill => powerPillSizeGain,
                _ => 0f,
            };
        }

        /// <summary>
        /// Size after eating a power pill that adds gain. Power pills can take a pac past the max size.
        /// </summary>
        public static float GrowFromPowerPill(float size, float gain)
        {
            return size + gain;
        }

        /// <summary>
        /// Size when a power pill that added gain wears off, never below START_SIZE.
        /// </summary>
        public static float WearOffPowerPill(float size, float gain)
        {
            return MathF.Max(START_SIZE, size - gain);
        }

        /// <summary>
        /// Size a pac starts (and respawns) at. Starving never takes a pac below it.
        /// </summary>
        public const float START_SIZE = 0f;

        /// <summary>
        /// Size after starving for the elapsed time, losing perSecond each second, but never below START_SIZE.
        /// </summary>
        public static float Starve(float size, float perSecond, TimeSpan elapsed)
        {
            return MathF.Max(START_SIZE, size - perSecond * (float)elapsed.TotalSeconds);
        }

        /// <summary>
        /// How hungry a pac is, 0 to 1: the share of msUntilStarvation that has passed since it last ate, so 1 means it
        /// is starving (shrinking). Always 1 when msUntilStarvation isn't positive, since the pac starves at once.
        /// </summary>
        public static float GetHungerFill(int msSinceEaten, int msUntilStarvation)
        {
            if (msUntilStarvation <= 0)
                return 1f;

            return Math.Clamp((float)msSinceEaten / msUntilStarvation, 0f, 1f);
        }

        /// <summary>
        /// Speed of a pac this size: baseSpeed at size 0, rising evenly to MAX_SIZE_SPEED_BONUS faster at the max size.
        /// </summary>
        public static float GetSpeed(float baseSpeed, float size, float maxSize)
        {
            return baseSpeed * (1f + MAX_SIZE_SPEED_BONUS * size / maxSize);
        }

        /// <summary>
        /// What a power up effect multiplies a pac's speed (GetSpeed) by: WARP_SPEED_SCALE for Warp Speed,
        /// INVINCIBLE_SPEED_SCALE for Invincible, ENERGY_TANK_SPEED_SCALE for Energy Tank, otherwise 1.
        /// </summary>
        public static float GetEffectSpeedScale(ePowerUp effect)
        {
            return effect switch
            {
                ePowerUp.Galaxian => WARP_SPEED_SCALE,
                ePowerUp.Starman => INVINCIBLE_SPEED_SCALE,
                ePowerUp.EnergyTank => ENERGY_TANK_SPEED_SCALE,
                _ => 1f,
            };
        }

        /// <summary>
        /// True if a pac kills a ghost it touches: it must have a power pill working and be at least maxSize.
        /// Otherwise the ghost kills the pac.
        /// </summary>
        public static bool CanKillGhost(float size, bool powered, float maxSize)
        {
            return powered && size >= maxSize;
        }

        /// <summary>
        /// True if two circles overlap. Positions and radii are in map cells.
        /// </summary>
        public static bool AreTouching(Point2 a, float radiusA, Point2 b, float radiusB)
        {
            var reach = radiusA + radiusB;
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy < reach * reach;
        }

        /// <summary>
        /// Radius of a pac in map cells.
        /// </summary>
        public static float GetRadius(float size)
        {
            return GetDiameter(size) * 0.5f;
        }

        /// <summary>
        /// True if two pacs overlap.
        /// </summary>
        public static bool IsTouchingPac(Point2 a, float sizeA, Point2 b, float sizeB)
        {
            return AreTouching(a, GetRadius(sizeA), b, GetRadius(sizeB));
        }

        /// <summary>
        /// True if a pac overlaps a ghost of the given radius.
        /// </summary>
        public static bool IsTouchingGhost(Point2 pacPosition, float size, Point2 ghostPosition, float ghostRadius = GHOST_RADIUS)
        {
            return AreTouching(pacPosition, GetRadius(size), ghostPosition, ghostRadius);
        }

        /// <summary>
        /// Width of a pac in map cells: MIN_DIAMETER at size 0, one cell wider for every SIZE_PER_EXTRA_CELL.
        /// </summary>
        public static float GetDiameter(float size)
        {
            return MIN_DIAMETER + size * CELLS_PER_SIZE;
        }

        /// <summary>
        /// True if a pac of the given size at pacPosition overlaps a flag at flagPosition. Positions are in map cells.
        /// </summary>
        public static bool IsTouchingFlag(Point2 pacPosition, float size, float flagX, float flagY)
        {
            return AreTouching(pacPosition, GetRadius(size), new Point2(flagX, flagY), FLAG_RADIUS);
        }
    }
}
