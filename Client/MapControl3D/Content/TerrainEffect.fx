
struct VS_IN
{
	uint4 pos0 : POSITION0;
	uint4 pos1 : POSITION1;
	uint4 pos2 : POSITION2;
	uint4 pos3 : POSITION3;
	uint4 occlusion : OCCLUSION;
	uint4 texPack : TEX;
	uint4 colorPack : COLOR;
};

struct GS_IN
{
	float4 pos0 : POSITION0;
	float4 pos1 : POSITION1;
	float4 pos2 : POSITION2;
	float4 pos3 : POSITION3;
	float3 posW0 : POSITIONW0;
	float3 posW1 : POSITIONW1;
	float3 posW2 : POSITIONW2;
	float3 posW3 : POSITIONW3;
	nointerpolation uint4 occlusion : OCCLUSION;
	nointerpolation uint4 texPack : TEX;
	nointerpolation uint4 colorPack : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float2 tex : TEXCOORD0;
	nointerpolation uint4 occlusion : OCCLUSION;
	nointerpolation uint4 texPack : TEX;
	nointerpolation uint4 colorPack : COLOR;
};

Buffer<float3> g_colorBuffer;		// GameColor -> RGB
Texture2DArray blockTextures;
sampler blockSampler;

bool g_disableLight;
bool g_disableBorders;
bool g_disableOcclusion;
bool g_disableTexture;
bool g_showOcclusionCorner;

cbuffer PerFrame
{
	matrix g_viewProjMatrix;

	float3 ambientColor;
	float3 diffuseColor;
	float3 specularColor;
	float3 lightDirection;
	float _pad0;
	float3 g_eyePos;
	float _pad1;
	float _pad2;
	float _pad3;
	float _pad4;
};

cbuffer PerObjectBuffer
{
	float3 g_chunkOffset;
};

GS_IN VSMain(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	float3 p0 = input.pos0.xyz;
	float3 p1 = input.pos1.xyz;
	float3 p2 = input.pos2.xyz;
	float3 p3 = input.pos3.xyz;

	p0 += g_chunkOffset;
	p1 += g_chunkOffset;
	p2 += g_chunkOffset;
	p3 += g_chunkOffset;

	output.posW0 = p0;
	output.posW1 = p1;
	output.posW2 = p2;
	output.posW3 = p3;

	output.pos0 = mul(float4(p0, 1), g_viewProjMatrix);
	output.pos1 = mul(float4(p1, 1), g_viewProjMatrix);
	output.pos2 = mul(float4(p2, 1), g_viewProjMatrix);
	output.pos3 = mul(float4(p3, 1), g_viewProjMatrix);

	output.occlusion = input.occlusion;
	output.texPack = input.texPack;
	output.colorPack = input.colorPack;

	return output;
}

[maxvertexcount(4)]
void GSMain(point GS_IN inputs[1], inout TriangleStream<PS_IN> OutputStream)
{
	PS_IN output = (PS_IN)0;

	GS_IN input = inputs[0];

	output.pos = input.pos0;
	output.posW = input.posW0;
	output.tex = float2(0, 0);

	output.texPack = input.texPack;
	output.colorPack = input.colorPack;

	output.occlusion = input.occlusion;

	OutputStream.Append(output);

	output.pos = input.pos1;
	output.posW = input.posW1;
	output.tex = float2(1, 0);
	OutputStream.Append(output);

	output.pos = input.pos2;
	output.posW = input.posW2;
	output.tex = float2(0, 1);
	OutputStream.Append(output);

	output.pos = input.pos3;
	output.posW = input.posW3;
	output.tex = float2(1, 1);
	OutputStream.Append(output);
}

float distanceFromNearestCorner(float2 tex)
{
	float2 tex2 = 0.5f - abs(tex - 0.5f);
	return length(tex2);
}

float distanceFromNearestEdge(float2 tex)
{
	float2 tex2 = 0.5f - abs(tex - 0.5f);
	return min(tex2.x, tex2.y);
}

int getQuadrant(float2 tex)
{
	int xq = (int)round(tex.x);
	int yq = (int)round(tex.y) * 2;
	return xq + yq;
}

float4 PSMain(PS_IN input) : SV_Target
{
	float3 litColor = float3(1, 1, 1);

	if (!g_disableLight)
	{
		float3 toEye = normalize(g_eyePos - input.posW);

		// Invert the light direction for calculations.
		float3 lightDir = -lightDirection;

		float3 ambient, diffuse, specular;

		ambient = ambientColor;

		float3 normal = cross(ddy(input.posW.xyz), ddx(input.posW.xyz));
		normal = -normalize(normal);

		float lightIntensity = dot(normal, lightDir);

		diffuse = specular = 0;

		if (lightIntensity > 0.0f)
		{
			diffuse = lightIntensity * diffuseColor;

			float3 v = reflect(-lightDir, normal);
				float specFactor = pow(max(dot(v, toEye), 0.0f), 64);
			specular = specFactor * specularColor;
		}

		litColor = ambient + diffuse + specular;
	}

	float occlusion = 1.0f;
	const float occlusionStep = 0.2f;

	if (!g_disableOcclusion)
	{
		float o1 = lerp(input.occlusion[0], input.occlusion[1], input.tex.x);
		float o2 = lerp(input.occlusion[2], input.occlusion[3], input.tex.x);
		occlusion = 1.0f - lerp(o1, o2, input.tex.y) * occlusionStep;
	}

	float border = 1.0f;

	if (!g_disableBorders)
	{
		float dist = distanceFromNearestEdge(input.tex);
		float2 ddEdge = fwidth(input.tex);

#ifdef SIMPLE_BORDER
		border = smoothstep(0, 0.05f, dist) * 0.7f + 0.3f;
#else
		float constWidth = min(ddEdge.x, ddEdge.y);

		const float edgeWidth = 1.0f;
		float edgeThreshold = constWidth * edgeWidth;

		const float lineSmooth = 0.02f;
		border = smoothstep(0, edgeThreshold + lineSmooth, dist) * 0.7f + 0.3f;
#endif

#define BORDER_FADE
#ifdef BORDER_FADE
		// fade the border based on eye distance and ddx/ddy
		float edist = length(g_eyePos - input.posW);
		const float fadeStart = 60;
		const float fadeLen = 20;
		border = 1 - border;
		float distMult = 1 - saturate((edist - fadeStart) / fadeLen);
		float ddMult = 1 - saturate(max(ddEdge.x, ddEdge.y) * 5);
		border *= distMult * ddMult;
		border = 1 - border;
#endif
	}

	/* background */
	float3 color = g_colorBuffer[input.colorPack[0]];

	if (!g_disableTexture)
	{
		float4 c1;
		c1 = blockTextures.Sample(blockSampler, float3(input.tex, input.texPack[1]));
		c1 = float4(c1.rgb * g_colorBuffer[input.colorPack[1]], c1.a);
		color = c1.rgb + (1.0f - c1.a) * color.rgb;

		float4 c2;
		c2 = blockTextures.Sample(blockSampler, float3(input.tex, input.texPack[2]));
		c2 = float4(c2.rgb * g_colorBuffer[input.colorPack[2]], c2.a);
		color = c2.rgb + (1.0f - c2.a) * color.rgb;
	}

	color = color * litColor * occlusion * border;

	if (g_showOcclusionCorner)
	{
		uint4 occ = input.occlusion;

		int quadrant = getQuadrant(input.tex);

		float o = 1.0f - occ[quadrant] / 4.0f;

		float cornerDist = distanceFromNearestCorner(input.tex);
		if (cornerDist < 0.2f)
			color = float3(o, o, o);
	}

	return float4(color, 1);
}

technique
{
	pass
	{
		Profile = 10.0;
		VertexShader = VSMain;
		GeometryShader = GSMain;
		PixelShader = PSMain;
	}
}
