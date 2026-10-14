using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Recolors the white ghost sheet so one sprite serves every ghost color.
    /// </summary>
    public static class GhostPalette
    {
        /// <summary>
        /// The ghost sheet's body color; only pixels exactly this color are recolored, so the outline and eyes keep theirs.
        /// </summary>
        public static readonly FastColor BODY_COLOR = FastColor.FromRgb(255, 255, 255);

        // Random ghost colors are rich: any hue, strongly saturated and bright, so they stand out on the dark board.
        public const float MIN_SATURATION = 0.75f;
        public const float MIN_VALUE = 0.7f;

        /// <summary>
        /// A random rich color: any hue, saturation MIN_SATURATION to 1 and value MIN_VALUE to 1.
        /// </summary>
        public static FastColor GetRandomColor(Random random)
        {
            var hue = (float)random.NextDouble() * 360f;
            var saturation = MIN_SATURATION + ((float)random.NextDouble() * (1f - MIN_SATURATION));
            var value = MIN_VALUE + ((float)random.NextDouble() * (1f - MIN_VALUE));

            return FastColor.FromHsv(hue, saturation, value);
        }

        /// <summary>
        /// A ghost's color for a level, the same on every client: GetRandomColor from a Random seeded with the level's
        /// map seed (Levels.GetSeed) and the ghost's id, so no color needs to be sent.
        /// </summary>
        public static FastColor GetColor(int levelSeed, int ghostId)
        {
            return GetRandomColor(new Random(GetColorSeed(levelSeed, ghostId)));
        }

        /// <summary>
        /// Seed for a ghost's color. Mixed rather than added, since level seeds are consecutive (server seed plus level)
        /// and adding the id would hand the next level's ghost 0 this level's ghost 1 color.
        /// </summary>
        public static int GetColorSeed(int levelSeed, int ghostId)
        {
            return unchecked((levelSeed * 397) ^ ghostId);
        }

        /// <summary>
        /// A copy of the pixels (packed like FastColor) with every pixel exactly <paramref name="from"/> set to
        /// <paramref name="to"/>. Every other pixel, transparent ones included, is copied unchanged.
        /// </summary>
        public static uint[] ReplaceColor(uint[] pixels, FastColor from, FastColor to)
        {
            var result = new uint[pixels.Length];

            for (var i = 0; i < pixels.Length; i++)
                result[i] = pixels[i] == from.PackedValue ? to.PackedValue : pixels[i];

            return result;
        }
    }
}
