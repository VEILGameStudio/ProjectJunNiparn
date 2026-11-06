# 02: Config fields

All coordinates are pixels of the **source frame** (before upscaling). Examples: `examples/phawin/cfg_*.json`.

| Field | Meaning |
|---|---|
| `crop` | `[x0,y0,x1,y1]` box around one figure |
| `scale` | upscale factor (4) |
| `bg`, `palette` | background colour and material colours (optional; Phawin's are the defaults) |
| `hip_y`, `foot_y` | row where the legs start, and row where the feet/sandals start |
| `mid_x` | centre line separating the left and right foot |
| `polys` | polygons around the parts (`head`, `arm`, `bag`, `sash`, `leg_SL`, ...) |
| `boxes` | small boxes such as `neck`, eyes, mouth |
| `parts` | (generic mode) list of `{name, poly, mats/not_mats, ymin}`, applied in order, later wins |
| `order` | layer order **from bottom to top**; also decides who owns a shared line |
| `full` | polygons a layer must cover underneath (hidden parts) |
| `fill` | flat colour for those covered areas, per layer |
| `outline_cuts` | layers that get a closing line along their cut |
| `pivots` | joint points (used by the simple rigid preview only) |
| `generic` | `true` = the simple rules (head/torso/hip/legs + parts). Leave it out for the full front rules (face, eyes, mouth, hair_front/back, chin_y, hem_y, head_box) |

## Making the other directions cheaply

`examples/phawin/make_cfgs.py` builds one back view and then:

- `offset(cfg, dx)`: the same figure, moved along the sheet (BL comes from B);
- `mirror(cfg, C)`: mirrors **the coordinates** around column C and swaps `_SL`/`_SR` (BR from B, SR from SL).

The art is the artist's own drawing for that direction; only the cut lines are mirrored. If the two
drawings differ (the bag is on one side), override that polygon afterwards, as SR does with `sash`.
