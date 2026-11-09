# Phawin - ThreeQuarter in-game sheet (idle + walk)

This is the sheet that is imported into
`Assets/_Project/Art/Sprites/ThreeQuarter/Characters/Phawin/Phawin_ThreeQuarter.png`.

- Rows in the mannequin order: Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft.
- Column 0 = idle, from Oak's v2 standing sheet (`source/phawin_idle_v2_original.png`, top row, cut out as
  `source/phawin_8dir_idle_v2_row1.png`). The v2 idle has the same head size as the walk drawing, which fixes
  the head growing when Phawin started walking. (The bottom row of that image is the SideView idle,
  see `../sideview-game/`.)
- Columns 1-5 = walk, from `../threequarter-walk/source/phawin_8dir_walk_original.png`.
- Cell 110x232, pivot and PPU in `Phawin_ThreeQuarter.json`. Already stretched x1.155. Do not stretch
  again and never tilt the card.

Rebuild, from the repo root (requires numpy, Pillow, opencv-python-headless):

```
P=ArtSource/Characters/Phawin
python3 $P/tools/build_game_sheet.py \
  $P/threequarter-game/source/phawin_8dir_idle_v2_row1.png 1 \
  $P/threequarter-walk/source/phawin_8dir_walk_original.png 5 \
  Down,DownLeft,Left,UpLeft,Up,UpRight,Right,DownRight \
  Down,DownRight,Right,UpRight,Up,UpLeft,Left,DownLeft \
  $P/threequarter-game Phawin_ThreeQuarter 1.155 1.625 10 32
python3 $P/tools/write_unity_assets.py
```

`write_unity_assets.py` copies both sheets (this one and `../sideview-game/Phawin_SideView.png`)
into `Assets/`, writes their sprite slicing, the clips, the controllers and `Player_Phawin.prefab`.
