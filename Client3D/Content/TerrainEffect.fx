
struct VS_IN
{
	uint4 pos : POSITION;
	uint occlusion : OCCLUSION;
	uint4 texPack : TEX;
	uint4 colorPack : COLOR;
};

struct GS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	nointerpolation int occlusion : OCCLUSION;
	nointerpolation uint4 texPack : TEX;
	nointerpolation uint4 colorPack : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float2 tex : TEXCOORD0;
	nointerpolation int occlusion[4] : OCCLUSION;
	nointerpolation uint4 texPack : TEX;
	nointerpolation uint4 colorPack : COLOR;
};

Buffer<float3> g_colorBuffer;		// GameColor -> RGB
Texture2DArray blockTextures;
sampler blockSampler;

bool g_disableLight;
bool g_showBorders;
bool g_disableOcclusion;
bool g_disableTexture;

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
	matrix worldMatrix;
};

GS_IN VSMain(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	// Change the position vector to be 4 units for proper matrix calculations.
	float4 pos = float4(input.pos.xyz, 1.0f);

	output.pos = mul(pos, worldMatrix);
	output.posW = output.pos.xyz;
	output.pos = mul(output.pos, g_viewProjMatrix);

	output.occlusion = input.occlusion;
	output.texPack = input.texPack;
	output.colorPack = input.colorPack;

	return output;
}

[maxvertexcount(6)]
void GSMain(lineadj GS_IN input[4], inout TriangleStream<PS_IN> OutputStream)
{
	PS_IN output = (PS_IN)0;

	/* FIRST */
	output.pos = input[0].pos;
	output.posW = input[0].posW;
	output.tex = float2(0, 0);
	output.texPack = input[0].texPack;
	output.colorPack = input[0].colorPack;

	output.occlusion[0] = input[0].occlusion;
	output.occlusion[1] = input[1].occlusion;
	output.occlusion[2] = input[2].occlusion;
	output.occlusion[3] = input[3].occlusion;

	OutputStream.Append(output);

	output.pos = input[1].pos;
	output.posW = input[1].posW;
	output.tex = float2(1, 0);
	output.texPack = input[1].texPack;
	output.colorPack = input[1].colorPack;
	OutputStream.Append(output);

	output.pos = input[2].pos;
	output.posW = input[2].posW;
	output.tex = float2(0, 1);
	output.texPack = input[2].texPack;
	output.colorPack = input[2].colorPack;
	OutputStream.Append(output);

	/* SECOND */
	output.pos = input[1].pos;
	output.posW = input[1].posW;
	output.tex = float2(1, 0);
	output.texPack = input[1].texPack;
	output.colorPack = input[1].colorPack;
	OutputStream.Append(output);

	output.pos = input[2].pos;
	output.posW = input[2].posW;
	output.tex = float2(0, 1);
	output.texPack = input[2].texPack;
	output.colorPack = input[2].colorPack;
	OutputStream.Append(output);

	output.pos = input[3].pos;
	output.posW = input[3].posW;
	output.texPack = input[3].texPack;
	output.colorPack = input[3].colorPack;
	output.tex = float2(1, 1);

	output.occlusion[0] = input[0].occlusion;
	output.occlusion[1] = input[1].occlusion;
	output.occlusion[2] = input[2].occlusion;
	output.occlusion[3] = input[3].occlusion;

	OutputStream.Append(output);

	OutputStream.RestartStrip();
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

	if (g_showBorders)
	{
		float val = min(input.tex.x, input.tex.y);
		val = min(val, (1.0f - input.tex.x));
		val = min(val, (1.0f - input.tex.y));

		border = smoothstep(0, 0.05f, val) * 0.7f + 0.3f;
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
