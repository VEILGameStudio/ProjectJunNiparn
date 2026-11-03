# 10 — Test_Forest: สิ่งที่ปรับแล้ว และวิธีปรับซ้ำ

ฉาก: `Assets/_Project/Scenes/Test/Test_Forest.unity` (6 แถว: Forest_Deep / Far / Mid / Near / Undergrowth / Foreground, 102 ต้น)

## 1. ระยะเทียบกับตารางใน 06 — ใช้ได้แล้ว ไม่ต้องย้าย

กล้อง SideView อยู่ที่ z ≈ −19.7 (distance 20, pitch 10) ดังนั้น D ≈ z + 19.7

| แถว | z ในฉาก | D | ตาราง 06 | ความกว้างที่ต้องการ (21:9 + กล้องเลื่อน ~20) | ในฉาก |
|---|---|---|---|---|---|
| Deep | 60–72 | ~86 | L1 (110) | ±44 | ±50 ✓ |
| Far | 28–36 | ~52 | L2 (60) | ±30 | ±36 ✓ |
| Mid | 10.6–15 | ~32 | L3 (35) | ±23 | ±28 ✓ |
| Near | 4.4–5.9 | ~25 | L4 (24–26) | ±20 | ±27 ✓ |
| Undergrowth | 2.2–2.8 | ~22 | L4 | — | ✓ |
| Foreground | −4.8 – −4.1 | ~15 | L6 (14) | — | ✓ |

## 2. ที่เปลี่ยนในรอบนี้ (preset `presets/forest-day.json`)

| | ก่อน | หลัง | เหตุผล |
|---|---|---|---|
| สีพื้นหลังกล้อง (L0) | `#121419` เกือบดำ | `#ADCCAD` เขียวเทาสว่าง | ต้องมี "อากาศ" ให้ชั้นไกลจางเข้าหา (05 §1) |
| Fog color | `#21333A` ฟ้าเข้ม | `#9EBDA3` | ไกล = สว่าง (contre-jour) ไม่ใช่ไกล = มืด |
| Fog start / end | 24 / 120 | 22 / 100 | Deep จมหมอก ~80%, Far ~40%, Mid ~13%, Near ~4% |
| Tint แถว Deep / Far | ฟ้าเข้ม 0.25–0.37 | 0.62–0.74 เขียวเทา | ให้หมอกทำงาน ไม่ใช่ทาสีมืดทับ |
| Tint Mid / Near | 0.55 / 0.86 (ใกล้สว่างสุด) | 0.62 / 0.55 | **ใกล้ต้องมืดกว่าไกล** — ก่อนหน้านี้กลับด้าน |
| Tint Undergrowth | 0.82 | 0.70–0.74 | ยังสว่างสุดในฉาก = กรอบหลังตัวละคร (hero) |
| Tint Foreground | 0.20 | 0.05–0.07 | foreground lip ต้องเป็นเงาเกือบดำ |

แต่ละต้นยังคง jitter สีเล็ก ๆ ของเดิม (ไม่ให้ทุกต้นสีเท่ากันเป๊ะ)
ภาพเปรียบเทียบ (จำลอง ไม่ใช่ภาพจาก Unity): `img/study/s01-test-forest-before-after.jpg`

## 3. ปรับซ้ำ / สลับเวลา

```bash
# จาก root ของ repo
S=.claude/skills/sideview-forest-background
python3 $S/scripts/forest_grade.py Assets/_Project/Scenes/Test/Test_Forest.unity Assets/_Project/Scenes/Test/Test_Forest.unity $S/presets/forest-dusk.json
python3 $S/scripts/forest_sim.py   Assets/_Project/Scenes/Test/Test_Forest.unity /tmp/preview.png "dusk" 0
```

- `forest-day.json` กลางวันย้อนแสง (ค่าปัจจุบันในฉาก)
- `forest-dusk.json` ค่ำ: หมอกฟ้า ใกล้มืดจัด — คู่กับคบเพลิง/ไฟเมื่อมี
- `forest-legacy-night.json` ค่าเดิมก่อนรอบนี้ (ย้อนกลับได้ คลาดเคลื่อน < 0.007)
- ปิด Unity ก่อนรันสคริปต์ หรือกด Reload เมื่อ Unity ถามว่าไฟล์ฉากเปลี่ยน

## 4. ต้องเช็กใน Unity (ภาพจำลองบอกไม่ได้)

1. Play แล้วเดินสุดซ้าย–ขวา ที่ 16:9 และ 21:9 → ไม่เห็นขอบแถว
2. material ต้นไม้รับ fog จริงไหม (ถ้าแถว Deep ไม่จางเลย = shader ไม่ได้เปิด fog)
3. Post-processing Volume ของโซน (ถ้ามี) อาจดึง exposure/สีเพี้ยนจากภาพจำลอง
4. ถ่าย screenshot เก็บไว้ที่ `img/study/` แล้วไล่ `07-critique-checklist.md`
