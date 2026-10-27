using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// The StarRainbow shader, set up for SpriteBatch: an invincible pac drawn white and colored one rainbow color at
    /// a time (StarRainbow.GetColor), sharp bilinear sampled like PixelArtEffect. SetTime must be called before each Begin.
    /// </summary>
    public class StarRainbowEffect : Effect
    {
        private readonly EffectParameter _TextureSize;
        private readonly EffectParameter _Phase;

        public StarRainbowEffect(Effect source) : base(source)
        {
            _TextureSize = Parameters["TextureSize"];
            _Phase = Parameters["Phase"];

            var colors = new Vector3[Rainbow.COLORS.Length];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = ClientColors.ToColor(Rainbow.COLORS[i]).ToVector3();

            Parameters["Colors"].SetValue(colors);
        }

        /// <summary>
        /// Sets how far round the colors the rainbow has run after a time.
        /// </summary>
        public void SetTime(TimeSpan time)
        {
            _Phase.SetValue(StarRainbow.GetPhase(time));
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
