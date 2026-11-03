// SpriteGrass
// The grass shader: grass tufts and whole strips of grass or undergrowth sway in the wind and part
// when the player walks through them. Same wind as the trees (FoliageWind.hlsl), plus three things
// grass needs that a tree does not:
// - Still Below Height: soil, roots and the dark band at the bottom of a strip never move.
// - Bend Curve: the tips move most and the middle less, so a blade bends instead of sliding.
// - Scene Lighting: 1 = lit by the scene like everything else, 0 = colours exactly as drawn
//   (for painted background strips that were never meant to be lit).
//
// Put this on: nothing. Make a material from it (Create > Material, pick Project/SpriteGrass)
// and use that material on the SpriteRenderer. See SpriteLit_FlowGrass.mat (tufts on the path),
// SpriteGrass_Undergrowth.mat and SpriteGrass_ForegroundLip.mat (painted strips in Test_Forest).
// Needs: the sprite's Mesh Type is Full Rect, its pivot is at the bottom (where the grass meets
// the ground), and there is transparent space above the tips, or moving pixels get cut off.
//
// Rules from CLAUDE.md this shader follows:
// - Alpha clipping, never alpha blending (the sprite is Opaque and writes depth).
// - No ShadowCaster pass, so a sprite never casts a flat card-shaped shadow.
// - The wind runs in every pass that draws the sprite (Forward, DepthOnly, DepthNormals).
// - The height mask is world height above the pivot, never UV.y.
// - Time is _Time, so the grass stops when the game is paused.
Shader "Project/SpriteGrass"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
        _Lighting ("Scene Lighting (1 = lit by the scene, 0 = colours exactly as drawn)", Range(0, 1)) = 1
        [NoScaleOffset] _FlowNoise ("Flow Noise (RG, sRGB off; 0.5 = still)", 2D) = "grey" {}
        _GustStrength ("Gust Strength (world units, big slow sway)", Float) = 0.05
        _RustleStrength ("Rustle Strength (world units, small fast ripple)", Float) = 0.03
        _GustSize ("Gust Size (world units across one gust)", Float) = 4
        _WindSpeed ("Wind Speed (world units per second the gusts travel)", Float) = 1
        _RootHeight ("Still Below Height (world units above the pivot that never move: soil, roots)", Float) = 0
        _FlowFullHeight ("Full Movement Height (world units above Still Below Height where the tips move fully)", Float) = 0.5
        _BendCurve ("Bend Curve (1 = straight lean, 2 = tips bend more than the middle)", Range(1, 3)) = 2
        _PushStrength ("Push Strength (world units a passing player shifts it)", Float) = 0.25
        _PushScale ("Push Scale (multiplies Push Strength)", Range(0, 1)) = 1
        _PushDepthReach ("Push Depth Reach (how far in front or behind the player still pushes)", Float) = 0.8
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
        TEXTURE2D(_FlowNoise);
        SAMPLER(sampler_FlowNoise);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _AlphaClipThreshold;
            float _Lighting;
            float4 _MainTex_TexelSize;
            float _GustStrength;
            float _RustleStrength;
            float _GustSize;
            float _WindSpeed;
            float _RootHeight;
            float _FlowFullHeight;
            float _BendCurve;
            float _PushStrength;
            float _PushScale;
            float _PushDepthReach;
            float _SnapToTexels;
        CBUFFER_END

        // The same wind as the trees.
        #include "Assets/_Project/Art/Shaders/FoliageWind.hlsl"

        // How much a grass pixel may move: nothing below Still Below Height, then rising to full
        // over Full Movement Height, curved so the tips bend more than the middle.
        float GrassMovementWeight(float heightAbovePivot)
        {
            float heightInBlade = saturate((heightAbovePivot - _RootHeight) / max(_FlowFullHeight, 0.001));
            return pow(heightInBlade, _BendCurve);
        }
        ENDHLSL

        // ---------------------------------------------------------------- Forward (colour)
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
                float fogFactor : TEXCOORD3;
                float heightAbovePivot : TEXCOORD4;
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
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = FoliageWindUV(input.uv, input.positionWS, GrassMovementWeight(input.heightAbovePivot));
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * input.color;
                clip(sprite.a - _AlphaClipThreshold);

                float3 normalWS = normalize(input.normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = sprite.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = half3(0, 0, 1);

                half3 litColor = UniversalFragmentPBR(inputData, surfaceData).rgb;
                litColor = MixFog(litColor, inputData.fogCoord);

                // Scene Lighting 0 keeps the colours exactly as drawn: no light, no shadow, no fog.
                half3 color = lerp(sprite.rgb, litColor, _Lighting);
                return half4(color, 1.0);
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
                float2 uv = FoliageWindUV(input.uv, input.positionWS, GrassMovementWeight(input.heightAbovePivot));
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
                float2 uv = FoliageWindUV(input.uv, input.positionWS, GrassMovementWeight(input.heightAbovePivot));
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
