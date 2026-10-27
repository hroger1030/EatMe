// A visible shock ring racing out from a blast or a BFG burst, drawn by MultiPlayerGameScreen as one square quad
// QuadPixels wide centred on where it went off: a bright core in CoreColor along the ring's middle line, RadiusPixels
// from the centre and WidthPixels wide, with a soft glow in EdgeColor either side, times the sprite color's alpha.
// The result has no alpha, so with premultiplied blending it adds its light to the board.
// Keep the maths in step with EatMeLibrary.ShockRings (GetBand, GetCore, GetGlow).

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// ShockRings.GLOW_SPREAD
#define GLOW_SPREAD 3.0

// the quad's size, and the ring's radius and core width, in screen pixels
float QuadPixels = 1.0;
float RadiusPixels = 0.0;
float WidthPixels = 1.0;

float3 CoreColor = float3(1.0, 1.0, 1.0);
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
	float distance = length((input.TextureCoordinates - 0.5) * QuadPixels);

	// ShockRings.GetBand, GetCore and GetGlow
	float band = abs(distance - RadiusPixels) / (WidthPixels * 0.5);
	float core = saturate(1.0 - band);
	float glow = saturate(1.0 - band / GLOW_SPREAD);
	glow *= glow;

	float3 color = (CoreColor * core + EdgeColor * glow) * input.Color.a;
	return float4(color, 0.0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
