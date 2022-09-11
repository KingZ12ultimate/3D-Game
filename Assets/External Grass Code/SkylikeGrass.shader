//
// Created by @Forkercat on 03/04/2021.
//
// A URP grass shader using compute shader rather than geometry shader.
// It includes "SkylikeGrass.hlsl" which contains vertex and fragment functions.
//
// Note that this shader works with the grass painter tool created by MinionsArt.
// Checkout the website for the tool scripts. I also made an updated version that
// introduces shortcuts just for convenience.
// https://www.patreon.com/posts/geometry-grass-46836032
//

Shader "Grass/SkylikeGrass"
{
    Properties
    {
        _BaseTex ("Base Texture", 2D) = "white" {}
        
        _TopColor("Top color", Color) = (1, 1, 1, 1) // Color of the highest layer
        _BaseColor("Base color", Color) = (1, 1, 1, 1) // Color of the lowest layer
        _AmbientStrength("Ambient Strength", Float) = 1.2
        _DiffuseStrength("Diffuse Strength", Float) = 2
    }

    SubShader {
        // UniversalPipeline needed to have this render in URP
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        // Forward Lit Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off // No culling since the grass must be double sided
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            // Signal this shader requires a compute buffer
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0

            // Alpha
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            
            // Lighting and shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // Register our functions
            #pragma vertex vert
            #pragma fragment frag

            // Include vertex and fragment functions
            #include "/SkylikeGrass.hlsl"

            ENDHLSL
        }
        
        // Shadow Casting Pass
        // In my use-case, I do not need it.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            // Signal this shader requires geometry function support
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0

            // Support all the various light  ypes and shadow paths
            #pragma multi_compile_shadowcaster

            // Register our functions
            #pragma vertex vert
            #pragma fragment frag

            // A custom keyword to modify logic during the shadow caster pass
            #define SHADERPASS_SHADOWCASTER

            #pragma shader_feature_local _ DISTANCE_DETAIL
            
            // Include vertex and fragment functions
            #include "SkylikeGrass.hlsl"
            
            ENDHLSL
        }
    }
}