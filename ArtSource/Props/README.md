# Props ทรงกล่องที่วาดเป็นรูป สำหรับห้อง ThreeQuarter

เทคนิคนี้ใช้กับ prop ทึบทรงกล่อง เช่น ตู้ ชั้นหนังสือ ลัง หรือโต๊ะ ที่ศิลปินวาดเป็นรูปเดียวแต่ต้องการให้อยู่ในเกมเหมือนเป็น 3D
ตัวอย่างแรกคือ `Wardrobe_Test` (ตู้เสื้อผ้า) ซึ่งอยู่ใน `Test_Zones`

## ทำไมไม่ใช้ sprite การ์ดใบเดียว

ของทึบที่กว้างใช้การ์ดตั้งใบเดียวไม่ได้ ถ้าวางการ์ดผ่านกลางฐาน ขอบล่างจะจมลงพื้นตรงมุม ("ทะลุพื้นตรงขอบ")
ถ้าเลื่อนการ์ดมาข้างหน้า ก็จะไปบังผู้เล่นที่ยืนอยู่หน้าตู้ วิธีที่ใช้คือ **เอารูปไปแปะลงบนหน้าของกล่อง 3D จริง**
(หน้า Front, Side และ Top) ทุก pixel จึงมีความลึกจริง ไม่จมพื้น และบังหรือโดนบังถูกต้องโดยไม่ต้องเขียนโค้ดเรียงลำดับ
เมื่อมองจากกล้อง ThreeQuarter ที่ไม่หมุน ภาพที่ได้จะตรงกับที่วาดไว้

ส่วนที่ยื่นตั้งขึ้นจากกล่อง เช่น รูปปั้นบนหลังตู้ ไม่ได้แปะบนหน้ากล่อง แต่แยกเป็น sprite billboard ปกติ
(CLAUDE.md §7: stretch ×1.155 ในรูป, ไม่เอียงการ์ด, ไม่มีเงา)

## ขั้นตอน

1. สร้างโฟลเดอร์ใหม่ใต้ `ArtSource/Props/<ชื่อ>/` ใส่รูปต้นฉบับ (`source.png`, พื้นหลังโปร่งใส) และ `prop.json`
   (ก็อปจาก `Wardrobe_Test/prop.json` แล้วแก้)
2. รัน `python3 tools/prop_faces.py check <ชื่อ>` แล้วเปิด `<ชื่อ>/check_fit.png`
   กรอบสีที่วาดทับรูปต้องตรงกับขอบของตู้ในรูป ถ้าไม่ตรง ให้แก้ `source_pitch`, `source_yaw` และขนาดกล่อง แล้วรันใหม่
3. รัน `python3 tools/prop_faces.py bake <ชื่อ>` ไฟล์จะออกมาที่ `<ชื่อ>/out/`:
   - `<output_name>_Front.png`, `_Side.png`, `_Top.png` คือ texture ของแต่ละหน้า
   - `<output_name>_<part>.png` คือ sprite ของส่วนที่ตั้งขึ้น (ถ้ามี)
   - `unity_setup.json` คือค่า position, rotation และ scale ของทุก quad และ pivot ของ sprite
   - `preview_game_camera.png` คือภาพจำลองจากกล้องเกม ให้เทียบกับรูปต้นฉบับ
4. ทำใน Unity ตามหัวข้อด้านล่าง

ต้องมี Python พร้อม `Pillow`, `numpy` และ `scipy` (`pip install pillow numpy scipy`)

## ช่องใน prop.json

| ช่อง | ความหมาย |
|---|---|
| `source` | ชื่อไฟล์รูปต้นฉบับในโฟลเดอร์เดียวกัน |
| `source_pitch`, `source_yaw` | มุมกล้องที่รูปต้นฉบับถูกวาด (องศา) ตู้ตัวอย่างวาดที่ประมาณ 19 / 48 ซึ่งต่างจากเกม (30 / 45) สคริปต์จะแก้มุมให้เอง หาค่าด้วย `check` |
| `width_z` | ความกว้างของหน้าตู้ (หน้าที่อยู่ซ้ายจอ) เป็นหน่วยโลก |
| `depth_x` | ความลึกของตู้ (หน้าข้างที่อยู่ขวาจอ) |
| `height` | ความสูงของตู้ |
| `margin` | ขอบเผื่อรอบแต่ละหน้า (หน่วยโลก) สำหรับส่วนที่ยื่นออกนอกกล่อง เช่น บัว หรือขาตู้ ค่าเริ่ม 0.15 |
| `texture_px_per_unit` | ความละเอียดของ texture หน้ากล่อง (pixel ต่อ 1 หน่วยโลก) 256 คมพอสำหรับ 1080p |
| `sprite_px_per_unit` | PPU ของ sprite ส่วนที่ตั้งขึ้น |
| `body_rows` | แถว pixel บนและล่างในรูปต้นฉบับที่เป็นตัวตู้ ใช้ปรับสเกลรูปให้เข้ากับกล่อง |
| `upright_parts` | รายการส่วนที่ตั้งขึ้น แต่ละอันมี `name`, `box` (กรอบ x0, y0, x1, y1 ในรูปต้นฉบับ) และ `base` (จุดที่ส่วนนั้นแตะหลังตู้) ถ้าไม่มีให้ใส่ `[]` |
| `output_name` | ชื่อขึ้นต้นของไฟล์ที่ออกมา |

## ประกอบใน Unity

แกนของกล่อง: หน้า Front หันไปทาง −x (ซ้ายจอ), หน้า Side หันไปทาง −z (ขวาจอ), หน้า Top หันขึ้น +y และจุดกำเนิดอยู่กลางฐาน
ถ้าจะหมุนตู้ในฉาก ต้องหมุนเป็นทีละ 90° เท่านั้น และต้องวาดหน้าที่หันเข้ากล้องใหม่ เพราะกล้องเห็นแค่สองหน้าข้าง

1. **Texture หน้ากล่อง** วางที่ `Assets/_Project/Art/Sprites/ThreeQuarter/Props/` ตั้ง Texture Type = Default
   (ไม่ใช่ Sprite) และ Alpha Is Transparency เปิด
2. **Material หน้าละ 1 อัน** วางที่ `Art/Materials/` ใช้ URP Lit แบบ Opaque และเปิด **Alpha Clipping**
   Base Map คือ texture ของหน้านั้น สีขาว, Smoothness 0, ปิด Specular Highlights, Environment Reflections และ Receive Shadows
   (ดู `Wardrobe_Test_Front.mat` เป็นตัวอย่าง)
3. **Prefab** (ดู `Prefabs/Props/Wardrobe_Test.prefab`) ประกอบด้วย:
   - `Face_Front`, `Face_Side`, `Face_Top`: Quad คนละใบ ใส่ position, rotation และ scale จาก `unity_setup.json`
     ตั้ง Cast Shadows = Off และไม่ต้องมี collider (ถ้าสร้าง Quad จากเมนู ให้ลบ MeshCollider ทิ้ง)
   - `ShadowBox`: Cube ขนาดเท่ากล่อง ตั้ง Cast Shadows = **Shadows Only** เพื่อให้ตู้มีเงาตกบนพื้นแบบของทึบ
   - `Blocking`: BoxCollider ขนาดเท่ากล่อง อยู่ใน layer `Blocking` เพื่อกันผู้เล่นเดินทะลุ
   - ส่วนที่ตั้งขึ้น (เช่น `Statue`): SpriteRenderer กับ `SpriteBillboard`, ใช้ material แบบ alpha clipping
     (`SpriteLit_Trunk`), Cast Shadows = Off และวางที่ position จาก `unity_setup.json`
   - ถ้าจะให้คลิกได้ ให้ใส่ collider `Interactable` บน root ตาม CLAUDE.md §6
4. วาง prefab ในฉากโดยให้ฐานแตะพื้น (y = ความสูงพื้น)

## ข้อจำกัด

- ใช้ได้กับห้อง **ThreeQuarter** เท่านั้น (กล้อง 30 / 45 ไม่หมุน) ใน SideView ต้องทำใหม่
- ใช้ได้เฉพาะของที่เป็นทรงกล่อง ของที่โค้งมาก เช่น ถังหรือโอ่ง ให้ทำเป็นโมเดล 3D ตาม CLAUDE.md §7
- ส่วนของรูปที่อยู่นอกกล่องเกิน `margin` จะถูกตัดหายไป ถ้าเป็นส่วนที่ตั้งขึ้น ให้แยกเป็น `upright_parts`
- PNG ในโฟลเดอร์นี้เก็บใน git ปกติ (ไม่ใช่ LFS) ตาม `.gitattributes` ของโฟลเดอร์
