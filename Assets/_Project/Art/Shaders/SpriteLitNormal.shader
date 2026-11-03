// SpriteLitNormal
// A lit sprite shader with alpha clipping, wind, and a normal map read from the sprite's
// Secondary Texture named "_NormalMap". This is what makes a flat tree card catch the light.
//
// Put this on: nothing. Make a material from it (Create > Material, pick Project/SpriteLitNormal)
// and use that material on the SpriteRenderer. See SpriteLit_Wind_Normal.mat.
// Needs: the sprite has a Secondary Texture called _NormalMap (Sprite Editor > Secondary Textures).
//
// Rules from CLAUDE.md this shader follows:
// - Alpha clipping, never alpha blending (the sprite is Opaque and writes depth).
// - No ShadowCaster pass, so a sprite never casts a flat card-shaped shadow.
// - The wind runs in every pass that draws the sprite (Forward, DepthOnly, DepthNormals).
// - The wind mask is object-space Y with no divisor (the pivot is at the feet).
// - Time is _Time, so wind stops when the game is paused.
Shader "Project/SpriteLitNormal"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NormalMap ("Normal Map (Secondary Texture)", 2D) = "bump" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
        _WindStrength ("Wind Strength (peak sway = 1.5 x this x height)", Float) = 0.12
        _WindSpeed ("Wind Speed", Float) = 2
        _WindFrequency ("Wind Frequency (higher = neighbours sway less in step)", Float) = 0.7
        _WindDirection ("Wind Direction (world)", Vector) = (1, 0, 0, 0)
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

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _NormalStrength;
            float _AlphaClipThreshold;
            float _WindStrength;
            float _WindSpeed;
            float _WindFrequency;
            float4 _WindDirection;
        CBUFFER_END

        // Moves the top of the card sideways. The feet (object-space Y = 0) stay still.
        float3 ApplyWind(float3 positionOS)
        {
            float3 windDirectionWS = _WindDirection.xyz;
            if (dot(windDirectionWS, windDirectionWS) < 0.0001)
            {
                windDirectionWS = float3(1.0, 0.0, 0.0);
            }
            float3 windDirectionOS = TransformWorldToObjectDir(windDirectionWS, false);

            // The phase comes from where the object stands, so neighbours do not sway in step.
            float3 objectOriginWS = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
            float phase = (objectOriginWS.x + objectOriginWS.z) * _WindFrequency;
            float t = _Time.y * _WindSpeed;
            float sway = sin(t + phase) + 0.5 * sin(2.3 * t + 1.7 * phase);

            float height = max(positionOS.y, 0.0);
            return positionOS + windDirectionOS * (sway * height * _WindStrength);
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = ApplyWind(input.positionOS);
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
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
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(sprite.a - _AlphaClipThreshold);

                // Unpack the tangent-space normal, scale its tilt, then move it to world space.
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = ApplyWind(input.positionOS);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.alphaTint = input.color.a * _Color.a;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * input.alphaTint;
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = ApplyWind(input.positionOS);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.alphaTint = input.color.a * _Color.a;
                output.normalWS = TransformObjectToWorldNormal(float3(0.0, 0.0, -1.0));
                return output;
            }

            void frag(Varyings input, out half4 outNormalWS : SV_Target0)
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * input.alphaTint;
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
