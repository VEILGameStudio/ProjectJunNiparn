// PropOutline
// A flat colour drawn on the BACK faces of a slightly bigger box, so only a rim shows around
// the box's silhouette. This is the outline for Props 3/4 (box props built from three quads).
//
// Put this on: nothing. Use the PropOutline material on the 'Outline' child of a 3/4 prop.
// Colour comes from the material, or from MeshHighlightEffect through a property block.
//
// Rules from CLAUDE.md this shader follows:
// - No ShadowCaster, DepthOnly or DepthNormals pass: the outline never casts a shadow and
//   never feeds SSAO.
// - It is a mesh rim, not a sprite, so it is the one thing here that may be alpha blended.
//   A box seen from outside has exactly one back face per pixel, so blended pieces never overlap.
Shader "Project/PropOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 0.6)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PropOutline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
