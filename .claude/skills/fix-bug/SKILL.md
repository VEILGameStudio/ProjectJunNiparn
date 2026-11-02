---
name: fix-bug
description: Use when a teammate reports something broken - an error, a crash, a wrong behaviour, "it worked before", "it breaks after loading a scene".
---

# Fix a bug

1. **Reproduce first.** Get the exact steps, scene and console error. If you cannot reproduce it, say so
   and ask for what is missing; do not guess-fix.
2. **Check the known traps before anything else** (CLAUDE.md §0):
   | Symptom | Usual cause |
   |---|---|
   | breaks after a scene reload, null on an event | missing unsubscribe in `OnDisable` |
   | one key walks diagonally | input not camera-relative |
   | sprites flicker / swap order | alpha blending |
   | camera stuck in a corner, not following | wrong preset value hidden by the confiner |
   | camera won't change projection | Brain's Lens Mode Override not ticked |
   | key back in the chest after load | one-shot id not saved / restored |
   | locked in a room after load | puzzle restore not re-firing `OnPuzzleCompleted` |
   | second puzzle breaks the first | singleton / static `Instance` on a puzzle |
   | Thai text broken while revealing | `DOText()` |
   | error after scene change from a tween | missing `SetLink(gameObject)` / not killed in `OnDisable` |
   | item vanished after loading a save | item not in `ItemDatabase`, or id changed |
3. **Find the owner** of the file the fix belongs in (CLAUDE.md §5). Core file → do not edit; write the
   fix for Oak. Someone else's gameplay file / scene → tell its owner.
4. Smallest fix that removes the cause, not the symptom. Never "fix" by restoring Y-sorting, tilting a
   sprite, hand-tuning a camera, or adding a package.
5. Regression test that fails without the fix (`write-tests`).
6. Hand back with CLAUDE.md §10, in Thai, including the cause in one plain sentence.
