---
name: make-character-sprite
description: Use when turning a flat character drawing (a turnaround or direction sheet) into split layers for 2D animation - cutting arms, legs, head, hair, eyes and mouth cleanly with no white halo, packing a .psb for Unity 2D Animation, or previewing a smooth skinned walk before rigging in Unity.
---

# Make a character sprite: from a flat drawing to animation layers

Built while making Phawin (the main character) from the artist's 6-direction sheet, 2026-11-06.
The real files are in the project's `character-sprite/phawin/` folder; the configs that made them are in `examples/phawin/`.

Before importing into Unity, read `import-sprite-art` too: stretch, PPU, pivot and material are
there and in CLAUDE.md §7. This skill covers the steps before that.

## The 7 core points

1. **Remove the white halo with maths, not an eraser.** Most of the drawing's edge pixels are a mix of
   line colour and background. Unmix the background (alpha unmixing), then give each edge pixel the
   colour of the nearest solid pixel. The edge stays smooth and nothing white is left
   (`references/01-layer-split.md`).
2. **Cut only along edges that already exist in the drawing:** hair/face, sleeve hem, trouser hem,
   bag edge. Never cut through the middle of a flat colour area. A cut through the middle shows
   immediately when a part moves.
3. **A shared outline belongs to the layer on top.** One line split between two layers looks broken
   the moment either one moves.
4. **Paint underneath wherever something sits on top** (the torso under the arm, the leg under the
   hip), with flat colour, extending past the visible edge. When an arm swings, what shows behind it
   is cloth, not a hole.
5. **Split by how it moves, not by organ.** Rigid parts (the head) need few layers. Bending parts
   (arms, legs) need a joint on the bone. Swinging parts (bag, sash) get their own layer and lag
   slightly behind.
6. **Smoothness comes from bones and weights, not from more cuts.** Rotating separate pieces makes
   the joints look broken, no matter how fine the cut. Use Sprite Skin (bones plus weights) in Unity
   2D Animation. `scripts/skin.py` does the same thing in Python, so the walk can be checked before
   any rigging in Unity (`references/03-skinning-walk.md`).
7. **Every direction comes from a separate drawing, never a mirror.** SideView has 6 facings, each
   drawn separately (CLAUDE.md §7). Mirroring is used only on the cut coordinates (the config); the
   art is always the artist's own drawing for that direction.

## Workflow

| Step | What to do | Tool / read |
|---|---|---|
| 1 | Extract one frame of each direction from the sheet as PNG (flat background) | any tool |
| 2 | Write a config: crop, polygons around each part, layer order, fill colour | `references/02-config.md`, examples in `examples/phawin/` |
| 3 | Split the layers | `python3 scripts/split_layers.py frame.png cfg.json out_dir` |
| 4 | Check `out_dir/_labels.png` (colour per layer), then the contact sheet | `python3 scripts/preview_layers.py out_dir cfg.json 1 sheet.png` |
| 5 | Pack the `.psb` | `python3 scripts/make_psb.py out_dir cfg.json F Phawin_F.psb` |
| 6 | Preview a smooth walk before rigging | `scripts/skin.py` + `scripts/rigs.py`, example `examples/phawin/walk_6dir_gif.py` |
| 7 | Import into Unity: PSD Importer, Mosaic, Character Rig, then rig and weight | `references/04-unity-import.md` |

Requires Python 3 with `numpy scipy pillow opencv-python psd-tools`.

## Layer order (bottom to top), Phawin front view

`hair_back, foot_L, foot_R, leg_L, leg_R, hip, neck, torso, necklace, sash, arm_L, upperarm_L,
arm_R, upperarm_R, bag, face, eye_R, eye_L, mouth, hair_front`

- `_R` and `_L` are from the **character's** point of view (Phawin's right hand is on the left of the screen).
- The side and back views have fewer layers (8 to 9) because half of the parts are hidden.
- Layer names in the psb are `<direction>_<part>`, such as `F_torso`, `SL_arm`, like the example `Assets/FW_MIHO`.

## Mistakes we have already made

| Symptom | Cause | Fix |
|---|---|---|
| White line around the edge | the background was cut out by thresholding | alpha unmixing (step 1 in the splitter) |
| Ghost line left on the body after the arm moves | grey anti-aliased pixels were labelled as the body colour | add `gray`/`lgray` to the palette and treat them as line |
| Dotted line along a cut | an outline was drawn on every cut | draw an outline only on layers listed in `outline_cuts` |
| White hole behind the swinging arm | nothing painted underneath | `full` polygon for the torso/hip |
| Legs cross each other | each leg turns around its own point | both legs turn around the same hip point |
| Joints look broken even with clean cuts | each piece is rotated rigidly | use bones and weights (point 6) |

## Limits

- The splitter guesses parts from colour plus polygons. A new character needs a new palette
  (`"palette"` in the config) and new polygons; it is not automatic.
- A small source (about 110x300 px per figure) gives jagged detail in thin parts such as the sash.
  If the artist has a larger file, use that.
- The bone positions in `rigs.py` are for Phawin's frame. A new character needs its own.
