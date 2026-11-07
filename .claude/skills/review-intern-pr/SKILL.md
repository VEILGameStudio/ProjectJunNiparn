---
name: review-intern-pr
description: Use when Oak asks to review, check or approve a teammate's (intern's) pull request, branch, commit or handback before it is merged into dev - checks it against the locked rules in CLAUDE.md and team-requests.md.
---

# Review a teammate's pull request

For Oak, not for the interns. The interns' own Claude follows `request-triage`; this is the second pair
of eyes that catches what compiles fine but breaks the design. Most of these bugs are invisible in a
quick read and in Play mode, so read the diff line by line.

Spec: CLAUDE.md (all of it), `.claude/rules/team-requests.md` §1–§2.

## 1. Collect
1. The diff against `dev` (`git diff origin/dev...<branch> --stat`, then file by file).
2. The PR body or issue: what was asked, and which skill the teammate's Claude said it used.
3. Who wrote it (Intern A = code, Intern B = data / scenes / art) — it decides which files they may touch.

Never review from the PR description alone. Read the code.

## 2. Ownership (team-requests.md §1, CLAUDE.md §5)
- Any edit to a file in an `[Oak]` folder (`Core/`, `Player/`, `Interaction/`, `Items/`, `Dialogue/`,
  `Save/`, `Settings/`, `Camera/`, `Environment/`, `UI/`, `Cutscene/`) or to `Assets/Inventory/ItemData.cs`
  → **blocker**, unless Oak asked for it. Reading, calling, inheriting and attaching are fine.
- Edits to `CLAUDE.md` or `.claude/` → blocker (only Oak changes these).
- A new folder outside the §5 map (for example a personal folder under `Scripts/`) → ask where it belongs.
- Edits to a scene the author does not own (`123.unity`, `Test_3D.unity`, anyone else's) → blocker.
- A new copy of something that already exists in a core folder (a second inventory, a second fader) →
  blocker: use the existing one.

## 3. Locked rules — grep first, then read (team-requests.md §2)

| Check | How to find it | Rule |
|---|---|---|
| Legacy input | `Input.GetKey`, `Input.GetAxis`, `Input.GetMouseButton`, `Input.mousePosition` | §4.9 — always sent back |
| Unsubscribe | every `+=` on `GameEvents` / a C# event has a `-=` in `OnDisable`, subscribe in `OnEnable` | §3 CRITICAL |
| Singleton | `static .* Instance`, `: Singleton<` outside the five managers | §3 |
| Sorting / depth code | `sortingOrder =`, sorting by `position.y` / `position.z`, scaling by distance, a parallax script | §0.7, §1 |
| Camera | writes to `Camera.main.transform`, `.orthographic =`, lens or rotation values set by hand, a new preset | §7, §11 |
| Time | `Time.timeScale =` outside `PauseManager` | §8 hard stop |
| Save | calls to `SaveManager` from gameplay code | §8 hard stop |
| Events | `GameEvents.Raise...` / `?.Invoke` of a core event from gameplay code; new events in `GameEvents` | §8 |
| Input lock | `DisableGameplay` / `EnableGameplay` from gameplay code | §8 hard stop |
| Typed text | a new `string` field shown to the player (messages, labels) instead of a dialogue asset / `LocalizedString` | §7 Dialogue |
| Item ids | an `ItemData.id` changed in an existing `.asset` | §7 CRITICAL |
| Sprites | sprite material not alpha-clipped, `Cast Shadows` on, a tilt, art in the wrong preset folder | §0, §5, §7 |
| Packages / asmdef | `Packages/manifest.json` changed, a new `.asmdef` under `Assets/_Project` | §4.12, §11 |
| DOTween | tween without `.SetLink(gameObject)`, stored tween not killed in `OnDisable`, tween started in `Update`, UI tween without `.SetUpdate(true)`, gameplay tween with it | §11 |
| Sound in events | an `AudioSource.Play` / `PlaySfx` dragged into a UnityEvent that restore re-fires | §7 Save |

## 3b. Unity code quality
After the locked rules, run the review list in `unity-code-standards` §7 (allocations in `Update`,
`Find*`, `?.` on Unity objects, Unity 6 API names). These usually go under "ควรแก้", not Blocker,
unless they break a locked rule. Shader or material changes: check against `unity-urp-shader` §1.

## 4. Design checks (read, not grep)
- **Climbed the ladder?** A new class where a field or a `UnityEvent` would do (CLAUDE.md §8). Before
  the sixth subclass of anything, the PR must say why fields and an event cannot do it.
- **One-shots**: every new interactable has a Unique Id; restore sets state and never toggles.
- **Consumed items**: an item used up before the last step of a puzzle (§8 CRITICAL).
- **Dialogue type** follows the Mini / Full / Choice table, not taste.
- **`PuzzleManager` shape**: a second puzzle written against the static `Instance` (§12) is a blocker.
- **Code rules §4**: header comment (what / where / Inspector), one summary per method, `[Tooltip]` on
  every serialized field, full-word names, a helpful error for every missing reference.

## 5. Tests and evidence (§9)
- Read `playModeTestRunnerEnabled` and say what you saw.
- A new system without tests, or a bug fix without a regression test → ask for them (or for the test code
  in the report while the setting is 0).
- A handback that claims "tests pass" with no counts, or a result nobody could have run → treat as unverified.

## 6. Merge safety
- `.meta` file missing for a new asset, or a `.meta` GUID changed for an existing one → blocker (this is
  how `ItemData.cs` lost its GUID once).
- Moves and renames inside the branch: only by `AssetDatabase.MoveAsset`; a delete-and-recreate shows as
  a new GUID.
- `.gitattributes` changed (LFS rules) → ask why; a changed LFS rule silently changes how every later image is stored.
- Merge conflicts in `.unity` / `.prefab` files: never resolved by hand-picking YAML lines; the owner
  re-does the change in the Editor.

## 7. Report to Oak (in Thai)
```
สรุป: <merge ได้ | แก้ก่อน merge | ส่งกลับ>
Blocker:
- <file:line> — <rule, CLAUDE.md §> — <what to do instead>
ควรแก้:
- ...
ข้อสังเกต (ไม่บล็อก):
- ...
สิ่งที่ตรวจไม่ได้จาก diff: <needs Unity / Play mode, and the manual checklist for it>
```
Blockers first, each with the exact rule. Do not approve, merge or comment on GitHub unless Oak asks.
If Oak asks for a comment on the PR, write it in simple Thai for the intern, one fix per bullet, with the
reason from CLAUDE.md in one sentence.
