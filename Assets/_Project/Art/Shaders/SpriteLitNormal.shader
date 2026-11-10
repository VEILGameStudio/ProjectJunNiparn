// SpriteLitNormal
// A lit sprite shader with alpha clipping, foliage wind, and a normal map read from the sprite's
// Secondary Texture named "_NormalMap". This is what makes a flat tree card catch the light.
//
// Put this on: nothing. Make a material from it (Create > Material, pick Project/SpriteLitNormal)
// and use that material on the SpriteRenderer. See SpriteLit_Wind_Normal.mat (leaves),
// SpriteLit_Wind_Normal_Overlay.mat (holey leaf layer) and SpriteLit_Trunk.mat (no wind).
// Grass uses its own shader, Project/SpriteGrass.
// Needs: the sprite has a Secondary Texture called _NormalMap (Sprite Editor > Secondary Textures).
// Without one the sprite is lit as if it were flat.
//
// How the wind works: see FoliageWind.hlsl, shared with SpriteGrass (CLAUDE.md section 7, "Wind").
//
// Rules from CLAUDE.md this shader follows:
// - Alpha clipping, never alpha blending (the sprite is Opaque and writes depth).
// - No ShadowCaster pass, so a sprite never casts a flat card-shaped shadow.
// - The wind runs in every pass that draws the sprite (Forward, DepthOnly, DepthNormals).
// - The wind's height mask is world height above the pivot (the pivot is at the feet), no UV.y.
// - Time is _Time, so wind stops when the game is paused.
// - The sprite needs Mesh Type Full Rect with a transparent margin, or moving pixels get cut off.
Shader "Project/SpriteLitNormal"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NormalMap ("Normal Map (Secondary Texture)", 2D) = "bump" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
        [NoScaleOffset] _FlowNoise ("Flow Noise (RG, sRGB off; 0.5 = still)", 2D) = "grey" {}
        _GustStrength ("Gust Strength (world units, big slow sway)", Float) = 0.04
        _RustleStrength ("Rustle Strength (world units, small fast ripple)", Float) = 0.02
        _GustSize ("Gust Size (world units across one gust)", Float) = 5
        _WindSpeed ("Wind Speed (world units per second the gusts travel)", Float) = 1
        _FlowFullHeight ("Full Movement Height (world units above the feet; 0 = whole sprite moves)", Float) = 1.5
        _PushStrength ("Push Strength (world units a passing player shifts it)", Float) = 0.2
        _PushScale ("Push Scale (1 = base leaves, about 0.25 = holey overlay layer)", Range(0, 1)) = 1
        _PushDepthReach ("Push Depth Reach (how far in front or behind the player still pushes)", Float) = 6
        [ToggleUI] _SnapToTexels ("Snap To Whole Texels", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        Cull Off
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);
        TEXTURE2D(_FlowNoise);
        SAMPLER(sampler_FlowNoise);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _NormalStrength;
            float _AlphaClipThreshold;
            float4 _MainTex_TexelSize;
            float _GustStrength;
            float _RustleStrength;
            float _GustSize;
            float _WindSpeed;
            float _FlowFullHeight;
            float _PushStrength;
            float _PushScale;
            float _PushDepthReach;
            float _SnapToTexels;
        CBUFFER_END

        // The wind itself is shared with SpriteGrass, so trees and grass always blow alike.
        #include "Assets/_Project/Art/Shaders/FoliageWind.hlsl"

        // How much a tree pixel may move: still at the feet, fully from Full Movement Height upward.
        float TreeMovementWeight(float heightAbovePivot)
        {
            if (_FlowFullHeight <= 0.0)
            {
                return 1.0;
            }
            return saturate(heightAbovePivot / _FlowFullHeight);
        }
        ENDHLSL

        // ---------------------------------------------------------------- Forward (lit colour)
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                float heightAbovePivot : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.heightAbovePivot = HeightAbovePivot(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;

                // SpriteRenderer gives no normals, so the card faces -Z in object space.
                output.normalWS = TransformObjectToWorldNormal(float3(0.0, 0.0, -1.0));
                output.tangentWS = TransformObjectToWorldDir(float3(1.0, 0.0, 0.0));
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = FoliageWindUV(input.uv, input.positionWS, TreeMovementWeight(input.heightAbovePivot));
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * input.color;
                clip(sprite.a - _AlphaClipThreshold);

                // Unpack the tangent-space normal, scale its tilt, then move it to world space.
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
                normalTS.xy *= _NormalStrength;
                normalTS = normalize(normalTS);

                // The card always shows its front face, so the fixed front normal is used.
                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS);
                float3 bitangentWS = cross(normalWS, tangentWS);
                // Flip the bitangent so green points up the screen for the upright card.
                bitangentWS = -bitangentWS;
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                float3 bumpedNormalWS = normalize(mul(normalTS, tangentToWorld));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = bumpedNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(bumpedNormalWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = sprite.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.normalTS = normalTS;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- DepthOnly (depth buffer)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float alphaTint : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float heightAbovePivot : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.heightAbovePivot = HeightAbovePivot(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.alphaTint = input.color.a * _Color.a;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = FoliageWindUV(input.uv, input.positionWS, TreeMovementWeight(input.heightAbovePivot));
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * input.alphaTint;
                clip(alpha - _AlphaClipThreshold);
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- DepthNormals (SSAO reads this)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float alphaTint : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float heightAbovePivot : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.heightAbovePivot = HeightAbovePivot(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.alphaTint = input.color.a * _Color.a;
                output.normalWS = TransformObjectToWorldNormal(float3(0.0, 0.0, -1.0));
                return output;
            }

            void frag(Varyings input, out half4 outNormalWS : SV_Target0)
            {
                float2 uv = FoliageWindUV(input.uv, input.positionWS, TreeMovementWeight(input.heightAbovePivot));
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * input.alphaTint;
                clip(alpha - _AlphaClipThreshold);

                float3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    outNormalWS = half4(packedNormalWS, 0.0);
                #else
                    outNormalWS = half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
