# Phawin - SideView profile walk (source art, stretched, laid out like the template)

From Oak's left/right walk sheet on issue #24 (`source/phawin_profile_leftright_walk_original.png`,
2 rows x 6 frames: top row walks left, bottom row walks right, drawn separately - not mirrored).
Built from the repo root with:

```
python3 ArtSource/Characters/Phawin/tools/build_template_sheet.py \
  ArtSource/Characters/Phawin/sideview-profile-walk/source/phawin_profile_leftright_walk_original.png 2 6 \
  Left,Right ArtSource/Characters/Phawin/sideview-profile-walk Phawin_SideView 1.015 Right,Left 1.617 10
```

- `Phawin_SideView.png` - row 0 Right, row 1 Left, 6 frames each, the mannequin's row order (Right
  first). Cell and pivot are in `Phawin_SideView.json`.
- The red/yellow glow around the figures in the source is almost all alpha 8 or less; the script
  drops it. A handful of reddish pixels at alpha 128+ remain (likely the amber pendant) - zoom the
  alpha-clip preview at the edges before import.
- `source/phawin_profile_left_walk_original.png` is the older 8-frame left-only sheet. It is drawn at
  a different size and has no matching right walk, so it is kept as reference and not used.
- The mannequin SideView template is a profile walk; CLAUDE.md section 7 and `PlayerAnimator` ask
  for 6 facings (front, back, four diagonals) in SideView. Which one SideView uses is Oak's
  decision - this sheet is not wired to anything until then.
- Already stretched x1.015. Do not stretch again, never tilt the card.
- `suggested_ppu` makes Phawin as tall as the SideView mannequin (1.617 units), a placeholder.
- Walk only, 10 fps. No idle frame exists.

Not imported into `Assets/` yet.
