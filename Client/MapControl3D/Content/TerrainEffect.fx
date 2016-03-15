
static const float PI = 3.14159265f;

struct VS_IN
{
	uint4 pos0 : POSITION0;
	uint4 pos1 : POSITION1;
	uint4 pos2 : POSITION2;
	uint4 pos3 : POSITION3;
	int4 occlusion : OCCLUSION;
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
	nointerpolation int4 occlusion : OCCLUSION;
	nointerpolation uint4 texPack : TEX;
	nointerpolation uint4 colorPack : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float2 tex : TEXCOORD0;
	nointerpolation int4 occlusion : OCCLUSION;
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
bool g_showOcclusionDebug;

float g_tunable1;
float g_tunable2;

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
	float2 p = abs(tex - 0.5f);
	float2 corner = float2(0.5f, 0.5f);
	return distance(p, corner);
}

float distanceFromNearestEdge(float2 tex)
{
	float2 p = 0.5f - abs(tex - 0.5f);
	return min(p.x, p.y);
}

float distanceFromNearestEdgeCenter(float2 tex)
{
	float2 p = abs(tex - 0.5f);
	float2 c1 = float2(0, 0.5f);
	float2 c2 = float2(0.5f, 0);

	return min(distance(p, c1), distance(p, c2));
}

int getQuadrant(float2 tex)
{
	int xq = (int)round(tex.x);
	int yq = (int)round(tex.y) * 2;
	return xq + yq;
}

int getSector(float2 tex)
{
	float2 p = tex - 0.5f;
	float2 ap = abs(p);

	if (ap.x < ap.y)
		return sign(p.y) + 2;
	else
		return sign(p.x) + 1;
}

// fade multipler [0:1] based on distance and angle
float getFadeMultiplier(float3 posW)
{
		const float eyedist = length(g_eyePos - posW);
		const float fadeStart = 70;
		const float fadeLen = 5;

		const float fadeDist = saturate(1 - (eyedist - fadeStart) / fadeLen);
		const float fadeAngle = saturate(1 - (fwidth(eyedist) * 6.25 - 1));

		return fadeDist * fadeAngle;
}

float4 PSMain(PS_IN input) : SV_Target
{
	float3 litColor = float3(1, 1, 1);

	const float fadeMult = getFadeMultiplier(input.posW);

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

	float occlusion = 0;

	if (!g_disableOcclusion)
	{
		const float occlusionStep = 0.2f;

		// occ [-1:1]
		float4 occ = input.occlusion / 3.0f;

		float o1 = lerp(occ[0], occ[1], input.tex.x);
		float o2 = lerp(occ[2], occ[3], input.tex.x);
		occlusion = lerp(o1, o2, input.tex.y) * occlusionStep;
	}

	float border = 1.0f;

	if (!g_disableBorders)
	{
		float dist = distanceFromNearestEdge(input.tex);
		float2 ddEdge = fwidth(input.tex);

#ifdef SIMPLE_BORDER
		border = smoothstep(0, 0.05f, dist) * 0.2f + 0.8f;
#else
		float constWidth = min(ddEdge.x, ddEdge.y);

		const float edgeWidth = 1.0f;
		float edgeThreshold = constWidth * edgeWidth;

		const float lineSmooth = 0.02f;
		border = smoothstep(0, edgeThreshold + lineSmooth, dist) * 0.2f + 0.8f;
#endif

		border = 1 - (1 - border) * fadeMult;
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

		// XXX slice hack. Show diagonal line pattern on slice level
		// this could be a normal texture
		if (input.texPack[3])
		{
			float f = sin((input.tex.x - input.tex.y) * 2 * PI);
			f = saturate(f);
			f *= fadeMult;
			color *= 1 - f * 0.5;
		}
	}

	color = color * litColor * border;

	color += occlusion;

	if (g_showOcclusionDebug)
	{
		int quadrant = getQuadrant(input.tex);

		int4 occ = input.occlusion;

		float o = (occ[quadrant] + 3) / 6.0f;

		float cornerDist = distanceFromNearestCorner(input.tex);
		if (cornerDist < 0.2f)
			color = float3(o, o, o);
		else if (cornerDist < 0.22f)
			color = 1 - float3(o, o, o);
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
