using EatMeLibrary;

namespace EatMeTests
{
    public class PowerUpEffectsTests
    {
        [TestCase(10000, 10000, 1f)]
        [TestCase(5000, 10000, 0.5f)]
        [TestCase(0, 10000, 0f)]
        [TestCase(-50, 10000, 0f)]
        [TestCase(20000, 10000, 1f)]
        [TestCase(5000, 0, 0f)]
        public void GetFill_IsShareOfDurationLeft_Clamped(int msLeft, int durationMs, float expected)
        {
            Assert.That(PowerUpEffects.GetFill(msLeft, durationMs), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void GetName_NoneAndPowerPill()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.None), Is.EqualTo(Const.STRING_POWER_UP_NONE));
            Assert.That(PowerUpEffects.GetName(ePowerUp.PowerPill), Is.EqualTo(Const.STRING_POWER_PILL));
        }

        [Test]
        public void GetName_CherryTwin()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.Cherry), Is.EqualTo(Const.STRING_CHERRY_TWIN));
        }

        [Test]
        public void GetName_SweetTooth()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.Strawberry), Is.EqualTo(Const.STRING_SWEET_TOOTH));
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.Strawberry), Is.False);
        }

        [Test]
        public void GetName_VitaminC()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.Orange), Is.EqualTo(Const.STRING_VITAMIN_C));
            Assert.That(PowerUpEffects.GetName(ePowerUp.Apple), Is.EqualTo(Const.STRING_DOCTOR_AWAY));
            Assert.That(PowerUpEffects.GetName(ePowerUp.Melon), Is.EqualTo(Const.STRING_MELON_ROLL));
            Assert.That(PowerUpEffects.GetName(ePowerUp.Solarian), Is.EqualTo(Const.STRING_WARP_SPEED));
        }

        private static Player MakePac(eTeam team, ePowerUp effect, bool alive = true)
        {
            return new Player { Team = team, Alive = alive, PowerUpEffect = effect, PowerUpMs = 1000 };
        }

        [Test]
        public void GetName_DinnerBellAndLockdown()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.Bell), Is.EqualTo(Const.STRING_DINNER_BELL));
            Assert.That(PowerUpEffects.GetName(ePowerUp.Key), Is.EqualTo(Const.STRING_LOCKDOWN));
        }

        [Test]
        public void GetGhostSpeedScale_DoubleWhileAnyLivingPacHasDinnerBell()
        {
            var bell = MakePac(eTeam.Red, ePowerUp.Bell);
            var plain = MakePac(eTeam.Blue, ePowerUp.None);

            Assert.That(PowerUpEffects.GetGhostSpeedScale([plain]), Is.EqualTo(1f));
            Assert.That(PowerUpEffects.GetGhostSpeedScale([plain, bell]), Is.EqualTo(PowerUpEffects.DINNER_BELL_GHOST_SPEED_SCALE));
            Assert.That(PowerUpEffects.GetGhostSpeedScale([bell, MakePac(eTeam.Blue, ePowerUp.Bell)]), Is.EqualTo(2f), "doesn't stack");
            Assert.That(PowerUpEffects.GetGhostSpeedScale([MakePac(eTeam.Red, ePowerUp.Bell, alive: false)]), Is.EqualTo(1f));
        }

        [Test]
        public void LocksSpawn_OnlyLivingEnemyWithLockdown()
        {
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUp.Key), eTeam.Blue), Is.True);
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUp.Key), eTeam.Red), Is.False);
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUp.Key, alive: false), eTeam.Blue), Is.False);
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUp.Bell), eTeam.Blue), Is.False);
        }

        [Test]
        public void IsInEffectRange_IsACircleOfEffectRadius()
        {
            var from = new FastG.Point2(10f, 10f);
            var radius = PowerUpEffects.EFFECT_RADIUS;
            var diagonal = radius / MathF.Sqrt(2f);

            Assert.That(PowerUpEffects.IsInEffectRange(from, from), Is.True);
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f + radius, 10f)), Is.True);
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f - diagonal + 0.1f, 10f + diagonal - 0.1f)), Is.True, "just inside on the diagonal");
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f - radius, 10f + radius)), Is.False, "the square's corners are outside");
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f + radius + 0.5f, 10f)), Is.False);
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f, 10f - radius - 0.5f)), Is.False);
        }

        [Test]
        public void GetTwisterSpeedScale_HalfWithinRadiusOfAnotherTwister()
        {
            var twister = MakePac(eTeam.Red, ePowerUp.Pretzel);
            twister.Location = new FastG.Point2(10f, 10f);
            var ghost = new Ghost();
            var near = new FastG.Point2(10f, 10f + PowerUpEffects.EFFECT_RADIUS);
            var far = new FastG.Point2(10f, 10f + PowerUpEffects.EFFECT_RADIUS + 1f);

            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, near, [twister]), Is.EqualTo(PowerUpEffects.TWISTER_SPEED_SCALE));
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, far, [twister]), Is.EqualTo(1f), "out of range");
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(twister, twister.Location, [twister]), Is.EqualTo(1f), "not the pac itself");

            var other = MakePac(eTeam.Blue, ePowerUp.Pretzel);
            other.Location = twister.Location;
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, near, [twister, other]), Is.EqualTo(PowerUpEffects.TWISTER_SPEED_SCALE), "doesn't stack");
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(twister, twister.Location, [twister, other]), Is.EqualTo(PowerUpEffects.TWISTER_SPEED_SCALE), "slowed by another pac's twister");

            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, near, [MakePac(eTeam.Red, ePowerUp.Cherry)]), Is.EqualTo(1f));
        }

        [Test]
        public void GetName_Twister()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.Pretzel), Is.EqualTo(Const.STRING_TWISTER));
        }

        [Test]
        public void Invincible_NameTrailAndRainbow()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUp.Starman), Is.EqualTo(Const.STRING_INVINCIBLE));
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.Starman), Is.True);
            Assert.That(StarRainbow.IsRainbow(ePowerUp.Starman), Is.True);
        }

        [Test]
        public void GetStunSpinAngle_TurnsAtSpinRate()
        {
            var quarterTurn = TimeSpan.FromSeconds(0.25 / PowerUpEffects.STUN_SPIN_TURNS_PER_SECOND);
            var fullTurn = TimeSpan.FromSeconds(1.0 / PowerUpEffects.STUN_SPIN_TURNS_PER_SECOND);

            Assert.That(PowerUpEffects.GetStunSpinAngle(TimeSpan.Zero), Is.EqualTo(0f));
            Assert.That(PowerUpEffects.GetStunSpinAngle(quarterTurn), Is.EqualTo(MathF.PI * 0.5f).Within(0.0001f));
            Assert.That(PowerUpEffects.GetStunSpinAngle(quarterTurn + fullTurn), Is.EqualTo(MathF.PI * 0.5f).Within(0.001f), "wraps every full turn");
        }

        [Test]
        public void GetMaxSize_OnlyEnergyTankDoubles()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
            {
                var expected = effect == ePowerUp.EnergyTank ? 200f : 100f;
                Assert.That(PowerUpEffects.GetMaxSize(effect, 100f), Is.EqualTo(expected), effect.ToString());
            }
        }

        [Test]
        public void GetEndSize_EnergyTank_OnlyShrinksPastMaxSize()
        {
            Assert.That(PowerUpEffects.GetEndSize(ePowerUp.EnergyTank, 180f, 100f), Is.EqualTo(100f));
            Assert.That(PowerUpEffects.GetEndSize(ePowerUp.EnergyTank, 60f, 100f), Is.EqualTo(60f));
            Assert.That(PowerUpEffects.GetEndSize(ePowerUp.Cherry, 180f, 100f), Is.Null);
        }

        [Test]
        public void GetEffectSpeedScale_EnergyTank()
        {
            Assert.That(PacRules.GetEffectSpeedScale(ePowerUp.EnergyTank), Is.EqualTo(PacRules.ENERGY_TANK_SPEED_SCALE));
            Assert.That(PacRules.ENERGY_TANK_SPEED_SCALE, Is.EqualTo(1.3f));
        }

        [Test]
        public void GetEffectSpeedScale_RollingThunderIsTwentyPercentFaster()
        {
            Assert.That(PacRules.GetEffectSpeedScale(ePowerUp.RollingThunder), Is.EqualTo(1.2f));
        }

        [Test]
        public void KillsGhostsInRange_OnlyDoctorAway()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
                Assert.That(PowerUpEffects.KillsGhostsInRange(effect), Is.EqualTo(effect == ePowerUp.Apple), effect.ToString());
        }

        [Test]
        public void PreventsStarving_VitaminCEnergyTankAndFullHeal()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
            {
                var expected = effect == ePowerUp.Orange || effect == ePowerUp.EnergyTank || effect == ePowerUp.HeartContainer;
                Assert.That(PowerUpEffects.PreventsStarving(effect), Is.EqualTo(expected), effect.ToString());
            }
        }

        [Test]
        public void HasSpeedTrail_WarpSpeedAndInvincible()
        {
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.Solarian), Is.True);
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.Starman), Is.True);
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.Cherry), Is.False);
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.PowerPill), Is.False);
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUp.None), Is.False);
        }

        [Test]
        public void GetLabel_IsTheName()
        {
            Assert.That(PowerUpEffects.GetLabel(ePowerUp.None), Is.EqualTo("No power up"));
            Assert.That(PowerUpEffects.GetLabel(ePowerUp.PowerPill), Is.EqualTo("Power Pill"));
        }

        [Test]
        public void EveryLabel_FitsThePanel()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
                Assert.That(PowerUpEffects.GetLabel(effect).Length, Is.LessThanOrEqualTo(PowerUpEffects.MAX_LABEL_LENGTH), effect.ToString());
        }

        [Test]
        public void EveryEffect_HasItsOwnName()
        {
            foreach (var effect in Enum.GetValues<ePowerUp>())
            {
                if (effect != ePowerUp.None)
                    Assert.That(PowerUpEffects.GetName(effect), Is.Not.EqualTo(Const.STRING_POWER_UP_NONE), effect.ToString());
            }
        }
    }
}
