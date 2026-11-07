---
name: unity-urp-shader
description: Use when making or changing a shader, Shader Graph, material, post-processing Volume or URP renderer feature, or when a scene has too many draw calls, overdraw, or a material breaks batching. Not for foliage wind (use sprite-wind-foliage).
---

# URP shaders, materials and post-processing

Project: Unity 6000.5.10f1, **URP 17.5, 3D Forward renderer**. HDRP, the Built-in pipeline and VFX Graph
are not used: never write HDRP Volume components, surface shaders, or VFX Graph assets.
`CLAUDE.md` wins over this skill. Shader code under `Art/Shaders/` is core art tech — ask Oak before
changing an existing shader.

## 1. Project rules first (CLAUDE.md "Sprites", "Wind", "Settings")

- **Sprite materials: Surface Type Opaque + Alpha Clipping. Never alpha blending.** Blended sprites do not
  write depth and flicker. The only exception is the soft blob shadow quad.
- Sprites **do not cast shadows** (Cast Shadows = Off). 3D floor, walls and props do.
- Foliage wind lives once in `Art/Shaders/FoliageWind.hlsl`, used by `SpriteLitNormal` (trees) and
  `SpriteGrass` (grass). Any wind change → `sprite-wind-foliage`. Never copy the wind into a third shader.
  `SpriteLitOutline` and `SpriteWind.shadersubgraph` are legacy.
- **Every pass a shader has must do the same UV / vertex work** (Forward, `DepthOnly`, `DepthNormals`,
  `ShadowCaster`). A pass that is missed compiles and draws SSAO or shadows in the wrong place.
- **Shader time follows game time** (`_Time` is scaled by `timeScale`). Never add a global time uniform.
- Brightness is the settings system's URP Global Volume (Color Adjustments → Post Exposure). Nothing else
  changes it.
- **No Depth of Field in a `ThreeQuarter` room** (orthographic, and it hides puzzle details). A `SideView`
  corridor may use DoF in its own zone Volume.
- The interactable highlight is a URP Shader Graph outline that follows the silhouette.

## 2. Shader Graph or HLSL

- **Shader Graph first.** Write HLSL only when Shader Graph cannot do it (shared code across shaders like
  `FoliageWind.hlsl`, texel-snapped UV tricks, custom passes).
- Shader Graph: reusable logic in **Sub Graphs**; label nodes and group them with a Sticky Note that says
  what the group does; expose only properties an artist should touch, with a sensible default.
- Naming for new graphs: `SG_<Category>_<Name>` (e.g. `SG_Sprite_Dissolve`). Existing shaders keep their names.
- HLSL in URP:
  - include URP's `Core.hlsl` / `Lighting.hlsl`, not Built-in `UnityCG.cginc`;
  - put every material property in `CBUFFER_START(UnityPerMaterial)` … `CBUFFER_END`, **the same list in
    every pass**, or the SRP Batcher turns off for that shader (the Inspector says "not compatible");
  - `half` where full `float` is not needed (colours, normalised values); `float` for positions and UV
    maths that must stay exact (texel snapping);
  - comment every line that is not obvious, in simple English.

## 3. Keywords and variants

- Every keyword doubles the variants. Use **`shader_feature_local`** for per-material toggles (stripped
  when no material uses them); `multi_compile` only for things that change at runtime (URP's own shadow
  and fog keywords).
- Prefer a float property + `lerp` over a keyword for small differences.

## 4. Post-processing

- Volume-based only. One Global Volume for the base look, plus each zone's own Volume where CLAUDE.md
  allows it. Use priority and blend distance; do not enable / disable effects from gameplay scripts.
- Keep it cheap: Bloom, Tonemapping, Color Adjustments, Vignette are fine. Ask before adding SSR-like or
  heavy custom effects.
- A custom effect is a `ScriptableRendererFeature` + `ScriptableRenderPass` using **`RecordRenderGraph`**.
  The old `Execute(ScriptableRenderContext, ref RenderingData)` no longer runs (Compatibility Mode was
  removed in Unity 6.4). Adding a renderer feature to the URP Renderer asset is a core change → ask Oak.

## 5. Draw calls and overdraw

- Check with the **Frame Debugger** (which draw broke a batch and why) and the Scene view
  **Overdraw** mode before changing anything.
- Keep every material **SRP Batcher compatible**. Many sprites with the same shader then batch even with
  different textures.
- GPU Instancing for many copies of the same mesh + material (grass tufts, stones) if the SRP Batcher is
  not already handling them.
- Overdraw: the usual culprit is large mostly-transparent cards. Trim empty space, but keep the
  transparent margin the wind needs (Full Rect mesh, CLAUDE.md "Wind").
- Particles: the built-in Particle System (Shuriken) is what we use. Set a max particle count on every
  system, and pool one-shot effects.

## 6. Hand back (Thai, CLAUDE.md §10)

Say which shader / material / Volume changed and why, and give a manual check:
- [ ] looks right in the Game view in a `ThreeQuarter` and a `SideView` area
- [ ] the material still says SRP Batcher compatible
- [ ] pause the game: anything time-driven stops; Scene view still previews
- [ ] no shadow / SSAO in the wrong place (all passes match)
- [ ] Frame Debugger draw count before → after, if performance was the reason
