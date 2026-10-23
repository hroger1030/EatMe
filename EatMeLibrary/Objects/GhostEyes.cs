using System;

namespace EatMeLibrary
{
    /// <summary>
    /// What is left of a dead ghost: its eyes, flying back to the ghost pen and circling it until the ghost respawns.
    /// They are only drawn, never touch anything, and ignore walls and the grid. Positions are in map cells.
    /// </summary>
    public class GhostEyes
    {
        /// <summary>
        /// The eyes move this many times GhostSpeed.
        /// </summary>
        public const float SPEED_SCALE = 3f;

        /// <summary>
        /// Radius in cells of the circle the eyes go round, from the pen's centre, just outside the 4x4 pen.
        /// </summary>
        public const float ORBIT_CELLS = 3f;

        public int GhostId { get; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public eFacing Facing { get; private set; } = eFacing.Up;

        /// <summary>
        /// False while flying to the circle, true once going round it.
        /// </summary>
        public bool IsCircling { get; private set; }

        /// <summary>
        /// Where on the circle the eyes are (radians, 0 to the right, clockwise on screen since y is down).
        /// </summary>
        public float OrbitAngle { get; private set; }

        public GhostEyes(int ghostId, float x, float y)
        {
            GhostId = ghostId;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Moves the eyes for the elapsed time at speed cells a second: straight to the nearest point on the circle round the pen's centre,
        /// then round it.
        /// </summary>
        public void Update(TimeSpan elapsed, float centreX, float centreY, float speed)
        {
            var step = speed * (float)elapsed.TotalSeconds;

            if (step <= 0f)
                return;

            if (IsCircling)
            {
                OrbitAngle = (OrbitAngle + step / ORBIT_CELLS) % (MathF.PI * 2f);
                var (orbitX, orbitY) = GetOrbitPosition(centreX, centreY, OrbitAngle);
                Facing = GetFacing(orbitX - X, orbitY - Y, Facing);
                X = orbitX;
                Y = orbitY;
                return;
            }

            var angle = GetAngleFromCentre(X, Y, centreX, centreY);
            var (targetX, targetY) = GetOrbitPosition(centreX, centreY, angle);
            var dx = targetX - X;
            var dy = targetY - Y;
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            Facing = GetFacing(dx, dy, Facing);

            if (distance <= step)
            {
                X = targetX;
                Y = targetY;
                OrbitAngle = angle;
                IsCircling = true;
                return;
            }

            X += dx / distance * step;
            Y += dy / distance * step;
        }

        /// <summary>
        /// The point on the circle round the centre at an angle.
        /// </summary>
        public static (float X, float Y) GetOrbitPosition(float centreX, float centreY, float angle)
        {
            return (centreX + MathF.Cos(angle) * ORBIT_CELLS, centreY + MathF.Sin(angle) * ORBIT_CELLS);
        }

        /// <summary>
        /// The angle from the centre to a point, so the eyes head for the nearest point on the circle; 0 (to the right)
        /// for the centre itself.
        /// </summary>
        public static float GetAngleFromCentre(float x, float y, float centreX, float centreY)
        {
            var dx = x - centreX;
            var dy = y - centreY;

            if (dx == 0f && dy == 0f)
                return 0f;

            var angle = MathF.Atan2(dy, dx);
            return angle < 0f ? angle + MathF.PI * 2f : angle;
        }

        /// <summary>
        /// The facing closest to a movement, so the eyes look where they are going; the current facing when not moving.
        /// </summary>
        public static eFacing GetFacing(float dx, float dy, eFacing current)
        {
            if (dx == 0f && dy == 0f)
                return current;

            if (MathF.Abs(dx) >= MathF.Abs(dy))
                return dx > 0f ? eFacing.Right : eFacing.Left;

            return dy > 0f ? eFacing.Down : eFacing.Up;
        }

        /// <summary>
        /// The centre of the ghost generator cells (the middle of the pen), or null if the map has none.
        /// </summary>
        public static (float X, float Y)? FindPenCentre(eGridDef[,] map)
        {
            var sumX = 0f;
            var sumY = 0f;
            var count = 0;

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if ((map[x, y] & eGridDef.GhostGenerator) == 0)
                        continue;

                    sumX += x + 0.5f;
                    sumY += y + 0.5f;
                    count++;
                }
            }

            return count == 0 ? null : (sumX / count, sumY / count);
        }
    }
}
