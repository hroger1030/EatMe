using EatMeLibrary;
using FastG;

namespace EatMeTests
{
    public class GhostAndPacRulesTests
    {
        private const float VISION = 10f;
        private const float MAX_SIZE = 100f;
        private static readonly Point2 GHOST = new(0f, 0f);

        private static Player MakePac(int id, float x, float y, bool hasFlag = false)
        {
            return new Player { Id = id, Location = new Point2(x, y), Alive = true, HasFlag = hasFlag, Size = 50f };
        }

        [Test]
        public void PickTarget_ChoosesNearestPacInVision()
        {
            var near = MakePac(1, 3f, 0f);
            var far = MakePac(2, 6f, 0f);
            var outOfSight = MakePac(3, 20f, 0f);

            Assert.That(GhostBrain.PickTarget(GHOST, null, [far, near, outOfSight], VISION), Is.SameAs(near));
            Assert.That(GhostBrain.PickTarget(GHOST, null, [outOfSight], VISION), Is.Null);
        }

        [Test]
        public void DoctorAway_GhostsIgnoreAndFleePac()
        {
            var apple = MakePac(1, 3f, 0f);
            apple.PowerUpEffect = ePowerUpEffect.DoctorAway;
            apple.PowerUpMs = 1000;
            var other = MakePac(2, 6f, 0f);

            Assert.That(GhostBrain.PickTarget(GHOST, null, [apple, other], VISION), Is.SameAs(other));
            Assert.That(GhostBrain.PickTarget(GHOST, apple, [apple], VISION), Is.Null, "drops it as a target");
            Assert.That(GhostBrain.FindThreat(GHOST, [apple, other], VISION), Is.SameAs(apple));
            Assert.That(GhostBrain.FindThreat(GHOST, [MakePac(3, 20f, 0f)], VISION), Is.Null);
        }

        [Test]
        public void PickTarget_PrefersFlagCarriers()
        {
            var near = MakePac(1, 2f, 0f);
            var carrier = MakePac(2, 8f, 0f, hasFlag: true);

            Assert.That(GhostBrain.PickTarget(GHOST, null, [near, carrier], VISION), Is.SameAs(carrier));
            Assert.That(GhostBrain.PickTarget(GHOST, carrier, [near, carrier], VISION), Is.SameAs(carrier));
        }

        [Test]
        public void PickTarget_KeepsChasingOutOfVision()
        {
            var target = MakePac(1, 30f, 0f);

            Assert.That(GhostBrain.PickTarget(GHOST, target, [target], VISION), Is.SameAs(target));
        }

        [Test]
        public void PickTarget_SwitchesWhenAnotherPacIsCloser()
        {
            var target = MakePac(1, 8f, 0f);
            var closer = MakePac(2, 2f, 0f);

            Assert.That(GhostBrain.PickTarget(GHOST, target, [target, closer], VISION), Is.SameAs(closer));
        }

        [Test]
        public void PickTarget_DropsDeadOrMissingTarget()
        {
            var target = MakePac(1, 3f, 0f);
            target.Alive = false;

            Assert.That(GhostBrain.PickTarget(GHOST, target, [target], VISION), Is.Null);
            Assert.That(GhostBrain.PickTarget(GHOST, MakePac(9, 1f, 0f), [], VISION), Is.Null);
        }

        [Test]
        public void GrowFromDot_AddsGain()
        {
            Assert.That(PacRules.GrowFromDot(50f, 1f, MAX_SIZE), Is.EqualTo(51f));
            Assert.That(PacRules.GrowFromDot(PacRules.START_SIZE, 2.5f, MAX_SIZE), Is.EqualTo(2.5f));
        }

        [Test]
        public void GrowFromDot_StopsAtMax_AndLeavesBiggerPacsAlone()
        {
            Assert.That(PacRules.GrowFromDot(99.5f, 1f, MAX_SIZE), Is.EqualTo(MAX_SIZE));
            Assert.That(PacRules.GrowFromDot(MAX_SIZE, 1f, MAX_SIZE), Is.EqualTo(MAX_SIZE));
            Assert.That(PacRules.GrowFromDot(150f, 1f, MAX_SIZE), Is.EqualTo(150f));
        }

        [Test]
        public void WearOffPowerPill_TakesGainOff_NeverBelowStartSize()
        {
            Assert.That(PacRules.WearOffPowerPill(150f, 100f), Is.EqualTo(50f));
            Assert.That(PacRules.WearOffPowerPill(85f, 100f), Is.EqualTo(PacRules.START_SIZE));
        }

        [Test]
        public void Starve_TakesPerSecondTimesSecondsOff()
        {
            Assert.That(PacRules.Starve(60f, 3f, 1f), Is.EqualTo(57f).Within(0.0001f));
            Assert.That(PacRules.Starve(60f, 3f, 0.5f), Is.EqualTo(58.5f).Within(0.0001f));
        }

        [Test]
        public void Starve_NeverGoesBelowStartSize()
        {
            Assert.That(PacRules.Starve(PacRules.START_SIZE, 3f, 1f), Is.EqualTo(PacRules.START_SIZE));
            Assert.That(PacRules.Starve(2f, 3f, 1f), Is.EqualTo(PacRules.START_SIZE));
        }

        [TestCase(0, 1000, 0f)]
        [TestCase(250, 1000, 0.25f)]
        [TestCase(1000, 1000, 1f)]
        [TestCase(5000, 1000, 1f)]
        [TestCase(-10, 1000, 0f)]
        [TestCase(0, 0, 1f)]
        public void GetHungerFill_IsShareOfStarvationTimePassed_Clamped(int msSinceEaten, int msUntilStarvation, float expected)
        {
            Assert.That(PacRules.GetHungerFill(msSinceEaten, msUntilStarvation), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void GetSpeed_RisesFromBaseAtZeroToTwentyPercentFasterAtMax()
        {
            Assert.That(PacRules.GetSpeed(6f, 0f, 100f), Is.EqualTo(6f).Within(0.0001f));
            Assert.That(PacRules.GetSpeed(6f, 50f, 100f), Is.EqualTo(6.6f).Within(0.0001f));
            Assert.That(PacRules.GetSpeed(6f, 100f, 100f), Is.EqualTo(7.2f).Within(0.0001f));
        }

        [Test]
        public void GetEffectSpeedScale_OnlySugarRushIsFaster()
        {
            Assert.That(PacRules.GetEffectSpeedScale(ePowerUpEffect.SugarRush), Is.EqualTo(PacRules.SUGAR_RUSH_SPEED_SCALE));
            Assert.That(PacRules.SUGAR_RUSH_SPEED_SCALE, Is.GreaterThan(1f));
            Assert.That(PacRules.GetEffectSpeedScale(ePowerUpEffect.PowerPill), Is.EqualTo(1f));
            Assert.That(PacRules.GetEffectSpeedScale(ePowerUpEffect.None), Is.EqualTo(1f));
        }

        [Test]
        public void GetEffectSizeGain_PowerPillAndMelonBelly()
        {
            Assert.That(PacRules.GetEffectSizeGain(ePowerUpEffect.PowerPill, 100f), Is.EqualTo(100f));
            Assert.That(PacRules.GetEffectSizeGain(ePowerUpEffect.MelonBelly, 100f), Is.EqualTo(PacRules.MELON_BELLY_SIZE_GAIN));
            Assert.That(PacRules.GetEffectSizeGain(ePowerUpEffect.SugarRush, 100f), Is.EqualTo(0f));
            Assert.That(PacRules.GetEffectSizeGain(ePowerUpEffect.None, 100f), Is.EqualTo(0f));
        }

        [Test]
        public void GetDotGainScale_OnlySweetToothIsTriple()
        {
            Assert.That(PacRules.GetDotGainScale(ePowerUpEffect.SweetTooth), Is.EqualTo(3f));
            Assert.That(PacRules.GetDotGainScale(ePowerUpEffect.SugarRush), Is.EqualTo(1f));
            Assert.That(PacRules.GetDotGainScale(ePowerUpEffect.PowerPill), Is.EqualTo(1f));
            Assert.That(PacRules.GetDotGainScale(ePowerUpEffect.None), Is.EqualTo(1f));
        }

        [Test]
        public void PickTarget_NeverChasesPoweredPac()
        {
            var powered = MakePac(1, 2f, 0f);
            powered.PowerUpEffect = ePowerUpEffect.PowerPill;
            powered.PowerUpMs = 1000;
            var small = MakePac(2, 6f, 0f);

            Assert.That(GhostBrain.PickTarget(GHOST, null, [powered, small], VISION), Is.SameAs(small));
            Assert.That(GhostBrain.PickTarget(GHOST, powered, [powered], VISION), Is.Null, "drops a target that ate a power pill");
        }

        [Test]
        public void FindThreat_NearestPoweredPacInVision()
        {
            var unpowered = MakePac(1, 1f, 0f);
            unpowered.Size = MAX_SIZE;
            var powered = MakePac(2, 5f, 0f);
            powered.PowerUpEffect = ePowerUpEffect.PowerPill;
            powered.PowerUpMs = 1000;
            var poweredFar = MakePac(3, 20f, 0f);
            poweredFar.PowerUpEffect = ePowerUpEffect.PowerPill;
            poweredFar.PowerUpMs = 1000;

            Assert.That(GhostBrain.FindThreat(GHOST, [unpowered, powered, poweredFar], VISION), Is.SameAs(powered));
            Assert.That(GhostBrain.FindThreat(GHOST, [unpowered, poweredFar], VISION), Is.Null, "a big pac without a pill isn't a threat");
        }

        [Test]
        public void ChooseFleeDirection_RunsAwayFromThreat()
        {
            // a long corridor: a threat to the right sends the ghost left, and the other way round
            var map = MazeMovementTests.MakeMap(["#########", "#.......#", "#########"]);

            Assert.That(GhostBrain.ChooseFleeDirection(map, 4, 1, 6, 1), Is.EqualTo(eFacing.Left));
            Assert.That(GhostBrain.ChooseFleeDirection(map, 4, 1, 2, 1), Is.EqualTo(eFacing.Right));
        }

        [Test]
        public void GrowFromPowerPill_AddsGain()
        {
            Assert.That(PacRules.GrowFromPowerPill(PacRules.START_SIZE, 100f), Is.EqualTo(100f));
            Assert.That(PacRules.GrowFromPowerPill(50f, 100f), Is.EqualTo(150f), "can push a pac past the max size");
        }

        [Test]
        public void CanKillGhost_OnlyPoweredAndAtLeastMax()
        {
            Assert.That(PacRules.CanKillGhost(100f, true, 100f), Is.True);
            Assert.That(PacRules.CanKillGhost(150f, true, 100f), Is.True);
            Assert.That(PacRules.CanKillGhost(99.9f, true, 100f), Is.False, "starved below max while powered");
            Assert.That(PacRules.CanKillGhost(100f, false, 100f), Is.False, "full from dots alone");
        }

        [Test]
        public void IsTouchingPac_UsesBothSizes()
        {
            // two size 0 pacs are one cell wide, so they touch when centres are under one cell apart
            Assert.That(PacRules.IsTouchingPac(new Point2(0f, 0f), 0f, new Point2(0.9f, 0f), 0f), Is.True);
            Assert.That(PacRules.IsTouchingPac(new Point2(0f, 0f), 0f, new Point2(1.1f, 0f), 0f), Is.False);
            Assert.That(PacRules.IsTouchingPac(new Point2(0f, 0f), 100f, new Point2(1.4f, 0f), 0f), Is.True);
        }

        [Test]
        public void IsTouchingGhost_GhostIsOneCellWide()
        {
            Assert.That(PacRules.IsTouchingGhost(new Point2(0f, 0f), 0f, new Point2(0.9f, 0f)), Is.True);
            Assert.That(PacRules.IsTouchingGhost(new Point2(0f, 0f), 0f, new Point2(1.1f, 0f)), Is.False);
        }

        [Test]
        public void IsTouchingGhost_RainbowGhostIsTwoCellsWide()
        {
            var radius = PacRules.GetGhostRadius(isRainbow: true);

            Assert.That(radius, Is.EqualTo(PacRules.GHOST_RADIUS * 2f));
            Assert.That(PacRules.IsTouchingGhost(new Point2(0f, 0f), 0f, new Point2(1.4f, 0f), radius), Is.True);
            Assert.That(PacRules.IsTouchingGhost(new Point2(0f, 0f), 0f, new Point2(1.6f, 0f), radius), Is.False);
        }

        [Test]
        public void GetGhostRadius_NormalLevel_IsGhostRadius()
        {
            Assert.That(PacRules.GetGhostRadius(isRainbow: false), Is.EqualTo(PacRules.GHOST_RADIUS));
        }
    }
}
