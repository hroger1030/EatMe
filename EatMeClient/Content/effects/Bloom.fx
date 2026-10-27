// Colour bloom: BoardPostProcess blurs the game board's glow target (only the glowing things, see
// EatMeLibrary.GlowSources) at half and quarter size with Blur, one direction of a Gaussian blur, and adds both over
// the board with Combine, many times over (Gain: DisplayAdjust.GetBloomTightGain and GetBloomWideGain).
// Keep the maths in step with EatMeLibrary.DisplayAdjust.GetBlurWeights.

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// DisplayAdjust.BLOOM_BLUR_TAPS
#define BLUR_TAPS 5

// one texel of the target being blurred, along the blur's direction
float2 TexelStep = float2(0.0, 0.0);

// centre first, then one per texel outwards, used on both sides
float Weights[BLUR_TAPS];

// how many times over Combine adds the blurred glow
float Gain = 1.0;

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

float4 BlurPS(VertexShaderOutput input) : COLOR
{
	float3 sum = tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb * Weights[0];

	for (int i = 1; i < BLUR_TAPS; i++)
	{
		float2 step = TexelStep * i;
		sum += tex2D(SpriteTextureSampler, input.TextureCoordinates + step).rgb * Weights[i];
		sum += tex2D(SpriteTextureSampler, input.TextureCoordinates - step).rgb * Weights[i];
	}

	return float4(sum, 1.0);
}

float4 CombinePS(VertexShaderOutput input) : COLOR
{
	return float4(tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb * Gain, 1.0);
}

technique Combine
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL CombinePS();
	}
};

technique Blur
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL BlurPS();
	}
};
