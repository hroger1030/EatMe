using EatMeLibrary;
using FastG;

namespace EatMeTests
{
    public class MazeMovementTests
    {
        /// <summary>
        /// A loop round a centre wall.
        /// </summary>
        private static readonly string[] RING =
        [
            "#####",
            "#...#",
            "#.#.#",
            "#...#",
            "#####",
        ];

        /// <summary>
        /// One row open from edge to edge: a warp tunnel.
        /// </summary>
        private static readonly string[] TUNNEL =
        [
            "#####",
            ".....",
            "#####",
        ];

        /// <summary>
        /// A loop round a centre wall, with a warp tunnel through the middle row.
        /// </summary>
        private static readonly string[] TUNNEL_LOOP =
        [
            "#######",
            "#.....#",
            "...#...",
            "#.....#",
            "#######",
        ];

        /// <summary>
        /// Builds a map from rows of text, '#' for wall and anything else open. map[x, y] is row y, column x.
        /// </summary>
        public static eGridDef[,] MakeMap(string[] rows)
        {
            var map = new eGridDef[rows[0].Length, rows.Length];

            for (var y = 0; y < rows.Length; y++)
                for (var x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == '#')
                        map[x, y] = eGridDef.Wall;

            return map;
        }

        private static Player MakePac(float x, float y, eFacing facing)
        {
            return new Player { Location = new Point2(x, y), Facing = facing, Alive = true };
        }

        [Test]
        public void Move_NothingHeld_KeepsGoingStraight()
        {
            var pac = MakePac(1.5f, 1.5f, eFacing.Right);

            var moved = MazeMovement.Move(MakeMap(RING), pac, null, 0.75f);

            Assert.That(moved, Is.True);
            Assert.That(pac.Location.X, Is.EqualTo(2.25f).Within(0.0001f));
            Assert.That(pac.Location.Y, Is.EqualTo(1.5f));
        }

        [Test]
        public void Move_IntoWall_StopsOnCellCentre()
        {
            var pac = MakePac(2.5f, 1.5f, eFacing.Right);

            var moved = MazeMovement.Move(MakeMap(RING), pac, null, 5f);

            Assert.That(moved, Is.False);
            Assert.That(pac.Location.X, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void Move_TurnWaitsForAnOpenCellCentre()
        {
            var pac = MakePac(1.5f, 1.5f, eFacing.Right);

            // (2, 2) is a wall, so the turn down can't happen until (3, 1)
            MazeMovement.Move(MakeMap(RING), pac, eFacing.Down, 0.5f);
            Assert.That(pac.Facing, Is.EqualTo(eFacing.Down), "turns at once where it can");

            var second = MakePac(1.8f, 1.5f, eFacing.Right);
            MazeMovement.Move(MakeMap(RING), second, eFacing.Down, 1.9f);

            Assert.That(second.Facing, Is.EqualTo(eFacing.Down));
            Assert.That(second.Location.X, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.That(second.Location.Y, Is.EqualTo(1.7f).Within(0.0001f));
        }

        [Test]
        public void Move_TurningBack_HappensAtOnce()
        {
            var pac = MakePac(2.2f, 1.5f, eFacing.Right);

            MazeMovement.Move(MakeMap(RING), pac, eFacing.Left, 0.1f);

            Assert.That(pac.Facing, Is.EqualTo(eFacing.Left));
            Assert.That(pac.Location.X, Is.EqualTo(2.1f).Within(0.0001f));
        }

        [Test]
        public void Move_HeldTowardWall_KeepsGoingTheOldWay()
        {
            var pac = MakePac(2.5f, 1.5f, eFacing.Right);

            MazeMovement.Move(MakeMap(RING), pac, eFacing.Up, 0.5f);

            Assert.That(pac.Facing, Is.EqualTo(eFacing.Right));
            Assert.That(pac.Location.X, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void Move_OffTheEdge_WarpsToTheOtherSide()
        {
            var pac = MakePac(0.5f, 1.5f, eFacing.Left);

            MazeMovement.Move(MakeMap(TUNNEL), pac, null, 1f);

            Assert.That(pac.Location.X, Is.EqualTo(4.5f).Within(0.0001f));
            Assert.That(pac.Location.Y, Is.EqualTo(1.5f));
        }

        [Test]
        public void Advance_JustPastTheEdge_StaysInsideTheMap()
        {
            var position = MazeMovement.Advance(new Point2(0f, 0.5f), eFacing.Left, 1e-8f, 100, 100);

            Assert.That(MazeMovement.GetCell(position).X, Is.InRange(0, 99));
        }

        [Test]
        public void Move_ManySmallSteps_StaysOnOpenCells()
        {
            var map = MakeMap(RING);
            var pac = MakePac(1.5f, 1.5f, eFacing.Right);
            var turns = new eFacing?[] { eFacing.Down, null, eFacing.Left, eFacing.Up, null, eFacing.Right };

            for (var i = 0; i < 600; i++)
            {
                MazeMovement.Move(map, pac, turns[i / 37 % turns.Length], 0.1f);
                var (x, y) = MazeMovement.GetCell(pac.Location);
                Assert.That(map[x, y].HasFlag(eGridDef.Wall), Is.False, $"step {i} at {pac.Location.X}, {pac.Location.Y}");
            }
        }

        [Test]
        public void GetDistanceToCentre_OnAndOffCentre()
        {
            Assert.That(MazeMovement.GetDistanceToCentre(new Point2(1.5f, 1.5f), eFacing.Right), Is.Zero);
            Assert.That(MazeMovement.GetDistanceToCentre(new Point2(1.2f, 1.5f), eFacing.Right), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(MazeMovement.GetDistanceToCentre(new Point2(1.2f, 1.5f), eFacing.Left), Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(MazeMovement.GetDistanceToCentre(new Point2(1.5f, 1.9f), eFacing.Down), Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void ToFacing_None_IsNull()
        {
            Assert.That(MazeMovement.ToFacing(eDirection.None), Is.Null);
            Assert.That(MazeMovement.ToFacing(eDirection.Up), Is.EqualTo(eFacing.Up));
            Assert.That(MazeMovement.ToFacing(eDirection.Right), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void IsHeadingToward()
        {
            var from = new Point2(1f, 1f);

            Assert.That(MazeMovement.IsHeadingToward(from, eFacing.Right, new Point2(2f, 1f)), Is.True);
            Assert.That(MazeMovement.IsHeadingToward(from, eFacing.Left, new Point2(2f, 1f)), Is.False);
            Assert.That(MazeMovement.IsHeadingToward(from, eFacing.Up, new Point2(1f, 0f)), Is.True);
        }

        [Test]
        public void FindFirstStep_TakesTheShortestWay()
        {
            var map = MakeMap(RING);

            Assert.That(GhostBrain.FindFirstStep(map, 1, 1, 3, 1), Is.EqualTo(eFacing.Right));
            Assert.That(GhostBrain.FindFirstStep(map, 1, 1, 1, 3), Is.EqualTo(eFacing.Down));
            Assert.That(GhostBrain.FindFirstStep(map, 1, 1, 1, 1), Is.Null);
        }

        [Test]
        public void FindFirstStep_KeepsOutOfWarpTunnels()
        {
            // through the tunnel is 3 steps, round the top or bottom is 6
            var step = GhostBrain.FindFirstStep(MakeMap(TUNNEL_LOOP), 1, 2, 5, 2);

            Assert.That(step, Is.Not.Null);
            Assert.That(step, Is.Not.EqualTo(eFacing.Left));
        }

        [Test]
        public void IsWarpCell_OpenEdgeCellsOnly()
        {
            var map = MakeMap(TUNNEL);

            Assert.That(MazeMovement.IsWarpCell(map, 0, 1), Is.True);
            Assert.That(MazeMovement.IsWarpCell(map, 4, 1), Is.True);
            Assert.That(MazeMovement.IsWarpCell(map, 2, 1), Is.False);
            Assert.That(MazeMovement.IsWarpCell(map, 0, 0), Is.False, "edge walls aren't tunnels");
        }

        [Test]
        public void CanMove_WithoutWarp_TunnelIsBlocked()
        {
            var map = MakeMap(TUNNEL);

            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Left), Is.True);
            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Left, false), Is.False);
            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Right, false), Is.True);
        }

        private static readonly string[] CORRIDOR =
        [
            "#####",
            "#...#",
            "#####",
        ];

        [Test]
        public void CanMove_EnemySpawnIsAWallForPacs_OwnSpawnIsNot()
        {
            var map = MakeMap(CORRIDOR);
            map[2, 1] |= eGridDef.SpawnPointBlue;

            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Right, true, MazeMovement.GetPacBlocked(eTeam.Red)), Is.False);
            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Right, true, MazeMovement.GetPacBlocked(eTeam.Blue)), Is.True);
        }

        [Test]
        public void CanMove_GhostGeneratorIsAWallForPacs([Values(eTeam.Red, eTeam.Blue)] eTeam team)
        {
            var map = MakeMap(CORRIDOR);
            map[2, 1] |= eGridDef.GhostGenerator;

            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Right, true, MazeMovement.GetPacBlocked(team)), Is.False);
        }

        [Test]
        public void CanMove_GhostsCantEnterAnySpawn([Values(eGridDef.SpawnPointRed, eGridDef.SpawnPointBlue)] eGridDef spawn)
        {
            var map = MakeMap(CORRIDOR);
            map[2, 1] |= spawn;

            Assert.That(MazeMovement.CanMove(map, 1, 1, eFacing.Right, false, MazeMovement.GHOST_BLOCKED), Is.False);
        }

        [Test]
        public void Move_PacStopsAtEnemySpawn()
        {
            var map = MakeMap(CORRIDOR);
            map[3, 1] |= eGridDef.SpawnPointRed;
            var pac = MakePac(1.5f, 1.5f, eFacing.Right);

            var moved = MazeMovement.Move(map, pac, null, 5f, blocked: MazeMovement.GetPacBlocked(eTeam.Blue));

            Assert.That(moved, Is.False);
            Assert.That(pac.Location.X, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void ChooseDirection_GhostNeverEntersASpawn()
        {
            var map = MakeMap(RING);
            map[3, 1] |= eGridDef.SpawnPointRed;

            // the shortest way to the target is through the spawn tile, so the ghost goes the long way round
            Assert.That(GhostBrain.FindFirstStep(map, 2, 1, 3, 2), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void Move_WithoutWarp_StopsAtTheTunnelMouth()
        {
            var ghost = MakePac(2.5f, 1.5f, eFacing.Left);

            var moved = MazeMovement.Move(MakeMap(TUNNEL), ghost, null, 5f, canWarp: false);

            Assert.That(moved, Is.False);
            Assert.That(ghost.Location.X, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void ChooseDirection_Wandering_NeverEntersATunnel()
        {
            var map = MakeMap(TUNNEL_LOOP);
            var random = new Random(1);

            // at (1, 2) heading left: up, down and the tunnel are ahead
            for (var i = 0; i < 20; i++)
                Assert.That(GhostBrain.ChooseDirection(map, 1, 2, eFacing.Left, null, random), Is.Not.EqualTo(eFacing.Left));
        }

        [Test]
        public void ChooseFleeDirection_NeverEntersATunnel()
        {
            // the tunnel leads furthest from the threat, but the only way a ghost may go is toward it
            Assert.That(GhostBrain.ChooseFleeDirection(MakeMap(TUNNEL), 1, 1, 3, 1), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void ChooseDirection_Wandering_NeverTurnsBackWhenItHasAChoice()
        {
            var map = MakeMap(RING);
            var random = new Random(1);

            // at (1, 1) heading up: the only ways are right and down (back)
            for (var i = 0; i < 20; i++)
                Assert.That(GhostBrain.ChooseDirection(map, 1, 1, eFacing.Up, null, random), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void ChooseDirection_DeadEnd_TurnsBack()
        {
            var map = MakeMap(["###", "#.#", "#.#", "###"]);

            Assert.That(GhostBrain.ChooseDirection(map, 1, 1, eFacing.Up, null, new Random(1)), Is.EqualTo(eFacing.Down));
        }

        [Test]
        public void ChooseDirection_WithTarget_FollowsThePath()
        {
            var map = MakeMap(RING);

            Assert.That(GhostBrain.ChooseDirection(map, 3, 1, eFacing.Right, (1, 3), new Random(1)), Is.Not.Null);
            Assert.That(GhostBrain.ChooseDirection(map, 1, 1, eFacing.Right, (1, 3), new Random(1)), Is.EqualTo(eFacing.Down));
        }
    }
}
