// A pac or ghost materialising as it spawns, drawn by MultiPlayerGameScreen with a LinearClamp sampler: the same
// sharp bilinear filter as PixelArt.fx, but each texel shows only once Threshold passes its noise value, and the
// texels just appearing are drawn in EdgeColor.
// Keep the maths in step with EatMeLibrary.SpawnDissolve and EatMeLibrary.PixelArtFilter.GetSampleTexel.

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// SpawnDissolve.EDGE_WIDTH
#define EDGE_WIDTH 0.15

// Size in texels of the texture being drawn; DissolveEffect sets it for every texture SpriteBatch switches to
float2 TextureSize = float2(1.0, 1.0);

// SpawnDissolve.GetThreshold: 0 shows nothing, 1 + EDGE_WIDTH shows everything
float Threshold = 0.0;

// the rim on the texels just appearing
float3 EdgeColor = float3(1.0, 1.0, 1.0);

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
	float4 color = tex2D(SpriteTextureSampler, uv) * input.Color;

	// SpawnDissolve.GetNoise
	float noise = frac(sin(texelFloor.x * 12.9898 + texelFloor.y * 78.233) * 43758.5453);

	if (noise >= Threshold)
		return float4(0.0, 0.0, 0.0, 0.0);

	// colors are premultiplied, so the rim keeps the texel's alpha
	if (noise >= Threshold - EDGE_WIDTH)
		color.rgb = EdgeColor * color.a;

	return color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
