using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The whirlpool circle (Const.IMAGE_EFFECT_RANGE) the client draws round a pac whose effect works on everything
    /// within the standard radius while it lasts, so players can see its reach: Doctor Away (apple) and Twister
    /// (pretzel). It is drawn PowerUpEffects.EFFECT_RADIUS round the pac, turning by GetRotation.
    /// </summary>
    public static class EffectRange
    {
        /// <summary>
        /// Full turns a second the circle spins.
        /// </summary>
        public const float TURNS_PER_SECOND = 1f;

        /// <summary>
        /// How opaque the circle is drawn, so the maze and everything on it shows through its arms.
        /// </summary>
        public const float ALPHA = 0.3f;

        /// <summary>
        /// True if a pac with this effect has its range drawn round it: Doctor Away and Twister.
        /// </summary>
        public static bool IsShown(ePowerUp effect)
        {
            return effect == ePowerUp.Apple || effect == ePowerUp.Pretzel;
        }

        /// <summary>
        /// How far round the circle has turned after a time, in radians, 0 to a full turn.
        /// </summary>
        public static float GetRotation(TimeSpan time)
        {
            var turns = time.TotalSeconds * TURNS_PER_SECOND;
            return (float)((turns - Math.Floor(turns)) * Math.PI * 2.0);
        }
    }
}
