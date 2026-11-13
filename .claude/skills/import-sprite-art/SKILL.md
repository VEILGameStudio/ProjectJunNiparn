---
name: import-sprite-art
description: Use when a teammate brings new art - a character, NPC, monster, prop, grass, background layer, normal map or 3D model - to put into the game, or says a sprite looks too tall, flat, flickering or floating.
---

# Import art into the world

Spec: CLAUDE.md §0 rules 3, 4, 6, 7, §1 (parallax), §5 (art folders), §7 "Sprites in the world",
"Giving the world depth".

## 1. Decide sprite or model
| Object | Technique |
|---|---|
| animated character / NPC | billboard + normal map |
| grass, trees, flowers | billboard + wind shader (a tree is split into Trunk / Leaves / LeavesOverlay: see the `sprite-wind-foliage` skill and CLAUDE.md §7 Wind) |
| chest, cart, barrel, small building — reads as a solid volume | real 3D model (MagicaVoxel → `.obj`) |
| a box-shaped prop (wardrobe, shelf, crate, table) that someone already **painted as one 3/4 picture**, for a ThreeQuarter room | bake the picture onto a 3D box: use the `add-prop-threequarter` skill |
No sprite stacking.

## 2. Sprite checklist
1. Folder by preset: `Art/Sprites/ThreeQuarter/` or `SideView/`. `Shared/` only for art with no viewing
   angle (UI, icons, VFX). When in doubt it is not shared.
2. **Stretch the finished image vertically** before export: ×1.155 for ThreeQuarter, ×1.015 for SideView.
   Width unchanged. It looks too tall when viewed flat — that is correct.
3. **PPU = exported (stretched) pixel height ÷ intended world height.**
4. Pivot at the feet.
5. Material uses **alpha clipping**, never alpha blending.
6. **Cast Shadows off.** Add a blob shadow quad (the one alpha-blended exception).
7. Add `SpriteBillboard`. Never tilt the card; there is no tilt field.
8. Normal map: one file the same size/layout as the sheet, start with the shared soft cylinder map.
9. Outlined sprites use Mesh Type Full Rect.

## 3. Background layers (SideView only)
Place at distance D for speed 20/D; PPU = 1080 ÷ (0.335 × D); width covers screen at 21:9 plus camera travel.

## 4. Symptoms → cause
- too tall next to 3D → PPU taken from pre-stretch height, or card tilted as well as stretched
- flickering / swapping where sprites overlap → alpha blending
- flat card-shaped shadow → shadows not off
- looks subtly wrong in one area → sprite from the other preset's folder

Hand back with CLAUDE.md §10, in Thai.
