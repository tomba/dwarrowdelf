
float4 VS( float2 vin : POSITION ) : SV_POSITION
{
	return float4(vin * 2 - 1, 0.0f, 1.0f);
}
