#pragma once

struct Vertex
{
    float3 position;
    float3 normal;
};

struct Boid3D {
    float3 position;
    float3 direction;
    float3 velocity;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float3 normal: TEXCOORD1;
    float3 posWS: TEXCOORD2;
    float3 color: TEXCOORD3;
};