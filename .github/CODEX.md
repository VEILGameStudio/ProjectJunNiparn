# สั่งงาน Codex (OpenAI / ChatGPT)

มี Codex ให้ใช้ 3 แบบ ใช้คนละคำสั่ง จะได้ไม่ทำงานซ้อนกัน ส่วน `@claude` ยังทำงานเหมือนเดิมทุกอย่าง

## ใช้แบบไหนดี

| อยากได้อะไร | ใช้แบบไหน | พิมพ์อะไร | ใช้เงินจากไหน |
|---|---|---|---|
| ให้แก้หรือเพิ่มของในโปรเจกต์ (โค้ด ข้อมูล ไฟล์) ผ่าน Issue แล้ว `/merge` เข้า test-dev แบบเดียวกับ Claude | **แบบ 2: GitHub Action** (หลัก) | `/codex` ใน Issue | OpenAI API key (จ่ายตามใช้) |
| รูปภาพ ให้เข้ากับ GDD และงานภาพในเกม โพสต์ลงใน Issue เลย ไม่สร้าง branch งาน | **แบบ 3: สร้างรูป** | `/codex-image` ใน Issue | OpenAI API key (คิดเป็นรูป) |
| ให้ Codex ตรวจ PR หรือสั่งงานจากหน้าเว็บ ChatGPT | **แบบ 1: ChatGPT Codex** (เสริม) | `@codex review` ใน PR | โควตา ChatGPT Plus/Pro ของคนที่ผูกบัญชี |

สรุปสั้นๆ: งานประจำผ่าน Issue ใช้ `/codex` หรือ `/codex-image`, ใช้ `@codex` เมื่ออยากใช้โควตา ChatGPT แทน API key
หรืออยากให้ Codex ตรวจ PR

ทำไม `/codex` ไม่ใช่ `@codex`: ถ้าต่อแบบ 1 ไว้ด้วย `@codex` จะไปปลุก Codex ของ ChatGPT ด้วย ถ้าใช้คำเดียวกันงานจะถูกทำสองรอบ

---

## แบบ 2: `/codex` ผ่าน GitHub Action (ไฟล์ `.github/workflows/codex.yml`)

**ตั้งค่าครั้งเดียว (Oak ทำ)**
1. สร้าง API key ที่ https://platform.openai.com/api-keys และเติมเครดิตใน Billing ของ platform (คนละส่วนกับ ChatGPT Plus)
2. เข้า repo บน GitHub > **Settings > Secrets and variables > Actions > New repository secret**
   - Name: `OPENAI_API_KEY`  Value: key ที่ได้ (ขึ้นต้น `sk-`)
3. (ไม่บังคับ) แท็บ **Variables** ของหน้าเดียวกัน ตั้ง `CODEX_MODEL` ถ้าอยากเลือกโมเดลเอง ว่างไว้ = ค่าเริ่มต้นของ Codex
4. `KITTANAI_TOKEN` ที่ใช้กับ Claude อยู่แล้ว ใช้ push ให้ Codex ด้วย ไม่ต้องทำเพิ่ม

**วิธีใช้**: เปิด Issue ด้วยฟอร์ม **"สั่งงาน Codex"** หรือพิมพ์ `/codex ...` ในคอมเมนต์ของ issue ไหนก็ได้
คนสั่งต้องมีสิทธิ์ Write ใน repo (codex-action เช็กเอง)

**สิ่งที่เกิดขึ้น**
1. Codex ทำงานต่อจาก `test-dev` ใน branch `codex/issue-<เลข>-<เวลา>` อ่าน `AGENTS.md` > `CLAUDE.md` > กฎทีม
2. Codex ไม่มี token และไม่มีอินเทอร์เน็ต workflow เป็นคน commit (ในชื่อ kittanai) push และโพสต์คำตอบของ Codex
3. ถามว่า `/merge` หรือ `/nomerge` ใช้ระบบเดียวกับ Claude (job `merge` ใน `claude.yml` ดูทั้ง `claude/issue-*` และ `codex/issue-*`)
4. `/codex` ครั้งถัดไปใน issue เดิม ทำต่อใน branch เดิม ปิด issue แล้ว branch ถูกลบให้

ต่างจาก Claude: ไม่มี Fable ตรวจคำขอก่อน, เรียก agent ใน `.claude/agents/` ไม่ได้ (อ่านเป็น checklist แทน), เปิดรูปที่แนบใน issue ไม่ได้

## แบบ 3: `/codex-image` สร้างรูปใน Issue (ไฟล์ `.github/workflows/codex-image.yml`)

ใช้ `OPENAI_API_KEY` ตัวเดียวกับแบบ 2 ไม่ต้องตั้งอะไรเพิ่ม

1. Codex (อ่านอย่างเดียว) อ่าน issue, สรุป GDD ใน `Docs/GDD/`, กฎงานภาพใน `CLAUDE.md` และรูปใน `ArtSource/`, `Assets/_Project/Art/` บน `test-dev`
2. เขียน prompt และเลือกรูปอ้างอิงจากโปรเจกต์ได้ไม่เกิน 4 รูป แล้วส่งให้ OpenAI Images API วาด
3. โพสต์รูปพร้อมคำอธิบายภาษาไทย รายชื่อรูปอ้างอิง และ prompt ที่ใช้ ลงใน issue
4. ไม่สร้าง branch งาน รูปถูกเก็บใน branch `codex-images` อันเดียว (มีแต่รูป ไม่ merge ไปไหน) เพื่อให้แสดงในคอมเมนต์ได้
   และแนบเป็นไฟล์ดาวน์โหลดในหน้า Actions ด้วย

ตัวแปรเสริม (Variables): `IMAGE_MODEL` (ค่าเริ่มต้น `gpt-image-1`), `IMAGE_QUALITY` (`auto` / `low` / `medium` / `high`)

รูปที่ได้เป็นภาพคอนเซ็ปต์ ถ้าจะใช้เป็น sprite ในเกม ยังต้องผ่าน `import-sprite-art` (ยืดภาพตามมุมกล้อง, PPU, โฟลเดอร์ preset)

## แบบ 1: ChatGPT Codex ผ่านบัญชี ChatGPT (ไม่มีไฟล์ใน repo นอกจาก `AGENTS.md`)

**ตั้งค่าครั้งเดียว (คนที่มี ChatGPT Plus/Pro ทำในบัญชีตัวเอง)**
1. เข้า https://chatgpt.com/codex แล้วกดเชื่อม GitHub
2. ติดตั้ง GitHub App **ChatGPT Codex Connector** ให้ org `VEILGameStudio` เลือกเฉพาะ repo `ProjectJunNiparn`
   (ต้องเป็น owner ของ org หรือขอให้ owner อนุมัติ)
3. ใน Codex settings สร้าง environment ให้ repo นี้ และถ้าอยากให้ตรวจ PR อัตโนมัติ เปิด **Code review**

**วิธีใช้**
- ใน PR: คอมเมนต์ `@codex review` (หรือ `@codex review เน้นเรื่อง ...`) Codex ตรวจตามหัวข้อ "Code Review Rules" ใน `AGENTS.md`
- สั่งงานแก้โค้ด: ที่หน้า chatgpt.com/codex เลือก repo นี้และ **branch `test-dev`** พิมพ์งาน แล้วกด Create PR
  ก่อน merge ให้เช็กว่า PR ชี้ไปที่ `test-dev` ไม่ใช่ `main` หรือ `dev`
- `@codex` ใน Issue: เอกสารของ OpenAI ที่ตรวจตอนตั้งระบบ (2026-11-25) พูดถึงแค่ใน PR ยังไม่ได้ยืนยันว่าใน Issue ใช้ได้
  งานผ่าน Issue ให้ใช้แบบ 2 แทน

แบบ 1 ไม่ผ่านระบบ `/merge` ของ repo นี้ และใช้โควตาของบัญชี ChatGPT ที่ผูกไว้
