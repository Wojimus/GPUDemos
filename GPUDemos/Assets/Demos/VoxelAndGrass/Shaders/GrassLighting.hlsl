#pragma once
#include "GrassTypes.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half3 IndirectLight(v2f i)
{
    //Get IndirectLighting
    half3 averageSH = SampleSH(TransformObjectToWorldNormal(i.normal));
    return averageSH;
}

half3 ShadeSingleLight(Light light, bool isAdditionalLight, v2f i)
{
    // light's distance & angle fade for point light & spot light (see GetAdditionalPerObjectLight(...) in Lighting.hlsl)
    half distanceAttenuation = min(0.5, light.distanceAttenuation);

    //Calculate Lighting
    half3 calculatedLighting = LightingLambert(light.color, light.direction,
                                               TransformObjectToWorldNormal(i.normal));
    
    // Final Light Result
    return calculatedLighting * light.shadowAttenuation * distanceAttenuation * (
        isAdditionalLight ? 1.5 : 1);
}

half3 CompositeAllLightResults(half3 indirectResult, half3 mainLightResult, half3 additionalLightSumResult,
                               half3 colour)
{
    //Lighting Sum
    //half3 rawLightSum = max(mainLightResult, additionalLightSumResult) + indirectResult;
    half3 rawLightSum = mainLightResult + additionalLightSumResult + indirectResult;

    // Return modified colour
    return colour * rawLightSum;
}

half4 CalculateLighting(half4 colour, v2f i)
{
    // Indirect lighting
    half3 indirectResult = IndirectLight(i);

    //Get Main Light
    Light mainLight = GetMainLight();

    //Shadow Coord
    float4 shadowCoord = TransformWorldToShadowCoord(i.posWS);

    //Shadow Attenuation
    mainLight.shadowAttenuation = MainLightRealtimeShadow(shadowCoord);

    //Main light
    half3 mainLightResult = ShadeSingleLight(mainLight, false, i);

    //Additional Lights
    half3 additionalLightSumResult = 0;
    // Returns the amount of lights affecting the object being renderer.
    // These lights are culled per-object in the forward renderer of URP.
    int additionalLightsCount = GetAdditionalLightsCount();
    for (int j = 0; j < additionalLightsCount; ++j)
    {
        // Similar to GetMainLight(), but it takes a for-loop index. This figures out the
        // per-object light index and samples the light buffer accordingly to initialized the
        // Light struct. If ADDITIONAL_LIGHT_CALCULATE_SHADOWS is defined it will also compute shadows.
        int perObjectLightIndex = GetPerObjectLightIndex(j);
        Light light = GetAdditionalPerObjectLight(perObjectLightIndex, i.posWS);
        // use original positionWS for lighting
        light.shadowAttenuation = AdditionalLightRealtimeShadow(perObjectLightIndex, i.posWS);
        // use offseted positionWS for shadow test

        // Different function used to shade additional lights.
        additionalLightSumResult += ShadeSingleLight(light, true, i);
    }

    //Calculate Lighting
    half3 colourWithLighting = CompositeAllLightResults(indirectResult, mainLightResult, additionalLightSumResult,
                                                        colour);

    return half4(colourWithLighting, colour.a);
}
