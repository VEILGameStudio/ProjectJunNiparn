# Phawin - ThreeQuarter in-game sheet (idle + walk)

This is the sheet that is imported into
`Assets/_Project/Art/Sprites/ThreeQuarter/Characters/Phawin/Phawin_ThreeQuarter.png`.

- Rows in the mannequin order: Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft.
- Column 0 = idle, from Oak's 8-direction standing sheet
  (`../threequarter-walk/source/phawin_8dir_idle_original.png`, top row; the bottom row is not used).
- Columns 1-5 = walk, from `../threequarter-walk/source/phawin_8dir_walk_original.png`.
- Cell 110x232, pivot and PPU in `Phawin_ThreeQuarter.json`. Already stretched x1.155. Do not stretch
  again and never tilt the card.
- Idle figures are scaled to the same height as the walk figures (183.5 px before the stretch).
  The idle drawing has a smaller head than the walk drawing (about 56 px against 67-78 px wide after
  the stretch), so the head grows a little when Phawin starts walking. That comes from the source art.

Rebuild, from the repo root (requires numpy, Pillow, opencv-python-headless):

```
P=ArtSource/Characters/Phawin
python3 $P/tools/build_game_sheet_threequarter.py \
  $P/threequarter-walk/source/phawin_8dir_idle_original.png 2 \
  $P/threequarter-walk/source/phawin_8dir_walk_original.png 5 \
  Down,DownLeft,Left,UpLeft,Up,UpRight,Right,DownRight \
  Down,DownRight,Right,UpRight,Up,UpLeft,Left,DownLeft \
  $P/threequarter-game Phawin_ThreeQuarter 1.155 1.625 10 32
python3 $P/tools/write_unity_assets.py
```

`write_unity_assets.py` copies both sheets (this one and `../sideview-profile-walk/Phawin_SideView.png`)
into `Assets/`, writes their sprite slicing, the clips, the controllers and `Player_Phawin.prefab`.
GUIDs come from file names, so running it again writes the same files.
