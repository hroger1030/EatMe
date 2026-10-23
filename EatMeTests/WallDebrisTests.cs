using EatMeLibrary;

namespace EatMeTests
{
    public class WallDebrisTests
    {
        private static eGridDef[,] LoneWall()
        {
            var map = new eGridDef[3, 3];
            map[1, 1] = eGridDef.Wall;
            return map;
        }

        [Test]
        public void Break_GivesFourQuartersEachFlyingOutwardAndUp()
        {
            var pieces = WallDebris.Break(LoneWall(), 1, 1, new Random(1));

            Assert.That(pieces.Select(p => p.Quarter), Is.EqualTo(new[] { 0, 1, 2, 3 }));

            foreach (var piece in pieces)
            {
                var isRight = piece.Quarter == 1 || piece.Quarter == 2;
                var isBottom = piece.Quarter >= 2;

                Assert.That(piece.StartX, Is.EqualTo(isRight ? 1.75f : 1.25f));
                Assert.That(piece.StartY, Is.EqualTo(isBottom ? 1.75f : 1.25f));
                Assert.That(Math.Sign(piece.VelocityX), Is.EqualTo(isRight ? 1 : -1));
                Assert.That(Math.Abs(piece.VelocityX), Is.InRange(WallDebris.MIN_SIDE_SPEED, WallDebris.MAX_SIDE_SPEED));
                Assert.That(piece.VelocityY, Is.InRange(WallDebris.MIN_LAUNCH_SPEED, WallDebris.MAX_LAUNCH_SPEED));
                Assert.That(Math.Abs(piece.Spin), Is.InRange(WallDebris.MIN_SPIN_TURNS * MathF.Tau, WallDebris.MAX_SPIN_TURNS * MathF.Tau));
            }
        }

        [Test]
        public void Break_KeepsHowEachQuarterLookedInTheWall()
        {
            var map = new eGridDef[4, 4];
            for (var x = 0; x < 4; x++)
                for (var y = 0; y < 4; y++)
                    map[x, y] = eGridDef.Wall;

            var pieces = WallDebris.Break(map, 1, 1, new Random(1));

            // inside a solid block nothing of the tube shows
            Assert.That(pieces.All(p => p.IsSolid), Is.True);

            var lone = WallDebris.Break(LoneWall(), 1, 1, new Random(1));

            foreach (var piece in lone)
            {
                WallTiles.GetQuarter(LoneWall(), 1, 1, piece.Quarter, out var sourceQuarter, out var quarterTurns);
                Assert.That(piece.IsSolid, Is.False);
                Assert.That(piece.SourceQuarter, Is.EqualTo(sourceQuarter));
                Assert.That(piece.QuarterTurns, Is.EqualTo(quarterTurns));
            }
        }

        [Test]
        public void Piece_RisesThenFallsOnAParabola()
        {
            var piece = new WallDebrisPiece { StartX = 5f, StartY = 5f, VelocityX = 2f, VelocityY = 12f, Spin = 3f };

            Assert.That(WallDebris.GetY(piece), Is.EqualTo(5f));

            // the top of the arc is at VelocityY / GRAVITY seconds
            piece.Age = TimeSpan.FromSeconds(12f / WallDebris.GRAVITY);
            Assert.That(WallDebris.GetY(piece), Is.EqualTo(5f - 12f * 12f / (2f * WallDebris.GRAVITY)).Within(0.0001f));
            Assert.That(WallDebris.GetX(piece), Is.EqualTo(5f + 2f * (float)piece.Age.TotalSeconds).Within(0.0001f));
            Assert.That(WallDebris.GetRotation(piece), Is.EqualTo(3f * (float)piece.Age.TotalSeconds).Within(0.0001f));
            Assert.That(WallDebris.IsFalling(piece), Is.False);

            // back at the start height, coming down
            piece.Age = TimeSpan.FromSeconds(2f * 12f / WallDebris.GRAVITY);
            Assert.That(WallDebris.GetY(piece), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(WallDebris.IsFalling(piece), Is.True);
        }

        [Test]
        public void IsGone_OnceFallenPastTheBottom()
        {
            var piece = new WallDebrisPiece { StartY = 5f, VelocityY = 10f };

            Assert.That(WallDebris.IsGone(piece, 20f), Is.False);

            piece.Age = TimeSpan.FromMilliseconds(1500);
            Assert.That(WallDebris.GetY(piece), Is.GreaterThan(20.5f));
            Assert.That(WallDebris.IsGone(piece, 20f), Is.True);
        }

        [Test]
        public void IsGone_NotWhileStillRisingBelowTheBottom()
        {
            var piece = new WallDebrisPiece { StartY = 30f, VelocityY = 10f, Age = TimeSpan.FromMilliseconds(100) };

            Assert.That(WallDebris.IsGone(piece, 20f), Is.False);
        }

        [Test]
        public void IsGone_AfterMaxSecondsWherever()
        {
            var piece = new WallDebrisPiece { StartY = 5f, VelocityY = 10f, Age = TimeSpan.FromMilliseconds(WallDebris.MAX_AGE_MS) };

            Assert.That(WallDebris.IsGone(piece, 1000f), Is.True);
        }
    }
}
