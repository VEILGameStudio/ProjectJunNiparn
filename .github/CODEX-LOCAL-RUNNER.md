# ให้ `/codex` รันบนคอมของ Oak (ใช้โควตา ChatGPT แทน API key)

Oak เลือกแบบนี้เมื่อ 2026-11-25 พอมีคนพิมพ์ `/codex` ใน Issue งานจะไปรันบนคอมของ Oak
ผ่าน **GitHub self-hosted runner** โดยใช้ Codex CLI ที่ login ด้วยบัญชี ChatGPT ของ Oak
ส่วนอื่นเหมือนเดิมทุกอย่าง (branch `codex/issue-...` จาก test-dev, ระบบ `/merge`, ลบ branch ตอนปิด issue)

> `/codex-image` ยังใช้ `OPENAI_API_KEY` บนเครื่องของ GitHub เหมือนเดิม เพราะ Codex CLI วาดรูปไม่ได้

ข้อควรรู้
- **คอมต้องเปิดอยู่ และ runner ต้องรันอยู่** ถ้าคอมปิดหรือหลับ งานจะรอในคิวของ GitHub จนกว่าจะเปิด (รอได้สูงสุด 24 ชม. แล้ว GitHub จะยกเลิก)
- งานของน้องรันบนคอมจริงๆ ในโฟลเดอร์ของ runner (ไม่ใช่โฟลเดอร์ Unity ของ Oak) sandbox ของ Codex บน Windows ยังไม่แน่นเท่า Linux
  workflow ยอมให้เฉพาะคนที่มีสิทธิ์ Write ใน repo สั่งได้
- แพ็กเกจ ChatGPT เป็นของคนเดียว การให้คนอื่นสั่งงานผ่านบัญชีตัวเองอาจขัดเงื่อนไขของ OpenAI เป็นการตัดสินใจของ Oak

---

## ติดตั้งครั้งเดียว (บนคอม Oak, Windows)

### 1. โปรแกรมที่ต้องมี
เปิด PowerShell แล้วรัน (มีอยู่แล้วก็ข้ามได้)
```powershell
winget install Git.Git
winget install OpenJS.NodeJS.LTS
winget install GitHub.cli
```
ปิด PowerShell แล้วเปิดใหม่ จากนั้น
```powershell
npm install -g @openai/codex
codex login
```
`codex login` จะเปิดเบราว์เซอร์ เลือก **Sign in with ChatGPT** แล้ว login ด้วยบัญชีที่มี Plus/Pro

### 2. เช็กว่า bash เป็นของ Git
```powershell
where.exe bash
```
บรรทัดแรกต้องเป็น `C:\Program Files\Git\bin\bash.exe`
ถ้าบรรทัดแรกเป็น `C:\Windows\System32\bash.exe` (ของ WSL) ให้เปิด **Edit the system environment variables > Environment Variables**
ใน **System variables > Path** เพิ่ม `C:\Program Files\Git\bin` แล้วกด Move Up ให้อยู่บนสุด

### 3. ติดตั้ง runner
1. เข้า repo บน GitHub > **Settings > Actions > Runners > New self-hosted runner** เลือก **Windows** / **x64**
2. ทำตามคำสั่งที่ GitHub โชว์ใน PowerShell โดยให้โฟลเดอร์อยู่ที่ `C:\actions-runner` (อย่าไว้ในโฟลเดอร์โปรเจกต์ Unity)
3. ตอน `config.cmd` ถาม
   - runner group / name: กด Enter ใช้ค่าเดิมได้
   - **labels: พิมพ์ `codex`** (สำคัญ workflow หา runner จาก label นี้)
   - work folder: กด Enter
   - **run as service? ตอบ `N`** เพื่อให้ runner รันในชื่อ Oak และใช้ ChatGPT login จากข้อ 1 ได้
4. เริ่ม runner: `C:\actions-runner\run.cmd` (ปล่อยหน้าต่างนี้เปิดไว้)
5. ให้เปิดเองตอนเปิดคอม: กด Win+R พิมพ์ `shell:startup` แล้วสร้าง shortcut ไปที่ `C:\actions-runner\run.cmd` ในโฟลเดอร์นั้น
6. ตั้ง Power & sleep ของ Windows ไม่ให้คอมหลับเอง

### 4. เปิดใช้
repo > **Settings > Secrets and variables > Actions > Variables > New repository variable**
- Name: `CODEX_RUNNER`  Value: `self-hosted`

ลองเปิด issue แล้วพิมพ์ `/codex ทดสอบ: บอกว่าในโปรเจกต์มี skill อะไรบ้าง` ดูที่หน้าต่าง `run.cmd` จะเห็นงานเข้ามา

## ปิดหรือกลับไปใช้ API key
ลบ variable `CODEX_RUNNER` (หรือเปลี่ยนเป็นค่าอื่น) งาน `/codex` จะกลับไปรันบนเครื่องของ GitHub และใช้ `OPENAI_API_KEY`

## ถ้ามีปัญหา
| อาการ | แก้ |
|---|---|
| งานค้าง "Waiting for a runner" | คอมปิด, `run.cmd` ไม่ได้เปิด, หรือ runner ไม่มี label `codex` |
| error เกี่ยวกับ `bash` หรือ `/bin/bash` | ทำข้อ 2 ใหม่ แล้วปิด-เปิด `run.cmd` |
| `codex: command not found` | ทำข้อ 1 ใหม่ แล้วปิด-เปิด `run.cmd` |
| Codex บอกว่ายังไม่ได้ login หรือ token หมดอายุ | รัน `codex login` อีกครั้ง |
| Codex บอกว่า sandbox ใช้ไม่ได้บน Windows | อัปเดต Codex CLI (`npm install -g @openai/codex`) ถ้ายังไม่ได้ แคป log ส่งมาให้ Claude ดู |
| `gh: command not found` | `winget install GitHub.cli` แล้วปิด-เปิด `run.cmd` |
