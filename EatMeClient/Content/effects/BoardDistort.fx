// Moves the game board's pixels: shockwave rings pushing outward from blasts, heat haze round Holy Fire flames and
// fireballs, the swirling pinch round BFG shots, and the sideways wobble while the own pac is stunned. BoardPostProcess.Distort draws the board target
// through it into the distort target.
// Keep the maths in step with EatMeLibrary.Shockwaves.GetOffset, HeatHaze.GetOffset, BfgWarp.GetOffset and StateGrade.

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// Shockwaves.MAX_SHOCKWAVES, HeatHaze.MAX_SOURCES and BfgWarp.MAX_SOURCES
#define MAX_SHOCKWAVES 6
#define MAX_HAZE 8
#define MAX_WARPS 4

// HeatHaze.WAVES_PER_PIXEL and StateGrade.WOBBLE_WAVES_PER_PIXEL
#define HAZE_WAVES_PER_PIXEL 0.18
#define WOBBLE_WAVES_PER_PIXEL 0.02

#define PI 3.14159265

// the board's size in pixels
float2 BoardSize = float2(1.0, 1.0);

// each ring's centre, radius and amplitude in board pixels; amplitude 0 is no ring
float4 Shockwaves[MAX_SHOCKWAVES];

// how wide each ring is in pixels
float ShockwaveWidth = 1.0;

// each hot thing's centre, radius and amplitude in board pixels; amplitude 0 is none
float4 HazeSources[MAX_HAZE];

// HeatHaze.GetPhase
float HazePhase = 0.0;

// each BFG shot's centre, radius in board pixels and pinch (BfgWarp.GetPinch); radius 0 is none
float4 Warps[MAX_WARPS];

// BfgWarp.SWIRL_RADIANS
float WarpSwirl = 0.0;

// the stun wobble in pixels and StateGrade.GetWobblePhase
float WobblePixels = 0.0;
float WobblePhase = 0.0;

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
	float2 position = input.TextureCoordinates * BoardSize;
	float2 offset = float2(0.0, 0.0);

	[unroll]
	for (int i = 0; i < MAX_SHOCKWAVES; i++)
	{
		float4 wave = Shockwaves[i];
		float2 fromWave = position - wave.xy;
		float distance = max(length(fromWave), 0.001);
		float band = (distance - wave.z) / (ShockwaveWidth * 0.5);
		float push = abs(band) < 1.0 ? sin(band * PI) * wave.w * (1.0 - abs(band)) : 0.0;
		offset += fromWave / distance * push;
	}

	[unroll]
	for (int j = 0; j < MAX_HAZE; j++)
	{
		float4 source = HazeSources[j];
		float2 fromSource = position - source.xy;
		float falloff = source.z > 0.0 ? saturate(1.0 - length(fromSource) / source.z) : 0.0;
		offset += float2(sin(fromSource.y * HAZE_WAVES_PER_PIXEL + HazePhase), cos(fromSource.x * HAZE_WAVES_PER_PIXEL + HazePhase)) * source.w * falloff;
	}

	[unroll]
	for (int k = 0; k < MAX_WARPS; k++)
	{
		float4 warp = Warps[k];
		float2 fromWarp = position - warp.xy;
		float falloff = warp.z > 0.0 ? saturate(1.0 - length(fromWarp) / warp.z) : 0.0;
		float weight = falloff * falloff;
		float angle = WarpSwirl * weight;
		float c = cos(angle);
		float s = sin(angle);
		float2 sampled = float2(fromWarp.x * c - fromWarp.y * s, fromWarp.x * s + fromWarp.y * c) * (1.0 + warp.w * weight);
		offset += fromWarp - sampled;
	}

	offset.x += sin(position.y * WOBBLE_WAVES_PER_PIXEL + WobblePhase) * WobblePixels;

	// each pixel shows what was offset away from it, so the picture moves by offset
	return tex2D(SpriteTextureSampler, (position - offset) / BoardSize) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
