---
name: add-prop-threequarter
description: Use when a teammate ticks "Props 3/4" on an issue, or brings ONE painted picture of a box-shaped prop (wardrobe, cupboard, bookshelf, crate, table, chest of drawers) to place in a ThreeQuarter room so it looks 3D. Bakes the picture onto the faces of a 3D box.
---

# Add a painted 3/4 prop (picture baked onto a 3D box)

Follow these steps **in order**. Do not skip one, do not change the order, do not invent a different
method. Every command is run from the repo root unless it says otherwise. The tool does the maths;
your job is to read numbers off pictures, run commands, and look at the results.

Background (read once): a wide solid prop cannot be one upright sprite card. Through the middle of its
base the card sinks into the floor at the corners; moved forward it covers the player standing in front.
So the picture is cut into three textures and laid on the Front, Side and Top faces of a real box.
Every pixel then has its real depth. Example already in the game: `Wardrobe_Test`
(`ArtSource/Props/Wardrobe_Test/`, `Assets/_Project/Prefabs/Props/Wardrobe_Test.prefab`).
Full Thai guide: `ArtSource/Props/README.md`.

This skill is type **D (new art)** in `.claude/rules/team-requests.md`. Still post the request-triage
summary first, and still run `unity-reviewer` at the end (step 11).

---

## Step 0 — Stop checks. If any one fails, stop and reply in Thai. Do not continue.

| Check | If it fails, reply |
|---|---|
| A picture is attached to the issue or a comment | "ยังไม่มีรูปแนบ กรุณาแนบรูปแล้วพิมพ์ @claude ใหม่" |
| The area is **ThreeQuarter** (a puzzle room). Not SideView | "เทคนิคนี้ใช้ได้แค่ห้อง ThreeQuarter ของใน SideView ต้องวาดใหม่สำหรับมุม SideView" |
| The object is basically **a box** (flat sides, square corners). Not round, not a character, not a plant | Round things (barrel, jar, pot): "ของทรงกลมต้องทำเป็นโมเดล 3D (MagicaVoxel) ตาม CLAUDE.md §7". Plants/characters: use `import-sprite-art` instead. |
| The picture shows **two side faces and the top**, with one vertical corner edge pointing at the viewer | "รูปต้องเห็นหน้า ข้าง และด้านบน (มุมตู้หันเข้าหาคนดู) รูปที่เห็นแค่หน้าเดียวใช้เทคนิคนี้ไม่ได้" |
| The background is transparent (PNG with alpha). Check: `python3 -c "from PIL import Image; im=Image.open('PATH').convert('RGBA'); print(im.size, im.getpixel((0,0)))"` and the corner alpha is 0 | "รูปต้องเป็น PNG พื้นหลังใส กรุณาลบพื้นหลังแล้วแนบใหม่" |

Never change the camera, never tilt anything, never write sorting code to make a prop "fit".

## Step 1 — Pick names

- `NAME` = English PascalCase, `<Thing>_<Place>`, e.g. `Bookshelf_Library`, `Crate_Cellar`. No spaces,
  no Thai. Check it is unused: `ls Assets/_Project/Prefabs/Props/` must not contain `NAME.prefab`.
- Folder = `ArtSource/Props/NAME/`.

## Step 2 — Put the picture in the folder

1. `mkdir -p ArtSource/Props/NAME`
2. Save the attached picture as `ArtSource/Props/NAME/source.png`. The GitHub action usually downloads
   issue images to a local folder and shows you the local path; copy from there. If you only have a URL,
   try `curl -sL -H "Authorization: token $GITHUB_TOKEN" -o ArtSource/Props/NAME/source.png "<url>"`.
   If you cannot get the file, stop and say so in Thai.
3. Convert to RGBA PNG if needed:
   `python3 -c "from PIL import Image; Image.open('ArtSource/Props/NAME/source.png').convert('RGBA').save('ArtSource/Props/NAME/source.png')"`
4. Copy the example config: `cp ArtSource/Props/tools/templates/prop.example.json ArtSource/Props/NAME/prop.json`
5. In `prop.json` set `"output_name": "NAME"`, and set `"upright_parts": []` for now.

## Step 3 — Set the height

`"height"` = how tall the box part is in the game, in Unity units (1 unit = 1 metre). Use the height
from the issue ("ของชิ้นนี้สูงเท่าไหร่ในเกม"). If the issue gives none, use this table and say in the
hand-back that you guessed:

| Object | height |
|---|---|
| wardrobe, tall cupboard | 2.1 |
| bookshelf | 2.0 |
| chest of drawers, counter | 1.0 |
| table, desk | 0.8 |
| crate, box, chest | 0.6 |
| bedside table, stool | 0.5 |

The height is the **box body only**: from the floor to the flat top. A statue or lamp standing on top
is not included.

## Step 4 — Read the four corner pixels

1. Run `python3 ArtSource/Props/tools/prop_faces.py grid ArtSource/Props/NAME`
2. Open `ArtSource/Props/NAME/grid.png` with the Read tool and look at it. The red numbers are source
   pixels (x along the top edge, y down the left edge). Thick lines every 50 px, thin every 10 px.
3. Find these four points on the **main box body** (ignore feet, knobs, a cornice that sticks out, and
   anything standing on top):

| Key | Where |
|---|---|
| `near_bottom` | the bottom of the vertical corner edge **closest to the viewer** (the lowest corner of the body) |
| `near_top` | the top of that same vertical edge, where it meets the top face. Same x as `near_bottom` (± a few px) |
| `left_bottom` | the bottom-left corner of the body: follow the bottom edge from `near_bottom` to the left end |
| `right_bottom` | the bottom-right corner of the body: follow the bottom edge from `near_bottom` to the right end |

   If the feet hide the bottom edge, follow the line where the body meets the feet.
   Sanity rules (the tool checks them too): `left_bottom` x < `near_bottom` x < `right_bottom` x;
   both side corners are **higher** on the picture (smaller y) than `near_bottom`; `near_top` y is much
   smaller than `near_bottom` y.
4. Write the four `[x, y]` pairs into `corners_px` in `prop.json`.

## Step 5 — Fit the camera and size

1. Run `python3 ArtSource/Props/tools/prop_faces.py fit ArtSource/Props/NAME`
   It prints `pitch`, `yaw`, `width_z`, `depth_x` and saves them into `prop.json`.
2. Expected ranges. Outside them, your corners are probably wrong: go back to step 4 once.
   - pitch 10 to 45, yaw 25 to 65.
   - `width_z` and `depth_x` are believable for the object (a wardrobe 1.0 × 0.45, a crate 0.6 × 0.6).
3. If it prints `warning: near_top is not straight above near_bottom`, re-read `near_top`.

## Step 6 — Check the fit by eye

1. Run `python3 ArtSource/Props/tools/prop_faces.py check ArtSource/Props/NAME`
2. Open `ArtSource/Props/NAME/check_fit.png` with the Read tool.
3. **Pass** when the blue box's edges lie along the body's edges: the near vertical edge on the near
   corner, the bottom edges along the bottom of the body, the top edges along the top. A cornice or
   feet sticking out a little past the blue lines is fine.
4. **Fail**: correct the corner pixels (step 4) and run steps 5–6 again. **At most 3 tries.** If it
   still does not fit, stop and tell the teammate in Thai that the drawing does not seem to be a straight
   box view, and attach `check_fit.png`. Do not tweak `source_pitch` / `source_yaw` by hand.

## Step 7 — Mark things that stand up from the top (only if there are any)

A statue, lamp, vase or plant standing **on top** of the box must not be painted onto the top face.
For each one add an entry to `upright_parts` (read the pixels from `grid.png`):

```json
{"name": "Statue", "box": [x0, y0, x1, y1], "base": [bx, by]}
```

- `name`: English PascalCase, one word.
- `box`: a rectangle around the thing in source pixels (left, top, right, bottom). Include its whole
  shape; it may overlap the box body a little at the bottom.
- `base`: the point where it touches the top of the box (the middle of its foot).

If nothing stands on top, leave `"upright_parts": []`. Things on the **front or side** (handles, a
lock, a painted sign) are not upright parts; they stay painted on the face.

## Step 8 — Bake and check the preview

1. Run `python3 ArtSource/Props/tools/prop_faces.py bake ArtSource/Props/NAME`
2. Open `ArtSource/Props/NAME/out/preview_game_camera.png` with the Read tool. This is the prop from
   the real game camera (pitch 30, yaw 45). Compare it with `source.png`.
3. **Pass** when: the whole object is there, no face is missing or blank, the top face is filled, and
   any upright part stands on the top. It will look a little more "from above" than the drawing; that
   is correct, the game camera is higher.
4. **Fail** examples and fixes:
   - a part of the drawing is cut off at an edge → raise `margin` from 0.15 to 0.25 and bake again
   - an upright part is missing or cut in half → its `box` is too small; enlarge it and bake again
   - a face is mostly empty → the fit was wrong; go back to step 6

## Step 9 — Write the Unity files

1. Run `python3 ArtSource/Props/tools/prop_faces.py unity ArtSource/Props/NAME`
2. It writes, and prints each path:
   - `Assets/_Project/Art/Sprites/ThreeQuarter/Props/NAME_Front.png`, `_Side.png`, `_Top.png` (+ `.meta`)
   - one sprite per upright part, `NAME_<part>.png` (+ `.meta`)
   - `Assets/_Project/Art/Materials/NAME_Front.mat`, `_Side.mat`, `_Top.mat` (+ `.meta`)
   - `Assets/_Project/Prefabs/Props/NAME.prefab` (+ `.meta`)
3. **Never edit these files by hand.** To change something, change `prop.json` and run steps 8–9 again;
   running `unity` again keeps every GUID, so nothing in a scene loses its link.
4. Delete the helper picture before committing: `rm ArtSource/Props/NAME/grid.png`.
   Keep `source.png`, `prop.json`, `check_fit.png` and `out/`.

## Step 10 — Do NOT place it in a scene

Scenes belong to their owners (CLAUDE.md §5) and break easily. Do not edit any `.unity` file. The
teammate drags the prefab in; give them the steps in the hand-back (step 12).

## Step 11 — Review

1. `git status` must show only files under `ArtSource/Props/NAME/` and the `NAME_*` files from step 9.
   Nothing else. If anything else changed, undo it.
2. Run the `unity-reviewer` agent on the change, as `.claude/rules/team-requests.md` step 7b says. Fix
   its Blockers and rerun until it says ผ่าน.
3. Commit and open a pull request into `dev`.

## Step 12 — Hand back, in simple Thai

Use this shape. Fill every line.

```
เพิ่ม <ชื่อของ> แล้ว (Props 3/4)

ไฟล์:
- Assets/_Project/Prefabs/Props/NAME.prefab  <- ตัวที่ลากใส่ฉาก
- Assets/_Project/Art/Sprites/ThreeQuarter/Props/NAME_*.png
- Assets/_Project/Art/Materials/NAME_*.mat
- ArtSource/Props/NAME/  (รูปต้นฉบับ + prop.json ไว้แก้แล้วรันใหม่)

ขนาดในเกม: กว้าง <width_z> × ลึก <depth_x> × สูง <height> หน่วย  (<บอกถ้าเดาความสูงเอง>)

วิธีใส่ใน Unity:
1. เปิดฉากห้อง ThreeQuarter ของตัวเอง
2. ลาก NAME.prefab จาก Project ไปวางในฉาก
3. ตั้ง Position Y = ความสูงพื้น (ปกติ 0) ให้ฐานแตะพื้นพอดี
4. หมุนได้แค่ Y = 0 เท่านั้น (ถ้าหมุน ด้านที่วาดจะหันออกจากกล้อง)
5. อยากให้คลิกได้ ให้ใส่ BoxCollider แบบ Is Trigger บนตัวหลัก layer Interactable (CLAUDE.md §6)

เช็กใน Editor (เทสอัตโนมัติเช็กไม่ได้):
- [ ] มองจากกล้องเกมแล้วหน้าตาเหมือนรูปที่วาด
- [ ] ไม่จมพื้นตรงมุม
- [ ] เดินไปหน้าตู้ ตัวละครบังตู้ เดินไปหลังตู้ ตู้บังตัวละคร
- [ ] เดินชนแล้วไม่ทะลุ
- [ ] มีเงาตกบนพื้น

เทส: ไม่มีโค้ดใหม่ จึงไม่มีเทสอัตโนมัติ ตรวจด้วยภาพ preview_game_camera.png แล้ว
ผลตรวจ: <unity-reviewer result>
```

Attach (link) `ArtSource/Props/NAME/out/preview_game_camera.png` and `check_fit.png` in the reply so
the teammate can see the result before opening Unity.

---

## What this skill never does

- Never uses one upright sprite card for a box-shaped prop.
- Never changes the camera, adds a tilt, or writes code that sorts sprites.
- Never edits a scene, a `.meta` GUID, or the generated `.prefab` / `.mat` by hand.
- Never uses this for SideView areas, round objects, characters or plants.
- Never guesses past a failed check: after 3 failed fits, stop and ask.
