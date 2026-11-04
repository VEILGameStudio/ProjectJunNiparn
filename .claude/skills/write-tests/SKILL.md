---
name: write-tests
description: Use after finishing any system or bug fix, or when a teammate asks to add or run tests.
---

# Write and run tests

Spec: CLAUDE.md §9.

1. **First read `playModeTestRunnerEnabled` in `ProjectSettings/ProjectSettings.asset` and report the
   value you saw.** While it is `0`, do not write test files into the project: put the test code in the
   report and say the setting is still off.
2. EditMode (`Assets/_Project/Tests/Editor/`) for pure logic — most tests belong here.
   PlayMode (`Assets/_Project/Tests/`) only for what needs the engine.
3. No asmdef for game code to make tests compile — ask Oak.
4. Names state the behaviour: `Stamina_CannotRun_UntilRegeneratedTo30`. Arrange / Act / Assert with blank lines.
5. Every test must be able to fail by breaking production code. Do not test Unity itself.
6. Camera-relative movement: always include the yaw-45 case.
7. Every fixed bug gets a regression test.
8. **Run them and report what you observed**: passed / failed counts and what each failure means, in
   Thai. If you cannot run them, say so and give the exact command. Never a guessed "all green".
