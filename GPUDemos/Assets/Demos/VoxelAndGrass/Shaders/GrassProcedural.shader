Shader "Lit/GrassProcedural"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0, 0.66, 0.2, 1)
        _Gradient ("Gradient Texture", 2D) = "white" {}
        _WindTexture ("Wind Texture", 2D) = "White" {}
        _WindFrequency ("Wind Frequency", float) = 1
        _WindAmplitude ("Wind Amplitude", float) = 1
    }
    SubShader
    {
        cull off
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                // "Lightmode" matches the "ShaderPassName" set in UniversalRenderPipeline.cs. 
                // SRPDefaultUnlit and passes with no LightMode tag are also rendered by Universal Render Pipeline

                // "Lightmode" tag must be "UniversalForward" in order to render lit objects in URP.
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GrassTypes.hlsl"
            #include "GrassLighting.hlsl"
            #include  "GrassAnimation.hlsl"

            StructuredBuffer<tri> triangles;
            int LODMultiplier;

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            sampler2D _Gradient;
            sampler2D _WindTexture;
            float _WindFrequency;
            float _WindAmplitude;
            CBUFFER_END

            float3 ApplyWind(float3 vertexPos, float2 uv)
            {
                //Gradient Noise
                float2 gradientNoise = Unity_GradientNoise_float(float2(vertexPos.x + _Time.y, vertexPos.z + _Time.y),
                                                                 _WindFrequency) * uv.y * _WindAmplitude;

                return vertexPos + float3(gradientNoise.x, 0, gradientNoise.y);
            }

            float3 CalculateNormal(float3 vertex1, float3 vertex2, float3 vertex3)
            {
                /*
                //Create the two vectors
                float3 vector1 = vertex2 - vertex1;
                float3 vector2 = vertex3 - vertex1;

                //Cross Product
                float3 crossProduct = cross(vector1, vector2);

                //Normalise
                float3 normal = normalize(crossProduct);
                */

                //OVERRIDE
                //Normal facing up for consistent lighting
                half3 normal = half3(0, 1, 0);

                //Return Normal
                return normal;
            }

            v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                v2f o; //Defining Output

                //Get Triangle And Vertex
                tri currentTriangle = triangles[instanceID * LODMultiplier];
                vertex currentVertex = currentTriangle.verts[vertexID];

                //Apply Wind
                currentVertex.position = ApplyWind(currentVertex.position, currentVertex.uv);

                //Get Normal
                float3 normal = CalculateNormal(
                    TransformObjectToHClip(currentTriangle.verts[0].position),
                    TransformObjectToHClip(currentTriangle.verts[1].position),
                    TransformObjectToHClip(currentTriangle.verts[2].position));

                //Get World pos
                float3 worldPos = mul(unity_ObjectToWorld, float4(currentVertex.position.xyz, 1.0)).xyz;

                //Set Outputs
                o.pos = TransformObjectToHClip(currentVertex.position);
                o.uv = currentVertex.uv;
                o.normal = normal;
                o.posWS = worldPos;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                //Colour Gradient
                half4 colour = _BaseColor * tex2D(_Gradient, i.uv);

                //Lighting
                colour = CalculateLighting(colour, i);

                //Display Final Colour
                return colour;
            }
            ENDHLSL
        }
    }
}