matrix g_world;

float2 g_colrow;	/* columns, rows */
float2 g_renderSize;		/* width, height */

int g_tileSize;

struct TileData
{
	int tilenum12;
	int tilenum34;
	int colornum;
	int bgcolornum;
};

Texture2DArray g_tileTextures;
Buffer<TileData> g_tileBuffer;
Buffer<int> g_colorBuffer;		// GameColor -> RGB

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
	int tinti = g_colorBuffer.Load(coloridx);
	
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

float4 get(in uint tileNum, in uint colorNum, in float2 texpos)
{
	if (tileNum == 0)
		return float4(0, 0, 0, 0.0f);

	float4 c = g_tileTextures.Sample(linearSampler, float3(texpos, tileNum));

	if (colorNum == 0)
		return c;

	float3 rgb = tint(c.rgb, colorNum);

	return float4(rgb, c.a);
}

float4 PS( PS_IN input ) : SV_Target
{
	float2 pos = input.pos.xy;

	float2 xy = pos - (g_renderSize - g_colrow * g_tileSize) / 2;

	if (xy.x < 0 || xy.y < 0 || xy.x >= g_colrow.x * g_tileSize || xy.y >= g_colrow.y * g_tileSize)
		return float4(1.0f, 0, 0, 1.0f);

	float2 tilepos = floor(xy / g_tileSize);
	
	TileData td;

	td = g_tileBuffer.Load(tilepos.y * g_colrow.x + tilepos.x);
	int tileNum12 = td.tilenum12;
	int tileNum34 = td.tilenum34;

	if (tileNum12 == 0 && tileNum34 == 0)
		return float4(0, 1.0f, 0, 1.0f);

	int t1 = (tileNum12 >> 0) & 0xffff;
	int t2 = (tileNum12 >> 16) & 0xffff;
	int t3 = (tileNum34 >> 0) & 0xffff;
	int t4 = (tileNum34 >> 16) & 0xffff;

	//int colorNum = td.colornum;

	int ci1 = 0; //(colorNum >> 0) & 0xff;
	int ci2 = 0; //(colorNum >> 8) & 0xff;
	int ci3 = 0; //(colorNum >> 16) & 0xff;
	int ci4 = 0; //(colorNum >> 24) & 0xff;

	float2 texpos = xy / g_tileSize;

	float4 c1, c2, c3, c4;

	c1 = get(t1, ci1, texpos);
	c2 = get(t2, ci2, texpos);
	c3 = get(t3, ci3, texpos);
	c4 = get(t4, ci4, texpos);

	float3 c;
	c = c1.rgb;
	c = c2.rgb * c2.a + c.rgb * (1.0f - c2.a);
	c = c3.rgb * c3.a + c.rgb * (1.0f - c3.a);
	c = c4.rgb * c4.a + c.rgb * (1.0f - c4.a);

	//c1.a = 1.0f; return c1;
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
