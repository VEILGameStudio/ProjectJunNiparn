// SpriteLitNormal
// A lit sprite shader with alpha clipping, foliage wind, and a normal map read from the sprite's
// Secondary Texture named "_NormalMap". This is what makes a flat tree card catch the light.
//
// Put this on: nothing. Make a material from it (Create > Material, pick Project/SpriteLitNormal)
// and use that material on the SpriteRenderer. See SpriteLit_Wind_Normal.mat (leaves),
// SpriteLit_Wind_Normal_Overlay.mat (holey leaf layer), SpriteLit_Trunk.mat (no wind) and
// SpriteLit_FlowGrass.mat (grass).
// Needs: the sprite has a Secondary Texture called _NormalMap (Sprite Editor > Secondary Textures).
// Without one the sprite is lit as if it were flat, which is fine for grass.
//
// How the wind works (CLAUDE.md section 7, "Wind"):
// The card itself never moves. Each pixel reads the sprite from a slightly shifted spot, so the
// leaves ripple inside the card. The shift comes from the Flow Noise texture (red = sideways,
// green = up and down, 0.5 = still), slid slowly across the world, plus a push from any
// FoliagePusher nearby (the player). Shifts are rounded to whole texels so the art stays sharp.
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

        // Written once per frame by the scene's FoliageWindFeed from every FoliagePusher.
        #define FOLIAGE_MAX_PUSHERS 8
        float4 _FoliagePusherPosition[FOLIAGE_MAX_PUSHERS]; // xyz = world centre, w = radius
        float4 _FoliagePusherPush[FOLIAGE_MAX_PUSHERS];     // xyz = push (0 to 1 long), w unused
        float _FoliagePusherCount;

        // World height of this point above the sprite's pivot (its feet).
        float HeightAbovePivot(float3 positionWS)
        {
            return positionWS.y - UNITY_MATRIX_M._m13;
        }

        // Reads the flow noise: red and green around 0.5 become a shift between -1 and 1.
        float2 SampleFlow(float2 noiseUV)
        {
            return (SAMPLE_TEXTURE2D(_FlowNoise, sampler_FlowNoise, noiseUV).rg - 0.5) * 2.0;
        }

        // Returns the UV to sample instead of uv, moved by the wind and by nearby pushers.
        // Call it in every pass before sampling _MainTex, so colour, depth and SSAO agree.
        float2 FoliageWindUV(float2 uv, float3 positionWS, float heightAbovePivot)
        {
            // The card's own axes in the world: right, up, and away from the camera.
            float3 cardRightWS = normalize(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
            float3 cardForwardWS = normalize(float3(UNITY_MATRIX_M._m02, UNITY_MATRIX_M._m12, UNITY_MATRIX_M._m22));
            float2 cardPosition = float2(dot(positionWS, cardRightWS), positionWS.y);

            // How much UV one world unit across the card is, measured from neighbouring pixels.
            // Works for any Pixels Per Unit, any scale, and inside a sprite atlas.
            float2 uvDx = ddx(uv);
            float2 uvDy = ddy(uv);
            float2 cardDx = ddx(cardPosition);
            float2 cardDy = ddy(cardPosition);
            float2 uvPerUnit;
            uvPerUnit.x = (uvDx.x * cardDx.x + uvDy.x * cardDy.x) / max(cardDx.x * cardDx.x + cardDy.x * cardDy.x, 1e-8);
            uvPerUnit.y = (uvDx.y * cardDx.y + uvDy.y * cardDy.y) / max(cardDx.y * cardDx.y + cardDy.y * cardDy.y, 1e-8);

            // Wind: big slow gusts plus a small fast ripple, both sliding sideways over time.
            float gustScale = 1.0 / max(_GustSize, 0.01);
            float travel = _Time.y * _WindSpeed;
            float2 gust = SampleFlow((cardPosition - float2(travel, 0.0)) * gustScale);
            float2 rustle = SampleFlow((cardPosition - float2(travel * 1.6, travel * 0.3)) * gustScale * 4.0 + 0.37);
            float2 offset = gust * _GustStrength + rustle * _RustleStrength;

            // Push: every pusher near this pixel shoves it along its movement and out from its centre.
            float depthReach = max(_PushDepthReach, 0.001);
            int pusherCount = (int)_FoliagePusherCount;
            [loop] for (int i = 0; i < pusherCount; i++)
            {
                float3 toPixel = positionWS - _FoliagePusherPosition[i].xyz;
                float2 acrossCard = float2(dot(toPixel, cardRightWS), toPixel.y);
                float distanceAcross = length(acrossCard);
                float reach = 1.0 - distanceAcross / max(_FoliagePusherPosition[i].w, 0.001);
                float falloff = saturate(reach) * saturate(reach) * saturate(1.0 - abs(dot(toPixel, cardForwardWS)) / depthReach);
                float3 push = _FoliagePusherPush[i].xyz;
                float2 pushAcross = float2(dot(push, cardRightWS), push.y);
                float2 outward = acrossCard / max(distanceAcross, 0.0001) * length(push);
                offset += (pushAcross * 0.6 + outward * 0.4) * falloff * _PushStrength * _PushScale;
            }

            // Feet stay still; full movement from Full Movement Height upward.
            if (_FlowFullHeight > 0.0)
            {
                offset *= saturate(heightAbovePivot / _FlowFullHeight);
            }

            float2 offsetUV = offset * uvPerUnit;
            // The texel-size check skips snapping if Unity did not pass the sprite's real texture size.
            if (_SnapToTexels > 0.5 && _MainTex_TexelSize.x > 0.0 && _MainTex_TexelSize.x < 0.25)
            {
                // Whole texels only: the art shifts, it never smears.
                offsetUV = round(offsetUV / _MainTex_TexelSize.xy) * _MainTex_TexelSize.xy;
            }
            // A pixel that moves right shows what was on its left.
            return uv - offsetUV;
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
                float2 uv = FoliageWindUV(input.uv, input.positionWS, input.heightAbovePivot);
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
                float2 uv = FoliageWindUV(input.uv, input.positionWS, input.heightAbovePivot);
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
                float2 uv = FoliageWindUV(input.uv, input.positionWS, input.heightAbovePivot);
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
