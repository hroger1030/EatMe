using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// The RainbowWall shader, set up for SpriteBatch: the walls, dots and power pills of a rainbow level, drawn white,
    /// colored by their world position with the flowing rainbow gradient (Rainbow.GetShade) and sharp bilinear sampled
    /// like PixelArtEffect. The shader has its own vertex shader, so SetView must give it the viewport's projection,
    /// where the world is and the time before each Begin.
    /// </summary>
    public class RainbowWallEffect : Effect
    {
        private readonly EffectParameter _MatrixTransform;
        private readonly EffectParameter _TextureSize;
        private readonly EffectParameter _WorldOffset;
        private readonly EffectParameter _Phase;

        public RainbowWallEffect(Effect source, int cellPixels) : base(source)
        {
            _MatrixTransform = Parameters["MatrixTransform"];
            _TextureSize = Parameters["TextureSize"];
            _WorldOffset = Parameters["WorldOffset"];
            _Phase = Parameters["Phase"];

            Parameters["InvCyclePixels"].SetValue(1f / (Rainbow.CYCLE_CELLS * cellPixels));

            var colors = new Vector3[Rainbow.COLORS.Length];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = ClientColors.ToColor(Rainbow.COLORS[i]).ToVector3();

            Parameters["Colors"].SetValue(colors);
        }

        /// <summary>
        /// Sets SpriteBatch's projection for the viewport, the world pixel position of its top left corner and how far
        /// the rainbow has flowed after a time.
        /// </summary>
        public void SetView(Viewport viewport, Vector2 worldOffset, TimeSpan time)
        {
            _MatrixTransform.SetValue(Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1));
            _WorldOffset.SetValue(worldOffset);
            _Phase.SetValue(Rainbow.GetFlowPhase(time));
        }

        protected override void OnApply()
        {
            // SpriteBatch binds each run's texture to slot 0 before applying the effect for it
            if (GraphicsDevice.Textures[0] is Texture2D texture)
                _TextureSize.SetValue(new Vector2(texture.Width, texture.Height));

            base.OnApply();
        }
    }
}
