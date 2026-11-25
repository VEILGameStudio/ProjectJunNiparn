# AGENTS.md — instructions for Codex (OpenAI) in this repository

Codex reads this file on its own, both from the GitHub Action (`/codex`, `/codex-image`) and from ChatGPT's
Codex connector (`@codex`). It is a pointer, not a second spec: **CLAUDE.md is the single source of truth**,
and every rule in it applies to Codex exactly as it applies to Claude. If this file and CLAUDE.md ever
disagree, CLAUDE.md wins.

## Before any work

1. Read `CLAUDE.md` in full.
2. A request from anyone who is not Oak follows `.claude/rules/team-requests.md`: its fixed order of steps,
   its locked rules (section 2) and the hard stops in CLAUDE.md section 8. Reply to teammates in simple Thai;
   code, comments and file names stay in English.
3. Use the matching skill in `.claude/skills/<name>/SKILL.md` for the request type (add-item, add-puzzle,
   fix-bug, import-sprite-art, ...). Skills are plain Markdown: read and follow them.
4. `.claude/agents/*.md` are advisor and reviewer checklists written for Claude subagents. Codex cannot call
   them as agents: read the matching advisor before editing, and apply `unity-reviewer.md` to your own
   change before you finish.
5. Story, characters, places and art direction: `Docs/GDD/` (summary of the ANODARD GDD). Where the GDD and
   CLAUDE.md disagree on systems or scale, CLAUDE.md wins (Oak's ruling, 2026-11-05).

## Hard limits

- Never edit `CLAUDE.md`, anything under `.claude/`, or `.github/` unless Oak asked for that change.
- Branches: work from `test-dev`. Never push to or open a pull request against `main` or `dev`.
- Unity cannot run here. Never report a test result you did not observe; give the command or the Editor steps.
- A design change, a locked rule or Oak-only core code is written up as a proposal
  (`.claude/skills/design-change-proposal`) and handed to @oak-kittanai, not implemented.

## Code Review Rules

Used by `@codex review` on pull requests. Review against CLAUDE.md and `.claude/rules/team-requests.md`, the
same way `.claude/agents/unity-reviewer.md` does. Flag as P0/P1 in particular:

- An event subscribed anywhere other than `OnEnable`, or not unsubscribed in `OnDisable`.
- Movement input built from world axes instead of the camera's flattened forward/right.
- Legacy input (`Input.GetKey`, `Input.GetAxis`, ...) in our own code.
- Any code that sorts sprites by position or scales them by distance, or a parallax script.
- A singleton that is not one of GameManager, SaveManager, AudioManager, UIManager, GameOverManager.
- A third camera preset, a per-level pitch or yaw, or camera values set by hand instead of from the preset.
- Sprite materials using alpha blending, sprites casting shadows, or a sprite card tilted toward the camera.
- A changed item id, a typed-text message field where a dialogue asset belongs, or an inventory capacity.
- A new package or an asmdef under `Assets/_Project`.
- Gameplay code that edits a core (`[Oak]`) file, raises a core event, writes `Time.timeScale` or calls `SaveManager`.
