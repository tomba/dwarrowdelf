matrix g_world;

float2 g_colrow;	/* columns, rows */
float2 g_renderOffset;

float g_tileSize;

struct TileData
{
	uint tile1;
	uint tile2;
	uint tile3;
	uint tile4;
	uint darkness;
};

Texture2DArray g_tileTextures;
StructuredBuffer<TileData> g_tileBuffer;
Buffer<uint> g_colorBuffer;		// GameColor -> RGB

SamplerState linearSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VS_IN
{
	float4 pos : POSITION;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
};

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(input.pos, g_world);;
	
	return output;
}


float3 Hue(float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 HSVtoRGB(in float3 HSV)
{
    return ((Hue(HSV.x) - 1) * HSV.y + 1) * HSV.z;
}

#define NO_ASM

float3 RGBtoHSV(in float3 RGB)
{
    float3 HSV = 0;
#ifdef NO_ASM
    HSV.z = max(RGB.r, max(RGB.g, RGB.b));
    float M = min(RGB.r, min(RGB.g, RGB.b));
    float C = HSV.z - M;
#else
    float4 RGBM = RGB.rgbr;
    asm { max4 HSV.z, RGBM };
    asm { max4 RGBM.w, -RGBM };
    float C = HSV.z + RGBM.w;
#endif
    if (C != 0)
    {
        HSV.y = C / HSV.z;
        float3 Delta = (HSV.z - RGB) / C;
        Delta.rgb -= Delta.brg;
        Delta.rg += float2(2,4);
        if (RGB.r >= HSV.z)
            HSV.x = Delta.b;
        else if (RGB.g >= HSV.z)
            HSV.x = Delta.r;
        else
            HSV.x = Delta.g;
        HSV.x = frac(HSV.x / 6);
    }
    return HSV;
}


float3 tint(in float3 input, in uint coloridx)
{
	uint tinti = g_colorBuffer.Load(coloridx);
	
	float3 tint;

	tint.r = (tinti >> 16) & 0xff;
	tint.g = (tinti >> 8) & 0xff;
	tint.b = (tinti >> 0) & 0xff;
	tint /= 255.0f;

	input = RGBtoHSV(input);
	tint = RGBtoHSV(tint);

	input.r = tint.r;
	input.g = tint.g;
	
	input = HSVtoRGB(input);

	return input;
}

float4 get(in uint tileNum, in uint colorNum, in uint bgColorNum, in float darkness, in float2 texpos)
{
	if (tileNum == 0)
		return float4(0, 0, 0, 0.0f);

	float4 c = g_tileTextures.Sample(linearSampler, float3(texpos, tileNum));

	if (colorNum != 0)
	{
		float3 rgb = tint(c.rgb, colorNum);

		c = float4(rgb, c.a);
	}
	
	if (bgColorNum != 0)
	{
		uint bgi = g_colorBuffer.Load(bgColorNum);

		float3 bg;
		bg.r = (bgi >> 16) & 0xff;
		bg.g = (bgi >> 8) & 0xff;
		bg.b = (bgi >> 0) & 0xff;
		bg /= 255.0f;

		c = float4(c.rgb * c.a + bg.rgb * (1.0f - c.a), 1.0f);
	}

	c.rgb = (1.0f - darkness) * c.rgb;

	return c;
}

float4 PS( PS_IN input ) : SV_Target
{
	float2 pos = input.pos.xy;

	float2 xy = pos - g_renderOffset;

	if (xy.x < 0 || xy.y < 0 || xy.x >= g_colrow.x * g_tileSize || xy.y >= g_colrow.y * g_tileSize)
		return float4(1.0f, 0, 0, 1.0f);

	float2 tilepos = floor(xy / g_tileSize);
	
	TileData td = g_tileBuffer[tilepos.y * g_colrow.x + tilepos.x];

	uint t1 = (td.tile1 >> 0) & 0xffff;
	uint t2 = (td.tile2 >> 0) & 0xffff;
	uint t3 = (td.tile3 >> 0) & 0xffff;
	uint t4 = (td.tile4 >> 0) & 0xffff;

	uint ci1 = (td.tile1 >> 16) & 0xff;
	uint ci2 = (td.tile2 >> 16) & 0xff;
	uint ci3 = (td.tile3 >> 16) & 0xff;
	uint ci4 = (td.tile4 >> 16) & 0xff;

	uint bi1 = (td.tile1 >> 24) & 0xff;
	uint bi2 = (td.tile2 >> 24) & 0xff;
	uint bi3 = (td.tile3 >> 24) & 0xff;
	uint bi4 = (td.tile4 >> 24) & 0xff;

	uint darkness = td.darkness;
	float d1 = ((darkness >> 0) & 0x7f) / 127.0f;
	float d2 = ((darkness >> 7) & 0x7f) / 127.0f;
	float d3 = ((darkness >> 14) & 0x7f) / 127.0f;
	float d4 = ((darkness >> 21) & 0x7f) / 127.0f;

	float2 texpos = xy / g_tileSize;

	float4 c1, c2, c3, c4;

	c1 = get(t1, ci1, bi1, d1, texpos);
	c2 = get(t2, ci2, bi2, d2, texpos);
	c3 = get(t3, ci3, bi3, d3, texpos);
	c4 = get(t4, ci4, bi4, d4, texpos);

	float3 c;
	c = c1.rgb;
	c = c2.rgb * c2.a + c.rgb * (1.0f - c2.a);
	c = c3.rgb * c3.a + c.rgb * (1.0f - c3.a);
	c = c4.rgb * c4.a + c.rgb * (1.0f - c4.a);

	return float4(c, 1.0f);
}

technique10 full
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( NULL );
        SetPixelShader( CompileShader( ps_4_0, PS() ) );
    }
}
