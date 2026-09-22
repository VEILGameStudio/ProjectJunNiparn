// SpriteOutline
// A URP sprite shader that draws the normal sprite plus a soft outline around its
// edges. Turn the outline on/off by changing Outline Thickness (0 = no outline).
// The HighlightEffect script changes the thickness at runtime.
//
// Works in both game modes: it has a pass for the URP 2D Renderer (2D scenes) and
// a pass for the URP 3D/Forward Renderer (2.5D scenes).
//
// How a non-coder uses it: right-click in Project > Create > Material, then set the
// material's Shader to "Game/Sprite Outline", and put that material on the sprite.

Shader "Game/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,0.6)
        _OutlineThickness ("Outline Thickness", Range(0,8)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color      : COLOR;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float4 color       : COLOR;
            float2 uv          : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;
        float4 _Color;
        float4 _OutlineColor;
        float _OutlineThickness;

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = IN.uv;
            OUT.color = IN.color;
            return OUT;
        }

        // Adds up the sprite's alpha at eight nearby points, used to find edges.
        half NeighborAlpha(float2 uv, float2 offset)
        {
            half a = 0;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, 0)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, 0)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0,  offset.y)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -offset.y)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x,  offset.y)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x,  offset.y)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, -offset.y)).a;
            a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, -offset.y)).a;
            return saturate(a);
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color * _Color;

            if (_OutlineThickness <= 0)
            {
                return tex;
            }

            float2 offset = _MainTex_TexelSize.xy * _OutlineThickness;
            half neighbor = NeighborAlpha(IN.uv, offset);

            // Outline shows where the sprite is empty but a neighbor is solid.
            half outline = saturate(neighbor - tex.a);
            half4 outlineCol = _OutlineColor;
            outlineCol.a *= outline;

            // Draw the sprite over the outline.
            half4 col;
            col.rgb = lerp(outlineCol.rgb, tex.rgb, tex.a);
            col.a = max(tex.a, outlineCol.a);
            return col;
        }
        ENDHLSL

        // Pass for the URP 2D Renderer (2D scenes).
        Pass
        {
            Name "Sprite2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        // Pass for the URP 3D/Forward Renderer (2.5D scenes).
        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
