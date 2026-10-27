using EatMeLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EatMeClient
{
    /// <summary>
    /// The game board's own render targets and full-board shader passes. MultiPlayerGameScreen draws the board into
    /// BoardTarget, everything in front of the maze into FrontTarget (its glowing things into GlowTarget), then this blurs the glow into a tight and a wide halo and adds them over
    /// the maze (the neon bloom), draws FrontTarget over the maze, moves its pixels (BoardDistort.fx: shockwaves, heat haze, the stun wobble) and finally draws it into
    /// the board's viewport on the frame through BoardGrade.fx (speed blur, chromatic kick, state grade, iris wipe). Every
    /// target is the board's size, so the bars, mini map and panels are never touched.
    /// </summary>
    public class BoardPostProcess
    {
        /// <summary>
        /// Times each halo is blurred each way; more makes it wider and softer.
        /// </summary>
        private const int TIGHT_BLUR_PASSES = 2;
        private const int WIDE_BLUR_PASSES = 3;

        /// <summary>
        /// Adds straight onto the target. BlendState.Additive would also multiply by the source's alpha.
        /// </summary>
        public static readonly BlendState BLEND_ADD = new()
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private readonly GraphicsDevice _Device;
        private readonly Effect _Bloom;
        private readonly Effect _Distort;
        private readonly Effect _Grade;

        private RenderTarget2D _BoardTarget;
        private RenderTarget2D _GlowTarget;
        private RenderTarget2D _FrontTarget;
        private RenderTarget2D _DistortTarget;
        private RenderTarget2D _TightTarget;
        private RenderTarget2D _TightBlurTarget;
        private RenderTarget2D _WideTarget;
        private RenderTarget2D _WideBlurTarget;

        /// <summary>
        /// The board as drawn, its pixels not yet moved.
        /// </summary>
        public RenderTarget2D BoardTarget => _BoardTarget;

        /// <summary>
        /// The board's glowing things on black, blurred into the bloom.
        /// </summary>
        public RenderTarget2D GlowTarget => _GlowTarget;

        /// <summary>
        /// Everything in front of the maze (items, deaths, pacs, ghosts, names, effects) on transparent, drawn over the
        /// maze, the bloom and the neon core by DrawFront.
        /// </summary>
        public RenderTarget2D FrontTarget => _FrontTarget;

        /// <summary>
        /// Each shockwave ring for Distort: centre, radius and amplitude in board pixels; amplitude 0 is no ring.
        /// </summary>
        public Vector4[] ShockwaveValues { get; } = new Vector4[Shockwaves.MAX_SHOCKWAVES];

        /// <summary>
        /// Each heat haze source for Distort: centre, radius and amplitude in board pixels; amplitude 0 is none.
        /// </summary>
        public Vector4[] HazeValues { get; } = new Vector4[HeatHaze.MAX_SOURCES];

        /// <summary>
        /// Each BFG shot's warp for Distort: centre and radius in board pixels and its pinch; radius 0 is none.
        /// </summary>
        public Vector4[] WarpValues { get; } = new Vector4[BfgWarp.MAX_SOURCES];

        public BoardPostProcess(GraphicsDevice device, Effect bloom, Effect distort, Effect grade)
        {
            _Device = device;
            _Bloom = bloom;
            _Distort = distort;
            _Grade = grade;

            _Bloom.Parameters["Weights"].SetValue(DisplayAdjust.GetBlurWeights());
        }

        /// <summary>
        /// Makes the targets the board's size, remaking them whenever it changes.
        /// </summary>
        public void Resize(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            if (_BoardTarget != null && _BoardTarget.Width == width && _BoardTarget.Height == height)
                return;

            MakeTarget(ref _BoardTarget, width, height);
            MakeTarget(ref _GlowTarget, width, height);
            MakeTarget(ref _FrontTarget, width, height);
            MakeTarget(ref _DistortTarget, width, height);
            MakeTarget(ref _TightTarget, width / 2, height / 2);
            MakeTarget(ref _TightBlurTarget, width / 2, height / 2);
            MakeTarget(ref _WideTarget, width / 4, height / 4);
            MakeTarget(ref _WideBlurTarget, width / 4, height / 4);
        }

        private void MakeTarget(ref RenderTarget2D target, int width, int height)
        {
            target?.Dispose();
            target = new RenderTarget2D(_Device, Math.Max(1, width), Math.Max(1, height), false, SurfaceFormat.Color, DepthFormat.None);
        }

        /// <summary>
        /// Blurs GlowTarget into the tight halo (half size) and the wide one (quarter size, from the tight one).
        /// </summary>
        public void BlurGlow(SpriteBatch spriteBatch)
        {
            Copy(spriteBatch, _GlowTarget, _TightTarget);
            Blur(spriteBatch, _TightTarget, _TightBlurTarget, TIGHT_BLUR_PASSES);

            Copy(spriteBatch, _TightTarget, _WideTarget);
            Blur(spriteBatch, _WideTarget, _WideBlurTarget, WIDE_BLUR_PASSES);
        }

        /// <summary>
        /// A plain linear copy, which scales it down to the destination's size.
        /// </summary>
        private void Copy(SpriteBatch spriteBatch, Texture2D source, RenderTarget2D destination)
        {
            _Device.SetRenderTarget(destination);
            spriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.LinearClamp);
            spriteBatch.Draw(source, destination.Bounds, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Blurs a target each way passes times, through spare (the same size), ending back in target.
        /// </summary>
        private void Blur(SpriteBatch spriteBatch, RenderTarget2D target, RenderTarget2D spare, int passes)
        {
            _Bloom.CurrentTechnique = _Bloom.Techniques["Blur"];

            for (var i = 0; i < passes; i++)
            {
                _Bloom.Parameters["TexelStep"].SetValue(new Vector2(1f / target.Width, 0f));
                DrawThrough(spriteBatch, target, spare, _Bloom);

                _Bloom.Parameters["TexelStep"].SetValue(new Vector2(0f, 1f / target.Height));
                DrawThrough(spriteBatch, spare, target, _Bloom);
            }
        }

        /// <summary>
        /// Draws one texture over the whole of another target through an effect.
        /// </summary>
        private void DrawThrough(SpriteBatch spriteBatch, Texture2D source, RenderTarget2D destination, Effect effect)
        {
            _Device.SetRenderTarget(destination);
            spriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.LinearClamp, effect: effect);
            spriteBatch.Draw(source, destination.Bounds, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Adds the tight and wide halos (BlurGlow) over the target being drawn, tightGain and wideGain times over.
        /// </summary>
        public void AddBloom(SpriteBatch spriteBatch, float tightGain, float wideGain)
        {
            var bounds = new Rectangle(0, 0, _BoardTarget.Width, _BoardTarget.Height);
            _Bloom.CurrentTechnique = _Bloom.Techniques["Combine"];

            _Bloom.Parameters["Gain"].SetValue(tightGain);
            spriteBatch.Begin(blendState: BLEND_ADD, samplerState: SamplerState.LinearClamp, effect: _Bloom);
            spriteBatch.Draw(_TightTarget, bounds, Color.White);
            spriteBatch.End();

            _Bloom.Parameters["Gain"].SetValue(wideGain);
            spriteBatch.Begin(blendState: BLEND_ADD, samplerState: SamplerState.LinearClamp, effect: _Bloom);
            spriteBatch.Draw(_WideTarget, bounds, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Draws FrontTarget over the target being drawn, pixel for pixel.
        /// </summary>
        public void DrawFront(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(_FrontTarget, new Rectangle(0, 0, _BoardTarget.Width, _BoardTarget.Height), Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Moves the board's pixels by ShockwaveValues, HazeValues and WarpValues (filled in by the caller), the stun wobble and
        /// their phases, and returns the texture to draw the board from: the board as it is when nothing moves it.
        /// </summary>
        public Texture2D Distort(SpriteBatch spriteBatch, bool isMoving, float shockwaveWidth, float hazePhase, float wobblePixels, float wobblePhase)
        {
            if (!isMoving)
                return _BoardTarget;

            _Distort.Parameters["BoardSize"].SetValue(new Vector2(_BoardTarget.Width, _BoardTarget.Height));
            _Distort.Parameters["Shockwaves"].SetValue(ShockwaveValues);
            _Distort.Parameters["ShockwaveWidth"].SetValue(shockwaveWidth);
            _Distort.Parameters["HazeSources"].SetValue(HazeValues);
            _Distort.Parameters["HazePhase"].SetValue(hazePhase);
            _Distort.Parameters["Warps"].SetValue(WarpValues);
            _Distort.Parameters["WarpSwirl"].SetValue(BfgWarp.SWIRL_RADIANS);
            _Distort.Parameters["WobblePixels"].SetValue(wobblePixels);
            _Distort.Parameters["WobblePhase"].SetValue(wobblePhase);

            DrawThrough(spriteBatch, _BoardTarget, _DistortTarget, _Distort);
            return _DistortTarget;
        }

        /// <summary>
        /// Draws the finished board (from Distort) into destination on the target being drawn, through BoardGrade.fx:
        /// streaked towards blurCentre by blurStrength, its colors split by aberrationPixels, graded, its edges washed in
        /// edgeColor and blacked out beyond irisRadius of irisCentre (board pixels).
        /// </summary>
        public void DrawGraded(SpriteBatch spriteBatch, Texture2D board, Rectangle destination, BoardGrade grade, Color edgeColor,
            Vector2 blurCentre, float blurStrength, float aberrationPixels, Vector2 irisCentre, float irisRadius)
        {
            _Grade.Parameters["BoardSize"].SetValue(new Vector2(board.Width, board.Height));
            _Grade.Parameters["BlurCentre"].SetValue(blurCentre);
            _Grade.Parameters["BlurStrength"].SetValue(blurStrength);
            _Grade.Parameters["AberrationPixels"].SetValue(aberrationPixels);
            _Grade.Parameters["Saturation"].SetValue(grade.Saturation);
            _Grade.Parameters["Tint"].SetValue(new Vector3(grade.TintR, grade.TintG, grade.TintB));
            _Grade.Parameters["Flash"].SetValue(grade.Flash);
            _Grade.Parameters["EdgeColor"].SetValue(edgeColor.ToVector3());
            _Grade.Parameters["EdgeStrength"].SetValue(grade.EdgeStrength);
            _Grade.Parameters["IrisCentre"].SetValue(irisCentre);
            _Grade.Parameters["IrisRadius"].SetValue(irisRadius);

            spriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.LinearClamp, effect: _Grade);
            spriteBatch.Draw(board, destination, Color.White);
            spriteBatch.End();
        }
    }
}
