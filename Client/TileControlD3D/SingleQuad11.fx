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

bool g_simpleTint;

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

float3 load_color(in uint coloridx)
{
	uint c = g_colorBuffer.Load(coloridx);
	
	float3 color;

	color.r = (c >> 16) & 0xff;
	color.g = (c >> 8) & 0xff;
	color.b = (c >> 0) & 0xff;
	color /= 255.0f;

	return color;
}

float3 RGBToHSL(in float3 color)
{
	float3 hsl; // init to 0 to avoid warnings ? (and reverse if + remove first part)
	
	float fmin = min(min(color.r, color.g), color.b);    //Min. value of RGB
	float fmax = max(max(color.r, color.g), color.b);    //Max. value of RGB
	float delta = fmax - fmin;             //Delta RGB value

	hsl.z = (fmax + fmin) / 2.0; // Luminance

	if (delta == 0.0)		//This is a gray, no chroma...
	{
		hsl.x = 0.0;	// Hue
		hsl.y = 0.0;	// Saturation
	}
	else                                    //Chromatic data...
	{
		if (hsl.z < 0.5)
			hsl.y = delta / (fmax + fmin); // Saturation
		else
			hsl.y = delta / (2.0 - fmax - fmin); // Saturation
		
		float deltaR = (((fmax - color.r) / 6.0) + (delta / 2.0)) / delta;
		float deltaG = (((fmax - color.g) / 6.0) + (delta / 2.0)) / delta;
		float deltaB = (((fmax - color.b) / 6.0) + (delta / 2.0)) / delta;

		if (color.r == fmax )
			hsl.x = deltaB - deltaG; // Hue
		else if (color.g == fmax)
			hsl.x = (1.0 / 3.0) + deltaR - deltaB; // Hue
		else if (color.b == fmax)
			hsl.x = (2.0 / 3.0) + deltaG - deltaR; // Hue

		if (hsl.x < 0.0)
			hsl.x += 1.0; // Hue
		else if (hsl.x > 1.0)
			hsl.x -= 1.0; // Hue
	}

	return hsl;
}

float HueToRGB(in float f1, in float f2, in float hue)
{
	if (hue < 0.0)
		hue += 1.0;
	else if (hue > 1.0)
		hue -= 1.0;
	float res;
	if ((6.0 * hue) < 1.0)
		res = f1 + (f2 - f1) * 6.0 * hue;
	else if ((2.0 * hue) < 1.0)
		res = f2;
	else if ((3.0 * hue) < 2.0)
		res = f1 + (f2 - f1) * ((2.0 / 3.0) - hue) * 6.0;
	else
		res = f1;
	return res;
}

float3 HSLToRGB(in float3 hsl)
{
	float3 rgb;
	
	if (hsl.y == 0.0)
	{
		rgb = float3(hsl.z, hsl.z, hsl.z); // Luminance
	}
	else
	{
		float f2;
		
		if (hsl.z < 0.5)
			f2 = hsl.z * (1.0 + hsl.y);
		else
			f2 = (hsl.z + hsl.y) - (hsl.y * hsl.z);
			
		float f1 = 2.0 * hsl.z - f2;
		
		rgb.r = HueToRGB(f1, f2, hsl.x + (1.0/3.0));
		rgb.g = HueToRGB(f1, f2, hsl.x);
		rgb.b= HueToRGB(f1, f2, hsl.x - (1.0/3.0));
	}
	
	return rgb;
}

float3 tint(in float3 input, in uint coloridx)
{
	float3 tint = load_color(coloridx);

	if (g_simpleTint)
	{
		return tint * input;
	}
	else
	{
		input = RGBToHSL(input);
		tint = RGBToHSL(tint);

		input.r = tint.r;
		input.g = tint.g;
	
		input = HSLToRGB(input);

		return input;
	}
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
		float3 bg = load_color(bgColorNum);

		c = float4(c.rgb + bg.rgb * (1.0f - c.a), 1.0f);
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
	c = c2.rgb + (1.0f - c2.a) * c.rgb;
	c = c3.rgb + (1.0f - c3.a) * c.rgb;
	c = c4.rgb + (1.0f - c4.a) * c.rgb;

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
