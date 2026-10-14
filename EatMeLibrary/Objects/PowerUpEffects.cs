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
                ePowerUpEffect.RocketScience => Const.STRING_ROCKET_SCIENCE,
                ePowerUpEffect.DinnerBell => Const.STRING_DINNER_BELL,
                ePowerUpEffect.Lockdown => Const.STRING_LOCKDOWN,
                _ => Const.STRING_POWER_UP_NONE,
            };
        }

        /// <summary>
        /// Every ghost moves this many times as fast while any living pac has Dinner Bell (bell).
        /// </summary>
        public const float DINNER_BELL_GHOST_SPEED_SCALE = 2f;

        /// <summary>
        /// The POW block stuns everything within this many cells (straight line) of the pac that ate it.
        /// </summary>
        public const float POW_RADIUS = 15f;

        /// <summary>
        /// How long the POW block's stun lasts.
        /// </summary>
        public const int POW_STUN_MS = 3000;

        /// <summary>
        /// Full turns a second a stunned pac or ghost spins in place on the client.
        /// </summary>
        public const float STUN_SPIN_TURNS_PER_SECOND = 2f;

        /// <summary>
        /// True if a position is within POW_RADIUS of where the POW block went off.
        /// </summary>
        public static bool IsInPowRange(Point2 from, Point2 to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            return dx * dx + dy * dy <= POW_RADIUS * POW_RADIUS;
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
        /// replaces it. Vitamin C and Doctor Away.
        /// </summary>
        public static bool IsPermanent(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.VitaminC || effect == ePowerUpEffect.DoctorAway;
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
        /// True if a pac with this effect leaves a speed trail (PacTrails): only Sugar Rush.
        /// </summary>
        public static bool HasSpeedTrail(ePowerUpEffect effect)
        {
            return effect == ePowerUpEffect.SugarRush;
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
