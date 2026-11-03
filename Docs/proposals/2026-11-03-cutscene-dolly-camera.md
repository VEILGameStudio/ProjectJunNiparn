# ข้อเสนอเปลี่ยนดีไซน์: กล้อง cutscene แบบ dolly + letterbox (SideView)

ผู้ขอ: Oak (ให้ Claude ศึกษาจาก reference ฉากป่า)   วันที่: 2026-11-03

## อยากให้เกมเป็นแบบไหน

ระหว่างบทสนทนา Full/Choice ในพื้นที่ `SideView` กล้องจะ **ขยับเข้าหาคนที่กำลังพูด** แบบนุ่ม ๆ แล้วถอยกลับเมื่อจบบทพูด
มีแถบดำบน–ล่าง (letterbox) เลื่อนเข้ามาตอนเริ่มบทสนทนา และตอนมีตัวเลือก กล้องจะเลื่อนเอียงไปทางตัวละครของตัวเลือกที่เมาส์ชี้อยู่
ตัวเลขทั้งหมดวัดจากคลิป reference จริง (ดู `.claude/skills/sideview-forest-background/references/08-camera-measurements.md`)

เกมต้นฉบับ zoom เป็นภาพแบน (ทุกชั้นขยายเท่ากัน) ของเราใช้กล้อง perspective อยู่แล้ว ถ้าใช้การ **dolly (ขยับกล้องเข้า)** แทนการ zoom
ชั้นหน้าจะขยายเร็วกว่าชั้นหลัง → ฉากลึกกว่าต้นฉบับ โดยไม่ต้องวาดอะไรเพิ่ม

## ติดกฎข้อไหน

- `team-requests.md` §2 ข้อ 3: ห้ามปรับกล้องในฉากด้วยมือ / ค่าในตาราง preset ถูกล็อก (CLAUDE.md §7 Camera)
- `team-requests.md` §2 ข้อ 15 / CLAUDE.md §12: ระบบใหม่ที่ไม่อยู่ใน spec
- CLAUDE.md §8 hard stops: "changing a camera preset's values", "adjusting a camera in a scene by hand", "locking or unlocking player input"
- โฟลเดอร์ `Scripts/Camera/`, `Scripts/Cutscene/`, `Scripts/Dialogue/` เป็นของ Oak

## ทำไมกฎนี้มีอยู่

ค่ากล้องถูกล็อกเพราะ sprite ทุกตัวถูกวาด/ยืดมาสำหรับมุมกล้องนั้น (pitch/yaw) และ FOV 19 คือขนาดตัวละครที่อาร์ตวาดเทียบไว้
(ตัวละครสูง 23% ของจอ) ถ้ามีคนแก้ค่ากล้องในฉากเอง ภาพจะผิดแบบที่ไม่มีใครรู้ว่าเป็นบั๊ก

**ข้อเสนอนี้ไม่แตะ pitch / yaw และไม่แตะค่าใน preset** — เปลี่ยนแค่ระยะกล้อง (distance) ชั่วคราวระหว่าง cutscene
ซึ่งเป็นค่าที่ CLAUDE.md อนุญาตให้ override ต่อ level อยู่แล้ว แล้วคืนค่าเดิมทุกครั้งที่จบ ไม่มีการหมุนกล้อง
มุมกล้องไม่เปลี่ยน sprite จึงไม่เพี้ยน (dolly ตรงเข้าหาตัวละครไม่เปลี่ยนมุมมอง)

## ทางเลือกที่ทำได้ตอนนี้ (ไม่ต้องอนุมัติ)

- Timeline (`PlayableDirector`) ใน §7 Cutscenes ใช้ได้อยู่แล้ว: ทำ cutscene ทีละอันด้วยกล้อง Cinemachine ตัวที่สองใน Timeline
  ข้อเสีย: ต้องทำมือทุกบทสนทนา และบทสนทนาธรรมดา (Full/Choice) ยังไม่มีกล้องเคลื่อน
- Letterbox เป็น UI ล้วน ทำได้โดยไม่แตะกล้อง (ยังต้องให้ Oak ตัดสินว่าจะผูกกับ `OnDialogueStarted` หรือไม่ เพราะ `Scripts/UI/` เป็นของ Oak)

## ถ้า Oak อนุมัติ ต้องแก้อะไรบ้าง

| สิ่งที่ต้องทำ | ที่ไหน | ขนาดงาน |
|---|---|---|
| `DialogueCameraFocus` (MonoBehaviour, ไม่ใช่ singleton): ฟัง `OnDialogueStarted/Ended` → ขยับ `CinemachinePositionComposer.CameraDistance` + เลื่อน target ไปที่คนพูด ด้วย DOTween | `Scripts/Camera/` (Oak) | เล็ก–กลาง |
| ข้อมูลต่อบทพูด: `focusTarget` (Transform), `focusZoom` (เช่น 1.25 = distance 16), `moveDuration`, `ease` | Dialogue asset (Oak) | เล็ก |
| ค่า default (จาก reference): zoom พัก ×1.25, คนพูดเปลี่ยน 0.5 s ease OutCubic, ดันเข้าอารมณ์ 1.6 s InOutSine, ถอยกลับ 1.0 s OutCubic, choice-hover เลื่อน 1.2 unit 0.8 s | ScriptableObject settings | เล็ก |
| Letterbox: 2 แถบ UI 10% ความสูง slide 0.4 s ใช้เวลาเกมปกติ (ไม่ใส่ `SetUpdate(true)`) เพราะบทสนทนาไม่ได้หยุดเวลาเกม | `Scripts/UI/` (Oak) | เล็ก |
| เมื่อ dolly เข้า ชั้น Foreground (D≈15) จะเข้ามาใกล้กล้อง → fade Foreground ออกเมื่อ distance < 17 | ฉาก / material | เล็ก |
| ปิดระบบนี้ใน `ThreeQuarter` (orthographic ไม่มี dolly, ใช้ OrthographicSize แทนได้ถ้าต้องการภายหลัง) | ตรวจ preset ใน `LevelSettings` | เล็ก |
| EditMode test: คืนค่า distance เดิมหลังจบบทสนทนา, ไม่แตะ pitch/yaw, ค่า preset ไม่เปลี่ยน | `Tests/Editor/` | เล็ก |

## ความเสี่ยง

- **Confiner**: กล้องที่เข้าใกล้มีมุมมองแคบลง ขอบเขต confiner ที่คำนวณจาก play area ต้องคำนวณใหม่ระหว่าง dolly ไม่งั้นกล้องจะถูกหนีบ (CLAUDE.md §11 เตือนว่า confiner ซ่อนกล้องพัง)
- **Foreground ทะลุกล้อง** ถ้าลืม fade
- **ตัวละครดูใหญ่ขึ้นระหว่างพูด** — ตั้งใจ แต่ถ้า dolly > ×1.5 sprite จะถูกขยายเกิน 1:1 ภาพแตก (ตัวละครวาดที่ 249 px สูงบนจอ 1080 → ×1.5 = 374 px ต้องมี source ≥ 374 px)
- ถ้าอนุญาต "distance ชั่วคราว" แล้วมีคนเอาไปใช้นอก dialogue → กลายเป็นการปรับกล้องมือ ต้องเขียนใน CLAUDE.md ว่าใช้ได้ที่ระบบนี้ระบบเดียว
