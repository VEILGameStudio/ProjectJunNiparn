# Phawin - SideView in-game sheet (Front, Back, Right, Left; idle + walk)

Imported into `Assets/_Project/Art/Sprites/SideView/Characters/Phawin/Phawin_SideView.png`.

- Rows: Front, Back, Right, Left. **Left is Right mirrored when the sheet is built** (Oak's decision,
  2026-11-10), so the bag and strap swap sides on the Left row. Nothing flips at runtime.
- Front and Right are the two drawings Oak chose for 2.5D (the same art as the Down and Right walk files).
  Back uses the Up walk file, because there is no separate 2.5D back drawing.
- Column 0 = idle, columns 1-8 = walk (8 frames, 10 fps). Sources and rebuild steps: `../source/README.md`.
- Cell 248x344, pivot at the feet in the exact middle of the cell (0.5), PPU 201.0 (Phawin as tall as
  the mannequin, 1.617 units; temporary). Already stretched x1.015. Never tilt the card.
- `PlayerAnimator.SnapToSixFacings` still sends 6 directions. `Phawin_SideView.controller` maps them to
  these 4 rows: front (0,-1) = Front, back (0,1) = Back, every diagonal with X = -1 = Left, X = 1 = Right.
