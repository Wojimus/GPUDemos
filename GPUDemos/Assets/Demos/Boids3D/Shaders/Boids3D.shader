Shader "Lit/Boids3D"
{
    Properties
    {
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
            #include "BoidTypes.hlsl"
            #include "BoidLighting.hlsl"

            StructuredBuffer<Vertex> boidVertices;
            StructuredBuffer<Boid3D> boids;
            float boidSize;
            
            float4 RotateAroundYInDegrees (float4 vertex, float degrees)
                 {
                     float alpha = degrees * PI / 180.0;
                     float sina, cosa;
                     sincos(alpha, sina, cosa);
                     float2x2 m = float2x2(cosa, -sina, sina, cosa);
                     return float4(mul(m, vertex.xz), vertex.yw).xzyw;
                 }

            v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                v2f o; //Defining Output

                //Get Boid And Vertex
                Boid3D currentBoid = boids[instanceID];
                Vertex currentVertex = boidVertices[vertexID];

                //Rotation
                float3 forward = currentBoid.direction;
                float3 right = normalize(cross(forward, float3(0,1,0)));
                float3 up = cross(right, forward);
                float3x3 rotationMatrix = float3x3(right, up, forward);

                //Calculate Vertex Position
                float3 vertexPos = mul(currentVertex.position, rotationMatrix) * boidSize + currentBoid.position;

                //Get World pos
                float3 worldPos = mul(unity_ObjectToWorld, float4(vertexPos, 1.0)).xyz;

                //Set Colour
                o.color = (normalize(currentBoid.velocity) + 1) / 2;

                //Set Outputs
                o.pos = TransformObjectToHClip(vertexPos);
                o.normal = currentVertex.normal;
                o.posWS = worldPos;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                //Colour Gradient
                half4 colour = half4(i.color.xyz, 1);
                colour.x = max(colour.x, 0.15);
                colour.y = 0.1f;
                colour.z = max(colour.z + colour.y, 0.15);

                //Display Final Colour
                return colour;
            }
            ENDHLSL
        }
        
    }
    FallBack "Diffuse"
}
