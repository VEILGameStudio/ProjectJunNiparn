# Phawin - SideView profile walk (source art, stretched, laid out like the template)

From Oak's profile walk sheet on issue #24 (`source/phawin_profile_left_walk_original.png`,
8 frames of one cycle, facing left). Built from the repo root with:

```
python3 ArtSource/Characters/Phawin/tools/build_template_sheet.py \
  ArtSource/Characters/Phawin/sideview-profile-walk/source/phawin_profile_left_walk_original.png 2 4 \
  Left ArtSource/Characters/Phawin/sideview-profile-walk Phawin_SideView 1.015 Right,Left 1.617 10
```

- `Phawin_SideView.png` - row 0 Right (empty, no art yet), row 1 Left, 8 frames. Cell and pivot are in
  `Phawin_SideView.json`.
- The mannequin SideView template is one profile row facing right; CLAUDE.md section 7 and
  `PlayerAnimator` ask for 6 facings in SideView. Which one SideView uses is Oak's decision -
  this sheet is not wired to anything until then.
- Already stretched x1.015. Do not stretch again, never tilt the card.
- `suggested_ppu` makes Phawin as tall as the SideView mannequin (1.617 units), a placeholder.
- Walk only, 10 fps. No idle frame exists.

Not imported into `Assets/` yet.
