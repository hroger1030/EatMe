using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    public class AnimatedTexture : BaseObject
    {
        private int _AnimationFrame;
        private TimeSpan _AnimationFrameTime;

        public int Rows { get; set; }
        public int Columns { get; set; }

        /// <summary>
        /// Rows in the whole texture when the animation plays only some of them (0 means Rows), starting at StartRow.
        /// </summary>
        public int SheetRows { get; set; }
        public int StartRow { get; set; }
        public TimeSpan AnimationDuration { get; set; }
        public Color Tint { get; set; } = Color.White;
        public bool Loop { get; set; }

        public bool IsComplete => !Loop && _AnimationFrame >= Rows * Columns;

        public AnimatedTexture() { }

        public void Restart()
        {
            _AnimationFrame = 0;
            _AnimationFrameTime = TimeSpan.Zero;
        }

        public void Update(TimeSpan elapsed)
        {
            if (IsComplete)
                return;

            var frameCount = Rows * Columns;
            var frameDuration = AnimationDuration / frameCount;

            _AnimationFrameTime += elapsed;

            if (_AnimationFrameTime >= frameDuration)
            {
                _AnimationFrameTime -= frameDuration;
                _AnimationFrame++;

                if (_AnimationFrame >= frameCount && Loop)
                    _AnimationFrame = _AnimationFrame % frameCount;
            }

            // update the current sprite rectangle.
            var frameWidth = Texture.Width / Columns;
            var frameHeight = Texture.Height / (SheetRows > 0 ? SheetRows : Rows);

            var column = _AnimationFrame % Columns;
            var row = StartRow + _AnimationFrame / Columns;

            SpriteSource = new Rectangle(column * frameWidth, row * frameHeight, frameWidth, frameHeight);
        }

        /// <summary>
        /// Returns the world-space axis-aligned box around the current sprite frame, accounting for scale and rotation.
        /// </summary>
        public FastG.AARectangle GetBoundingBox()
        {
            var cos = MathF.Abs(MathF.Cos(Facing));
            var sin = MathF.Abs(MathF.Sin(Facing));
            var width = (SpriteSource.Width * cos + SpriteSource.Height * sin) * Scale;
            var height = (SpriteSource.Width * sin + SpriteSource.Height * cos) * Scale;

            return new FastG.AARectangle(Location, width, height);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            if (IsComplete)
                return;

            var origin = new Vector2(SpriteSource.Width * 0.5f, SpriteSource.Height * 0.5f);

            spriteBatch.Draw(Texture, drawPosition, SpriteSource, Tint, Facing, origin, Scale, SpriteEffects.None, 0f);
        }
    }
}
 