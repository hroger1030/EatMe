// Sharp bilinear filter for the pixel art on the game board, drawn by MultiPlayerGameScreen with a LinearClamp
// sampler. Each texel is drawn as a solid block and only the last half screen pixel either side of a texel edge
// blends, so scaled, rotated and sub-pixel sprites get smooth edges without going blurry.
// Keep the maths in step with EatMeLibrary.PixelArtFilter.GetSampleTexel.

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// Size in texels of the texture being drawn; PixelArtEffect sets it for every texture SpriteBatch switches to
float2 TextureSize = float2(1.0, 1.0);

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 texel = input.TextureCoordinates * TextureSize;
	float2 texelsPerPixel = fwidth(texel);

	float2 region = clamp(0.5 - 0.5 * texelsPerPixel, 0.0, 0.5);
	float2 texelFloor = floor(texel);
	float2 fromCentre = texel - texelFloor - 0.5;
	float2 ramp = max(0.5 - region, 0.0001);
	float2 offset = (fromCentre - clamp(fromCentre, -region, region)) * 0.5 / ramp;

	float2 uv = (texelFloor + 0.5 + offset) / TextureSize;
	return tex2D(SpriteTextureSampler, uv) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
