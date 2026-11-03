// FoliageFlow.hlsl  (SKILL REFERENCE — not in Assets/, not compiled, not tested in Unity)
// Per-pixel wind and player push for foliage sprites, the technique from the reference clip:
// a flow-noise texture (R = sideways, G = up/down, 0.5 = still) plus a push from nearby
// "disturbers" (the player), turned into a whole-texel UV offset in the fragment stage.
//
// How to use inside SpriteLitNormal-style shaders:
//   1. #include this after Core.hlsl, inside HLSLINCLUDE.
//   2. Add the per-material fields below to the UnityPerMaterial CBUFFER (keeps SRP Batcher).
//   3. In EVERY pass that samples _MainTex for colour or for clip() (Forward, DepthOnly,
//      DepthNormals), pass positionWS and heightOS from the vertex stage and call
//      FoliageFlowUV(uv, positionWS, heightOS) before sampling. Miss one pass and SSAO/depth
//      show the undisplaced silhouette while the colour moves.
//
// Per-material (put these inside CBUFFER_START(UnityPerMaterial)):
//   float4 _MainTex_TexelSize;   // set by Unity: (1/width, 1/height, width, height)
//   float  _FlowTexels;          // wind strength in sprite texels (clip: ~1-2 on a 121 px tree)
//   float  _FlowScale;           // world units -> noise UV (smaller = bigger gusts; clip: gusts larger than the tree)
//   float4 _FlowScroll;          // noise UV per second (xy)
//   float  _DisturbTexels;       // player push strength in texels (clip: ~5-6)
//   float  _DisturbScale;        // 1 on the base leaves, ~0.25 on the holey overlay layer
//   float  _FlowFullHeight;      // 0 = whole sprite moves (tree canopy); >0 = height in world
//                                // units where grass reaches full movement (feet stay still)

TEXTURE2D(_FlowNoise);
SAMPLER(sampler_FlowNoise);

// Globals written once per frame by the scene's FoliageWindFeed script.
#define FOLIAGE_MAX_DISTURBERS 8
float4 _FoliageDisturberPosition[FOLIAGE_MAX_DISTURBERS]; // xyz = world position, w = radius
float4 _FoliageDisturberPush[FOLIAGE_MAX_DISTURBERS];     // xyz = world push (direction x 0..1), w unused
float _FoliageDisturberCount;

// Returns the sprite UV to sample, moved by wind and by nearby disturbers.
// cardRightWS / cardUpWS: the card's right and up in world space (object X and Y).
float2 FoliageFlowUV(float2 uv, float3 positionWS, float heightOS, float3 cardRightWS, float3 cardUpWS)
{
    // Noise coordinates on the card's own plane, so it works at yaw 0 and yaw 45 alike.
    float2 planePosition = float2(dot(positionWS, cardRightWS), positionWS.y);
    float2 noiseUV = planePosition * _FlowScale + _Time.y * _FlowScroll.xy;
    float2 flow = SAMPLE_TEXTURE2D(_FlowNoise, sampler_FlowNoise, noiseUV).rg - 0.5;
    // Unity UV y points up; a GameMaker-style texture (green = down) needs flow.y = -flow.y here.
    float2 offsetTexels = flow * 2.0 * _FlowTexels;

    float3 pushWS = float3(0.0, 0.0, 0.0);
    int count = (int)_FoliageDisturberCount;
    for (int i = 0; i < count; i++)
    {
        float3 toPixel = positionWS - _FoliageDisturberPosition[i].xyz;
        float falloff = saturate(1.0 - length(toPixel) / max(_FoliageDisturberPosition[i].w, 0.001));
        pushWS += _FoliageDisturberPush[i].xyz * (falloff * falloff);
    }
    float2 pushOnCard = float2(dot(pushWS, cardRightWS), dot(pushWS, cardUpWS));
    offsetTexels += pushOnCard * (_DisturbTexels * _DisturbScale);

    // Grass: feet still, tips move. Trees leave _FlowFullHeight at 0 and skip this.
    if (_FlowFullHeight > 0.0)
    {
        offsetTexels *= saturate(heightOS / _FlowFullHeight);
    }

    // Whole texels only, so pixel art stays crisp (the clip never shows half pixels).
    offsetTexels = round(offsetTexels);
    // A pixel that moves right reads from its left.
    return uv - offsetTexels * _MainTex_TexelSize.xy;
}
