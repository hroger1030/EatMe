// An invincible pac in the rainbow, drawn by MultiPlayerGameScreen with a LinearClamp sampler and the white pac sprite:
// the same sharp bilinear filter as PixelArt.fx, the whole sprite one color at a time: the smooth blend round the
// seven rainbow colors at Phase, running round as Phase moves.
// Keep the maths in step with EatMeLibrary.StarRainbow.GetColor and EatMeLibrary.Rainbow.GetGradientColor.

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

#define COLOR_COUNT 7

// Size in texels of the texture being drawn; StarRainbowEffect sets it for every texture SpriteBatch switches to
float2 TextureSize = float2(1.0, 1.0);

// StarRainbow.GetPhase
float Phase = 0.0;

// Rainbow.COLORS
float3 Colors[COLOR_COUNT];

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

float3 GetGradientColor(float t)
{
	float position = frac(t) * COLOR_COUNT;
	float3 color = float3(0.0, 0.0, 0.0);

	// each color's share falls off over one step either side of it, wrapping from violet back to red
	[unroll]
	for (int i = 0; i < COLOR_COUNT; i++)
	{
		float distance = abs(position - i);
		distance = min(distance, COLOR_COUNT - distance);
		color += Colors[i] * saturate(1.0 - distance);
	}

	return color;
}

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
	float3 rainbow = GetGradientColor(Phase);

	return tex2D(SpriteTextureSampler, uv) * float4(rainbow, 1.0) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
