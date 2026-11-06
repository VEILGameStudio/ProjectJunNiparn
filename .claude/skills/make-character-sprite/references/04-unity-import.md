# 04: Into Unity (PSD Importer + 2D Animation)

The project already has **PSD Importer** and **2D Animation**. A reference rig is in `Assets/FW_MIHO`.

1. Put the `.psb` in `Art/Sprites/SideView/Characters/<Name>/` (or `ThreeQuarter/`). Never a preset it was not drawn for (CLAUDE.md §5).
2. **Stretch before packing:** stretch every layer PNG vertically by ×1.015 (SideView) or ×1.155
   (ThreeQuarter), all layers by the same amount. Width stays the same.
3. Importer: Texture Type Sprite, **Mosaic** on, **Character Rig** on, Use Layer Grouping on.
4. **PPU = stretched height of the full figure in px ÷ world height.** Pivot at the feet (Bottom Center or custom).
5. Sprite Editor → Skinning Editor: put bones at the same points as `rigs.py`, Auto Geometry, Auto
   Weights, then fix the weights at the knees and elbows.
6. Material: alpha clipping, Cast Shadows Off, blob shadow under the feet (CLAUDE.md §0 rules 3 and 4).
7. Use a Sprite Library / Sprite Resolver if a part needs to swap drawings (an eye blinking, a hand closing).

Facing in `PlayerAnimator`: SideView has 6 facings, never mirrored; ThreeQuarter has 8. The two
straight profiles for 8 directions need their own drawings.
