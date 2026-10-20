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
                ePowerUpEffect.Warp => Const.STRING_WARP,
                ePowerUpEffect.BananaPeel => Const.STRING_BANANA_PEEL,
                ePowerUpEffect.BlockDrop => Const.STRING_BLOCK_DROP,
                ePowerUpEffect.HammerTime => Const.STRING_HAMMER_TIME,
                ePowerUpEffect.Bfg => Const.STRING_BFG,
                ePowerUpEffect.SuperSize => Const.STRING_SUPER_SIZE,
                ePowerUpEffect.EnergyTank => Const.STRING_ENERGY_TANK,
                ePowerUpEffect.ScrewAttack => Const.STRING_SCREW_ATTACK,
                ePowerUpEffect.Pepper => Const.STRING_PEPPER,
                ePowerUpEffect.SpreadShot => Const.STRING_SPREAD_SHOT,
                ePowerUpEffect.ForceField => Const.STRING_FORCE_FIELD,
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
        /// True if a pac with this effect moves through walls: Raccoon Tail, and Super Size (which smashes them as it goes).
        /// </summary>
        public static bool PassesThroughWalls(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.RaccoonTail || effect == ePowerUpEffect.SuperSize;
        }

        /// <summary>
        /// True if a pac with this effect smashes every wall it moves into (never the outer wall) into a dot cell: Super Size.
        /// </summary>
        public static bool SmashesWalls(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.SuperSize;
        }

        /// <summary>
        /// A pac's size while Super Size (mushroom) works. It goes back to PacRules.START_SIZE when the effect ends.
        /// </summary>
        public const float SUPER_SIZE = 300f;

        /// <summary>
        /// A pac's size while Energy Tank works.
        /// </summary>
        public const float ENERGY_TANK_SIZE = 150f;

        /// <summary>
        /// The size an effect sets the pac to when it starts (Super Size, Energy Tank), or null if it doesn't set one.
        /// </summary>
        public static float? GetStartSize(ePowerUpEffect effect)
        {
            return effect switch
            {
                ePowerUpEffect.SuperSize => SUPER_SIZE,
                ePowerUpEffect.EnergyTank => ENERGY_TANK_SIZE,
                _ => null,
            };
        }

        /// <summary>
        /// The size an effect puts the pac back to when it ends (Super Size: PacRules.START_SIZE; Energy Tank, when another
        /// buff replaces it: maxSize, the normal PacMaxSize), or null if it leaves the size alone.
        /// </summary>
        public static float? GetEndSize(ePowerUpEffect effect, float maxSize)
        {
            return effect switch
            {
                ePowerUpEffect.SuperSize => PacRules.START_SIZE,
                ePowerUpEffect.EnergyTank => maxSize,
                _ => null,
            };
        }

        /// <summary>
        /// True if a pac with this effect kills any enemy pac or ghost it touches, and a ghost touching it doesn't kill
        /// it: Screw Attack. An invincible pac still beats it, and weapons still kill it.
        /// </summary>
        public static bool KillsOnContact(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.ScrewAttack;
        }

        /// <summary>
        /// True if a pac with this effect can eat dots and power pills: every effect but Screw Attack.
        /// </summary>
        public static bool CanEat(ePowerUpEffect effect)
        {
            return effect != ePowerUpEffect.ScrewAttack;
        }

        /// <summary>
        /// True if a pac with this effect can pick up an enemy flag: only with no effect at all (a power pill counts as one).
        /// </summary>
        public static bool CanPickUpFlag(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.None;
        }

        /// <summary>
        /// True if this pac seals its team's flags in a force field, so enemies can't pick them up: it is alive and has
        /// Force Field (triforce) working.
        /// </summary>
        public static bool SealsFlags(Player pac)
        {
            return HasActiveEffect(pac, ePowerUpEffect.ForceField);
        }

        /// <summary>
        /// True if a ghost touching a pac with this effect is stunned (STUN_MS) instead of killing it: Pepper.
        /// </summary>
        public static bool StunsGhosts(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.Pepper;
        }

        /// <summary>
        /// How long a stun lasts unless the item says otherwise (banana peel, spread shot, pepper). Being stunned again
        /// starts it over.
        /// </summary>
        public const int STUN_MS = 2000;

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
        /// True if a pac with this effect flashes white (IsFireFlashWhite): Fireball and Screw Attack.
        /// </summary>
        public static bool FlashesWhite(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.FireFlower || effect == ePowerUpEffect.ScrewAttack;
        }

        /// <summary>
        /// A Screw Attack pac turns this many full turns for every cell it moves, so it spins very fast, and only while moving.
        /// </summary>
        public const float SCREW_TURNS_PER_CELL = 2f;

        /// <summary>
        /// The angle (radians, 0 to 2 pi) a Screw Attack pac at (x, y) is turned to.
        /// </summary>
        public static float GetScrewAngle(float x, float y)
        {
            var turns = (x + y) * SCREW_TURNS_PER_CELL;
            return (turns - MathF.Floor(turns)) * MathF.PI * 2f;
        }

        /// <summary>
        /// True if a flashing pac (FlashesWhite) is drawn white at a time in seconds (otherwise its normal color).
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
        /// True for events that shake every player's screen, wherever they are: Super Size smashing walls.
        /// </summary>
        public static bool ShakesEveryScreen(eGameEvent eventType)
        {
            return eventType == eGameEvent.WallSmash;
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
        /// replaces it. Vitamin C, Doctor Away, Extra Life and Energy Tank.
        /// </summary>
        public static bool IsPermanent(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.DoctorAway || effect == ePowerUpEffect.ExtraLife
                || effect == ePowerUpEffect.EnergyTank;
        }

        /// <summary>
        /// True if a pac with this effect never gets hungry: its time since eating stays at 0. Vitamin C, Melon Belly and
        /// Energy Tank (so it stays ENERGY_TANK_SIZE).
        /// </summary>
        public static bool PreventsStarving(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.MelonBelly || effect == ePowerUpEffect.EnergyTank;
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
