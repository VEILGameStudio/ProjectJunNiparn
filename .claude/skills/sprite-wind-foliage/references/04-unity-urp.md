# 04 — ลมแบบคลิปในโปรเจกต์นี้ (Unity 6.5 URP) — ใช้งานจริงแล้ว

สถานะ (2026-11-03): Oak สั่งให้ **แทนลมแบบเดิม** ด้วยวิธีนี้ และแก้ CLAUDE.md §7 "Wind" ให้ตรงแล้ว
⚠ ยังไม่เคยเปิดใน Unity — compile/ภาพจริงต้องเช็กในเครื่อง Oak (รายการเช็กอยู่ท้ายไฟล์)

## 1. ไฟล์ที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
|---|---|
| `Assets/_Project/Art/Shaders/SpriteLitNormal.shader` | `FoliageWindUV()` เลื่อน UV ใน Forward / DepthOnly / DepthNormals (ลบลม vertex เดิมออกแล้ว) |
| `Assets/_Project/Art/Shaders/FoliageFlowNoise.png` | noise RG 256² tileable, import sRGB **ปิด**, Wrap Repeat |
| `Art/Materials/SpriteLit_Wind_Normal.mat` | ใบหลัก (ลมเต็ม, ดันเต็ม) |
| `Art/Materials/SpriteLit_Wind_Normal_Overlay.mat` | ใบเจาะรู (ลม **เท่ากัน**, `Push Scale` 0.25) |
| `Art/Materials/SpriteLit_Trunk.mat` | ลำต้น (ไม่มีลม) |
| `Art/Materials/SpriteLit_FlowGrass.mat` | หญ้า (`Full Movement Height` 0.5, `Push Depth Reach` 0.8) |
| `Art/Sprites/ThreeQuarter/Trees/Layers/` | 5 ต้น × (Trunk, Leaves, LeavesOverlay, Layers_N) ขอบใส 32 px, Full Rect |
| `Prefabs/Trees/Tree_*.prefab` | `Visual` = ลำต้น, ลูก `Leaves` (z −0.05), `LeavesOverlay` (z −0.1) |
| `Scripts/Environment/FoliagePusher.cs` | บน Player root (`Prefabs/Player.prefab`) — แปลงการเดินเป็นแรงดัน |
| `Scripts/Environment/FoliageWindFeed.cs` | 1 อันต่อฉาก (ไม่ใช่ singleton) — ส่งแรงดันสูงสุด 8 ตัวให้ shader |
| `Scenes/Test/Test_Forest.unity` | object `FoliageWind` + หญ้า 26 กอใช้ `SpriteLit_FlowGrass` |

## 2. ค่าใน material (หน่วย world unit ทั้งหมด)

| ช่อง | ใบหลัก | ใบเจาะรู | ลำต้น | หญ้า | มาจาก |
|---|---|---|---|---|---|
| Gust Strength | 0.04 | 0.04 | 0 | 0.05 | คลิป: ทั้งพุ่มโยก 1–2.5% ของความกว้าง |
| Rustle Strength | 0.02 | 0.02 | 0 | 0.03 | คลิป: ไหวเฉพาะที่ ~0.6% |
| Gust Size | 5 | 5 | – | 4 | ลมก้อนใหญ่กว่าต้นไม้ |
| Wind Speed | 1 | 1 | – | 1 | gust 5 unit ÷ 1 unit/s ≈ 0.2 Hz ตามคลิป |
| Full Movement Height | 1.5 | 1.5 | – | 0.5 | โคน/หญ้าที่ฐานต้นนิ่ง |
| Push Strength | 0.2 | 0.2 | 0 | 0.25 | คลิป: ~4% ของความกว้าง |
| Push Scale | 1 | **0.25** | – | 1 | "dulled a lot" |
| Push Depth Reach | 6 | 6 | – | 0.8 | ต้นไม้หลังทางเดิน (z 4–6) ยังตอบสนอง; หญ้าเฉพาะกอที่เดินทับ |
| Snap To Whole Texels | ✓ | ✓ | ✓ | ✓ | ภาพคม ไม่เบลอ |

**ห้ามให้ค่าลมของใบหลักกับใบเจาะรูต่างกัน** — ตอนนิ่งจะเห็นภาพซ้อน

## 3. ทำงานยังไงใน shader (สั้น ๆ)

1. ตำแหน่งบนการ์ด = (แกนขวาของการ์ด · positionWS, positionWS.y) — ใช้ได้ทั้ง yaw 0 และ 45
2. UV ต่อ 1 world unit วัดจาก `ddx/ddy` ⇒ ไม่ต้องรู้ PPU, scale, หรือ atlas
3. offset = gust × Gust Strength + rustle × Rustle Strength + แรงดันจาก pusher ทุกตัว (ตามทิศเดิน 60% + ผลักออกจากตัว 40%) × falloff ระยะบนการ์ด × falloff ความลึก
4. คูณ height ramp (`Full Movement Height`) แล้วปัดเป็น texel เต็ม (ถ้า Unity ส่ง `_MainTex_TexelSize` มา)
5. `uv − offset` ใช้กับทั้งสี, normal map, และ `clip()` ในทุก pass

## 4. ทำต้นไม้ใหม่ให้มีลมแบบนี้ (สำหรับน้อง B)

1. วาดต้นไม้ทั้งต้นตามปกติ + normal map (`_N`) ขนาดเท่ากัน
2. รัน `python3 -I .claude/skills/sprite-wind-foliage/scripts/split_tree_layers.py tree.png tree_N.png <Layers folder> <ชื่อ>`
   (พืชที่ลำต้นเป็นสีเขียว เช่น กล้วย: ต่อท้าย `32 48 0.65 6 all`)
3. ตั้ง import ของ 3 ภาพ: Sprite, Mesh Type **Full Rect**, Pivot Custom (0.5, ค่า pivot y ที่สคริปต์พิมพ์), PPU เท่าต้นเดิม,
   Secondary Texture `_NormalMap` = ไฟล์ `_Layers_N`
4. Duplicate prefab ต้นไม้ที่มีอยู่ แล้วเปลี่ยน sprite ทั้ง 3 ชั้น (material ไม่ต้องเปลี่ยน)
5. ย้อมสี/Flip X ต้องทำ **ทั้ง 3 renderer**

## 5. ข้อจำกัดที่รู้แล้ว

- `SpriteLitOutline` (shadergraph) + `SpriteWind.shadersubgraph` ยังเป็นลม vertex แบบเก่า — ฉากอื่น (Test_Zones) ยังใช้อยู่
- ถ้า `_MainTex_TexelSize` ไม่ถูกส่งมาสำหรับ SpriteRenderer การปัด texel จะถูกข้าม (ใบยังขยับได้ แต่อาจนุ่ม/เบลอเล็กน้อย)
- `Push Depth Reach` 6 ทำให้ต้นไม้ตอบสนองเมื่อผู้เล่นเดิน "ผ่านหน้า" แม้ไม่ได้แตะจริง — ตั้งใจให้เหมือนคลิป ปรับลดได้ต่อ material
- pusher เกิน 8 ตัว ตัวที่เกินไม่ดันใบ (log warning ครั้งเดียว)

## 6. เช็กใน Unity ครั้งแรก

1. Console ไม่มี error ของ shader `Project/SpriteLitNormal` และ script ใหม่ compile ผ่าน
2. เปิด Test_Forest กด Play: ใบไม้ไหวเบา ๆ (รอบละ ~5 วิ) ลำต้นนิ่ง ไม่มีภาพซ้อนตอนนิ่ง
3. เดินผ่านหน้าต้นไม้: ใบล่างแหวกตามทิศเดิน ใบชั้นบนขยับน้อยกว่า แล้วค่อย ๆ คืนที่
4. เดินทับหญ้าในทางเดิน: หญ้าเอนหนี โคนนิ่ง
5. กด pause (เปิดเมนู): ทุกอย่างหยุด
6. ขอบใบไม่ถูกตัดเป็นเส้นตรง (ถ้าโดน = Mesh Type ยังเป็น Tight หรือขอบใสไม่พอ)
7. เปิด SSAO: เงารอบใบขยับตามใบ
