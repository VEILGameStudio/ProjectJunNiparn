---
name: unity-code-standards
description: Use when writing or reviewing any Unity C# in this project, when asked "what is the Unity way to do X", when code is slow or allocates every frame, or before using a Unity API from memory that may have changed in Unity 6.
---

# Unity code standards (Unity 6000.5.10f1, URP 17.5)

The Unity checklist for this project. It sits **under** `CLAUDE.md`: where they differ, CLAUDE.md
wins, and the table in §1 already lists the places they used to differ. Use it while writing code,
and use it again as a review list (with `review-intern-pr` and `fix-bug`).

## 1. Project rules that beat general Unity advice

Generic Unity guides (and the source this skill was adapted from) say some things that are **wrong
for this project**. Do not "fix" our code towards them.

| General advice says | This project does | Where |
|---|---|---|
| Put every code folder in an `.asmdef` | **No asmdef for our code** under `Assets/_Project` | CLAUDE.md §4.12 |
| UI Toolkit for new runtime UI; UGUI is legacy | **UGUI + TextMeshPro** (`PersistentCanvas`, `ScreenSwitcher`); UI Toolkit is not used. UGUI is not deprecated | CLAUDE.md §4.10, `add-ui-screen` |
| Addressables, never `Resources.Load` | No Addressables package; do not add one. Content lives in ScriptableObjects referenced in the Inspector | CLAUDE.md §4.8 |
| `async/await` for loading | No `async/await`; use coroutines or events | CLAUDE.md §4.3 |
| `record`, `init`, switch-expression tricks | Plain classes and readable code for interns | CLAUDE.md §4.3–4.4 |
| DOTS / ECS / Burst for speed | Not used. MonoBehaviour + ScriptableObject only | — |
| Interfaces everywhere | Use the existing bases first (subclass `InteractableBase`, never implement `IInteractable` directly; `Singleton<T>`; `GameEvents`) | CLAUDE.md §3, §6 |
| Sort sprites in code / Y-sort | **Never.** The depth buffer sorts | CLAUDE.md §0 rule 7 |

## 2. Writing a MonoBehaviour

- `[SerializeField] private` + `[Header]` + `[Tooltip]` on every field. No public fields for the Inspector.
- Cache components in `Awake()`. Never `GetComponent<>()` inside `Update`, `FixedUpdate` or a loop that
  runs every frame. Use `[RequireComponent]` when the script needs another component.
- **No `Find()`, `FindObjectOfType()`, `FindFirstObjectByType()`, `GameObject.Find`, or `SendMessage()` in
  game code.** Get references from the Inspector, a manager `Instance`, or a `GameEvents` event.
  `FindObjectsByType` is allowed in editor tools and validation checks only (CLAUDE.md uses it for one).
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Every time (CLAUDE.md §3, the worst bug here).
- A coroutine that can outlive its reason is stopped in `OnDisable` (`StopCoroutine` with the stored
  handle). A coroutine started on an object that is then disabled stops silently: start it on an object
  that stays active.
- Unity objects are compared with `== null` / `!= null`. **Never `is null`, `?.` or `??` on a
  `UnityEngine.Object`**: they skip Unity's "destroyed" check and pass on a destroyed object.
- An `Update()` with nothing to do is deleted, or the script disables itself (`enabled = false`) until
  it is needed. Prefer events over checking a value every frame.
- `DontDestroyOnLoad` only through the `Singleton<T>` Inspector flag, on the managers CLAUDE.md lists.
- If two scripts depend on each other at start-up, do not rely on luck: initialise in `Awake`, connect in
  `OnEnable`/`Start`, or wait for `GameEvents.OnSceneReady`. Do not change Script Execution Order without
  asking Oak.

## 3. Nothing allocates every frame

Garbage made every frame causes stutters on low-end PCs. In `Update`, `FixedUpdate`, `LateUpdate`,
physics callbacks and anything they call:
- no `new List<>`, no string building (`+`, `$""`) except inside a log that only runs on an error,
  no LINQ, no `foreach` over something that boxes, no lambdas that capture variables;
- physics queries use the non-allocating forms with a buffer kept in a field:
  `Physics.RaycastNonAlloc`, `Physics.OverlapSphereNonAlloc`, `Physics.SphereCastNonAlloc`;
- anything spawned and destroyed often (sparkles, footstep dust, floating text) uses
  `UnityEngine.Pool.ObjectPool<T>` instead of `Instantiate` / `Destroy`;
- strings built in a loop use `StringBuilder`.
Check with the Profiler's **GC Alloc** column before and after. Never optimise something you have not
measured: say what the Profiler showed.

## 4. Scenes, prefabs and assets (YAML)

- **Never hand-edit `.unity`, `.prefab` or `.asset` YAML while a Unity Editor may be open on the project.**
  The Editor keeps its own copy and its next save overwrites or conflicts with the edit. Make the change
  in the Editor, or give the teammate Inspector steps. If you cannot tell, assume it is open.
- If YAML must be edited with no Editor open: keep every `fileID` and `guid`, never invent a GUID, never
  edit or delete a `.meta` file, and open the scene afterwards to confirm it loads.
- Scenes and prefabs have owners (CLAUDE.md §5). `123.unity` and `Test_3D.unity` are never edited by
  anyone but their owner.

## 5. APIs that changed in Unity 6 (do not trust memory)

Training data mostly knows Unity 2022. Before using an API you are not sure of, check it against this
table, then the Unity 6000.5 docs. If you cannot verify it, **say it is unverified** instead of guessing.

| Old (2022 and earlier) | Unity 6 |
|---|---|
| `Rigidbody.velocity`, `.drag`, `.angularDrag` | `linearVelocity`, `linearDamping`, `angularDamping` |
| `FindObjectOfType<T>()`, `FindObjectsOfType<T>()` | `FindFirstObjectByType` / `FindAnyObjectByType`, `FindObjectsByType` (still banned in game code, §2) |
| `Input.GetKey`, `Input.GetAxis`, `Input.mousePosition` | New Input System through `InputReader` / the `GameControls` asset (CLAUDE.md §4.9) |
| `CinemachineVirtualCamera`, namespace `Cinemachine` | `CinemachineCamera`, namespace `Unity.Cinemachine` (CLAUDE.md "Cinemachine") |
| URP `ScriptableRenderPass.Execute(ScriptableRenderContext, ref RenderingData)` | `RecordRenderGraph(RenderGraph, ContextContainer)`; Compatibility Mode was removed in 6.4 |
| `OnPreRender` / `OnPostRender` on a camera | `RenderPipelineManager.beginCameraRendering` / `endCameraRendering` |
| `Object.GetInstanceID()` as a stable key | 6.4–6.5 move towards `EntityId`; do not store instance IDs in save data at all |
| Built-in render pipeline shaders (`Standard`, surface shaders) | URP only; Built-in RP is deprecated in 6.5 |
| UGUI `Text` | `TextMeshProUGUI` (CLAUDE.md §4.10) |

## 6. Measuring

- **Profiler** (CPU, GC Alloc, Rendering), **Frame Debugger** for draw calls and overdraw, **Memory
  Profiler** for leaks across scene loads.
- Rendering and shader questions → `unity-urp-shader`. UI performance → `add-ui-screen`
  (`references/ugui-performance.md`).

## 7. Review list (paste into a review)

- [ ] no asmdef, no async/await, no Addressables, no UI Toolkit, no legacy `Input`
- [ ] `[SerializeField] private` + Header + Tooltip; header comment on the script
- [ ] no `GetComponent` / `Find*` / `SendMessage` in per-frame or game code
- [ ] events: `OnEnable` subscribe, `OnDisable` unsubscribe; coroutines stopped
- [ ] `== null` on Unity objects, no `?.` / `??` / `is null`
- [ ] no allocation in `Update` / physics callbacks; NonAlloc queries; pooled spawns
- [ ] no hand-edited scene/prefab YAML; no `.meta` touched
- [ ] Unity 6 API names (§5); anything unverified is marked as such
