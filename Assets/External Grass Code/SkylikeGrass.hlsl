//
// Created by @Forkercat on 03/04/2021.
//
// A URP grass shader using compute shader rather than geometry shader.
// This file contains vertex and fragment functions. It also defines the
// structures which should be the same with the ones used in SkylikeGrassCompute.compute.
//
// References & Credits:
// 1. GrassBlades.hlsl (https://gist.github.com/NedMakesGames/3e67fabe49e2e3363a657ef8a6a09838)
// 2. GrassGeometry.shader (https://pastebin.com/VQHj0Uuc)
//

// Make sure this file is not included twice
#ifndef SKYLIKE_GRASS_INCLUDED
#define SKYLIKE_GRASS_INCLUDED

// Include some helper functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// This describes a vertex on the generated mesh
struct DrawVertex
{
    float3 positionWS; // The position in world space
    float2 uv;
    float3 brushColor;
};

// A triangle on the generated mesh
struct DrawTriangle
{
    float3 normalWS;
    float3 pivotWS;
    DrawVertex vertices[3]; // The three points on the triangle
};

// A buffer containing the generated mesh
StructuredBuffer<DrawTriangle> _DrawTriangles;

struct v2f
{
    float4 positionCS : SV_POSITION; // Position in clip space
    float2 uv : TEXCOORD0;          // The height of this vertex on the grass blade
    float3 positionWS : TEXCOORD1; // Position in world space
    float3 normalWS : TEXCOORD2;   // Normal vector in world space
    float3 brushColor : COLOR;
};

// Properties
float4 _BaseTex_ST;
TEXTURE2D(_BaseTex);
SAMPLER(sampler_BaseTex);

float4 _TopColor;
float4 _BaseColor;
float _AmbientStrength;
float _DiffuseStrength;

float _FogStartDistance;
float _FogEndDistance;

uniform float _HighlightRadius;  // set by compute renderer (not exposed)
uniform float3 _MovingPosition;  // set by player controller
uniform float _MovingSpeedPercent;

// ----------------------------------------

// Vertex function

// -- retrieve data generated from compute shader
v2f vert(uint vertexID : SV_VertexID)
{
    // Initialize the output struct
    v2f output;

    // Get the vertex from the buffer
    // Since the buffer is structured in triangles, we need to divide the vertexID by three
    // to get the triangle, and then modulo by 3 to get the vertex on the triangle
    DrawTriangle tri = _DrawTriangles[vertexID / 3];
    DrawVertex input = tri.vertices[vertexID % 3];

    // No Billboard
    // output.positionCS = TransformWorldToHClip(input.positionWS);

    // Billboard
    float4 pivotWS = float4(tri.pivotWS, 1);
    float4 pivotVS = mul(UNITY_MATRIX_V, pivotWS);
    
    float4 worldPos = float4(input.positionWS, 1);
    float4 flippedWorldPos = float4(-1, 1, -1, 1) * (worldPos - pivotWS) + pivotWS;
    float4 viewPos = flippedWorldPos - pivotWS + pivotVS;
    
    output.positionCS = mul(UNITY_MATRIX_P, viewPos);
  
  
    output.positionWS = input.positionWS;
    
    output.normalWS = normalize(tri.normalWS);
    
    output.uv = input.uv;

    output.brushColor = input.brushColor;

    return output;
}

// ----------------------------------------

// Fragment function

half4 frag(v2f input) : SV_Target
{
#ifdef SHADERPASS_SHADOWCASTER
    // For Shadow Caster Pass
    return 0;
#else

//     float shadow = 0;
// #if SHADOWS_SCREEN
//     half4 shadowCoord = ComputeScreenPos(input.positionCS);
// #else
//     half4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
// #endif  // SHADOWS_SCREEN
//   
//     Light mainLight = GetMainLight(shadowCoord);
//
// #ifdef _MAIN_LIGHT_SHADOWS
//     shadow = mainLight.shadowAttenuation;
// #endif

    Light mainLight = GetMainLight();
  
    float3 baseColor = lerp(_BaseColor, _TopColor, saturate(input.uv.y)) * input.brushColor;
    
    float3 ambient = baseColor * _AmbientStrength;
    float3 diffuse = baseColor * _DiffuseStrength;

    float NdotL = max(0, dot(mainLight.direction, input.normalWS));
    diffuse *= NdotL;

    // Combine
    float4 combined = float4(ambient + diffuse, 1);

    // Fog
    float distFromCamera = distance(_WorldSpaceCameraPos, input.positionWS);
    float fogFactor = (distFromCamera - _FogStartDistance) / (_FogEndDistance - _FogStartDistance);
    combined.rgb = MixFog(combined.rgb, 1 - saturate(fogFactor));

    // Interactor Highlight
    float distFromMovingPosition = distance(_MovingPosition, input.positionWS);
    if (distFromMovingPosition < _HighlightRadius)
    {
        combined.rgb *= (1 + _MovingSpeedPercent);
    }

    // Texture Mask Color (pure white + alpha)
    half4 texMaskColor = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, input.uv);
    
    return combined * texMaskColor;

#endif  // SHADERPASS_SHADOWCASTER
}

#endif  // SKYLIKE_GRASS_INCLUDED
