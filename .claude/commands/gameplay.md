---
description: Build a gameplay feature through intern, senior review, then tests
---

Task from the intern: $ARGUMENTS

Run this pipeline:
1. Use the unity-intern subagent to implement it.
2. Use the unity-senior subagent to review what was produced.
3. If the verdict is CHANGES REQUIRED, send the findings back to unity-intern and review again.
   Repeat at most twice. If it is still not approved, stop and report to me.
4. If the verdict is ESCALATE TO OAK, stop immediately and show me the FOR OAK section. Do not
   work around it yourself.
5. Once approved, use the unity-tester subagent to write and run tests.
6. Report: what was built, the review verdict, and the test results.