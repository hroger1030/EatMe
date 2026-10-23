using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// One quarter of a smashed wall flying off: launched up and outward from its quarter of the cell, spinning, and
    /// falling under gravity until it drops off the bottom of the board. It is only drawn, never touches anything.
    /// Positions are in map cells, y down, like everything else on the board.
    /// </summary>
    public class WallDebrisPiece
    {
        /// <summary>
        /// The smashed cell and which quarter of it this is (0 top left, clockwise, as in WallTiles).
        /// </summary>
        public int CellX { get; set; }
        public int CellY { get; set; }
        public int Quarter { get; set; }

        /// <summary>
        /// How the quarter looked in the wall before it was smashed (WallTiles.GetQuarter). With IsSolid it was inside a
        /// solid block, so it is only the wall fill color.
        /// </summary>
        public bool IsSolid { get; set; }
        public int SourceQuarter { get; set; }
        public int QuarterTurns { get; set; }

        /// <summary>
        /// The quarter's centre when it was smashed.
        /// </summary>
        public float StartX { get; set; }
        public float StartY { get; set; }

        /// <summary>
        /// Cells a second at launch: VelocityY is upward.
        /// </summary>
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }

        /// <summary>
        /// Radians a second, clockwise.
        /// </summary>
        public float Spin { get; set; }
        public TimeSpan Age { get; set; }
    }

    /// <summary>
    /// The debris a smashed wall breaks into: four quarters (WallDebrisPiece), each on its own parabolic arc.
    /// </summary>
    public static class WallDebris
    {
        public const int PIECES_PER_WALL = 4;

        // cells a second, upward at launch
        public const float MIN_LAUNCH_SPEED = 8f;
        public const float MAX_LAUNCH_SPEED = 14f;

        // cells a second, outward from the cell's centre (left quarters go left, right ones right)
        public const float MIN_SIDE_SPEED = 2f;
        public const float MAX_SIDE_SPEED = 7f;

        // cells a second squared, downward
        public const float GRAVITY = 30f;

        // turns a second, either way
        public const float MIN_SPIN_TURNS = 1f;
        public const float MAX_SPIN_TURNS = 3f;

        /// <summary>
        /// A piece is dropped after this long wherever it is, so one that flew off a side doesn't live for ever.
        /// </summary>
        public const int MAX_AGE_MS = 4000;

        private const float QUARTER_NEAR = 0.25f;
        private const float QUARTER_FAR = 0.75f;
        private const float HALF = 0.5f;

        /// <summary>
        /// Breaks wall cell (x, y) into its four quarters, each looking as it did in <paramref name="map"/> (the map before
        /// the wall went, so the pieces match the wall around them) and launched up and outward with random speeds and spin.
        /// </summary>
        public static List<WallDebrisPiece> Break(eGridDef[,] map, int x, int y, Random random)
        {
            var pieces = new List<WallDebrisPiece>(PIECES_PER_WALL);

            for (var quarter = 0; quarter < PIECES_PER_WALL; quarter++)
            {
                var isRight = quarter == 1 || quarter == 2;
                var isBottom = quarter >= 2;
                var isSolid = !WallTiles.GetQuarter(map, x, y, quarter, out var sourceQuarter, out var quarterTurns);
                var sideSpeed = Lerp(MIN_SIDE_SPEED, MAX_SIDE_SPEED, (float)random.NextDouble());
                var spinTurns = Lerp(MIN_SPIN_TURNS, MAX_SPIN_TURNS, (float)random.NextDouble());

                pieces.Add(new WallDebrisPiece
                {
                    CellX = x,
                    CellY = y,
                    Quarter = quarter,
                    IsSolid = isSolid,
                    SourceQuarter = sourceQuarter,
                    QuarterTurns = quarterTurns,
                    StartX = x + (isRight ? QUARTER_FAR : QUARTER_NEAR),
                    StartY = y + (isBottom ? QUARTER_FAR : QUARTER_NEAR),
                    VelocityX = isRight ? sideSpeed : -sideSpeed,
                    VelocityY = Lerp(MIN_LAUNCH_SPEED, MAX_LAUNCH_SPEED, (float)random.NextDouble()),
                    Spin = spinTurns * MathF.Tau * (random.Next(2) == 0 ? -1f : 1f),
                });
            }

            return pieces;
        }

        public static float GetX(WallDebrisPiece piece)
        {
            return piece.StartX + piece.VelocityX * (float)piece.Age.TotalSeconds;
        }

        /// <summary>
        /// Up at the launch speed, then down ever faster under GRAVITY.
        /// </summary>
        public static float GetY(WallDebrisPiece piece)
        {
            var t = (float)piece.Age.TotalSeconds;
            return piece.StartY - piece.VelocityY * t + HALF * GRAVITY * t * t;
        }

        /// <summary>
        /// The piece's spin so far, in radians, added to the quarter's own turns when it is drawn.
        /// </summary>
        public static float GetRotation(WallDebrisPiece piece)
        {
            return piece.Spin * (float)piece.Age.TotalSeconds;
        }

        public static bool IsFalling(WallDebrisPiece piece)
        {
            return GRAVITY * (float)piece.Age.TotalSeconds > piece.VelocityY;
        }

        /// <summary>
        /// True once the piece has fallen past <paramref name="bottomY"/> (the bottom of the board on screen, in cells),
        /// or has flown for MAX_AGE_MS. A piece still rising below the bottom isn't gone yet, as it may come into view.
        /// </summary>
        public static bool IsGone(WallDebrisPiece piece, float bottomY)
        {
            if (piece.Age.TotalMilliseconds >= MAX_AGE_MS)
                return true;

            return IsFalling(piece) && GetY(piece) - HALF > bottomY;
        }

        private static float Lerp(float from, float to, float amount)
        {
            return from + (to - from) * amount;
        }
    }
}
