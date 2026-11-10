# Phawin - source drawings

- `walk_8dir/<Facing>.png` - Oak's walk sheets (walk-eight-separate.zip, 2026-11-10). One file per facing,
  1774x887, 4 columns x 2 rows = 8 frames read left to right, then the next row. Facing names are screen
  directions: Down = toward the viewer, Up = away.
- `idle_8dir.png` - Oak's 8-direction standing sheet. Only the top row is used; its columns are
  Down, DownLeft, Left, UpLeft, Up, UpRight, Right, DownRight. Its proportions match the walk sheets.

Both game sheets are built from these files:

| Game sheet | Rows | Built from |
|---|---|---|
| `../threequarter-game/` (3/4 rooms) | 8 facings | all 8 walk files + the 8 idle figures |
| `../sideview-game/` (2.5D corridors) | Front, Back, Right, Left | Down, Up, Right, and Right mirrored |

Rebuild, from the repo root (requires numpy, Pillow, opencv-python-headless):

```
P=ArtSource/Characters/Phawin
python3 $P/tools/build_game_sheet.py $P/threequarter-game/build.json
python3 $P/tools/build_game_sheet.py $P/sideview-game/build.json
python3 $P/tools/write_unity_assets.py
```

`build_game_sheet.py` cuts, scales, lines up and stretches the figures. `write_unity_assets.py` copies
both sheets into `Assets/` and writes the sprite slicing, clips, controllers and `Player_Phawin.prefab`.
Sizes, facings and fps are in each folder's `build.json`.
