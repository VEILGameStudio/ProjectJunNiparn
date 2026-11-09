# Phawin - ThreeQuarter walk (source art, stretched, laid out like the template)

From Oak's 6-direction walk sheet on issue #24 (`source/phawin_6dir_walk_original.png`, 4 frames x 6
facings). Oak said this sheet is drawn for the 3/4 view. Built from the repo root with:

```
python3 ArtSource/Characters/Phawin/tools/build_template_sheet.py \
  ArtSource/Characters/Phawin/threequarter-walk/source/phawin_6dir_walk_original.png 6 4 \
  Down,Up,DownLeft,DownRight,UpLeft,UpRight \
  ArtSource/Characters/Phawin/threequarter-walk Phawin_ThreeQuarter 1.155 \
  Down,DownRight,Right,UpRight,Up,UpLeft,Left,DownLeft 1.625 10
```

The source rows are front, back, front-left, front-right, back-left, back-right; the third argument
maps them to the template's facings. Swap names there if a row turns out to face the other way.

**Do not import into a ThreeQuarter area until Right and Left are drawn.** `PlayerAnimator` picks
Right/Left whenever the player walks straight left or right, and an empty row makes Phawin vanish.

Note: CLAUDE.md section 7 currently gives SideView the 6 facings (front, back, 4 diagonals) and
ThreeQuarter 8. Oak decides which preset this sheet belongs to before it is imported.

- `Phawin_ThreeQuarter.png` - rows in the same order as `Placeholder_Mannequin_ThreeQuarter.png`:
  Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft. Cell and pivot are in
  `Phawin_ThreeQuarter.json`.
- **Right and Left rows are empty** - there is no art for them yet (never mirror one from the other).
- Already stretched x1.155 (CLAUDE.md section 7). Do not stretch again, never tilt the card.
- `suggested_ppu` in the JSON makes Phawin as tall as the mannequin (1.625 units). It is a
  placeholder until Oak sets Phawin's real height.
- Walk only, 4 frames, 10 fps like the template. There is no idle column: no standing frame exists.
- `*_Walk_<facing>.gif` previews; `*_alphaclip_preview.png` shows what alpha clipping at 0.5 keeps.

Not imported into `Assets/` yet.
