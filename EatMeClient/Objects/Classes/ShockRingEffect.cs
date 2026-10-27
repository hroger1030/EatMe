using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    /// <summary>
    /// The ShockRing shader, set up for SpriteBatch: one visible shock ring (ShockRings), drawn as a square quad
    /// centred on where it went off. Each ring has its own size and colors, so SetRing must be called before each Begin.
    /// </summary>
    public class ShockRingEffect : Effect
    {
        private readonly EffectParameter _QuadPixels;
        private readonly EffectParameter _RadiusPixels;
        private readonly EffectParameter _WidthPixels;
        private readonly EffectParameter _CoreColor;
        private readonly EffectParameter _EdgeColor;

        public ShockRingEffect(Effect source) : base(source)
        {
            _QuadPixels = Parameters["QuadPixels"];
            _RadiusPixels = Parameters["RadiusPixels"];
            _WidthPixels = Parameters["WidthPixels"];
            _CoreColor = Parameters["CoreColor"];
            _EdgeColor = Parameters["EdgeColor"];
        }

        /// <summary>
        /// Sets the quad's size, the ring's radius and core width, all in screen pixels, and its colors.
        /// </summary>
        public void SetRing(float quadPixels, float radiusPixels, float widthPixels, Color core, Color edge)
        {
            _QuadPixels.SetValue(quadPixels);
            _RadiusPixels.SetValue(radiusPixels);
            _WidthPixels.SetValue(widthPixels);
            _CoreColor.SetValue(core.ToVector3());
            _EdgeColor.SetValue(edge.ToVector3());
        }
    }
}
