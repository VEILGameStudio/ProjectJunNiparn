---
name: level-readability
description: Use when checking or improving whether players can read a level or screen at a glance - what is clickable, where to go, what the goal is, what is blocked - or when someone says players get lost, miss a clue, click everything, or can't tell props from interactables. Covers layout, value contrast, guiding the eye and HD-2D camera pitfalls, with a screenshot check script.
---

# Level readability — จัด layout ให้ผู้เล่นเข้าใจง่าย

เป้าหมาย: ผู้เล่นมองจอ **3 วินาที** แล้วตอบได้ 4 ข้อ

1. **ฉันอยู่ไหน** (landmark, ดินแดน)
2. **ไปทางไหนได้** (ทางเดิน vs ทางตัน)
3. **อะไรคลิกได้** (ของ puzzle vs ของประดับ)
4. **อะไรขวางอยู่** (lock / เป้าหมาย)

ถ้าข้อไหนตอบไม่ได้ = ปัญหาความชัด ไม่ใช่ปัญหา puzzle — แก้ที่ฉากก่อนเพิ่มคำใบ้

สเปก: CLAUDE.md §1 (ThreeQuarter "whole room readable"), §6 (collider คลิก/ชน), §7 (กล้อง, highlight, UI scale)
งานศิลป์ฉาก SideView ละเอียดอยู่ใน `sideview-forest-background` (value/ชั้น/สี) — skill นี้ดูจาก "ผู้เล่นเข้าใจไหม"

## หัวใจ 8 ข้อ

1. **ลำดับชั้นการมอง (visual hierarchy)**: ภวิน > ของที่ต้องคลิก/lock > ทางไป > ของประดับ > ฉากหลัง
   ชั้นบนต้องชนะชั้นล่างด้วย **value contrast ก่อนสี** (หรี่ตาแล้วยังเห็น) — `references/01-visual-hierarchy.md`
2. **ของคลิกได้ต้องมี "ภาษา" ของตัวเอง**: ขอบชัดกว่า, อิ่มสีกว่า, ไม่จมในลายพื้นหลัง, + highlight ขาวที่มีอยู่แล้ว (CLAUDE.md §7)
   ของประดับห้ามหน้าตาเหมือนของคลิกได้ (เช่น กล่องประดับรูปเดียวกับกล่องที่เปิดได้)
3. **นำสายตาด้วยฉาก ไม่ใช่ลูกศร**: แสง, เส้นนำ (ทางเดิน, รั้ว, ราก), กรอบ (ซุ้ม, ประตู), ทิศที่ NPC มอง, landmark — `references/02-guiding-the-eye.md`
4. **เห็น lock ก่อน**: เข้าห้องแล้วสิ่งแรกที่ตาไปหยุดควรเป็นเป้าหมาย/สิ่งที่ขวาง
5. **ทางไปดูเป็นทาง ทางตันดูเป็นทางตัน**: ช่องเปิดสว่างกว่า, พื้นต่อเนื่อง; ทางตันปิดด้วยมวลมืด/ผนัง ไม่ใช่ "พื้นต่อแต่เดินไม่ได้"
6. **ไม่มีอะไรสำคัญถูกบัง**: ThreeQuarter = sprite สูงขอบล่างจอบังของด้านหลัง; SideView = foreground lip / ชั้นหน้ากล้องบังระนาบเดิน — `references/03-hd2d-camera.md`
7. **ภาษาภาพสม่ำเสมอทั้งเกม**: สัญลักษณ์ผู้พิทักษ์ (ดอกไม้/สายน้ำ/ดวงอาทิตย์/ภูเขา จาก P7), สีของ clue, รูปทรงของประตูที่ล็อก — ใช้แบบเดิมทุกดินแดน
8. **พิสูจน์ด้วยคนเล่นจริง ไม่ใช่ความรู้สึกคนทำ**: คนทำฉากรู้คำตอบอยู่แล้ว มองไม่เห็นปัญหาเอง — `references/04-playtest.md`

## Workflow: เช็กฉาก/ห้องหนึ่ง

1. **จับภาพหน้าจอ** จาก Game view (ไม่ใช่ Scene view) ที่ 1920×1080 — ทั้งตอนเข้าห้องและตอนยืนกลางห้อง
2. **รันสคริปต์**
   ```
   python3 .claude/skills/level-readability/scripts/readability_check.py shot.png \
       --box player:900,600,120,260 --box lock:1400,380,160,200 --box clue_frame:300,300,90,90 \
       --out shot_readability.png
   ```
   (`--box ชื่อ:x,y,กว้าง,สูง` พิกัดพิกเซลจากมุมซ้ายบน; ชื่อ `player` = ภวิน; ใส่ทุกของที่คลิกได้)
   ได้ภาพ 4 ช่อง: ต้นฉบับ / ขาวดำ / หรี่ตา (squint) / 3 ระดับ value พร้อมกรอบ และรายงาน contrast ต่อกล่อง
   Pillow + numpy เท่านั้น (ถ้าไม่มี: `pip install pillow numpy`)
3. **อ่านผล**
   - กล่องที่ `LOW` = ของสำคัญจมในพื้นหลัง → แก้ value (สว่าง/มืดกว่ารอบข้าง) หรือเปลี่ยนพื้นหลังรอบมัน
   - ช่อง squint: ของที่ "หาย" ไปตอนหรี่ตา = ผู้เล่นจะมองข้าม
   - ช่อง 3 ระดับ: ภวินกับ lock ควรตกคนละระดับกับพื้นที่ล้อมรอบ
   - ของประดับที่ contrast สูงกว่าของคลิกได้ = แย่งความสนใจ → ลด
4. **ไล่ checklist** `references/05-checklist.md` ข้อที่สคริปต์ตรวจไม่ได้ (ทาง, บัง, ภาษาภาพ, UI)
5. **สรุปให้ทีม (ไทย)**: ตาราง ปัญหา / ผู้เล่นจะเจออะไร / แก้ยังไง / ใคร (อาร์ต = ปรับภาพ, Intern B = ย้ายของ/ตั้ง Inspector)
6. ถ้าแก้ได้ด้วยการย้ายของในฉาก → เป็นงาน Intern B ในฉากที่ตัวเองเป็นเจ้าของเท่านั้น; ห้ามแก้ฉากคนอื่น

## ห้ามใช้แก้ปัญหาความชัด (ชนกฎ)

| อยากทำ | ทำไมไม่ได้ | ทางที่ถูก |
|---|---|---|
| เปลี่ยนมุม/ซูมกล้องห้องนี้ | preset ล็อก (CLAUDE.md §0.5, §7) | ย้ายของ, ลดความสูงของที่บัง, ใช้ override ที่ CLAUDE.md อนุญาต (zoom/bounds ใน `LevelSettings`) |
| เขียนโค้ดเรียง sprite ให้ของอยู่หน้า | ห้าม sort ตามตำแหน่ง (§0.7) | ย้ายของ/ใช้ depth จริง |
| ทำ sprite โปร่งแสงเมื่อบังผู้เล่น | alpha blending ห้าม (§0.3) และเป็นระบบใหม่ | ไม่วางของสูงหน้าทางเดิน; ถ้าจำเป็นจริง → `design-change-proposal` |
| ปรับ brightness ของฉากจาก trigger | brightness อยู่ใน settings เท่านั้น (§7) | ปรับแสงในฉาก (light/volume ของฉาก) |
| เพิ่ม UI ลูกศรนำทาง | ระบบ UI ใหม่ = งานออกแบบ | นำด้วยฉากก่อน; ถ้ายังจำเป็น → `add-ui-screen` + ขอ Oak |

## ไฟล์ใน skill นี้

- `references/01-visual-hierarchy.md` — value, contrast, saturation, ขอบ, ขนาด; ภาษาของของคลิกได้
- `references/02-guiding-the-eye.md` — แสง, เส้นนำ, กรอบ, landmark, breadcrumb, NPC มอง, เสียง
- `references/03-hd2d-camera.md` — กับดักความชัดเฉพาะ ThreeQuarter / SideView ของเกมนี้
- `references/04-playtest.md` — วิธีเทสต์กับคนเล่นครั้งแรก + สิ่งที่จด
- `references/05-checklist.md` — checklist ก่อนส่งฉาก
- `scripts/readability_check.py` — ภาพ squint/value + contrast ต่อกล่อง
