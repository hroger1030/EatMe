using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public static class ImageFactory
    {
        private const int DEATH_FRAME_COLUMNS = 11;
        private const int DEATH_FRAME_ROWS = 1;
        private const float DEATH_DURATION = 1.1f;

        /// <summary>
        /// The pac death animation, played once at a world location, drawn the given width in pixels and tinted.
        /// </summary>
        public static AnimatedTexture GeneratePlayerDeath(Texture2D texture, FastG.Point2 location, float width, Color tint)
        {
            var frameWidth = texture.Width / DEATH_FRAME_COLUMNS;
            var frameHeight = texture.Height / DEATH_FRAME_ROWS;

            var output = new AnimatedTexture()
            {
                Texture = texture,
                Rows = DEATH_FRAME_ROWS,
                Columns = DEATH_FRAME_COLUMNS,
                AnimationDuration = DEATH_DURATION,
                Scale = width / frameWidth,
                Tint = tint,
                Loop = false,
                Location = location,
                SpriteSource = new Rectangle(0, 0, frameWidth, frameHeight),
            };

            return output;
        }

        /// <summary>
        /// A copy of the white ghost sheet with its body in the given color; the outline and eyes are unchanged.
        /// </summary>
        public static Texture2D GenerateGhostSheet(Texture2D whiteSheet, Color bodyColor)
        {
            var pixels = new uint[whiteSheet.Width * whiteSheet.Height];
            whiteSheet.GetData(pixels);

            var output = new Texture2D(whiteSheet.GraphicsDevice, whiteSheet.Width, whiteSheet.Height);
            output.SetData(GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, ClientColors.ToFastColor(bodyColor)));

            return output;
        }

        /// <summary>
        /// The ghost sheet with its white body made transparent, leaving only the eyes, for dead ghosts (GhostEyes).
        /// </summary>
        public static Texture2D GenerateGhostEyesSheet(Texture2D whiteSheet)
        {
            var pixels = new uint[whiteSheet.Width * whiteSheet.Height];
            whiteSheet.GetData(pixels);

            var output = new Texture2D(whiteSheet.GraphicsDevice, whiteSheet.Width, whiteSheet.Height);
            output.SetData(GhostPalette.ReplaceColor(pixels, GhostPalette.BODY_COLOR, FastColor.TRANSPARENT));

            return output;
        }

        /// <summary>
        /// The ghost sheet with its white body filled with the rainbow gradient left to right across each frame,
        /// moved round by phase (0 to 1), for rainbow levels.
        /// </summary>
        public static Texture2D GenerateRainbowGhostSheet(Texture2D whiteSheet, int frameCount, float phase)
        {
            var pixels = new uint[whiteSheet.Width * whiteSheet.Height];
            whiteSheet.GetData(pixels);

            var output = new Texture2D(whiteSheet.GraphicsDevice, whiteSheet.Width, whiteSheet.Height);
            output.SetData(Rainbow.ReplaceWithGradient(pixels, whiteSheet.Width, whiteSheet.Width / frameCount, GhostPalette.BODY_COLOR, phase));

            return output;
        }
    }
}
