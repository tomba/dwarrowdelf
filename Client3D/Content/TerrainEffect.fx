
struct VS_IN
{
	float3 pos : POSITION;
	float3 tex : TEXCOORD0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float3 tex : TEXCOORD0;
};

Texture2DArray blockTextures;
sampler blockSampler;

cbuffer PerFrame
{
	matrix g_viewProjMatrix;

	float4 ambientColor;
	float4 diffuseColor;
	float4 specularColor;
	float3 lightDirection;
	float _pad0;
	float3 g_eyePos;
	float _pad1;
	bool g_showBorders;
	float _pad2;
	float _pad3;
	float _pad4;
};

cbuffer PerObjectBuffer
{
	matrix worldMatrix;
};

PS_IN VSMain(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	// Change the position vector to be 4 units for proper matrix calculations.
	float4 pos = float4(input.pos, 1.0f);

	output.pos = mul(pos, worldMatrix);
	output.posW = output.pos.xyz;
	output.pos = mul(output.pos, g_viewProjMatrix);

	output.tex = input.tex;

	return output;
}

float4 PSMain(PS_IN input) : SV_Target
{
	float d = 0.01f;

	float3 toEye = normalize(g_eyePos - input.posW);

	// Invert the light direction for calculations.
	float3 lightDir = -lightDirection;

	float4 ambient, diffuse, specular;

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

	float4 litColor = ambient + diffuse + specular;

	float4 textureColor = blockTextures.Sample(blockSampler, float3(input.tex));

	float4 color = litColor * textureColor;

	if (g_showBorders)
	{
		float val = min(input.tex.x, input.tex.y);
		val = min(val, (1.0f - input.tex.x));
		val = min(val, (1.0f - input.tex.y));

		val = smoothstep(0, 0.1f, val) * 0.3f + 0.7f;

		color *= val;
	}

	return color;
}

technique
{
	pass
	{
		Profile = 10.0;
		VertexShader = VSMain;
		PixelShader = PSMain;
	}
}
