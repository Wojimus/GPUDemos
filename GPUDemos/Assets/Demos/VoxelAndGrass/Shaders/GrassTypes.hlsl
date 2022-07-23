#pragma once

struct vertex
{
    float3 position;
    float2 uv;
};

struct tri
{
    vertex verts[3];
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 pos : SV_POSITION;
    float3 normal: TEXCOORD1;
    float3 posWS: TEXCOORD2;
};
