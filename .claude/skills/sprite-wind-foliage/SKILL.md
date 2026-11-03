---
name: sprite-wind-foliage
description: Use when making trees, bushes or grass sway in the wind or react when the player walks or jumps through them - per-pixel flow-map wind, a player "disturbance" push, the "leaves look like slime" problem and the layered-tree fix, or tuning the wind on SpriteLitNormal sprites.
---

# Sprite wind & foliage disturbance — ลมผ่านต้นไม้/ใบหญ้า

ถอดจากคลิป devlog 78 วินาที (เกม pixel art side-scroller ใน GameMaker): ต้นไม้ไหวตามลมทีละพิกเซล
และใบแหวกเมื่อตัวละครกระโดดทะลุ ครั้งแรกออกมาเป็น "slime" แล้วแก้ด้วยการแยกชั้นต้นไม้
ตัวเลขทั้งหมดในโฟลเดอร์นี้วัดจากคลิปจริง (สคริปต์อยู่ใน `scripts/`) ส่วนไหนเดาจะเขียนกำกับไว้

## หัวใจ 7 ข้อ

1. **ลมที่ดูเป็นใบไม้คือการเลื่อนพิกเซล ไม่ใช่การเฉือนการ์ด** — อ่าน texture ที่ตำแหน่งเลื่อน (UV offset)
   ค่าการเลื่อนมาจาก texture สี: **R = ซ้าย/ขวา, G = ขึ้น/ลง, 0.5 = นิ่ง** ("เหลืองขี้ม้า" = R 0.5 G 0.5)
2. **ลม = noise ลายใหญ่กว่าต้นไม้ เลื่อนช้า ๆ** — โยกรอบละ ~5 วินาที (0.17–0.22 Hz), ทั้งพุ่มโยกแค่ 1–2.5% ของความกว้าง,
   ใบไหวภายใน ~0.6% ลมเบามากก็พอ ตาเห็น "ลม" จากขอบเฉดสีที่ขยับทีละพิกเซล
3. **ผู้เล่นดัน = disturbance บวกเข้ากับลม** — จุดนุ่มรอบตัวผู้เล่น ทิศตามที่ผู้เล่นเคลื่อน จางลงหลังผ่านไป ระบบเดียวใช้กับพืชทุกชนิด
4. **slime เกิดเพราะ offset เรียบเกินไป** — พิกเซลติดกันเลื่อนเกือบเท่ากัน ทั้งพุ่มยืดเป็นเยลลี่ ลมเบาไม่เห็น แต่แรงดันผู้เล่นโชว์ชัด
5. **แก้ด้วยการแยกชั้น:** ลำต้น (ไม่มีลม) → ใบหลัก (ลม + ดันเต็ม) → ผู้เล่น → **ใบซ้ำเจาะรู (ลมเท่ากัน + ดันแค่ ~¼)**
   ลมเท่ากันทำให้ตอนนิ่งดูเป็นภาพเดียว ดันไม่เท่ากันทำให้ "แหวกเห็นความลึก" เฉพาะตอนโดนแตะ
6. **ชั้นเจาะรูเหลือใบ ~2/3** — ลบเฉดมืดสุด (ร่องเงา) + จุดเล็ก ๆ กระจาย ห้ามแก้รูปทรงที่เหลือ
7. **ทุกพิกเซลเลื่อนเป็นจำนวนเต็ม** — pixel art ต้องไม่มีครึ่งพิกเซล; ทุก pass ที่อ่าน texture ต้องเลื่อนเหมือนกัน

## Workflow

| อยากทำ | อ่าน / ใช้ |
|---|---|
| เข้าใจว่าคลิปทำอะไร ทีละวินาที | `references/01-clip-breakdown.md` |
| ตั้งค่าลม / ระบบผู้เล่นดัน / ตัวเลขที่วัดได้ | `references/02-flow-and-disturbance.md` |
| วาด/แยกชั้นต้นไม้ แก้ slime | `references/03-layering-fix.md` |
| ใส่ใน Unity URP ของโปรเจกต์นี้ (shader, กับดัก, ค่าเริ่มต้น, หญ้า) | `references/04-unity-urp.md` + `snippets/` |
| ลองค่าโดยไม่เปิด Unity | `python3 -I scripts/foliage_flow_sim.py <out> [overlay_scale] [wind_px] [disturb_px]` |
| ทำ texture noise ลม | `python3 -I scripts/make_flow_noise.py out.png 256 4 0.0` (หรือใช้ `img/w09-flow-noise-256.png`) |
| วัดลมในคลิป ref ใหม่ | `python3 -I scripts/measure_foliage_motion.py clip.mp4 t0 t1 art_px canopy_box anchor_box` |

## ไฟล์ในโฟลเดอร์นี้

| ไฟล์ | เนื้อหา |
|---|---|
| `references/01-clip-breakdown.md` | คำบรรยายเต็ม + สิ่งที่เห็นต่อช่วงเวลา + สรุปเทคนิค |
| `references/02-flow-and-disturbance.md` | การเข้ารหัสสี, ตัวเลขลมที่วัดได้, disturbance texture, ทำไมเป็น slime + ทางแก้ 3 แบบ |
| `references/03-layering-fix.md` | ชั้นต้นไม้ 4 ชั้น, การวัดชั้นเจาะรู, วิธีวาดสำหรับ artist |
| `references/04-unity-urp.md` | เทียบกับ `SpriteLitNormal`, ต่อเข้า shader, กับดัก SpriteRenderer, ซ้อนการ์ดใน 3D, หญ้า, ค่าเริ่มต้น |
| `references/img/w01–w05` | เฟรมจากคลิป: ต้นไม้, texture ลม/disturbance, ชั้น, heatmap การเคลื่อนไหว, ก่อน–หลังแก้ |
| `references/img/w06–w08` | ภาพจำลอง Python: slime vs แยกชั้น (ภาพนิ่ง + GIF), ชั้นที่สร้างอัตโนมัติ |
| `references/img/w09-flow-noise-256.png` | texture ลม tileable พร้อมใช้ (import: sRGB ปิด) |
| `snippets/FoliageFlow.hlsl` | ฟังก์ชัน `FoliageFlowUV` สำหรับ shader (ต้นแบบ ยังไม่ compile) |
| `snippets/FoliageDisturber.cs` | `FoliageDisturber` (บนผู้เล่น) + `FoliageWindFeed` (ส่งค่าให้ shader) (ต้นแบบ ยังไม่ compile) |
| `scripts/` | `measure_foliage_motion.py`, `foliage_flow_sim.py`, `make_flow_noise.py` |

## ก่อนนำเข้าโปรเจกต์

- **ยังไม่มีอะไรในนี้ถูกใส่ใน `Assets/`** — snippets ไม่เคยรันใน Unity
- ขัด/ต้องเพิ่มใน CLAUDE.md: §7 Wind เขียนว่าลมเป็น vertex displacement; shader และ `Scripts/Player/` เป็นของ Oak
  ⇒ ถ้าเป็นคำขอจากน้องในทีม ใช้ skill `design-change-proposal` ส่ง Oak ก่อน
- ไม่ขัด: ซ้อนชั้นด้วย z ของการ์ด (depth buffer) ไม่ใช่โค้ด sort; ยังเป็น alpha clip; `_Time` หยุดตอน pause; ไม่ใช้ MaterialPropertyBlock

## Critique — เช็กก่อนส่ง

- [ ] ตอนไม่มีใครแตะ ต้นไม้ดูเป็นภาพเดียว (ไม่มีภาพซ้อน ขอบไม่สั่นสองชั้น)
- [ ] ลมเบา: ทั้งพุ่มโยก ≤ 2.5% ของความกว้าง, รอบละ 4–6 วินาที — ถ้าดูเหมือน "ต้นไม้เต้น" แปลว่าแรงไป
- [ ] ผู้เล่นทะลุ: เห็นชั้นล่างแหวกขณะก้อนใบชั้นบนเกือบนิ่ง ไม่ใช่ทั้งก้อนยืด
- [ ] ผู้เล่นถูกก้อนใบบังบางส่วนเมื่ออยู่ในพุ่ม (ถ้าตั้งใจให้ทะลุได้)
- [ ] ลำต้นนิ่งสนิท, เท้าหญ้านิ่ง
- [ ] ไม่มีพิกเซลครึ่ง ๆ / เบลอ, ไม่มีพิกเซลจาก sprite อื่นโผล่ที่ขอบ, ขอบ silhouette ไม่แหว่ง
- [ ] pause แล้วทุกอย่างหยุด, เล่นต่อแล้วไม่กระตุก
- [ ] SSAO/เงารอบใบตรงกับใบที่เห็น (ทุก pass เลื่อนเหมือนกัน)
