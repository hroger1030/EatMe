// Draws the finished game board onto the frame: the radial speed blur towards the own pac with the chromatic
// aberration kick, then the state color grade, the starving red edges and the iris wipe. BoardPostProcess.DrawGraded
// draws the board through it into the board's viewport.
// Keep the maths in step with EatMeLibrary.SpeedBlur, StateGrade.ApplyGrade, StateGrade.GetEdgeWeight and
// IrisWipe.GetCoverage.

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// SpeedBlur.TAPS, StateGrade.EDGE_POWER and IrisWipe.EDGE_PIXELS
#define BLUR_TAPS 6
#define EDGE_POWER 3.0
#define IRIS_EDGE_PIXELS 4.0

// the board's size in pixels
float2 BoardSize = float2(1.0, 1.0);

// the speed blur's centre (the own pac) in board pixels, and how long its streaks are (SpeedBlur.GetStrength)
float2 BlurCentre = float2(0.0, 0.0);
float BlurStrength = 0.0;

// how far red and blue split from green at the board's edge (ChromaticKick)
float AberrationPixels = 0.0;

// StateGrade's grade
float Saturation = 1.0;
float3 Tint = float3(1.0, 1.0, 1.0);
float Flash = 0.0;
float3 EdgeColor = float3(1.0, 0.0, 0.0);
float EdgeStrength = 0.0;

// the iris wipe's centre in board pixels and radius (IrisWipe.GetRadius)
float2 IrisCentre = float2(0.0, 0.0);
float IrisRadius = 100000.0;

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
	float2 uv = input.TextureCoordinates;
	float2 position = uv * BoardSize;

	// red and blue pulled apart from the board's centre, most at the edges
	float2 split = (uv - 0.5) * 2.0 * AberrationPixels / BoardSize;

	// streaks towards the own pac: samples along the line to it (SpeedBlur.GetSampleScale)
	float2 centre = BlurCentre / BoardSize;
	float3 color = float3(0.0, 0.0, 0.0);

	[unroll]
	for (int i = 0; i < BLUR_TAPS; i++)
	{
		float scale = 1.0 - BlurStrength * i / (BLUR_TAPS - 1);
		float2 sampleUv = centre + (uv - centre) * scale;
		color.r += tex2D(SpriteTextureSampler, sampleUv + split).r;
		color.g += tex2D(SpriteTextureSampler, sampleUv).g;
		color.b += tex2D(SpriteTextureSampler, sampleUv - split).b;
	}

	color /= BLUR_TAPS;

	// the grade (StateGrade.ApplyGrade)
	float luma = dot(color, float3(0.299, 0.587, 0.114));
	color = (luma + (color - luma) * Saturation) * Tint;
	color = lerp(color, float3(1.0, 1.0, 1.0), Flash);

	// red edges (StateGrade.GetEdgeWeight)
	float edge = pow(saturate(max(abs(uv.x * 2.0 - 1.0), abs(uv.y * 2.0 - 1.0))), EDGE_POWER);
	color = lerp(color, EdgeColor, edge * EdgeStrength);

	// iris wipe (IrisWipe.GetCoverage)
	color *= saturate((IrisRadius - length(position - IrisCentre)) / IRIS_EDGE_PIXELS);

	return float4(color, 1.0) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
