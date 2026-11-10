# Phawin - ThreeQuarter in-game sheet (8 facings, idle + walk)

Imported into `Assets/_Project/Art/Sprites/ThreeQuarter/Characters/Phawin/Phawin_ThreeQuarter.png`.

- Rows in the mannequin order: Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft.
- Column 0 = idle, columns 1-8 = walk (8 frames, 10 fps). Sources and rebuild steps: `../source/README.md`.
- Cell 188x297, pivot at the feet, PPU 170.5 (Phawin as tall as the mannequin, 1.625 units; temporary).
- Already stretched x1.155. Do not stretch again and never tilt the card.
- The figure is scaled down from about 427 px to 240 px before the stretch; change `target_height` in
  `build.json` for a sharper or smaller sheet (PPU follows automatically).
