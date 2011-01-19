matrix g_world;

int g_columns;
int g_rows;

int g_tileSize;

int g_width;
int g_height;

struct TileData
{
	int tilenum;
	int colornum;
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

//#define NO_ASM

float3 RGBtoHSV(in float3 RGB)
{
    float3 HSV = 0;
//#if NO_ASM
    HSV.z = max(RGB.r, max(RGB.g, RGB.b));
    float M = min(RGB.r, min(RGB.g, RGB.b));
    float C = HSV.z - M;
//#else
//    float4 RGBM = RGB.rgbr;
//    asm { max4 HSV.z, RGBM };
//    asm { max4 RGBM.w, -RGBM };
//    float C = HSV.z + RGBM.w;
//#endif
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

	tint.r = (float)((tinti >> 16) & 0xff) / 255.0f;
	tint.g = (float)((tinti >> 8) & 0xff) / 255.0f;
	tint.b = (float)((tinti >> 0) & 0xff) / 255.0f;

	input = RGBtoHSV(input);
	tint = RGBtoHSV(tint);

	input.r = tint.r;
	input.g = tint.g;
	
	input = HSVtoRGB(input);

	return input;
}

float4 get(in uint tileNum, in uint colorNum, in float texx, in float texy)
{
	if (tileNum == 0)
		return float4(0, 0, 0, 0.0f);

	float4 c = g_tileTextures.Sample(linearSampler, float3(texx, texy, tileNum));

	if (colorNum == 0)
		return c;

	float3 rgb = tint(c.rgb, colorNum);

	return float4(rgb, c.a);
}

float4 PS( PS_IN input ) : SV_Target
{
	float x = input.pos.x - (g_width - g_columns * g_tileSize) / 2;
	float y = input.pos.y - (g_height - g_rows * g_tileSize) / 2;

	if (x < 0 || y < 0 || x >= g_columns * g_tileSize || y >= g_rows * g_tileSize)
		return float4(1.0f, 0, 0, 1.0f);

	int col = (int)(x / g_tileSize);
	int row = (int)(y / g_tileSize);
	
	TileData td;

	td = g_tileBuffer.Load(row * g_columns + col);
	int tileNum = td.tilenum;

	if (tileNum == 0)
		return float4(0, 1.0f, 0, 1.0f);

	int t1 = (tileNum >> 0) & 0xff;
	int t2 = (tileNum >> 8) & 0xff;
	int t3 = (tileNum >> 16) & 0xff;
	int t4 = (tileNum >> 24) & 0xff;

	int colorNum = td.colornum;

	int ci1 = (colorNum >> 0) & 0xff;
	int ci2 = (colorNum >> 8) & 0xff;
	int ci3 = (colorNum >> 16) & 0xff;
	int ci4 = (colorNum >> 24) & 0xff;

	float texx = x / g_tileSize;
	float texy = y / g_tileSize;

	float4 c1, c2, c3, c4;

	c1 = get(t1, ci1, texx, texy);
	c2 = get(t2, ci2, texx, texy);
	c3 = get(t3, ci3, texx, texy);
	c4 = get(t4, ci4, texx, texy);

	/*
	if (t1 == 0)
		c1 = float4(0, 0, 0, 0.0f);
	else
		c1 = g_tileTextures.Sample(linearSampler, float3(texx, texy, t1));

	if (t2 == 0)
		c2 = float4(0, 0, 0, 0.0f);
	else
		c2 = g_tileTextures.Sample(linearSampler, float3(texx, texy, t2));

	if (t3 == 0)
		c3 = float4(0, 0, 0, 0.0f);
	else
		c3 = g_tileTextures.Sample(linearSampler, float3(texx, texy, t3));

	if (t4 == 0)
		c4 = float4(0, 0, 0, 0.0f);
	else
		c4 = g_tileTextures.Sample(linearSampler, float3(texx, texy, t4));
	*/
	float3 c;
	c = c1.rgb;
	c = c2.rgb * c2.a + c.rgb * (1.0f - c2.a);
	c = c3.rgb * c3.a + c.rgb * (1.0f - c3.a);
	c = c4.rgb * c4.a + c.rgb * (1.0f - c4.a);
	/*
	uint cidx1 = td.colornum & 0xff;
	if (cidx1 == 0)
		c = c1.rgb;
	else
		c = tint(c1.rgb, cidx1);
		*/
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
