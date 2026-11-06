---
name: request-triage
description: Use first on any request from a teammate (intern) to add, change or fix something in the game, before reading code or editing files. Classifies the request and routes it.
---

# Request triage

Run the fixed process in `.claude/rules/team-requests.md` §3, steps 1–5, and post the result to the
teammate in Thai in this shape before doing anything else:

```
เข้าใจว่าอยากให้: <what should happen in the game, one sentence>
ประเภท: <A–G and its name>
ติดกฎล็อกไหม: <none | which rule in §2 / which CLAUDE.md §8 hard stop>
ทางที่เลือก (ขั้นบันได): <rung 1–5> เพราะ <why the lower rung is not enough>
ไฟล์ที่จะแตะ: <path — owner>
ขั้นต่อไป: <the skill to use next, or "ส่งให้ Oak">
```

Rules:
- If the restatement can be read two ways, ask one question and stop there.
- Type F or G, or any file owned by someone else: do not edit anything. For G use
  `design-change-proposal`; for F write exactly which core field / event / signature is needed and why.
- Type A–E: go straight on to the matching skill:
  - item → `add-item` · dialogue / message text → `add-dialogue`
  - chest, door, switch, anything clicked or touched → `add-interactable`
  - puzzle, mini game, QTE → `add-puzzle`
  - new room, corridor, camera zone → `setup-area` · sprite, prop, model, normal map → `import-sprite-art`
  - cutscene, intro, video, Timeline → `add-cutscene` · menu, panel, popup, HUD, button → `add-ui-screen`
  - music, sound effect, ambience → `add-audio`
  - bug → `fix-bug`
- Design thinking before anything is built (no files change; ends in a card or brief, and a new puzzle
  or new story content goes to Oak before it is built):
  - think up / improve / critique a puzzle, monster or trap → `puzzle-design`
  - plan or review a level's layout, or a level-layout question → `level-design-qa`
  - players get lost, miss clues, can't tell what is clickable → `level-readability`
- One request at a time. If the message holds several, list them and triage each separately.
