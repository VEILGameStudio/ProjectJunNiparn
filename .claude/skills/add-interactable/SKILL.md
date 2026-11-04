---
name: add-interactable
description: Use when a teammate wants something the player clicks or touches - a chest, crate, drawer, door, switch, sign, note, floor plate - or wants an existing one to behave differently.
---

# Add or change an interactable

Spec: CLAUDE.md §6 (interaction architecture) and §8 (`ContainerInteractable` rule).

## 1. Find the existing class first
On disk today (`Assets/_Project/Scripts/Interaction/`): `InteractableBase`, `ContainerInteractable`,
`Chest`, `Door`, `ZoneDoor`, `PickupItem`, `MessageInteractable`, plus `DialogueInteractable` /
`ChoiceInteractable` in `Dialogue/` and `HeldItemCheck` in `Items/`. The spec also names
`PuzzleInteractable` and `QTEInteractable`; if they are still not on disk, say so instead of writing a copy.
Always re-list the folder first — this list goes stale.

## 2. Climb the ladder (do not skip)
| Need | How | Who |
|---|---|---|
| different contents / different words | change fields, drag a dialogue asset | Intern B |
| something extra also happens (sound, door, animation) | wire the UnityEvent (`OnOpened` etc.) | Intern B |
| needs the item *in hand* | `Required Item` + tick **Must Be Held** | Intern B |
| genuinely new logic no fields + event can express | new subclass of `InteractableBase` in `Scripts/Gameplay/Interactables/`, override `OnInteract` only | Intern A |

An empty chest is the same chest with Contents left blank. Never a class per object; always a unique id
per object.

## 3. Colliders (explain to Intern B exactly like this)
"อันเล็กคือที่ตัวละครชน อันใหญ่คือที่เมาส์คลิก"
- Blocking: small, at the base, layer `Blocking`, not trigger.
- Clickable: box around the sprite/mesh, layer `Interactable`, trigger, on the root.
- Touch zone (optional): layer `TouchZone`, trigger, for walk-in spots.
Each collider on its own GameObject.

## 4. Must hold
- Every one-shot has a Unique Id; saved as `<scene>/<id>`; restore sets state, never toggles, plays no sound.
- Sounds go in the component's sound field, never dragged into the UnityEvent.
- `blockedMessage` and container messages are **Mini** dialogue.
- New message fields are dialogue assets, never `string`.
- Traps are `HazardTrap` + `IDamageable`, not interactables; disarmable = add `DisarmInteractable`.
- Click and touch call the same `Interact`. Never duplicate it.

## 5. Stop and report if
It needs a new field on `InteractableBase`, a new `GameEvents` event, input locking, `SaveManager`
calls, or `Time.timeScale` (CLAUDE.md §8 hard stops).

Hand back with CLAUDE.md §10, in Thai.
