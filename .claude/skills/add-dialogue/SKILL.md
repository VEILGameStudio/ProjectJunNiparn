---
name: add-dialogue
description: Use when a teammate wants new or changed dialogue, story text, an object's message ("It's locked", "Empty"), a choice, or Thai/English text shown to the player.
---

# Add or change dialogue

Spec: CLAUDE.md §7 "Dialogue", §4.10, §11 DOTween note.

## 1. Pick the type from the table, not by taste
| Type | Use it for |
|---|---|
| **Mini** | an object's reaction: no speaker, no choice, no consequence ("ว่างเปล่า", "ล็อกอยู่", "ได้กุญแจ") |
| **Full** | a conversation with a character, or story text meant to be read |
| **Choice** | only when the answer changes what happens next |

`blockedMessage` is always Mini. A container's empty / taken message is Mini.
Full: player always on the left, the other character on the right, never swapped.

## 2. Author as JSON, import to an asset
1. Write the JSON in `Assets/_Project/Data/Dialogue/Json/` following an existing file
   (e.g. `intro_grandpa.json`).
2. Import with the Dialogue importer (`Scripts/Dialogue/Editor/DialogueImporter.cs`) into
   `Data/Dialogue/`.
3. Drag the asset into the field. **Never type text into a field.**

## 3. Must hold
- Every new message field is a dialogue asset reference. The three legacy string fields
  (`blockedMessage`, `PickupItem.pickupMessage`, `MessageInteractable.message`) are converted only in
  one planned pass by Oak — not alongside other work.
- Text is TextMeshPro with the Thai font fallback.
- Never `DOText()` for reveal; Thai combining marks break. Use `maxVisibleCharacters` by grapheme cluster.
- Choice outcomes go through UnityEvents.
- Story content itself (what characters say, plot) is Oak's call; if the request changes story, confirm with Oak.

Hand back with CLAUDE.md §10, in Thai.
