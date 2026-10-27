using EatMeLibrary;

namespace EatMeTests
{
    /// <summary>
    /// The smaller board shader helpers: HeatHaze, BfgWarp, SpeedBlur, IrisWipe, SpawnDissolve,
    /// StarRainbow, ShockRings and EffectRange.
    /// </summary>
    public class BoardEffectsTests
    {
        private const float TOLERANCE = 0.0001f;

        [Test]
        public void HeatHaze_OnlyFlamesAndFireballs()
        {
            foreach (var kind in Enum.GetValues<eWeaponEffect>())
                Assert.That(HeatHaze.IsSource(kind), Is.EqualTo(kind is eWeaponEffect.HolyFlame or eWeaponEffect.Fireball), kind.ToString());
        }

        [Test]
        public void HeatHaze_FadesOutAtItsRadius()
        {
            Assert.That(HeatHaze.GetOffset(60f, 0f, 60f, 2.5f, 1f), Is.EqualTo((0f, 0f)));
            var (x, y) = HeatHaze.GetOffset(0f, 0f, 60f, 2.5f, MathF.PI / 2f);
            Assert.That(x, Is.EqualTo(2.5f).Within(TOLERANCE));
            Assert.That(y, Is.EqualTo(0f).Within(TOLERANCE));
        }

        [Test]
        public void HeatHaze_PhaseGoesOnceRoundEachCycle()
        {
            Assert.That(HeatHaze.GetPhase(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(HeatHaze.GetPhase(TimeSpan.FromMilliseconds(HeatHaze.CYCLE_MS / 2)), Is.EqualTo(MathF.PI).Within(TOLERANCE));
            Assert.That(HeatHaze.GetPhase(TimeSpan.FromMilliseconds(HeatHaze.CYCLE_MS)), Is.EqualTo(0f).Within(TOLERANCE));
        }

        [Test]
        public void BfgWarp_OnlyBfgShots()
        {
            foreach (var kind in Enum.GetValues<eWeaponEffect>())
                Assert.That(BfgWarp.IsSource(kind), Is.EqualTo(kind == eWeaponEffect.BfgShot), kind.ToString());
        }

        [Test]
        public void BfgWarp_NothingMovesAtOrPastItsRadius()
        {
            Assert.That(BfgWarp.GetOffset(60f, 0f, 60f, BfgWarp.SWIRL_RADIANS, BfgWarp.PINCH), Is.EqualTo((0f, 0f)));
            Assert.That(BfgWarp.GetOffset(0f, 90f, 60f, BfgWarp.SWIRL_RADIANS, BfgWarp.PINCH), Is.EqualTo((0f, 0f)));
            Assert.That(BfgWarp.GetOffset(10f, 10f, 0f, BfgWarp.SWIRL_RADIANS, BfgWarp.PINCH), Is.EqualTo((0f, 0f)));
        }

        [Test]
        public void BfgWarp_PinchPullsTowardTheCentre()
        {
            // with no swirl, a pixel inside shows what lies further out, so the picture is pulled in (the offset points in)
            var (x, y) = BfgWarp.GetOffset(30f, 0f, 60f, 0f, BfgWarp.PINCH);
            Assert.That(x, Is.EqualTo(-30f * BfgWarp.PINCH * 0.25f).Within(TOLERANCE));
            Assert.That(y, Is.EqualTo(0f).Within(TOLERANCE));
        }

        [Test]
        public void BfgWarp_SwirlTurnsWithoutPinch()
        {
            // a quarter of the way out the weight is 0.75 squared; turning keeps the distance
            var (x, y) = BfgWarp.GetOffset(15f, 0f, 60f, 1f, 0f);
            var angle = 0.75f * 0.75f;
            Assert.That(15f - x, Is.EqualTo(15f * MathF.Cos(angle)).Within(TOLERANCE));
            Assert.That(-y, Is.EqualTo(15f * MathF.Sin(angle)).Within(TOLERANCE));
        }

        [Test]
        public void BfgWarp_PinchPulsesBelowItsPeak()
        {
            for (var ms = 0; ms < BfgWarp.PULSE_MS * 2; ms += 13)
                Assert.That(BfgWarp.GetPinch(TimeSpan.FromMilliseconds(ms)),
                    Is.InRange(BfgWarp.PINCH * (1f - BfgWarp.PULSE_SHARE) - TOLERANCE, BfgWarp.PINCH + TOLERANCE));

            Assert.That(BfgWarp.GetPinch(TimeSpan.FromMilliseconds(BfgWarp.PULSE_MS / 4f)), Is.EqualTo(BfgWarp.PINCH).Within(TOLERANCE));
        }

        [Test]
        public void StarRainbow_OnlyInvincible()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
                Assert.That(StarRainbow.IsRainbow(effect), Is.EqualTo(effect == ePowerUp.Starman), effect.ToString());
        }

        [Test]
        public void StarRainbow_PhaseGoesOnceRoundEachCycle()
        {
            Assert.That(StarRainbow.GetPhase(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(StarRainbow.GetPhase(TimeSpan.FromMilliseconds(StarRainbow.CYCLE_MS / 2)), Is.EqualTo(0.5f).Within(TOLERANCE));
            Assert.That(StarRainbow.GetPhase(TimeSpan.FromMilliseconds(StarRainbow.CYCLE_MS)), Is.EqualTo(0f).Within(TOLERANCE));
        }

        [Test]
        public void StarRainbow_OneColorAtATimeRoundTheRainbow()
        {
            Assert.That(StarRainbow.GetColor(TimeSpan.Zero), Is.EqualTo(Rainbow.COLORS[0]));
            Assert.That(StarRainbow.GetColor(TimeSpan.FromMilliseconds(StarRainbow.CYCLE_MS / 7f)), Is.EqualTo(Rainbow.COLORS[1]));
            Assert.That(StarRainbow.GetColor(TimeSpan.FromMilliseconds(StarRainbow.CYCLE_MS)), Is.EqualTo(Rainbow.COLORS[0]));
        }

        [Test]
        public void SpeedBlur_OnlyForFastEffects()
        {
            Assert.That(SpeedBlur.GetStrength(ePowerUp.Solarian, false), Is.EqualTo(SpeedBlur.WARP_SPEED_STRENGTH));
            Assert.That(SpeedBlur.GetStrength(ePowerUp.RollingThunder, false), Is.EqualTo(SpeedBlur.ROLLING_THUNDER_STRENGTH));
            Assert.That(SpeedBlur.GetStrength(ePowerUp.Melon, true), Is.EqualTo(SpeedBlur.MELON_ROLL_STRENGTH));
            Assert.That(SpeedBlur.GetStrength(ePowerUp.Melon, false), Is.EqualTo(0f));
            Assert.That(SpeedBlur.GetStrength(ePowerUp.None, false), Is.EqualTo(0f));
        }

        [Test]
        public void SpeedBlur_SamplesRunFromThePixelTowardsThePac()
        {
            Assert.That(SpeedBlur.GetSampleScale(0, 0.1f), Is.EqualTo(1f));
            Assert.That(SpeedBlur.GetSampleScale(SpeedBlur.TAPS - 1, 0.1f), Is.EqualTo(0.9f).Within(TOLERANCE));
        }

        [Test]
        public void ShockRings_OnlyBlastsAndBfgBursts()
        {
            foreach (var eventType in Enum.GetValues<eGameEvent>())
                Assert.That(ShockRings.IsSource(eventType), Is.EqualTo(eventType is eGameEvent.Blast or eGameEvent.BfgBurst), eventType.ToString());
        }

        [Test]
        public void ShockRings_RaceOutToTheStandardRadius()
        {
            var half = TimeSpan.FromMilliseconds(ShockRings.DURATION_MS / 2);
            var end = TimeSpan.FromMilliseconds(ShockRings.DURATION_MS);

            Assert.That(ShockRings.GetRadiusCells(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(ShockRings.GetRadiusCells(half), Is.GreaterThan(PowerUpEffects.EFFECT_RADIUS * 0.5f), "fast at first");
            Assert.That(ShockRings.GetRadiusCells(end), Is.EqualTo(PowerUpEffects.EFFECT_RADIUS).Within(TOLERANCE));
            Assert.That(ShockRings.GetWidthPixels(TimeSpan.Zero), Is.EqualTo(ShockRings.START_WIDTH_PIXELS));
            Assert.That(ShockRings.GetWidthPixels(end), Is.EqualTo(ShockRings.END_WIDTH_PIXELS).Within(TOLERANCE));
        }

        [Test]
        public void ShockRings_FadeAwayByTheEnd()
        {
            Assert.That(ShockRings.GetAlpha(TimeSpan.Zero), Is.EqualTo(1f));
            Assert.That(ShockRings.GetAlpha(TimeSpan.FromMilliseconds(ShockRings.DURATION_MS / 2)), Is.GreaterThan(0.5f));
            Assert.That(ShockRings.GetAlpha(TimeSpan.FromMilliseconds(ShockRings.DURATION_MS)), Is.EqualTo(0f));
            Assert.That(ShockRings.GetAlpha(TimeSpan.FromMilliseconds(-1)), Is.EqualTo(0f));
        }

        [Test]
        public void ShockRings_CoreAndGlowFallOffFromTheLine()
        {
            Assert.That(ShockRings.GetBand(100f, 100f, 10f), Is.EqualTo(0f));
            Assert.That(ShockRings.GetBand(105f, 100f, 10f), Is.EqualTo(1f));
            Assert.That(ShockRings.GetCore(0f), Is.EqualTo(1f));
            Assert.That(ShockRings.GetCore(1f), Is.EqualTo(0f));
            Assert.That(ShockRings.GetGlow(0f), Is.EqualTo(1f));
            Assert.That(ShockRings.GetGlow(1f), Is.GreaterThan(0f), "the glow reaches past the core");
            Assert.That(ShockRings.GetGlow(ShockRings.GLOW_SPREAD), Is.EqualTo(0f));
            Assert.That(ShockRings.GetReachPixels(100f, 10f), Is.EqualTo(100f + 5f * ShockRings.GLOW_SPREAD));
        }

        [Test]
        public void ShockRings_KeepAtMostMaxAndDropFinished()
        {
            var rings = new ShockRings();

            for (var i = 0; i < ShockRings.MAX_RINGS + 2; i++)
                rings.Add(i, 0f, eGameEvent.Blast, 0);

            Assert.That(rings.Rings.Count, Is.EqualTo(ShockRings.MAX_RINGS));
            Assert.That(rings.Rings[0].X, Is.EqualTo(2f), "the oldest made way");

            rings.Update(ShockRings.DURATION_MS);
            Assert.That(rings.Rings, Is.Empty);
        }

        [Test]
        public void EffectRange_OnlyDoctorAwayAndTwister()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
                Assert.That(EffectRange.IsShown(effect), Is.EqualTo(effect is ePowerUp.Apple or ePowerUp.Pretzel), effect.ToString());
        }

        [Test]
        public void EffectRange_TurnsAtItsRate()
        {
            Assert.That(EffectRange.GetRotation(TimeSpan.Zero), Is.EqualTo(0f));
            var quarter = TimeSpan.FromSeconds(0.25 / EffectRange.TURNS_PER_SECOND);
            Assert.That(EffectRange.GetRotation(quarter), Is.EqualTo(MathF.PI * 0.5f).Within(TOLERANCE));
            var fullTurn = TimeSpan.FromSeconds(1.0 / EffectRange.TURNS_PER_SECOND);
            Assert.That(EffectRange.GetRotation(fullTurn + quarter), Is.EqualTo(MathF.PI * 0.5f).Within(TOLERANCE), "wraps after a full turn");
        }

        [Test]
        public void IrisWipe_OpensOverTheStartOfTheCountdown()
        {
            const long END_MS = 10000;
            const long START_MS = END_MS - LevelPhases.START_COUNTDOWN_MS;

            Assert.That(IrisWipe.GetOpenFraction(START_MS, END_MS, false, 0), Is.EqualTo(0f));
            Assert.That(IrisWipe.GetOpenFraction(START_MS + IrisWipe.IRIS_MS / 2, END_MS, false, 0), Is.EqualTo(0.5f).Within(TOLERANCE));
            Assert.That(IrisWipe.GetOpenFraction(START_MS + IrisWipe.IRIS_MS, END_MS, false, 0), Is.EqualTo(1f));
            Assert.That(IrisWipe.GetOpenFraction(END_MS + 60000, END_MS, false, 0), Is.EqualTo(1f));
        }

        [Test]
        public void IrisWipe_ClosesAtTheEndOfTheVictoryPause()
        {
            const long VICTORY_MS = 50000;

            Assert.That(IrisWipe.GetOpenFraction(VICTORY_MS, 0, true, VICTORY_MS), Is.EqualTo(1f));
            Assert.That(IrisWipe.GetOpenFraction(VICTORY_MS + LevelPhases.VICTORY_MS, 0, true, VICTORY_MS), Is.EqualTo(0f));
        }

        [Test]
        public void IrisWipe_OpenReachesTheFarthestCorner()
        {
            var radius = IrisWipe.GetRadius(1f, 30f, 40f, 100f, 100f);
            Assert.That(IrisWipe.GetCoverage(MathF.Sqrt(70f * 70f + 60f * 60f), radius), Is.EqualTo(1f));
            Assert.That(IrisWipe.GetRadius(0f, 30f, 40f, 100f, 100f), Is.EqualTo(0f));
            Assert.That(IrisWipe.GetCoverage(10f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void SpawnDissolve_MaterialisesOverSpawnInTime()
        {
            Assert.That(SpawnDissolve.GetThreshold(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(SpawnDissolve.GetThreshold(TimeSpan.FromMilliseconds(SpawnDissolve.SPAWN_IN_MS)), Is.EqualTo(1f + SpawnDissolve.EDGE_WIDTH).Within(TOLERANCE));
            Assert.That(SpawnDissolve.IsSpawning(TimeSpan.Zero), Is.True);
            Assert.That(SpawnDissolve.IsSpawning(TimeSpan.FromMilliseconds(SpawnDissolve.SPAWN_IN_MS)), Is.False);
            Assert.That(SpawnDissolve.IsSpawning(TimeSpan.MaxValue), Is.False);
        }

        [Test]
        public void SpawnDissolve_NothingShowsAtTheStartAndAllWithNoRimAtTheEnd()
        {
            var end = SpawnDissolve.GetThreshold(TimeSpan.FromMilliseconds(SpawnDissolve.SPAWN_IN_MS));

            for (var x = 0; x < 20; x++)
            {
                for (var y = 0; y < 20; y++)
                {
                    var noise = SpawnDissolve.GetNoise(x, y);
                    Assert.That(noise, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                    Assert.That(SpawnDissolve.IsVisible(noise, 0f), Is.False);
                    Assert.That(SpawnDissolve.IsVisible(noise, end), Is.True);
                    Assert.That(SpawnDissolve.IsEdge(noise, end), Is.False);
                }
            }
        }
    }
}
