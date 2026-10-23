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
        /// The name shown on the player state panel for an effect, "No power up" when there isn't one.
        /// </summary>
        public static string GetName(ePowerUp effect)
        {
            return effect switch
            {
                ePowerUp.PowerPill => Const.STRING_POWER_PILL,
                ePowerUp.Cherry => Const.STRING_CHERRY_TWIN,
                ePowerUp.Strawberry => Const.STRING_SWEET_TOOTH,
                ePowerUp.Orange => Const.STRING_VITAMIN_C,
                ePowerUp.Apple => Const.STRING_DOCTOR_AWAY,
                ePowerUp.Melon => Const.STRING_MELON_ROLL,
                ePowerUp.Galaxian => Const.STRING_WARP_SPEED,
                ePowerUp.Bell => Const.STRING_DINNER_BELL,
                ePowerUp.Key => Const.STRING_LOCKDOWN,
                ePowerUp.Pretzel => Const.STRING_TWISTER,
                ePowerUp.Starman => Const.STRING_INVINCIBLE,
                ePowerUp.Reinforcements => Const.STRING_REINFORCEMENTS,
                ePowerUp.Bomb => Const.STRING_BOMB,
                ePowerUp.SuperLeaf => Const.STRING_RACCOON_TAIL,
                ePowerUp.WhiteSword => Const.STRING_SWORD_SWING,
                ePowerUp.HolyWater => Const.STRING_HOLY_FIRE,
                ePowerUp.FireFlower => Const.STRING_FIREBALL,
                ePowerUp.Pear => Const.STRING_WARP,
                ePowerUp.Banana => Const.STRING_BANANA_PEEL,
                ePowerUp.Tetromino => Const.STRING_BLOCK_DROP,
                ePowerUp.Hammer => Const.STRING_HAMMER_TIME,
                ePowerUp.BFG => Const.STRING_BFG,
                ePowerUp.Mushroom => Const.STRING_SUPER_SIZE,
                ePowerUp.EnergyTank => Const.STRING_ENERGY_TANK,
                ePowerUp.ScrewAttack => Const.STRING_SCREW_ATTACK,
                ePowerUp.PepperShaker => Const.STRING_PEPPER,
                ePowerUp.SpreadGun => Const.STRING_SPREAD_SHOT,
                ePowerUp.Triforce => Const.STRING_FORCE_FIELD,
                ePowerUp.PowBlock => Const.STRING_POW,
                ePowerUp.HeartContainer => Const.STRING_FULL_HEAL,
                ePowerUp.PokeBall => Const.STRING_GOTCHA,
                ePowerUp.BlueShell => Const.STRING_BLUE_SHELL,
                _ => Const.STRING_POWER_UP_NONE,
            };
        }

        /// <summary>
        /// True if a pac with this effect passes through enemy pacs and ghosts (no fights, no ghost contact either
        /// way): Raccoon Tail.
        /// </summary>
        public static bool PassesThrough(ePowerUp effect)
        {
            return effect == ePowerUp.SuperLeaf;
        }

        /// <summary>
        /// True if a pac with this effect moves through walls: Raccoon Tail, and Super Size (which smashes them as it goes).
        /// </summary>
        public static bool PassesThroughWalls(ePowerUp effect)
        {
            return effect == ePowerUp.SuperLeaf || effect == ePowerUp.Mushroom;
        }

        /// <summary>
        /// True if a pac with this effect smashes every wall it moves into (never the outer wall) into a dot cell: Super Size.
        /// </summary>
        public static bool SmashesWalls(ePowerUp effect)
        {
            return effect == ePowerUp.Mushroom;
        }

        /// <summary>
        /// A pac's size while Super Size (mushroom) works. It goes back to PacRules.START_SIZE when the effect ends.
        /// </summary>
        public const float SUPER_SIZE = 300f;

        /// <summary>
        /// While Energy Tank works, dots can grow a pac to this many times PacMaxSize.
        /// </summary>
        public const float ENERGY_TANK_CAPACITY_SCALE = 2f;

        /// <summary>
        /// The size an effect sets the pac to when it starts (Super Size), or null if it doesn't set one.
        /// </summary>
        public static float? GetStartSize(ePowerUp effect)
        {
            return effect switch
            {
                ePowerUp.Mushroom => SUPER_SIZE,
                _ => null,
            };
        }

        /// <summary>
        /// The size an effect puts the pac back to when it ends (Super Size: PacRules.START_SIZE; Energy Tank: maxSize,
        /// the normal PacMaxSize, if the pac is bigger), or null if it leaves the size alone.
        /// </summary>
        public static float? GetEndSize(ePowerUp effect, float size, float maxSize)
        {
            return effect switch
            {
                ePowerUp.Mushroom => PacRules.START_SIZE,
                ePowerUp.EnergyTank => MathF.Min(size, maxSize),
                _ => null,
            };
        }

        /// <summary>
        /// The most dots (and the spawn) can grow a pac with this effect: maxSize (PacMaxSize) times
        /// ENERGY_TANK_CAPACITY_SCALE for Energy Tank, otherwise maxSize.
        /// </summary>
        public static float GetMaxSize(ePowerUp effect, float maxSize)
        {
            return effect == ePowerUp.EnergyTank ? maxSize * ENERGY_TANK_CAPACITY_SCALE : maxSize;
        }

        /// <summary>
        /// True if a pac with this effect kills any enemy pac or ghost it touches, and a ghost touching it doesn't kill
        /// it: Invincible, Screw Attack and Super Size. Invincible beats the other two, and weapons still kill them.
        /// </summary>
        public static bool KillsOnContact(ePowerUp effect)
        {
            return effect == ePowerUp.Starman || effect == ePowerUp.ScrewAttack || effect == ePowerUp.Mushroom;
        }

        /// <summary>
        /// True if a pac with this effect can eat dots and power pills: every effect but Screw Attack.
        /// </summary>
        public static bool CanEat(ePowerUp effect)
        {
            return effect != ePowerUp.ScrewAttack;
        }

        /// <summary>
        /// True if a pac with this effect can pick up an enemy flag: only with no effect at all (a power pill counts as one).
        /// </summary>
        public static bool CanPickUpFlag(ePowerUp effect)
        {
            return effect == ePowerUp.None;
        }

        /// <summary>
        /// True if this pac seals its team's flags in a force field, so enemies can't pick them up: it is alive and has
        /// Force Field (triforce) working.
        /// </summary>
        public static bool SealsFlags(Player pac)
        {
            return HasActiveEffect(pac, ePowerUp.Triforce);
        }

        /// <summary>
        /// True if a ghost touching a pac with this effect is stunned (STUN_MS) instead of killing it: Pepper.
        /// </summary>
        public static bool StunsGhosts(ePowerUp effect)
        {
            return effect == ePowerUp.PepperShaker;
        }

        /// <summary>
        /// How long a stun lasts unless the item says otherwise (banana peel, spread shot, pepper). Being stunned again
        /// starts it over.
        /// </summary>
        public const int STUN_MS = 2000;

        /// <summary>
        /// A pac with Fireball (fire flower) flashes white this often.
        /// </summary>
        public const int FIRE_FLASH_MS = 50;

        /// <summary>
        /// True if a pac with this effect flashes white (IsFireFlashWhite): Fireball and Screw Attack.
        /// </summary>
        public static bool FlashesWhite(ePowerUp effect)
        {
            return effect == ePowerUp.FireFlower || effect == ePowerUp.ScrewAttack;
        }

        /// <summary>
        /// A Screw Attack pac turns this many full turns for every cell it moves, so it spins fast, and only while moving.
        /// Kept well under half a turn per snapshot, or the spin looks like it runs backwards.
        /// </summary>
        public const float SCREW_TURNS_PER_CELL = 0.5f;

        /// <summary>
        /// The angle (radians, 0 to 2 pi) a Screw Attack pac at (x, y) is turned to: clockwise moving right or down and
        /// counter clockwise moving left or up.
        /// </summary>
        public static float GetScrewAngle(float x, float y)
        {
            var turns = (x + y) * SCREW_TURNS_PER_CELL;
            return (turns - MathF.Floor(turns)) * MathF.PI * 2f;
        }

        /// <summary>
        /// True if a flashing pac (FlashesWhite) is drawn white at a time (otherwise its normal color).
        /// </summary>
        public static bool IsFireFlashWhite(TimeSpan time)
        {
            return time.TotalMilliseconds % (FIRE_FLASH_MS * 2) < FIRE_FLASH_MS;
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
        /// the effect does anything else to them: a Blast (the POW block, a bomb).
        /// </summary>
        public static bool ShakesScreen(eGameEvent eventType)
        {
            return eventType == eGameEvent.Blast;
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
        /// The angle (radians, 0 to 2 pi) a stunned pac or ghost is turned to at a time, spinning
        /// STUN_SPIN_TURNS_PER_SECOND clockwise.
        /// </summary>
        public static float GetStunSpinAngle(TimeSpan time)
        {
            var turns = (float)time.TotalSeconds * STUN_SPIN_TURNS_PER_SECOND;
            return (turns - MathF.Floor(turns)) * MathF.PI * 2f;
        }

        /// <summary>
        /// True if the pac has this effect working and is alive, so the effect counts on the rest of the level.
        /// </summary>
        public static bool HasActiveEffect(Player pac, ePowerUp effect)
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
                if (HasActiveEffect(pac, ePowerUp.Bell))
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
                if (pac != self && HasActiveEffect(pac, ePowerUp.Pretzel) && IsInEffectRange(pac.Location, position))
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
            return pac.Team != team && HasActiveEffect(pac, ePowerUp.Key);
        }

        /// <summary>
        /// True if a pac with this effect kills every ghost within EFFECT_RADIUS each tick while it lasts. Doctor Away.
        /// </summary>
        public static bool KillsGhostsInRange(ePowerUp effect)
        {
            return effect == ePowerUp.Apple;
        }

        /// <summary>
        /// True if a pac with this effect never gets hungry: its time since eating stays at 0. Vitamin C, Energy Tank
        /// and Full Heal.
        /// </summary>
        public static bool PreventsStarving(ePowerUp effect)
        {
            return effect == ePowerUp.Orange || effect == ePowerUp.EnergyTank || effect == ePowerUp.HeartContainer;
        }

        /// <summary>
        /// True if a pac with this effect leaves a speed trail (PacTrails): Warp Speed and Invincible.
        /// </summary>
        public static bool HasSpeedTrail(ePowerUp effect)
        {
            return effect == ePowerUp.Galaxian || effect == ePowerUp.Starman;
        }

        /// <summary>
        /// An invincible pac switches between its team color and yellow this often.
        /// </summary>
        public const int STAR_BLINK_MS = 80;

        /// <summary>
        /// True if an invincible pac is drawn yellow at a time (otherwise its team color): yellow for the first
        /// STAR_BLINK_MS of every two.
        /// </summary>
        public static bool IsStarBlinkYellow(TimeSpan time)
        {
            return time.TotalMilliseconds % (STAR_BLINK_MS * 2) < STAR_BLINK_MS;
        }

        /// <summary>
        /// The most characters a player state panel label may have, so it fits the panel at 1920x1080 (196 pixels of
        /// text in JLS Space Gothic 30, about 16 pixels for an average letter).
        /// </summary>
        public const int MAX_LABEL_LENGTH = 12;

        /// <summary>
        /// The player state panel's label for an effect: just the effect's name, e.g. "Cherry Twin", or "No power up".
        /// </summary>
        public static string GetLabel(ePowerUp effect)
        {
            return GetName(effect);
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
