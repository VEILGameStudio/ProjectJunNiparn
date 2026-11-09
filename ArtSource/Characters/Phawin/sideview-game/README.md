# Phawin - SideView in-game sheet (6 facings, idle + walk)

Imported into `Assets/_Project/Art/Sprites/SideView/Characters/Phawin/Phawin_SideView.png`.

SideView uses the 6 facings of CLAUDE.md section 7 and `PlayerAnimator.SnapToSixFacings`:
front, back, front-left, front-right, back-left, back-right. Each is drawn separately, never mirrored.

- Rows: Front, Back, FrontLeft, FrontRight, BackLeft, BackRight (same as the source column order).
- Column 0 = idle, from the bottom row of Oak's v2 standing image
  (`../threequarter-game/source/phawin_idle_v2_original.png`, cut out as `source/phawin_6dir_idle_row2.png`).
- Columns 1-5 = walk, from `source/phawin_6dir_walk_original.png` (6 columns x 5 frame rows).
- Cell 162x317, pivot at the feet, PPU 184.3 (Phawin as tall as the mannequin, 1.617 units; temporary).
  Already stretched x1.015. Do not stretch again and never tilt the card.
- The controller `Phawin_SideView.controller` is the mannequin ThreeQuarter controller with each blend
  tree's 8 children replaced by these 6 at FaceX/FaceY (0,-1) (0,1) (-1,-1) (1,-1) (-1,1) (1,1).
- Known: the standing drawing has a smaller head than the walk drawing (head width about 39% of the
  height against 48%). Both are scaled to the same height, so the head grows a little on the first step.

The older left/right profile walk in `../sideview-profile-walk/` is no longer used in game (the 6-facing
snap never asks for a pure side view); it is kept as reference.

Rebuild, from the repo root:

```
P=ArtSource/Characters/Phawin
python3 $P/tools/build_game_sheet.py \
  $P/sideview-game/source/phawin_6dir_idle_row2.png 1 \
  $P/sideview-game/source/phawin_6dir_walk_original.png 5 \
  Front,Back,FrontLeft,FrontRight,BackLeft,BackRight \
  Front,Back,FrontLeft,FrontRight,BackLeft,BackRight \
  $P/sideview-game Phawin_SideView 1.015 1.617 10 32
python3 $P/tools/write_unity_assets.py
```
