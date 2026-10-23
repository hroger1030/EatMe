using EatMeLibrary;
using FastG;

namespace EatMeTests
{
    /// <summary>
    /// MelonRoll's rules: what a roll can go into, which way it starts, how it moves and stops, and when bots use it.
    /// </summary>
    public class MelonRollTests
    {
        // a corridor with an inner wall at (3, 1); the ends are the outer wall
        private static readonly string[] CORRIDOR = ["#########", "#..#....#", "#########"];

        private static Player MakePac(float x, float y, eFacing facing)
        {
            return new Player { Id = 1, Team = eTeam.Red, Location = new Point2(x, y), Facing = facing, Alive = true, PowerUpEffect = ePowerUp.Melon, PowerUpMs = 1000 };
        }

        [Test]
        public void CanRollInto_OpenCellsAndInnerWalls_NotTheOuterWallBlocksOrClosedRooms()
        {
            var map = MazeMovementTests.MakeMap(CORRIDOR);
            var blocked = MazeMovement.GetPacBlocked(eTeam.Red);

            Assert.That(MelonRoll.CanRollInto(map, 1, 1, blocked), Is.True, "open");
            Assert.That(MelonRoll.CanRollInto(map, 3, 1, blocked), Is.True, "an inner wall gets smashed");
            Assert.That(MelonRoll.CanRollInto(map, 0, 1, blocked), Is.False, "the outer wall");

            map[5, 1] = eGridDef.Wall | eGridDef.TetrisBlock;
            Assert.That(MelonRoll.CanRollInto(map, 5, 1, blocked), Is.False, "a Tetromino block");

            map[6, 1] = eGridDef.GhostGenerator;
            Assert.That(MelonRoll.CanRollInto(map, 6, 1, blocked), Is.False, "the ghost generator");

            map[7, 1] = eGridDef.SpawnPointBlue;
            Assert.That(MelonRoll.CanRollInto(map, 7, 1, blocked), Is.False, "the enemy spawn");

            map[7, 1] = eGridDef.SpawnPointRed;
            Assert.That(MelonRoll.CanRollInto(map, 7, 1, blocked), Is.True, "its own spawn");
        }

        [Test]
        public void Move_RollsThroughInnerWalls_AndStopsAtTheOuterWall()
        {
            var map = MazeMovementTests.MakeMap(CORRIDOR);
            var pac = MakePac(1.5f, 1.5f, eFacing.Right);
            var blocked = MazeMovement.GetPacBlocked(eTeam.Red);

            Assert.That(MelonRoll.Move(map, pac, 2.25f, blocked), Is.True);
            Assert.That(pac.Location.X, Is.EqualTo(3.75f).Within(0.0001f), "inside the inner wall");

            Assert.That(MelonRoll.Move(map, pac, 20f, blocked), Is.False, "stopped");
            Assert.That(pac.Location.X, Is.EqualTo(7.5f).Within(0.0001f), "on the centre of the last cell");
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void GetStartFacing_HeldWayWhenItCanGoThatWay_OtherwiseFacing()
        {
            var map = MazeMovementTests.MakeMap(["#####", "#...#", "#...#", "#####"]);
            var blocked = MazeMovement.GetPacBlocked(eTeam.Red);
            var pac = MakePac(2.5f, 1.5f, eFacing.Right);

            Assert.That(MelonRoll.GetStartFacing(map, pac, eFacing.Down, blocked), Is.EqualTo(eFacing.Down), "any open way from a centre");
            Assert.That(MelonRoll.GetStartFacing(map, pac, eFacing.Up, blocked), Is.EqualTo(eFacing.Right), "up is the outer wall");
            Assert.That(MelonRoll.GetStartFacing(map, pac, null, blocked), Is.EqualTo(eFacing.Right));

            pac.Location = new Point2(2.75f, 1.5f);
            Assert.That(MelonRoll.GetStartFacing(map, pac, eFacing.Down, blocked), Is.EqualTo(eFacing.Right), "between centres only straight on");
            Assert.That(MelonRoll.GetStartFacing(map, pac, eFacing.Left, blocked), Is.EqualTo(eFacing.Left), "or straight back");

            pac.Location = new Point2(3.5f, 1.5f);
            Assert.That(MelonRoll.GetStartFacing(map, pac, null, blocked), Is.Null, "facing the outer wall");
            Assert.That(MelonRoll.GetStartFacing(map, pac, eFacing.Left, blocked), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void GetSpinAngle_TurnsSpinTurnsPerSecond()
        {
            Assert.That(MelonRoll.GetSpinAngle(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(MelonRoll.GetSpinAngle(TimeSpan.FromSeconds(0.25f / MelonRoll.SPIN_TURNS_PER_SECOND)), Is.EqualTo(MathF.PI / 2f).Within(0.0001f));
            Assert.That(MelonRoll.GetSpinAngle(TimeSpan.FromSeconds(0.5f / MelonRoll.SPIN_TURNS_PER_SECOND)), Is.EqualTo(MathF.PI).Within(0.0001f));
        }

        [Test]
        public void ShouldUseAction_RollsAtAThreatOrTarget_NotWhileRolling()
        {
            var map = MazeMovementTests.MakeMap(["############", "#..........#", "############"]);
            var me = MakePac(1.5f, 1.5f, eFacing.Left);
            me.Size = 10f;
            var ghost = new Ghost { Id = 0, Location = new Point2(10.5f, 1.5f), Alive = true };
            var world = new BotWorld { Map = map, MaxSize = 100f, Pacs = [me], Ghosts = [ghost] };

            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "far away and behind");

            me.Facing = eFacing.Right;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True, "in line ahead");

            me.Facing = eFacing.Left;
            ghost.Location = new Point2(4.5f, 1.5f);
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.True, "close by");

            me.IsRolling = true;
            Assert.That(PacBrain.ShouldUseAction(world, me), Is.False, "already rolling");
        }
    }
}
