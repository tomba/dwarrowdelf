
struct VS_IN
{
	float3 pos : SV_POSITION;
	float4 color : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float4 color : COLOR0;
};

matrix viewProjMatrix;
matrix worldMatrix;

PS_IN VSMain(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	float4 pos = float4(input.pos, 1);

	output.pos = mul(pos, worldMatrix);
	output.posW = output.pos.xyz;
	output.pos = mul(output.pos, viewProjMatrix);

	output.color = input.color;

	return output;
}

float4 PSMain(PS_IN input) : SV_Target
{
	float4 color = input.color;
	color.a = 0.50f;

	return color;
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
