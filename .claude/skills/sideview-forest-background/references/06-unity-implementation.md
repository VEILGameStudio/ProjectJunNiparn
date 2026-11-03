# 06 — ลงเกมใน Unity (โปรเจกต์นี้)

โปรเจกต์นี้คือ Unity 6 URP 3D, preset `SideView` = **กล้อง perspective** จึงได้ parallax จากระยะจริงฟรี
(ดู `CLAUDE.md` §1 "Parallax backgrounds are built by placing layers")
ตัวเลขด้านล่างคิดจากสูตรใน CLAUDE.md: ระนาบที่ตัวละครเดินอยู่ห่างกล้อง D = 20, ความสูงที่มองเห็นที่ระยะ D = 0.335 × D,
ความเร็วบนจอ = 20 / D, **PPU = 1080 ÷ (0.335 × D)** ที่ 1080p

ตัวอย่างใช้งานจริงอยู่แล้ว: `Assets/_Project/Scenes/Test/Test_Forest.unity` (6 แถวต้นไม้ + linear fog)

## 1. ตารางระยะต่อชั้น (ค่าแนะนำเริ่มต้น)

| ชั้น | D (จากกล้อง) | ความเร็วบนจอ | PPU @1080p | สูงที่เห็น (unit) | กว้างขั้นต่ำ @21:9 (unit) |
|---|---|---|---|---|---|
| L6 Foreground lip | 14 | 1.43× (เร็วกว่าตัวละคร) | 230 | 4.7 | 10.9 + T |
| ระนาบตัวละคร / L5 พื้น | 20 | 1.0× | 161 | 6.7 | 15.6 + T |
| L4 Hero set | 23–26 | 0.87–0.77× | 140–124 | 7.7–8.7 | 18–20 + T |
| L3 Mid trunks | 35 | 0.57× | 92 | 11.7 | 27.4 + T |
| L2 Far trunks | 60 | 0.33× | 54 | 20.1 | 46.9 + T |
| L1 Deep silhouettes | 110 | 0.18× | 29 | 36.9 | 86 + T |
| L0 Haze plate | 250 | 0.08× | 13 | 83.8 | 195 + T |

- **T** = ระยะที่กล้องเลื่อนทั้งหมดในฉาก (world unit) — กล้องเลื่อนกี่ unit ทุกชั้นต้องกว้างเพิ่มเท่านั้น
- ระยะห่างระหว่างชั้นควรโตแบบ **ทวีคูณ** (×1.5–2 ต่อชั้น) ไม่ใช่บวกเท่ากัน — ความเร็วต่างกันจะดูเป็นขั้นบันไดสวย
- Far clip ของกล้องต้อง > 250 ถ้าใช้ L0 ที่ 250 (หรือย้าย L0 มาใกล้ขึ้นแล้วขยาย)

## 2. วาดอย่างไรให้ขนาดตรง

- **วาดทุกชั้นที่ความละเอียดจอ** (ภาพรวมทั้งฉากเหมือน img/03) แล้วตั้ง PPU ตามตาราง → ตอนกล้องอยู่จุดเริ่ม ภาพในเกมจะตรงกับภาพวาดพิกเซลต่อพิกเซล
- ถ้า export ทุกชั้นด้วย PPU ตัวละคร (161) ชั้นไกลจะเล็กผิดไปหลายเท่า (L0 เล็กลง 12 เท่า) — ความผิดพลาดยอดฮิต
- ระวัง: SideView ใช้การยืดภาพแนวตั้ง ×1.015 สำหรับ billboard (ดู skill `import-sprite-art`) — ชั้น BG ที่ตั้งตรงหันหน้ากล้องตรง ๆ
  ให้เช็กกับ CLAUDE.md อีกครั้งว่าต้องยืดหรือไม่ก่อน export

## 3. แสงและบรรยากาศ

| ต้องการ | วิธีใน URP | หมายเหตุ |
|---|---|---|
| Aerial perspective | **Linear fog** สีเดียวกับ L0, start ≈ 25, end ≈ 150–250 | Test_Forest ทำแล้ว; bake ส่วนหนึ่งในภาพด้วยจะคุมสีได้แม่นกว่า |
| Time-of-day | **Volume profile** ต่อเวลา (Color Adjustments, White Balance, Shadows/Midtones/Highlights, Tonemapping) blend ด้วย weight | ภาพฐานต้องวาดแสงกลาง ๆ |
| God rays | quad โปร่งใสแบบ additive วางระหว่าง L2–L3 (D ≈ 45), alpha pulse ช้า | ⚠ ขัดกฎ "alpha clip เท่านั้น" — เป็น VFX ต้องเสนอผ่าน skill `design-change-proposal` ก่อน |
| ฝุ่น/หิ่งห้อย | Particle System ในช่วง D 20–40 | ⚠ เช่นเดียวกัน (alpha blend) |
| คบเพลิง/กองไฟ | Point light + sprite ใช้ `SpriteLitNormal` เพื่อรับแสง + flicker script/animation | ตัวละครมี normal map จึงรับ rim/local light ได้ |
| Bloom, vignette, grain | Volume: Bloom (threshold ~0.9), Vignette 0.2–0.3, Film Grain เบา ๆ | grain แทน texture กระดาษของ ref ได้ระดับหนึ่ง |
| Foreground lip มืด | sprite สีเกือบดำ D ≈ 14, ไม่รับแสง (unlit) | ห้ามบัง UI/จุดยืนตัวละคร |

## 4. กล้อง cutscene / dialogue (แบบคลิป 2)

- **Dolly เข้า (ขยับกล้องเข้า) ดีกว่าเปลี่ยน FOV** — dolly ทำให้ชั้นใกล้ขยายเร็วกว่าชั้นไกล = รู้สึกลึกขึ้น;
  FOV zoom ขยายทุกชั้นเท่ากันเหมือนภาพแบน (แบบที่ ref ทำ)
- Zoom 1.3–1.5× (dolly จาก D 20 → ~14), pan ให้คนพูดอยู่กึ่งกลาง/1/3 จอ, ease in-out 0.6–1.0 วินาที
- Letterbox เป็น UI 2 แถบดำบน–ล่าง slide เข้า ~0.4 วินาที
- ⚠ CLAUDE.md กำหนดว่ากล้องไม่หมุนระหว่างเล่น — dolly/pan ไม่หมุน จึงไม่ขัด แต่ระบบกล้อง cutscene เป็นฟีเจอร์ใหม่ ต้องเสนอก่อนทำ
- เมื่อ dolly เข้า ชั้นที่ใกล้กว่าตำแหน่งกล้องใหม่ (L6 ที่ D 14) ต้องหลบหรือ fade ออก ไม่งั้นกล้องทะลุ

## 5. ลำดับการประกอบฉากใน Unity

1. ตั้งกล้อง/ระนาบเดินตาม preset SideView (ใช้ skill `setup-area`)
2. วาง L0 → L6 ตามตาราง §1, PPU ตามตาราง, alpha clip, Cast Shadows off
3. Play แล้วเดินสุดซ้าย–ขวา + ลอง 21:9 → ห้ามเห็นขอบชั้นใด ๆ
4. เปิด fog + volume ของเวลาที่ต้องการ
5. ใส่ local light, (ถ้าอนุมัติแล้ว) rays/particles
6. Screenshot แล้วไล่ `07-critique-checklist.md`
