using System;

namespace EatMeClient
{
    public static class RandHelper
    {
        /// <summary>
        /// Returns a float in [0, 1), exclusive of 1.
        /// </summary>
        public static float UnitFloat() => Random.Shared.NextSingle();

        /// <summary>
        /// Returns an int in [0, max], inclusive.
        /// </summary>
        public static int Int(int max) => Random.Shared.Next(max + 1);

        /// <summary>
        /// Returns an int in [min, max], inclusive.
        /// </summary>
        public static int Int(int min, int max) => Random.Shared.Next(min, max + 1);
    }
}