// SpriteLitOutline
// The Custom Function used by the SpriteLitOutline shader graph.
// It draws the sprite, plus a highlight outline in the sprite's transparent border:
// a transparent pixel that has a solid pixel next to it becomes part of the outline.
// The outline goes out through Emission, so it stays bright even in dark places.
//
// Only public Shader Graph types are used here (UnityTexture2D, SAMPLE_TEXTURE2D),
// never URP's internal sprite variables, so a URP update should not break it.
//
// Put this on: nothing. The SpriteLitOutline shader graph uses it.
// The sprite needs a transparent border (at least as wide as the outline) and its
// import setting Mesh Type = Full Rect, or the outline has nowhere to be drawn.

#ifndef SPRITE_LIT_OUTLINE_INCLUDED
#define SPRITE_LIT_OUTLINE_INCLUDED

// The highest alpha among the 8 pixels around uv, "offset" away.
float HighestNeighbourAlpha(UnityTexture2D mainTex, float2 uv, float2 offset)
{
    float alpha = 0.0;
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2( offset.x, 0.0)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2(-offset.x, 0.0)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2(0.0,  offset.y)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2(0.0, -offset.y)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2( offset.x,  offset.y)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2(-offset.x,  offset.y)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2( offset.x, -offset.y)).a);
    alpha = max(alpha, SAMPLE_TEXTURE2D(mainTex.tex, mainTex.samplerstate, uv + float2(-offset.x, -offset.y)).a);
    return alpha;
}

// MainTex:     the sprite texture (the SpriteRenderer fills it in).
// Thickness:   outline width in texture pixels. 0 = no outline.
// ClipMask:    the dissolve mask from the graph (0.5 to 1). Raising the Alpha Clip
//              Threshold above 0.5 makes the sprite dissolve away in patches.
void SpriteOutline_float(UnityTexture2D MainTex, float2 UV, float4 VertexColor, float4 Tint,
                         float Thickness, float4 OutlineColor, float ClipMask,
                         out float3 BaseColor, out float3 Emission, out float Alpha)
{
    float4 sprite = SAMPLE_TEXTURE2D(MainTex.tex, MainTex.samplerstate, UV) * VertexColor;

    // Measure the texture size here instead of trusting _MainTex_TexelSize,
    // because the texture is set per renderer by the SpriteRenderer.
    float width;
    float height;
    MainTex.tex.GetDimensions(width, height);
    float2 offset = float2(1.0 / width, 1.0 / height) * Thickness;

    float neighbourAlpha = HighestNeighbourAlpha(MainTex, UV, offset);
    float isOutline = step(0.5, neighbourAlpha) * (1.0 - step(0.5, sprite.a)) * step(0.0001, Thickness);

    BaseColor = sprite.rgb * Tint.rgb * (1.0 - isOutline);
    Emission = OutlineColor.rgb * OutlineColor.a * isOutline;
    Alpha = min(max(sprite.a, isOutline), ClipMask);
}

#endif
