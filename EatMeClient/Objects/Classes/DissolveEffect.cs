using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// The Dissolve shader, set up for SpriteBatch: a pac or ghost materialising as it spawns (SpawnDissolve), sharp
    /// bilinear sampled like PixelArtEffect. Each sprite has its own threshold, so SetSinceSpawn must be called before
    /// each Begin.
    /// </summary>
    public class DissolveEffect : Effect
    {
        private readonly EffectParameter _TextureSize;
        private readonly EffectParameter _Threshold;

        public DissolveEffect(Effect source) : base(source)
        {
            _TextureSize = Parameters["TextureSize"];
            _Threshold = Parameters["Threshold"];
        }

        /// <summary>
        /// Sets how far the sprite has materialised from how long ago it spawned.
        /// </summary>
        public void SetSinceSpawn(TimeSpan sinceSpawn)
        {
            _Threshold.SetValue(SpawnDissolve.GetThreshold(sinceSpawn));
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
