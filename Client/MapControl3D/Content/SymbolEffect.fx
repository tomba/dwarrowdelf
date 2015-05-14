struct VSIn
{
	float3 PosW		: POSITION;
	float4 Color	: COLOR;
	uint Tex		: TEXIDX;
};

struct VSOut
{
	float3 PosW		: POSITION;
	float4 Color	: COLOR;
	uint Tex		: TEXIDX;
};

typedef VSOut GSIn;

struct GSOut
{
	float4 PosH		: SV_POSITION;
	float3 PosW		: POSITION;
	float3 Tex		: TEXCOORD;
	float4 Color	: COLOR;
};

typedef GSOut PSIn;

cbuffer cbPerFrame : register(b0)
{
	float4x4 g_viewProjMatrix;
	float3 gEyePosW;
	float _pad00;
	float3 screenUp;
	float _pad01;
};

cbuffer PerObjectBuffer
{
	float3 g_chunkOffset;
};

Texture2DArray g_texture;
sampler g_sampler;

VSOut VSMain(VSIn vin)
{
	VSOut vout;

	vout.PosW = vin.PosW + g_chunkOffset + 0.5f;
	vout.Color = vin.Color;
	vout.Tex = vin.Tex;

	return vout;
}

void AddRect(GSIn gin, inout TriangleStream< GSOut > output, in float3 up, in float3 right)
{
	const float2 size = float2(1, 1);

	const float hWidth = 0.5f * size.x;
	const float hHeight = 0.5f * size.y;

	const float2 gTexC[4] =
	{
		float2(1, 1),
		float2(1, 0),
		float2(0, 1),
		float2(0, 0),
	};

	float4 v[4];
	v[0] = float4(gin.PosW + hWidth * right - hHeight * up, 1.0f);
	v[1] = float4(gin.PosW + hWidth * right + hHeight * up, 1.0f);
	v[2] = float4(gin.PosW - hWidth * right - hHeight * up, 1.0f);
	v[3] = float4(gin.PosW - hWidth * right + hHeight * up, 1.0f);

	GSOut gsout;
	[unroll]
	for (int i = 0; i < 4; ++i)
	{
		gsout.PosH = mul(v[i], g_viewProjMatrix);
		gsout.PosW = v[i].xyz;
		gsout.Tex = float3(gTexC[i], gin.Tex);
		gsout.Color = gin.Color;

		output.Append(gsout);
	}
}

[maxvertexcount(4)]
void GSMain(point GSIn gin[1], inout TriangleStream< GSOut > output)
{
	float3 up, right, look;

	if (BILLBOARD_MODE == 1) {
		/* sprite is always upright */
		up = float3(0, 0, 1);
		look = gin[0].PosW - gEyePosW;
		look.z = 0;
		look = normalize(look);
		right = cross(up, look);
	} else if (BILLBOARD_MODE == 2) {
		/* sprite face follows camera */
		look = normalize(gin[0].PosW - gEyePosW);
		right = normalize(cross(float3(0, 0, 1), look));
		up = cross(look, right);
	} else if (BILLBOARD_MODE == 3) {
		/* sprite is flat on the ground, orientation given by 'screenUp' */
		up = screenUp;
		right = float3(-up.y, up.x, 0);
		gin[0].PosW += float3(0, 0, -0.49f);
	}

	AddRect(gin, output, up, right);
}

[maxvertexcount(8)]
void GSMainCross(point GSIn gin[1], inout TriangleStream< GSOut > output)
{
	float3 up = float3(0, 0, 1);
	float3 right1 = float3(1, 1, 0);

	AddRect(gin, output, up, right1);

	output.RestartStrip();

	float3 right2 = float3(-1, 1, 0);

	AddRect(gin, output, up, right2);
}

float4 PSMain(PSIn pin) : SV_TARGET
{
	float4 color = g_texture.Sample(g_sampler, pin.Tex);

	// if the texture pixel is transparent, skip this pixel
	clip(color.a - 0.1f);

	color *= pin.Color;
	return color;
}

technique ModeUpright
{
	pass
	{
		Profile = 10.0;
		Preprocessor = { "#define BILLBOARD_MODE 1" };
		VertexShader = VSMain;
		GeometryShader = GSMain;
		PixelShader = PSMain;
	}
}

technique ModeFollow
{
	pass
	{
		Profile = 10.0;
		Preprocessor = { "#define BILLBOARD_MODE 2" };
		VertexShader = VSMain;
		GeometryShader = GSMain;
		PixelShader = PSMain;
	}
}

technique ModeFlat
{
	pass
	{
		Profile = 10.0;
		Preprocessor = { "#define BILLBOARD_MODE 3" };
		VertexShader = VSMain;
		GeometryShader = GSMain;
		PixelShader = PSMain;
	}
}

technique ModeCross
{
	pass
	{
		Profile = 10.0;
		VertexShader = VSMain;
		GeometryShader = GSMainCross;
		PixelShader = PSMain;
	}
}
