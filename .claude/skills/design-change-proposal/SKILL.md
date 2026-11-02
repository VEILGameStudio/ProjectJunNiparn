---
name: design-change-proposal
description: Use when a request would change a locked rule (team-requests.md §2), a CLAUDE.md hard stop, core code, the story, or anything only Oak decides. Writes a proposal for Oak instead of implementing.
---

# Design change proposal

Do **not** implement any part of the change, not even a "temporary" or "test" version.

1. Tell the teammate in Thai, kindly and in one or two sentences, which rule it touches and why that rule
   exists (quote the reason from CLAUDE.md, not your opinion).
2. Offer the closest thing that is allowed today, if there is one.
3. Write the proposal in this shape (Thai), and give it to the teammate to send to Oak, or save it under
   `Docs/proposals/YYYY-MM-DD-<short-name>.md` if the repo has that folder:

```
# ข้อเสนอเปลี่ยนดีไซน์: <ชื่อสั้นๆ>
ผู้ขอ: <ชื่อ>   วันที่: <YYYY-MM-DD>
อยากให้เกมเป็นแบบไหน: <ผลที่ผู้เล่นเห็น>
ติดกฎข้อไหน: <team-requests.md §2 ข้อ… / CLAUDE.md §…>
ทำไมกฎนี้มีอยู่: <เหตุผลจาก CLAUDE.md>
ทางเลือกที่ทำได้ตอนนี้: <…>
ถ้า Oak อนุมัติ ต้องแก้อะไรบ้าง: <ไฟล์/ระบบ/อาร์ตที่กระทบ, ประมาณงาน>
ความเสี่ยง: <เช่น save เก่าพัง, อาร์ตต้องวาดใหม่>
```

4. Stop. The change happens only after Oak updates CLAUDE.md or says so directly.
