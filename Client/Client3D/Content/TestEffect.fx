
struct VS_IN
{
	float3 pos : POSITION;
	float4 color : COLOR0;
};

struct GS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float4 color : COLOR0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float2 tex : TEXCOORD0;
	nointerpolation float4 color[4] : COLOR0;
};

matrix viewProjMatrix;
matrix worldMatrix;

GS_IN VSMain(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	// Change the position vector to be 4 units for proper matrix calculations.
	float4 pos = float4(input.pos, 1.0f);

	output.pos = mul(pos, worldMatrix);
	output.posW = output.pos.xyz;
	output.pos = mul(output.pos, viewProjMatrix);

	output.color = input.color;

	return output;
}


[maxvertexcount(6)]
void GSMain(lineadj GS_IN input[4], inout TriangleStream<PS_IN> OutputStream)
{
	PS_IN output = (PS_IN)0;

	/* FIRST */
	output.pos = input[0].pos;
	output.tex = float2(0, 0);

	output.color[0] = input[0].color;
	output.color[1] = input[1].color;
	output.color[2] = input[2].color;
	output.color[3] = input[3].color;

   	OutputStream.Append(output);

	output.pos = input[1].pos;
	output.tex = float2(1, 0);
   	OutputStream.Append(output);

	output.pos = input[2].pos;
	output.tex = float2(0, 1);
   	OutputStream.Append(output);

	/* SECOND */
	output.pos = input[1].pos;
	output.tex = float2(1, 0);
   	OutputStream.Append(output);

	output.pos = input[2].pos;
	output.tex = float2(0, 1);
   	OutputStream.Append(output);

	output.pos = input[3].pos;
	output.tex = float2(1, 1);

	output.color[0] = input[0].color;
	output.color[1] = input[1].color;
	output.color[2] = input[2].color;
	output.color[3] = input[3].color;

	OutputStream.Append(output);

	OutputStream.RestartStrip();
}

float4 PSMain(PS_IN input) : SV_Target
{
	float4 c1 = lerp(input.color[0], input.color[1], input.tex.x);
	float4 c2 = lerp(input.color[2], input.color[3], input.tex.x);

	float4 color = lerp(c1, c2, input.tex.y);

	return color;
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
