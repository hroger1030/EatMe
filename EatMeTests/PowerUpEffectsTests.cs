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
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.None), Is.EqualTo(Const.STRING_POWER_UP_NONE));
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.PowerPill), Is.EqualTo(Const.STRING_POWER_PILL));
        }

        [Test]
        public void GetName_SugarRush()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.SugarRush), Is.EqualTo(Const.STRING_SUGAR_RUSH));
        }

        [Test]
        public void GetName_SweetTooth()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.SweetTooth), Is.EqualTo(Const.STRING_SWEET_TOOTH));
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUpEffect.SweetTooth), Is.False);
        }

        [Test]
        public void GetName_VitaminC()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.VitaminC), Is.EqualTo(Const.STRING_VITAMIN_C));
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.DoctorAway), Is.EqualTo(Const.STRING_DOCTOR_AWAY));
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.MelonBelly), Is.EqualTo(Const.STRING_MELON_BELLY));
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.RocketScience), Is.EqualTo(Const.STRING_SCIENCE));
        }

        private static Player MakePac(eTeam team, ePowerUpEffect effect, bool alive = true)
        {
            return new Player { Team = team, Alive = alive, PowerUpEffect = effect, PowerUpMs = 1000 };
        }

        [Test]
        public void GetName_DinnerBellAndLockdown()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.DinnerBell), Is.EqualTo(Const.STRING_DINNER_BELL));
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.Lockdown), Is.EqualTo(Const.STRING_LOCKDOWN));
        }

        [Test]
        public void GetGhostSpeedScale_DoubleWhileAnyLivingPacHasDinnerBell()
        {
            var bell = MakePac(eTeam.Red, ePowerUpEffect.DinnerBell);
            var plain = MakePac(eTeam.Blue, ePowerUpEffect.None);

            Assert.That(PowerUpEffects.GetGhostSpeedScale([plain]), Is.EqualTo(1f));
            Assert.That(PowerUpEffects.GetGhostSpeedScale([plain, bell]), Is.EqualTo(PowerUpEffects.DINNER_BELL_GHOST_SPEED_SCALE));
            Assert.That(PowerUpEffects.GetGhostSpeedScale([bell, MakePac(eTeam.Blue, ePowerUpEffect.DinnerBell)]), Is.EqualTo(2f), "doesn't stack");
            Assert.That(PowerUpEffects.GetGhostSpeedScale([MakePac(eTeam.Red, ePowerUpEffect.DinnerBell, alive: false)]), Is.EqualTo(1f));
        }

        [Test]
        public void LocksSpawn_OnlyLivingEnemyWithLockdown()
        {
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUpEffect.Lockdown), eTeam.Blue), Is.True);
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUpEffect.Lockdown), eTeam.Red), Is.False);
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUpEffect.Lockdown, alive: false), eTeam.Blue), Is.False);
            Assert.That(PowerUpEffects.LocksSpawn(MakePac(eTeam.Red, ePowerUpEffect.DinnerBell), eTeam.Blue), Is.False);
        }

        [Test]
        public void IsInEffectRange_IsASquareOfEffectRadius()
        {
            var from = new FastG.Point2(10f, 10f);
            var radius = PowerUpEffects.EFFECT_RADIUS;

            Assert.That(PowerUpEffects.IsInEffectRange(from, from), Is.True);
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f + radius, 10f)), Is.True);
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f - radius, 10f + radius)), Is.True, "the corners are inside");
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f + radius + 0.5f, 10f)), Is.False);
            Assert.That(PowerUpEffects.IsInEffectRange(from, new FastG.Point2(10f, 10f - radius - 0.5f)), Is.False);
        }

        [Test]
        public void GetTwisterSpeedScale_HalfWithinRadiusOfAnotherTwister()
        {
            var twister = MakePac(eTeam.Red, ePowerUpEffect.Twister);
            twister.Location = new FastG.Point2(10f, 10f);
            var ghost = new Ghost();
            var near = new FastG.Point2(10f, 10f + PowerUpEffects.EFFECT_RADIUS);
            var far = new FastG.Point2(10f, 10f + PowerUpEffects.EFFECT_RADIUS + 1f);

            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, near, [twister]), Is.EqualTo(PowerUpEffects.TWISTER_SPEED_SCALE));
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, far, [twister]), Is.EqualTo(1f), "out of range");
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(twister, twister.Location, [twister]), Is.EqualTo(1f), "not the pac itself");

            var other = MakePac(eTeam.Blue, ePowerUpEffect.Twister);
            other.Location = twister.Location;
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, near, [twister, other]), Is.EqualTo(PowerUpEffects.TWISTER_SPEED_SCALE), "doesn't stack");
            Assert.That(PowerUpEffects.GetTwisterSpeedScale(twister, twister.Location, [twister, other]), Is.EqualTo(PowerUpEffects.TWISTER_SPEED_SCALE), "slowed by another pac's twister");

            Assert.That(PowerUpEffects.GetTwisterSpeedScale(ghost, near, [MakePac(eTeam.Red, ePowerUpEffect.SugarRush)]), Is.EqualTo(1f));
        }

        [Test]
        public void GetName_Twister()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.Twister), Is.EqualTo(Const.STRING_TWISTER));
        }

        [Test]
        public void Invincible_NameTrailAndBlink()
        {
            Assert.That(PowerUpEffects.GetName(ePowerUpEffect.Invincible), Is.EqualTo(Const.STRING_INVINCIBLE));
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUpEffect.Invincible), Is.True);

            Assert.That(PowerUpEffects.IsStarBlinkYellow(0f), Is.True);
            Assert.That(PowerUpEffects.IsStarBlinkYellow(PowerUpEffects.STAR_BLINK_SECONDS * 1.5f), Is.False);
            Assert.That(PowerUpEffects.IsStarBlinkYellow(PowerUpEffects.STAR_BLINK_SECONDS * 2.5f), Is.True);
        }

        [Test]
        public void GetStunSpinAngle_TurnsAtSpinRate()
        {
            var quarterTurn = 0.25f / PowerUpEffects.STUN_SPIN_TURNS_PER_SECOND;

            Assert.That(PowerUpEffects.GetStunSpinAngle(0f), Is.EqualTo(0f));
            Assert.That(PowerUpEffects.GetStunSpinAngle(quarterTurn), Is.EqualTo(MathF.PI * 0.5f).Within(0.0001f));
            Assert.That(PowerUpEffects.GetStunSpinAngle(quarterTurn + 1f / PowerUpEffects.STUN_SPIN_TURNS_PER_SECOND), Is.EqualTo(MathF.PI * 0.5f).Within(0.001f), "wraps every full turn");
        }

        [Test]
        public void IsPermanent_VitaminCDoctorAwayAndEnergyTank()
        {
            foreach (var effect in Enum.GetValues<ePowerUpEffect>())
            {
                var expected = effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.DoctorAway || effect == ePowerUpEffect.EnergyTank;
                Assert.That(PowerUpEffects.IsPermanent(effect), Is.EqualTo(expected), effect.ToString());
            }
        }

        [Test]
        public void PreventsStarving_VitaminCMelonBellyAndEnergyTank()
        {
            foreach (var effect in Enum.GetValues<ePowerUpEffect>())
            {
                var expected = effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.MelonBelly || effect == ePowerUpEffect.EnergyTank;
                Assert.That(PowerUpEffects.PreventsStarving(effect), Is.EqualTo(expected), effect.ToString());
            }
        }

        [Test]
        public void GetScoreScale_OnlyRocketScienceDoubles()
        {
            foreach (var effect in Enum.GetValues<ePowerUpEffect>())
            {
                var expected = effect == ePowerUpEffect.RocketScience ? 2 : 1;
                Assert.That(PowerUpEffects.GetScoreScale(effect), Is.EqualTo(expected), effect.ToString());
            }
        }

        [Test]
        public void HasSpeedTrail_OnlySugarRush()
        {
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUpEffect.SugarRush), Is.True);
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUpEffect.PowerPill), Is.False);
            Assert.That(PowerUpEffects.HasSpeedTrail(ePowerUpEffect.None), Is.False);
        }

        [Test]
        public void GetLabel_IsTheName()
        {
            Assert.That(PowerUpEffects.GetLabel(ePowerUpEffect.None), Is.EqualTo("No power up"));
            Assert.That(PowerUpEffects.GetLabel(ePowerUpEffect.PowerPill), Is.EqualTo("Power Pill"));
        }

        [Test]
        public void EveryLabel_FitsThePanel()
        {
            foreach (var effect in Enum.GetValues<ePowerUpEffect>())
                Assert.That(PowerUpEffects.GetLabel(effect).Length, Is.LessThanOrEqualTo(PowerUpEffects.MAX_LABEL_LENGTH), effect.ToString());
        }

        [Test]
        public void EveryEffect_HasItsOwnName()
        {
            foreach (var effect in Enum.GetValues<ePowerUpEffect>())
            {
                if (effect != ePowerUpEffect.None)
                    Assert.That(PowerUpEffects.GetName(effect), Is.Not.EqualTo(Const.STRING_POWER_UP_NONE), effect.ToString());
            }
        }
    }
}
