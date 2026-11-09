# Phawin - ThreeQuarter walk (source art, stretched, laid out like the template)

From Oak's 8-direction walk sheet on issue #24 (`source/phawin_8dir_walk_original.png`, 8 columns =
facings, 5 rows = frames). Built from the repo root with:

```
python3 ArtSource/Characters/Phawin/tools/build_template_sheet.py \
  ArtSource/Characters/Phawin/threequarter-walk/source/phawin_8dir_walk_original.png 5 8 \
  Down,DownLeft,Left,UpLeft,Up,UpRight,Right,DownRight \
  ArtSource/Characters/Phawin/threequarter-walk Phawin_ThreeQuarter 1.155 \
  Down,DownRight,Right,UpRight,Up,UpLeft,Left,DownLeft 1.625 10 32 cols normalize
```

The third argument names the source columns left to right. Swap names there if a column turns out to
face the other way.

- `Phawin_ThreeQuarter.png` - rows in the same order as `Placeholder_Mannequin_ThreeQuarter.png`:
  Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft. All 8 rows are filled, 5 walk frames
  each, 10 fps. Cell and pivot are in `Phawin_ThreeQuarter.json`.
- Every facing was drawn separately (mirroring one side onto the other differs by 41-46 of 255 on
  average), so none of them is a flipped copy.
- Haze at alpha 32 or below is removed (`32` argument). The red/yellow cloud in the source is alpha 1-2
  and the grey fringe is below 32; alpha clipping at 0.5 would drop both anyway.
- **Rows rescaled (`normalize`).** In the source each frame row is drawn smaller than the one above it
  (median figure height 202, 185.5, 183.5, 182, 173.5 px), so the figure would shrink and pop back
  every loop. Each source row gets one uniform scale to 183.5 px before the stretch. After that the
  stretched figures are 207-216 px tall.
- Already stretched x1.155 (CLAUDE.md section 7). Do not stretch again, never tilt the card.
- `suggested_ppu` in the JSON makes Phawin as tall as the mannequin (1.625 units). It is a
  placeholder until Oak sets Phawin's real height.
- Walk only. There is no idle column: no standing frame exists in the source.
- `*_Walk_<facing>.gif` previews; `*_alphaclip_preview.png` shows what alpha clipping at 0.5 keeps.
- `source/phawin_6dir_walk_original.png` is the earlier 6-direction sheet (no Right/Left). Kept as
  reference only; the sheet above no longer uses it.

Not imported into `Assets/` yet.
