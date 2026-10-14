using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// The PixelArt shader (sharp bilinear filtering), set up for SpriteBatch. The shader needs the size of the
    /// texture it is drawing, which changes from sprite to sprite, so before each run of sprites SpriteBatch draws
    /// it reads the texture SpriteBatch has bound and sets TextureSize from it.
    /// </summary>
    public class PixelArtEffect : Effect
    {
        private readonly EffectParameter _TextureSize;

        public PixelArtEffect(Effect source) : base(source)
        {
            _TextureSize = Parameters["TextureSize"];
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
