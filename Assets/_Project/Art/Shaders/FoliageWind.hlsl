// FoliageWind.hlsl
// The flow-map wind shared by every foliage shader: SpriteLitNormal (trees) and SpriteGrass (grass).
// One copy of the wind, so trees and grass always blow the same way (CLAUDE.md section 7, "Wind").
//
// Put this on: nothing. A shader includes it with
//   #include "FoliageWind.hlsl"
// after its UnityPerMaterial CBUFFER, which must declare these fields:
//   _MainTex_TexelSize, _GustStrength, _RustleStrength, _GustSize, _WindSpeed,
//   _PushStrength, _PushScale, _PushDepthReach, _SnapToTexels
// and after TEXTURE2D(_FlowNoise) and SAMPLER(sampler_FlowNoise).
//
// How it works: the card itself never moves. Each pixel reads the sprite from a slightly shifted
// spot, so the leaves ripple inside the card. The shift comes from the Flow Noise texture
// (red = sideways, green = up and down, 0.5 = still), slid slowly across the world, plus a push
// from any FoliagePusher nearby (the player). Shifts are rounded to whole texels so the art stays sharp.

#ifndef PROJECT_FOLIAGE_WIND_INCLUDED
#define PROJECT_FOLIAGE_WIND_INCLUDED

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
// movementWeight is how much this pixel may move (0 = still, 1 = fully); each shader works it out
// from the height above the pivot. Call this in every pass before sampling _MainTex, so colour,
// depth and SSAO agree.
float2 FoliageWindUV(float2 uv, float3 positionWS, float movementWeight)
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

    offset *= movementWeight;

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

#endif
