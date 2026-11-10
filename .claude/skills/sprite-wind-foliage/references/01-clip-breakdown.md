# 01 — ถอดคลิป Wind ผ่านต้นไม้ (78 วินาที)

คลิป: devlog แนวตั้ง 720×1280, 30 fps, เกม pixel-art side-scroller (ตัวละครชื่อ Kira)
เอนจินในคลิปคือ **GameMaker** (sprite ชื่อ `sEnvOakTreeDistOverlay`, หน้าต่าง sprite editor มี Collision Mask / Nine Slice / Fps 30)
ต้นไม้ใหญ่เป็น sprite **121 × 151 px** วาดแบบ pixel art แล้วขยายบนจอ ~6 เท่า (1 art px ≈ 6 px ในคลิป)

## คำบรรยายเต็ม (อ่านจาก caption ที่ฝังในภาพ ทีละ 0.25 วินาที)

| เวลา | คำพูด | ภาพที่เห็น |
|---|---|---|
| 0–2 | "Updating the trees in my game." | ฉากจริง ต้นใหญ่ + Kira วิ่งผ่าน |
| 2–5 | "Yesterday I made these mini trees interactable," | พุ่มเล็ก 2 ต้นสั่นเมื่อ Kira กระโดดทะลุ |
| 5–9 | "while it was [late?] … I think it's time we [make the big] ones do the same," | ต้นใหญ่ในห้องทดสอบ (พื้นตาราง) |
| 10–17 | "hooking it up to the disturbance system I set up last week. This scrolling noise texture is how [much each pixel] of the tree should move." | texture noise เต็มจอ เขียว→ส้ม→แดง |
| 18–23 | "The more red it is means more right, and the more green, it should go down." | ต้นไม้โยกเบา ๆ ตามลม |
| 24–30 | "Piss yellow means the pixel doesn't change, because it's 50 red and 50 green." | noise (บน) **+** texture disturbance (กลาง, สีเหลืองขี้ม้า มีจุดชมพูตรงตัวละคร) |
| 31–35 | "The tree looks [good] by itself. The wind looks good, but let's see what happens" | ต้นไม้ + ลมอย่างเดียว |
| 36–41 | "when Kira goes through the leaves. You — why on earth does it look like slime?" | Kira กระโดดเข้าพุ่มใบ ใบไม้ยืดเหลวรอบตัว |
| 41–44 | "Well, that's because the texture changes smoothly." | noise + disturbance ซ้อนกัน |
| 45–51 | "These are the tree's layers. The trunk is different because we don't want it to sway in the wind." | sprite 3 เฟรม: 1/3 ต้นเต็ม, 2/3 ใบ, 3/3 ลำต้น+กิ่ง |
| 51–54 | "But here's a duplicate leaf layer, but it has a bunch of holes in it." | `sEnvOakTreeDistOverlay` = ใบซ้ำที่เจาะรู |
| 55–60 | "What I do is layer this on top of the tree, then I apply the same wind displacement, so it looks the same," | ต้นไม้ดูเหมือนเดิมทุกอย่างตอนมีแค่ลม |
| 60–63 | "but the interaction is dulled a lot." | Kira กระโดดผ่าน |
| 63–71 | "This makes it have two interactions, making the tree feel a lot more real than the slime we had before." | ซูมเข้าใกล้: Kira อยู่ *ใน* พุ่มใบ บางส่วนถูกใบบัง |
| 71–78 | "Anyways, you can play the Soul Flame demo for free on Steam…" | ฉากจริง (ปิดท้ายโฆษณา) |

## สรุปเทคนิคเป็นภาษาคน

1. **ลมไม่ได้ขยับ vertex** — ขยับ *ทีละพิกเซล* ด้วยการเลื่อนตำแหน่งที่อ่าน texture (UV displacement / flow map)
2. **ค่าการเลื่อนอ่านจาก texture สี** — R = ซ้าย/ขวา, G = ขึ้น/ลง, 0.5 = ไม่ขยับ ("piss yellow" = R 0.5 G 0.5)
3. **ลม = noise ที่เลื่อน (scroll) ไปเรื่อย ๆ**, **ตัวละคร = texture disturbance** ที่ระบบเกมวาดจุดลงไปตรงที่ตัวละครอยู่ แล้ว **บวกกัน**
4. **ปัญหา slime** — noise และ disturbance ต่างก็ "เรียบ" (smooth gradient) พิกเซลข้างเคียงจึงเลื่อนเกือบเท่ากัน ใบไม้ทั้งก้อนจึงยืดเหมือนเยลลี่ ไม่ใช่ใบไม้หลายใบแหวกออก
5. **วิธีแก้ = แยกชั้น**
   - ลำต้นเป็นชั้นแยก ไม่โดนลม
   - ใบไม้หลัก โดนลม + โดนตัวละครเต็มที่
   - **ใบซ้ำที่เจาะรู วางทับข้างบน** โดนลม *เท่ากัน* (ตอนนิ่งจึงดูเหมือนต้นเดียว) แต่โดนตัวละคร *น้อยมาก*
   - ผล: ตอนตัวละครทะลุ ชั้นล่างแหวก ชั้นบนแทบไม่ขยับ → เห็นเป็น "สองชั้นของใบไม้" มีความลึก แทนที่จะเป็นก้อนเดียวที่ยืด
6. จากภาพซูมช่วง 63–64 วินาที ตัวละครถูกก้อนใบบังบางส่วน ⇒ ชั้นใบเจาะรูถูกวาด **หน้าตัวละคร** ด้วย
   (คำบรรยายไม่ได้พูดตรง ๆ แต่ภาพแสดงชัด — ดู `img/w05-slime-vs-layered.jpg`)

## ภาพประกอบ

| ไฟล์ | เห็นอะไร |
|---|---|
| `img/w01-reference-tree.jpg` | ต้นไม้ในห้องทดสอบ ขนาดเทียบตัวละคร |
| `img/w02-flow-noise-and-disturbance.jpg` | ซ้าย: noise ลม (ส้มแดง), ขวา: noise + texture disturbance สีเหลืองขี้ม้า จุดชมพูคือตัวละคร |
| `img/w03-tree-layers.jpg` | 4 ชั้น: ต้นเต็ม / ใบ / ลำต้น+กิ่ง / ใบซ้ำเจาะรู |
| `img/w04-motion-diff.jpg` | heatmap พิกเซลที่เปลี่ยนใน 0.5 วินาที: ลมอย่างเดียว / slime / แยกชั้น |
| `img/w05-slime-vs-layered.jpg` | ซูมก่อน–หลังแก้ ขณะตัวละครทะลุใบ |
