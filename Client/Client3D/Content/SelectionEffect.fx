
struct VS_IN
{
	float3 pos : SV_POSITION;
	float4 color : COLOR;
	float2 tex : TEXCOORD0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float3 color : COLOR0;
	float2 tex : TEXCOORD0;
};

cbuffer PerFrame
{
	matrix viewProjMatrix;
};

cbuffer PerObject
{
	matrix worldMatrix;
	float3 s_cubeColor;
};

PS_IN VSMain(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	float4 pos = float4(input.pos, 1);

	output.pos = mul(pos, worldMatrix);
	output.posW = output.pos.xyz;
	output.pos = mul(output.pos, viewProjMatrix);

	output.color = input.color.xyz;
	output.tex = input.tex;

	return output;
}

float4 PSMain(PS_IN input) : SV_Target
{
	float3 color = s_cubeColor * input.color;

	float border = 1.0f;

	{
		float2 ddEdge = fwidth(input.tex);

#if SIMPLE_BORDER
			float val = min(input.tex.x, input.tex.y);
			val = min(val, (1.0f - input.tex.x));
			val = min(val, (1.0f - input.tex.y));

			border = smoothstep(0, 0.05f, val) * 0.7f + 0.3f;
#else
			float2 edgeDist2 = min(input.tex, 1.0f - input.tex);
			float edgeDist = min(edgeDist2.x, edgeDist2.y);

			float constWidth = min(ddEdge.x, ddEdge.y);

			const float edgeWidth = 1.0f;
			float edgeThreshold = constWidth * edgeWidth;

			const float lineSmooth = 0.02f;
			border = smoothstep(0, edgeThreshold + lineSmooth, edgeDist) * 0.7f + 0.3f;
#endif
	}

	color *= border;

	return float4(color, 0.5f);
}

technique
{
	pass
	{
		Profile = 10.0;
		VertexShader = VSMain;
		GeometryShader = NULL;
		PixelShader = PSMain;
	}
}
