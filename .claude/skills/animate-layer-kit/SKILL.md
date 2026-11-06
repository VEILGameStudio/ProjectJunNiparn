---
name: animate-layer-kit
description: Use when a teammate brings a layered character kit (PNG layers on one shared canvas plus assembled.png, often a zip from an image generator) and wants it animated - a front walk or idle/blink GIF, or a rigged PSB for Unity 2D Animation.
---

# Animate a layered character kit — ทำ animation จากชุด layer ตัวละคร

ใช้เมื่อมีชุด layer ของตัวละคร (ทุก layer อยู่บน canvas ขนาดเดียวกัน วางตรงตำแหน่งแล้ว + `assembled.png`)
แล้วอยากได้ GIF ท่าเดิน / ยืนหายใจ+กะพริบตา หรือไฟล์ PSB ที่แยกชิ้นไว้ทำ rig ใน Unity

ตัวอย่างที่ทำสำเร็จแล้ว: Phawin มุมหน้า — rig file `rigs/phawin_front.json`

## หัวใจ 6 ข้อ (ทำไมต้องทำแบบนี้)

1. **kit แบบนี้ "ต่อกันพอดี" ไม่ "ซ้อนกัน"** — ทุก layer คือส่วนที่มองเห็นเท่านั้น ข้างใต้ว่างเปล่า บางรอยตัดเป็นเส้นตรง
   (ผมหน้าม้าถูกตัดเป็นรูปหลายเหลี่ยม ปอยผมไปอยู่ใน layer หน้า) ขยับทีละ layer = เห็นรู
2. **รวมชิ้นที่ขยับด้วยกันก่อน** — หน้า+ผม+ปาก = หัว, แขน+แขนเสื้อ = แขน, แขนข้างที่ถือกระเป๋า+สาย+กระเป๋า = ชิ้นเดียว
   รวมแบบบวก alpha (ขอบที่แชร์กันรวมกลับเป็นภาพเดิมเป๊ะ) ห้าม alpha-over (จะเห็นเส้นจาง ๆ ทุกรอยต่อ)
3. **เติมส่วนที่ซ่อนอยู่ แค่เป็นแถบกว้าง `reach` px จากส่วนที่มองเห็น** — เสื้อใต้สายกระเป๋า, กางเกงใต้มือ, ผิวใต้คาง
   `reach` ต้องมากกว่าระยะที่ข้อต่อขยับมากสุด (มือในท่าเดิน ~19 px → ใช้ 26) อย่าเติมเต็มพื้นที่ใต้ชิ้นบน:
   ลองแล้วทั้ง convex hull และลากเส้นขอบต่อ ได้ "แผ่นผี" ลอยออกมาข้างแขนเวลาแขนแกว่ง
4. **ขยับด้วยการดัดโค้ง ไม่ใช่หมุนทั้งชิ้น** — แขนเสื้องอจากตะเข็บไหล่ (บนสุดไม่หมุน ล่างหมุนเต็ม), ขากางเกงย่น
   ช่วงระหว่างเชือกผูกเอวกับลายขอบขาเวลายกเท้า, ปลายผมเด้งตามหัวช้ากว่านิดหนึ่ง หมุนแข็งทั้งชิ้น Oak บอกว่า "ดูแตก"
5. **เส้นขอบไปกับชิ้นที่มันเป็นขอบให้** — เศษเส้นดำเล็ก ๆ หรือเส้นตะเข็บที่ kit ตัดไปไว้ใน layer ล่าง ต้องย้ายไปอยู่กับชิ้นบน
   ไม่งั้นแขนขยับแล้วเส้นยังค้างอยู่ที่เดิม (script ทำให้อัตโนมัติ)
6. **ตรวจด้วยตาก่อนส่งทุกครั้ง** — `check_sheet.png` ดันแขนกับหัวแรงกว่าใน GIF 1.5 เท่า ถ้าตรงนั้นยังดูดี GIF ก็ดี

## ขั้นตอน

### 1. รับ kit
- ถ้าแนบเป็น zip ใน Issue (ลิงก์ `https://github.com/user-attachments/files/...zip` — repo นี้เป็น public โหลดได้ด้วย `curl -L`)
  ให้แตกลงโฟลเดอร์ชั่วคราวก่อน แล้วคัดเฉพาะ `assembled.png`, `layers/*.png`, `layout.json`, `README*` ไปไว้ที่
  `ArtSource/Characters/<Name>/<view>-kit/` (`<view>` = `front`, `back`, `side-left` …) ไม่เอา `.psd/.ora` ที่ซ้ำ
- ถ้าอยู่ใน repo แล้ว ใช้ path นั้นเลย
- อ่าน README ของ kit ด้วย: L/R หมายถึงฝั่งคนดูหรือฝั่งตัวละคร
- `ArtSource/` อยู่นอก `Assets/` โดยตั้งใจ: Unity ไม่ import ไฟล์ต้นฉบับพวกนี้

### 2. Rig file
- Phawin มุมหน้า: `rigs/phawin_front.json` ใช้ได้เลย ถ้าชื่อ layer และขนาด canvas ตรงกัน
- ตัวละครอื่น / มุมอื่น: copy เป็น `rigs/<name>_<view>.json` แล้วแก้ โดยดูจาก `assembled.png` (ค่าทั้งหมดเป็น px บน canvas, y ลงล่าง)

| key | คืออะไร |
|---|---|
| `layers` | ชื่อไฟล์ใน `layers/` ทุกไฟล์ |
| `fixes` | พิกเซลที่ kit ตัดผิด layer: `bluish_dark` (ผ้าสีกรมในมือ/ข้างกระเป๋า), `light_neutral` (เสื้อสีครีมในจี้) |
| `splits` | layer ที่เป็นของสองชิ้น ตัดที่แนว y เช่นคอใต้คาง = ตัว ไม่ใช่หัว |
| `parts` / `order` | ชิ้นที่ขยับด้วยกัน และลำดับวาด ล่าง → บน |
| `rig` | หน้าที่ของแต่ละชิ้น: `body`, `head` (pivot = คาง, ศูนย์กลางพุ่มผม), `eye` (`outer` = หางตาอยู่ฝั่งไหน), `arm` (pivot = ไหล่, `bend_from_y`/`bend_to_y` = ช่วงที่แขนเสื้องอ), `swing` (จี้/ของห้อย), `leg`/`foot` (`side` R/L) |
| `lines` | เส้นอก/เอว, บน/ล่างขากางเกง, ช่วงที่ขากางเกงย่น, พื้น |
| `walk` / `idle` | ระยะยกเท้า, ขึ้นลง, โยกข้าง, องศาแขน (บวก = ตามเข็มนาฬิกา), จำนวนเฟรม, ms ต่อเฟรม |

### 3. รัน
```
pip install numpy opencv-python pillow psd-tools
S=.claude/skills/animate-layer-kit
python3 -I $S/scripts/build_parts.py <kit_dir> $S/rigs/<rig>.json /tmp/lk_build
python3 -I $S/scripts/animate.py     /tmp/lk_build $S/rigs/<rig>.json <out_dir> check
python3 -I $S/scripts/animate.py     /tmp/lk_build $S/rigs/<rig>.json <out_dir> all
python3 -I $S/scripts/export_rig.py  /tmp/lk_build $S/rigs/<rig>.json <out_dir>
```
- `build_parts.py` ต้องพิมพ์ `rest pose vs assembled.png: mean diff` ต่ำกว่า 0.5 ถ้าสูงกว่า = ชื่อ layer/การรวมผิด
- เปิด `check_sheet.png` ดูเอง: รู, แผ่นสีลอยข้างแขน, เส้นดำขาด ๆ ค้างที่เดิม, ตาปิดดูเป็นปื้น
  แก้ที่ rig file (`fixes`, `splits`, `reach`, จุด pivot) ห้ามแก้ด้วยการลดการขยับจนแทบไม่ขยับ
- `<out_dir>` = `ArtSource/Characters/<Name>/<view>-anim/` commit: GIF, `*_rig.psb`, `*_rig.json`, `*_parts_sheet.png`
  (ไม่ต้อง commit `parts/*.png` เพราะอยู่ใน PSB แล้ว)

### 4. ส่งงาน (ภาษาไทย ตาม CLAUDE.md §10)
ไฟล์ที่ได้ + path, สิ่งที่ script เติมให้ (ดูจากบรรทัด `fix:` / `crumbs` / `seam`), สิ่งที่ยังไม่ได้ทำ

## ขอบเขต — อย่าข้าม

- **ยังไม่ใช่ sprite ในเกม** ไฟล์พวกนี้ยังไม่ยืด: การเอาเข้าเกม (ยืด ×1.015 SideView / ×1.155 ThreeQuarter, PPU จากความสูงหลังยืด,
  pivot ที่เท้า, โฟลเดอร์ตาม preset) เป็นงานของ `import-sprite-art` ห้ามวางผลลัพธ์ใน `Assets/` จาก skill นี้
- **ทำได้ทีละมุมมอง** มุมข้าง/หลังต้องมี kit ของมุมนั้นเอง SideView ต้องมี 6 ทิศ วาดแยกทุกทิศ ห้าม flip กระจก (CLAUDE.md §7)
  ไม่มี kit มุมนั้น = บอกทีมว่าต้องการ kit อะไรเพิ่ม อย่าเดาวาดเอง
- ท่าเดินใน GIF เป็นการเดินอยู่กับที่หันหน้าเข้ากล้อง ไม่ใช่การเดินข้ามจอ
