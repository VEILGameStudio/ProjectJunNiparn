# Blockout kit — ThreeQuarter (2D isometric grid box)

ชุดรูปทรงเทา (grey box) แบบ GridBox แต่เป็นภาพ 2D ไว้ให้ art วาดทับเป็น base
ทุกชิ้นวาดจากมุมกล้อง `ThreeQuarter` ของเกมจริง: orthographic, pitch 30, yaw 45
ซึ่งตรงกับ isometric 2:1 พอดี (พื้น 1 ช่องเป็นข้าวหลามตัด กว้าง:สูง = 2:1)

ขนาด: **1 unit = 108 px ที่ 1080p** (ขนาดเดียวกับในเกม) ไฟล์ `2x` = 216 px ต่อ unit ไว้วาดละเอียด
เส้นกริดบนทุกหน้า: เส้นเข้มทุก 1 unit, เส้นจางทุก 0.25 unit ตรงกับกริดโลกจริง ชิ้นที่วางติดกันเส้นจึงต่อกันพอดี

## ไฟล์

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `kit_sheet.png` | ดูทุกชิ้นพร้อมชื่อ |
| `shapes/1x/grey/`, `shapes/2x/grey/` | แต่ละชิ้นแบบสีเทามีแสงเงา (บนสว่าง ซ้ายกลาง ขวาเข้ม) |
| `shapes/1x/lines/`, `shapes/2x/lines/` | แต่ละชิ้นแบบเส้นอย่างเดียว พื้นใส ไว้วางบนสุดแล้ววาดใต้ |
| `shapes/pivots.json` | จุดที่ชิ้นแตะพื้น (กลาง footprint) เป็น px และเป็นค่า pivot ของ Unity |
| `floor_grid_1x.png`, `floor_grid_2x.png` | กริดพื้น 16 x 16 unit พื้นใส |
| `example_scene_1x.png`, `_2x.png` | ตัวอย่างฉากเล็กที่ต่อจาก kit |
| `example_scene_2x.psd` | ฉากเดียวกัน แยก layer ทีละชิ้น + layer เส้น + layer "paint here" |

ชื่อชิ้นบอกขนาด: `block_2x1_h1` = กว้าง 2 ตามแกน x, ลึก 1 ตามแกน z, สูง 1
`upPlusX` / `upMinusZ` = ทางลาดหรือบันไดสูงขึ้นไปทาง +x / -z
ในภาพ แกน +x ชี้ไปทางขวาบน, แกน +z ชี้ไปทางซ้ายบน

## วิธีใช้วาดทับ

1. เปิด `example_scene_2x.psd` หรือวางชิ้นจาก `shapes/2x/grey/` บน `floor_grid_2x.png`
2. วาดใน layer ใหม่ ใช้เส้นกริดจับสัดส่วนและระดับความสูง
3. ถ้าต้องการเส้นเป็นไกด์บนสุด ใช้ไฟล์ `lines` ตั้งเป็น Multiply

## ข้อควรรู้ก่อนเอาเข้าเกม (CLAUDE.md §7)

- ภาพนี้คือ "สิ่งที่กล้องเห็น" ใช้เป็น base สำหรับ concept, paintover และวาด prop
- **ของที่เป็นก้อนทึบ (กล่อง หีบ เกวียน บ้านเล็ก) ในเกมเป็นโมเดล 3D** ไม่ใช่ sprite และพื้น/ผนัง/บันไดเป็น geometry 3D
  ภาพที่วาดจาก kit นี้จึงใช้เป็นแบบให้ปั้นโมเดลหรือทำ texture ได้ แต่อย่าเอาไปเรียงเป็น tile 2D ในฉาก
- ถ้าวาดเป็น **sprite ยืน** (ต้นไม้ ป้าย พุ่มไม้) แล้วจะเอาเข้าเกม ต้องยืดแนวตั้ง **x 1.155** ตอน export
  และคิด PPU จากความสูงหลังยืด (ดู skill `import-sprite-art`) แล้วเก็บใน `Art/Sprites/ThreeQuarter/`
- kit นี้ใช้กับพื้นที่ `ThreeQuarter` เท่านั้น ห้ามใช้กับ `SideView`

## สร้างใหม่

```
pip install pillow numpy psd-tools
python3 tools/build_blockout_kit.py
```

เพิ่มหรือแก้ชิ้นได้ที่ฟังก์ชัน `kit_shapes()` และฉากตัวอย่างที่ `example_scene()` ใน script
