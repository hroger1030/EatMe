using EatMeLibrary;

namespace EatMeTests
{
    public class GhostEyesTests
    {
        private const float TOLERANCE = 0.001f;

        [Test]
        public void Update_FliesStraightToTheCircleAtTheGivenSpeed()
        {
            var eyes = new GhostEyes(0, 20f, 10f);

            eyes.Update(TimeSpan.FromMilliseconds(500), 10f, 10f, 6f);

            Assert.That(eyes.X, Is.EqualTo(17f).Within(TOLERANCE));
            Assert.That(eyes.Y, Is.EqualTo(10f).Within(TOLERANCE));
            Assert.That(eyes.Facing, Is.EqualTo(eFacing.Left));
            Assert.That(eyes.IsCircling, Is.False);
        }

        [Test]
        public void Update_StartsCirclingOnReachingTheCircle()
        {
            var eyes = new GhostEyes(0, 10f, 20f);

            eyes.Update(TimeSpan.FromSeconds(10), 10f, 10f, 6f);

            Assert.That(eyes.IsCircling, Is.True);
            Assert.That(eyes.X, Is.EqualTo(10f).Within(TOLERANCE));
            Assert.That(eyes.Y, Is.EqualTo(10f + GhostEyes.ORBIT_CELLS).Within(TOLERANCE));
        }

        [Test]
        public void Update_FromInsideThePenFliesOutToTheCircle()
        {
            var eyes = new GhostEyes(0, 10f, 10f);

            eyes.Update(TimeSpan.FromSeconds(10), 10f, 10f, 6f);

            Assert.That(eyes.IsCircling, Is.True);
            Assert.That(eyes.X, Is.EqualTo(10f + GhostEyes.ORBIT_CELLS).Within(TOLERANCE));
            Assert.That(eyes.Y, Is.EqualTo(10f).Within(TOLERANCE));
        }

        [Test]
        public void Update_CirclingStaysOnTheCircleAndMovesAtTheGivenSpeed()
        {
            var eyes = new GhostEyes(0, 10f + GhostEyes.ORBIT_CELLS, 10f);
            eyes.Update(TimeSpan.FromMilliseconds(10), 10f, 10f, 6f);
            Assert.That(eyes.IsCircling, Is.True);

            var startAngle = eyes.OrbitAngle;
            eyes.Update(TimeSpan.FromMilliseconds(100), 10f, 10f, 6f);

            var dx = eyes.X - 10f;
            var dy = eyes.Y - 10f;
            Assert.That(MathF.Sqrt(dx * dx + dy * dy), Is.EqualTo(GhostEyes.ORBIT_CELLS).Within(TOLERANCE));
            Assert.That(eyes.OrbitAngle - startAngle, Is.EqualTo(0.6f / GhostEyes.ORBIT_CELLS).Within(TOLERANCE));

            // clockwise on screen from the right of the centre is downward
            Assert.That(eyes.Facing, Is.EqualTo(eFacing.Down));
        }

        [TestCase(1f, 0f, eFacing.Right)]
        [TestCase(-1f, 0.5f, eFacing.Left)]
        [TestCase(0.2f, 1f, eFacing.Down)]
        [TestCase(0f, -1f, eFacing.Up)]
        public void GetFacing_LooksTheWayItMoves(float dx, float dy, eFacing expected)
        {
            Assert.That(GhostEyes.GetFacing(dx, dy, eFacing.Up), Is.EqualTo(expected));
        }

        [Test]
        public void GetFacing_KeepsTheFacingWhenStill()
        {
            Assert.That(GhostEyes.GetFacing(0f, 0f, eFacing.Left), Is.EqualTo(eFacing.Left));
        }

        [Test]
        public void FindPenCentre_IsTheMiddleOfTheGhostGenerator()
        {
            var map = new eGridDef[10, 10];
            map[4, 4] = map[5, 4] = map[4, 5] = map[5, 5] = eGridDef.GhostGenerator;

            var centre = GhostEyes.FindPenCentre(map);

            Assert.That(centre.HasValue, Is.True);
            Assert.That(centre.Value.X, Is.EqualTo(5f).Within(TOLERANCE));
            Assert.That(centre.Value.Y, Is.EqualTo(5f).Within(TOLERANCE));
        }

        [Test]
        public void FindPenCentre_IsNullWithNoGenerator()
        {
            Assert.That(GhostEyes.FindPenCentre(new eGridDef[4, 4]).HasValue, Is.False);
        }

        [Test]
        public void ClientGameState_EyesComeWithAKillAndGoWhenTheGhostRespawns()
        {
            var state = new ClientGameState();
            state.Apply(new WelcomeMessage { PlayerId = 3, Team = eTeam.Blue, ServerName = "Srv", LoadingScreenTips = [] }, 0);
            state.Apply(new LevelStartMessage { Level = 1 }, 0);
            Assert.That(state.PenCentre.HasValue, Is.True);

            var kill = new GameEventMessage { EventType = eGameEvent.GhostKilled, ActorId = 3, TargetId = 2, X = 1.5f, Y = 1.5f };
            state.Apply(kill, 0);
            state.Apply(kill, 0);
            Assert.That(state.Eyes.Count, Is.EqualTo(1), "one pair of eyes per ghost");
            Assert.That(state.Eyes[0].GhostId, Is.EqualTo(2));

            state.UpdateGhostEyes(TimeSpan.FromMilliseconds(100));
            Assert.That(state.Eyes[0].X != 1.5f || state.Eyes[0].Y != 1.5f, Is.True, "the eyes moved");

            state.Apply(new SnapshotMessage { Tick = 1, Ghosts = [new GhostState(2, 1.5f, 1.5f, eFacing.Up, false)] }, 0);
            Assert.That(state.Eyes.Count, Is.EqualTo(1), "still dead");

            state.Apply(new SnapshotMessage { Tick = 2, Ghosts = [new GhostState(2, 10.5f, 10.5f, eFacing.Up, true)] }, 0);
            Assert.That(state.Eyes, Is.Empty, "gone once it respawns");
        }

        [Test]
        public void ClientGameState_LevelStartClearsTheEyes()
        {
            var state = new ClientGameState();
            state.Apply(new LevelStartMessage { Level = 1 }, 0);
            state.AddGhostEyes(0, 1.5f, 1.5f);

            state.Apply(new LevelStartMessage { Level = 2 }, 0);

            Assert.That(state.Eyes, Is.Empty);
        }
    }
}
