// Colour bloom, drawn by TitleScreen.Draw over the finished frame before the Brightness pass.
// BrightPass keeps what is above Threshold in each channel; Blur is one direction of a Gaussian blur.
// Keep the maths in step with EatMeLibrary.DisplayAdjust.ApplyBloomThreshold and GetBlurWeights.

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

float Threshold = 0.5;

// one texel of the target being blurred, along the blur's direction
float2 TexelStep = float2(0.0, 0.0);

// centre first, then one per texel outwards, used on both sides
float Weights[BLUR_TAPS];

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

float4 BrightPassPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	float3 bright = saturate((color.rgb - Threshold) / (1.0 - Threshold));
	return float4(bright, 1.0);
}

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

technique BrightPass
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL BrightPassPS();
	}
};

technique Blur
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL BlurPS();
	}
};
