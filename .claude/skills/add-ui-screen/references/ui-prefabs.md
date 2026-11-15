# UI prefabs — one source of truth

Every piece of in-game UI is a prefab under `Assets/_Project/Prefabs/UI/`. A change is made on the
prefab, never on a copy in a scene, so one edit reaches every scene.

## Folders

```
Assets/_Project/Prefabs/
  GameSystems.prefab          Managers + UIRoot, dropped once into every game scene      [Oak]
  UI/
    UIRoot.prefab             Canvas (Scale With Screen Size 1920x1080, Match 0.5),        [Oak]
                              EventSystem (InputSystemUIInputModule), UIManager,
                              DialogueManager, CutsceneManager, InventoryUI.
                              Holds every prefab below as a nested prefab.
    HUD/                      always-on screen parts: InventoryHud (bag button + hand)    [Oak]
    Panels/                   things that open and close: dialogue panels, inventory,
                              settings, and new panels (pause, gallery, puzzle screens)   [Oak / Intern B]
    Overlays/                 drawn over everything: SkipButton, VideoScreen, FadeImage   [Oak]
    Widgets/                  small reusable pieces: UI_Button, UI_Button_Secondary,      [Oak]
                              UI_Panel, UI_Popup, SlotPrefab
```

`HUD/`, `Panels/` and `Overlays/` are created by **Tools > JunNiparn > UI > 1. Split UIRoot Into Nested
Prefabs**, and the four base widgets by **2. Create Base Widgets**. Both run once, in Unity.

## Why DontDestroyOnLoad and not an additive UI scene

`UIRoot` sits inside `GameSystems`, and `UIManager` is a `Singleton` with Persist Across Scenes ticked,
so the first copy survives every scene load and later copies destroy themselves. Every scene therefore
shows the same UI, and any scene can still be pressed Play on its own because it carries
`GameSystems`. An additive UI scene would give the same result with a second loading path to keep
working, and a scene played on its own would have no UI. One mechanism, not two.

## Nested prefabs and variants

- **Nested prefab** = the same piece used in several places. A button inside a popup is a `UI_Button`
  instance, so restyling `UI_Button` restyles every button.
- **Prefab variant** = the same piece with a small difference. `UI_Button_Secondary` is `UI_Button` with
  another colour; `UI_Popup` is `UI_Panel` with a smaller window and a close button. A variant keeps
  everything else from its base, so change the base for anything that should change everywhere.
- Making a variant: right-click the base prefab > **Create > Prefab Variant**.

## The no-override rule

A scene never changes a UI prefab. `UIOverrideGuard` warns in the Console when a scene is saved with a
change on anything under `Prefabs/UI/`, and **Tools > JunNiparn > UI > Revert UI Overrides In Open
Scenes** puts them back. If a scene seems to need a different look, it is a variant (or a field on the
prefab), made in `Prefabs/UI/` — not an override.

Overrides *inside* a prefab on a nested prefab are allowed only in a variant's own file (that is what a
variant is). `UIRoot` itself should show no overrides on its nested parts: change the part's prefab.

## Opening and closing panels — `UIPanel` + `UIManager`

`UIManager` (singleton, on `UIRoot`) is the one place that opens and closes panels. A panel is any
prefab with a `UIPanel` on its root, placed inside `UIRoot`. `UIManager` finds them all at start and
closes them, except those with **Start Open** ticked.

| Who | How |
|---|---|
| A button (Intern B) | OnClick → drag the panel → `UIPanel.Open` / `UIPanel.Close` / `UIPanel.Toggle` |
| Code (Intern A) | `UIManager.Instance.Open("Pause")`, `Close("Pause")`, `CloseTop()`, `CloseAll()`, `IsAnyPanelOpen` |

- **Pauses Game** freezes the game through `PauseManager` while the panel is open. Two pausing panels
  open at once keep the game frozen until both close.
- Panels do not close each other. A "Settings" button in a pause popup wires two OnClick entries:
  `Settings.Open` and `Pause.Close`.
- Draw order is the order inside `UIRoot`: lower in the Hierarchy draws on top. Keep `Overlays/` last.
- Animations: a `DOTweenAnimation` on the panel root with Id `open` / `close`, Auto Play off,
  **Ignore TimeScale on**.
- `First Selected` gives a gamepad somewhere to start.

Panels in a menu scene that is not a game scene (main menu, credits) still use `ScreenSwitcher` there.
The existing dialogue and inventory panels keep their own scripts for now (`DialogueManager`,
`InventoryUI`); moving them onto `UIPanel` is a separate, later change.

## Adding a new panel (for example a pause popup)

1. Project window → `Prefabs/UI/Widgets/UI_Popup` (or `UI_Panel` for a full screen) → right-click →
   **Create > Prefab Variant**. Move it into `Prefabs/UI/Panels/` and name it, e.g. `PausePanel`.
2. Open it. On `UIPanel`: Panel Id `Pause`, tick **Pauses Game**, drag a button into First Selected.
3. Add buttons by dragging `UI_Button` (or `UI_Button_Secondary`) from `Widgets/` into the Window —
   never by GameObject > UI > Button, which makes a copy that is not linked to the style.
4. Keep the Window inside **935 canvas units high** and anchored to the centre or an edge; check the
   Game view at 16:9 and 21:9 (2560×1080).
5. Open `UIRoot` (Oak's prefab: ask first) and drag the new prefab in, above `Overlays`.
6. Wire whatever opens it: a button with `UIPanel.Open`, or `UIManager.Instance.Open("Pause")` in code.
7. Play, open it, and check the Console: `UIManager` names any empty or repeated Panel Id.
