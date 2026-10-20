using System;
using System.Collections.Generic;
using FastG;

namespace EatMeLibrary
{
    /// <summary>
    /// Rules for power up effects shared by the server and the client's player state panel.
    /// </summary>
    public static class PowerUpEffects
    {
        /// <summary>
        /// The name shown on the player state panel for an effect, "None" when there isn't one.
        /// </summary>
        public static string GetName(ePowerUpEffect effect)
        {
            return effect switch
            {
                ePowerUpEffect.PowerPill => Const.STRING_POWER_PILL,
                ePowerUpEffect.SugarRush => Const.STRING_SUGAR_RUSH,
                ePowerUpEffect.SweetTooth => Const.STRING_SWEET_TOOTH,
                ePowerUpEffect.VitaminC => Const.STRING_VITAMIN_C,
                ePowerUpEffect.DoctorAway => Const.STRING_DOCTOR_AWAY,
                ePowerUpEffect.MelonBelly => Const.STRING_MELON_BELLY,
                ePowerUpEffect.RocketScience => Const.STRING_SCIENCE,
                ePowerUpEffect.DinnerBell => Const.STRING_DINNER_BELL,
                ePowerUpEffect.Lockdown => Const.STRING_LOCKDOWN,
                ePowerUpEffect.Twister => Const.STRING_TWISTER,
                ePowerUpEffect.Invincible => Const.STRING_INVINCIBLE,
                ePowerUpEffect.ExtraLife => Const.STRING_EXTRA_LIFE,
                ePowerUpEffect.Reinforcements => Const.STRING_REINFORCEMENTS,
                ePowerUpEffect.MorphBall => Const.STRING_MORPH_BALL,
                ePowerUpEffect.RaccoonTail => Const.STRING_RACCOON_TAIL,
                ePowerUpEffect.SwordSwing => Const.STRING_SWORD_SWING,
                ePowerUpEffect.HolyFire => Const.STRING_HOLY_FIRE,
                ePowerUpEffect.FireFlower => Const.STRING_FIREBALL,
                _ => Const.STRING_POWER_UP_NONE,
            };
        }

        /// <summary>
        /// A pac with Extra Life (1-up) that dies comes back this long after, where it died.
        /// </summary>
        public const int EXTRA_LIFE_RESPAWN_MS = 1000;

        /// <summary>
        /// True if a pac with this effect passes through enemy pacs and ghosts (no fights, no ghost contact either
        /// way): Morph Ball and Raccoon Tail.
        /// </summary>
        public static bool PassesThrough(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.MorphBall || effect == ePowerUpEffect.RaccoonTail;
        }

        /// <summary>
        /// True if a pac with this effect moves through walls too: Raccoon Tail.
        /// </summary>
        public static bool PassesThroughWalls(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.RaccoonTail;
        }

        /// <summary>
        /// A rolling (Morph Ball) pac turns this many full turns for every cell it moves.
        /// </summary>
        public const float ROLL_TURNS_PER_CELL = 0.75f;

        /// <summary>
        /// The angle (radians, 0 to 2 pi) a rolling pac at (x, y) is turned to: it turns ROLL_TURNS_PER_CELL for each
        /// cell it moves, so it only spins while moving.
        /// </summary>
        public static float GetRollAngle(float x, float y)
        {
            var turns = (x + y) * ROLL_TURNS_PER_CELL;
            return (turns - MathF.Floor(turns)) * MathF.PI * 2f;
        }

        /// <summary>
        /// A pac with Fireball (fire flower) flashes white this often.
        /// </summary>
        public const float FIRE_FLASH_SECONDS = 0.05f;

        /// <summary>
        /// True if a pac with Fireball is drawn white at a time in seconds (otherwise its normal color).
        /// </summary>
        public static bool IsFireFlashWhite(float seconds)
        {
            return seconds % (FIRE_FLASH_SECONDS * 2f) < FIRE_FLASH_SECONDS;
        }

        /// <summary>
        /// Every ghost moves this many times as fast while any living pac has Dinner Bell (bell).
        /// </summary>
        public const float DINNER_BELL_GHOST_SPEED_SCALE = 2f;

        /// <summary>
        /// The standard radius in cells of every power up effect that works on an area around the pac (IsInEffectRange):
        /// a square reaching this far horizontally and vertically from it. The POW block's stun, Twister's slow.
        /// </summary>
        public const float EFFECT_RADIUS = 15f;

        /// <summary>
        /// Everyone within EFFECT_RADIUS of a pac with Twister (pretzel), other than that pac, moves this many times as fast.
        /// </summary>
        public const float TWISTER_SPEED_SCALE = 0.5f;

        /// <summary>
        /// How long the POW block's stun lasts.
        /// </summary>
        public const int POW_STUN_MS = 3000;

        /// <summary>
        /// Full turns a second a stunned pac or ghost spins in place on the client.
        /// </summary>
        public const float STUN_SPIN_TURNS_PER_SECOND = 2f;

        /// <summary>
        /// True for area effect events that shake the screen of everyone inside the standard radius, whether or not
        /// the effect does anything else to them: the POW block.
        /// </summary>
        public static bool ShakesScreen(eGameEvent eventType)
        {
            return eventType == eGameEvent.Pow;
        }

        /// <summary>
        /// True if a position is inside the standard radius of an effect's centre: no more than EFFECT_RADIUS cells
        /// away horizontally and no more than EFFECT_RADIUS vertically (a square area).
        /// </summary>
        public static bool IsInEffectRange(Point2 from, Point2 to)
        {
            return MathF.Abs(to.X - from.X) <= EFFECT_RADIUS && MathF.Abs(to.Y - from.Y) <= EFFECT_RADIUS;
        }

        /// <summary>
        /// The angle (radians, 0 to 2 pi) a stunned pac or ghost is turned to at a time in seconds, spinning
        /// STUN_SPIN_TURNS_PER_SECOND clockwise.
        /// </summary>
        public static float GetStunSpinAngle(float seconds)
        {
            var turns = seconds * STUN_SPIN_TURNS_PER_SECOND;
            return (turns - MathF.Floor(turns)) * MathF.PI * 2f;
        }

        /// <summary>
        /// True if the pac has this effect working and is alive, so the effect counts on the rest of the level.
        /// </summary>
        public static bool HasActiveEffect(Player pac, ePowerUpEffect effect)
        {
            return pac.Alive && pac.PowerUpEffect == effect && pac.PowerUpMs > 0;
        }

        /// <summary>
        /// What every ghost's speed is multiplied by: DINNER_BELL_GHOST_SPEED_SCALE while any living pac has Dinner Bell
        /// (however many do), otherwise 1.
        /// </summary>
        public static float GetGhostSpeedScale(IEnumerable<Player> pacs)
        {
            foreach (var pac in pacs)
            {
                if (HasActiveEffect(pac, ePowerUpEffect.DinnerBell))
                    return DINNER_BELL_GHOST_SPEED_SCALE;
            }

            return 1f;
        }

        /// <summary>
        /// What the speed of a pac or ghost at position is multiplied by: TWISTER_SPEED_SCALE if it is within
        /// EFFECT_RADIUS of any living pac with Twister (pretzel) other than itself (however many), otherwise 1.
        /// </summary>
        public static float GetTwisterSpeedScale(GameEntity self, Point2 position, IEnumerable<Player> pacs)
        {
            foreach (var pac in pacs)
            {
                if (pac != self && HasActiveEffect(pac, ePowerUpEffect.Twister) && IsInEffectRange(pac.Location, position))
                    return TWISTER_SPEED_SCALE;
            }

            return 1f;
        }

        /// <summary>
        /// True if this pac keeps dead pacs of the given team from respawning: it is alive, on the other team, and has
        /// Lockdown (key) working.
        /// </summary>
        public static bool LocksSpawn(Player pac, eTeam team)
        {
            return pac.Team != team && HasActiveEffect(pac, ePowerUpEffect.Lockdown);
        }

        /// <summary>
        /// Points scored with Rocket Science (galaxian) are worth this many times as much.
        /// </summary>
        public const int ROCKET_SCIENCE_SCORE_SCALE = 2;

        /// <summary>
        /// True if an effect never runs out: its timer stays full and it lasts until the pac dies or another effect
        /// replaces it. Vitamin C, Doctor Away and Extra Life.
        /// </summary>
        public static bool IsPermanent(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.DoctorAway || effect == ePowerUpEffect.ExtraLife;
        }

        /// <summary>
        /// True if a pac with this effect never gets hungry: its time since eating stays at 0. Vitamin C and Melon Belly.
        /// </summary>
        public static bool PreventsStarving(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.MelonBelly;
        }

        /// <summary>
        /// What points scored by a pac with this effect are multiplied by: ROCKET_SCIENCE_SCORE_SCALE for Rocket Science,
        /// otherwise 1.
        /// </summary>
        public static int GetScoreScale(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.RocketScience ? ROCKET_SCIENCE_SCORE_SCALE : 1;
        }

        /// <summary>
        /// True if a pac with this effect leaves a speed trail (PacTrails): Sugar Rush and Invincible.
        /// </summary>
        public static bool HasSpeedTrail(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.SugarRush || effect == ePowerUpEffect.Invincible;
        }

        /// <summary>
        /// An invincible pac switches between its team color and yellow this often.
        /// </summary>
        public const float STAR_BLINK_SECONDS = 0.08f;

        /// <summary>
        /// True if an invincible pac is drawn yellow at a time in seconds (otherwise its team color): yellow for the
        /// first STAR_BLINK_SECONDS of every two.
        /// </summary>
        public static bool IsStarBlinkYellow(float seconds)
        {
            return seconds % (STAR_BLINK_SECONDS * 2f) < STAR_BLINK_SECONDS;
        }

        /// <summary>
        /// The player state panel's label for an effect: "Power Up: " and the effect's name, e.g. "Power Up: None".
        /// </summary>
        public static string GetLabel(ePowerUpEffect effect)
        {
            return $"{Const.STRING_POWER_UP} {GetName(effect)}";
        }

        /// <summary>
        /// How full the player state panel's meter is, 0 to 1: the share of durationMs still left. 0 when durationMs isn't positive.
        /// </summary>
        public static float GetFill(int msLeft, int durationMs)
        {
            if (durationMs <= 0)
                return 0f;

            return Math.Clamp((float)msLeft / durationMs, 0f, 1f);
        }
    }
}
