---
name: sideview-forest-background
description: Use when designing, painting, critiquing or setting up a side-view (SideView) background - especially forest scenes - or when someone asks how to make a scene feel deep, layered, readable, framed, lit by time of day, or "like the reference clip" (layer stack, layout, camera, parallax, color, light, haze).
---

# Side-view forest background — senior artist playbook

ความรู้ชุดนี้ถอดมาจาก reference (คลิป gameplay ฉากป่า 2 คลิป + ภาพ pipeline 3 ภาพ: rough → lineart/value → final color kit)
ศึกษาแบบ "ศิลปินดูงานศิลปิน" ไม่ได้ยึดกฎ GDD — ส่วนที่ชนกับกฎใน `CLAUDE.md` จะเขียนกำกับไว้ใน
`references/06-unity-implementation.md` ให้เห็นชัดว่าต้องขออนุมัติก่อน

ภาพอ้างอิงอยู่ที่ `references/img/` (01–07) เปิดดูประกอบทุกครั้ง

## หัวใจ 7 ข้อ (ถ้าจำได้แค่นี้ก็พอ)

1. **ฉากคือเวทีละคร (proscenium)** — ซ้ายมีต้นไม้ใหญ่เป็น "ปีกเวที", ขวามีก้อนมวลใหญ่ (willow) ปิด, กลางคือจุดสนใจ
   (กระท่อมรูป A ประตูอยู่กลางจอ = จุดที่ตัวละครยืน). กรอบซ้าย–ขวาทำให้ตาไม่หลุดออกนอกจอ
2. **ความลึกมาจาก value ก่อนสี** — ไกล = สว่าง จาง contrast ต่ำ, ใกล้ = มืด เข้ม contrast สูง (ย้อนแสง/contre-jour)
   แสงอยู่ "ข้างหลัง" ป่าเสมอ ต้นไม้ด้านหน้าจึงกลายเป็นเงาตัดกับหมอกสว่าง
3. **5+1 ชั้น**: Haze plate → Deep silhouettes → Far trunks → Mid trunks → Hero set (ต้นใหญ่/กระท่อม/willow) → Ground + Foreground lip
   แต่ละชั้นต่างกันด้วย value, saturation, ความหนาเส้น, ปริมาณ detail — ห้ามมีสองชั้นที่ "หน้าตาเหมือนกัน"
4. **จังหวะลำต้น (rhythm)** — ลำต้นแนวตั้งระยะห่างไม่เท่ากัน ความกว้างไม่เท่ากัน และ **ทะลุขอบบนจอ** เสมอ
   (ทำให้ต้นไม้ดูสูงกว่าจอ = รู้สึกตัวเล็กในป่าใหญ่). ยอดที่โผล่ให้เห็นคือยอดหัก/ตอ เป็น silhouette ที่น่าสนใจ
5. **สี: ฐานเย็น + accent เดียวที่ร้อน/สด** — ทั้งฉากเขียวเทา-ฟ้า, มีมอส lime สดเป็นตัวเชื่อมทุกชั้น และไฟ/คบเพลิงสีส้มเป็นจุดร้อน
   ตัวละครอิ่มสีและเส้นหนากว่าฉากเพื่อให้อ่านออก
6. **ออกแบบฉากรอบ UI ตั้งแต่ rough แรก** — rough วาง HUD ทับก่อนวาดต้นไม้ ระดับพื้นถูกล็อกให้อยู่เหนือแถบ UI
   และวาดเกินกรอบกล้องเผื่อ zoom/pan/จอกว้าง
7. **ฉากเดียวกัน หลายอารมณ์** — วาดฉากเป็นกลาง แล้วเปลี่ยนเวลา (กลางวันเขียวเหลือง → ค่ำฟ้าน้ำเงิน) ด้วย color grade + แสงเฉพาะจุด
   ไม่ต้องวาดใหม่

## Workflow เมื่อได้โจทย์ "ทำฉาก side-view ใหม่"

1. อ่าน `references/01-reference-breakdown.md` เพื่อดูว่า reference ทำอะไรไว้บ้าง (ภาพ 01–07)
2. **Blocking (rough)** — ตาม `references/04-production-pipeline.md` ขั้น 1: วาง UI, เส้นพื้น, ขนาดตัวละคร, กรอบกล้อง, ปีกเวทีซ้าย/ขวา, จุดสนใจกลาง
3. **Layer plan** — กำหนดชั้นตาม `references/02-layer-stack.md` (ตาราง value/sat/เส้น/detail ต่อชั้น)
4. **Composition check** — ตาม `references/03-composition-layout.md` (thumbnail ขาวดำ, squint test, tangents, rhythm)
5. **Value pass** — greyscale ก่อนลงสี ต้องอ่านความลึกออกโดยไม่มีสี
6. **Color + light** — ตาม `references/05-color-light-atmosphere.md` (palette ต่อชั้น, haze, god rays, time-of-day grade)
7. **แยก layer + kit** — export ชั้นละไฟล์ + prop kit (หญ้า เห็ด ดอกไม้) ตาม pipeline ขั้น 4
8. **ลงเกม** — ตาม `references/06-unity-implementation.md` (ระยะ D ต่อชั้น, PPU, ความกว้าง, fog, volume, กล้อง dialogue)
9. **Critique** — ไล่ `references/07-critique-checklist.md` ทุกข้อก่อนส่ง

## ไฟล์ในโฟลเดอร์นี้

| ไฟล์ | เนื้อหา |
|---|---|
| `references/01-reference-breakdown.md` | ถอดคลิป/ภาพทีละชิ้น ว่าเห็นอะไร ใช้เทคนิคอะไร |
| `references/02-layer-stack.md` | โครงสร้างชั้นความลึก + กฎของแต่ละชั้น |
| `references/03-composition-layout.md` | การจัดวาง เวที จังหวะ สัดส่วน มุมกล้อง |
| `references/04-production-pipeline.md` | ขั้นตอนผลิตจาก rough → final → แยก asset |
| `references/05-color-light-atmosphere.md` | สี แสง หมอก บรรยากาศ เวลา |
| `references/06-unity-implementation.md` | แปลงทั้งหมดเป็นตัวเลขใน Unity ของโปรเจกต์นี้ |
| `references/07-critique-checklist.md` | เช็กลิสต์ตรวจงาน + ข้อผิดพลาดที่เจอบ่อย |
| `references/img/` | ภาพอ้างอิง (rough, value, color kit, in-game day/dusk, cutscene wide/zoom) |
