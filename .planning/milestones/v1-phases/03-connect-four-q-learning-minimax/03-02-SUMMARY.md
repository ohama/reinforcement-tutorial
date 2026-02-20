---
phase: 03-connect-four-q-learning-minimax
plan: "02"
subsystem: ai
tags: [fsharp, minimax, alpha-beta, negamax, expecto, connect-four]

# Dependency graph
requires:
  - phase: 03-01
    provides: ConnectFour.Domain (Board, Cell, opponent, emptyBoard, idx, rows, cols) and ConnectFour.Rules (legalMoves, applyMove, isGameOver, checkWinner, dropRow)

provides:
  - Minimax.fs: Negamax Alpha-Beta with scoreWindow, allWindows, evaluateBoard, minimaxAB, chooseMoveAB, chooseMoveNaive
  - MinimaxTests.fs: 6 Expecto tests verifying AB equivalence, pruning, winning moves, blocking

affects:
  - 03-03-q-learning (Q-agent will be compared against chooseMoveAB)
  - 03-04-plan (competition: Minimax vs Q-Learning)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Negamax style: negate child score (-minimaxAB nextBoard opp ...) to unify max/min"
    - "Bounded infinity: NegInf = -1_000_000 / PosInf = 1_000_000 to prevent int overflow on negation"
    - "Center-first move ordering: List.sortBy (fun c -> abs(c - 3))"
    - "pruneCount as ref int threaded through recursion for observability"

key-files:
  created:
    - 03-connect-four/src/ConnectFour/Minimax.fs
    - 03-connect-four/tests/ConnectFour.Tests/MinimaxTests.fs
  modified:
    - 03-connect-four/src/ConnectFour/ConnectFour.fsproj
    - 03-connect-four/tests/ConnectFour.Tests/ConnectFour.Tests.fsproj
    - 03-connect-four/tests/ConnectFour.Tests/Main.fs

key-decisions:
  - "NegInf/PosInf = +-1_000_000 (not Int32.MinValue/MaxValue) — negation of MinValue overflows to MinValue in .NET"
  - "Negamax invariant: child score negated at each level — single unified max function"
  - "chooseMoveNaive included in Minimax.fs as reference implementation for equivalence testing"
  - "Blocking test uses 1-sided threat (cols 0,1,2) so only one blocking column (col 3) exists"

patterns-established:
  - "heuristic: net window score (player windows - opponent windows) + center column bonus"
  - "window scoring: 4-in-row=10000, 3=50, 2=3, 1=1; any opponent piece = 0"
  - "terminal scoring: 10000+depth favors faster wins / slower losses"

# Metrics
duration: 2min
completed: 2026-02-20
---

# Phase 3 Plan 02: Minimax Alpha-Beta Summary

**Negamax Alpha-Beta pruning for Connect Four with heuristic evaluation and Expecto equivalence tests confirming identical move choice to naive full-search Minimax**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-20T00:09:01Z
- **Completed:** 2026-02-20T00:11:22Z
- **Tasks:** 2 of 2
- **Files modified:** 5

## Accomplishments

- Implemented Negamax Alpha-Beta in `Minimax.fs` — pure, no I/O, zero overflow risk
- `evaluateBoard` uses net window heuristic (horizontal, vertical, diagonal, anti-diagonal) plus center bonus
- 6 Expecto tests pass: center column preference, AB/naive equivalence, winning move, blocking, legal move, prune count
- Alpha-Beta prunes branches (confirmed by pruneCount > 0 on non-trivial boards at depth 4)

## Task Commits

1. **Task 1: Implement Minimax.fs — Negamax with Alpha-Beta pruning** - `77696f4` (feat)
2. **Task 2: Add MinimaxTests.fs — Expecto Alpha-Beta equivalence tests** - `12177a8` (feat)

## Files Created/Modified

- `03-connect-four/src/ConnectFour/Minimax.fs` - Negamax AB implementation: scoreWindow, allWindows, evaluateBoard, minimaxAB, chooseMoveAB, chooseMoveNaive
- `03-connect-four/src/ConnectFour/ConnectFour.fsproj` - Added Minimax.fs after Rules.fs
- `03-connect-four/tests/ConnectFour.Tests/MinimaxTests.fs` - 6 Expecto test cases
- `03-connect-four/tests/ConnectFour.Tests/ConnectFour.Tests.fsproj` - Added MinimaxTests.fs before Main.fs
- `03-connect-four/tests/ConnectFour.Tests/Main.fs` - Added minimaxTests to test suite

## Decisions Made

- Used `NegInf = -1_000_000` and `PosInf = 1_000_000` instead of `Int32.MinValue/MaxValue` — negating `MinValue` in .NET overflows back to `MinValue`, causing incorrect alpha comparisons
- Kept `chooseMoveNaive` inside `Minimax.fs` (not separate module) — it's test scaffolding, not production AI
- Blocking test was redesigned to use 1-sided threat: Yellow at cols 0,1,2 means only col 3 blocks (no ambiguity). Original double-sided threat (cols 3,4,5) creates unblockable fork — Minimax correctly returns -10000 for all moves

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed blocking test scenario (double-sided threat was unblockable)**

- **Found during:** Task 2 (MinimaxTests.fs verification)
- **Issue:** Test placed Yellow at cols 3,4,5 creating a double-sided threat (can extend to col 2 OR col 6). Minimax correctly scored all Red moves as -10000 (loss in 1) but picked col 3 due to center preference. The test expected col 2 or col 6, failing.
- **Fix:** Changed test to use Yellow at cols 0,1,2 — left edge prevents extension left, col 3 is the only blocking move. Minimax correctly chose col 3.
- **Files modified:** `MinimaxTests.fs`
- **Verification:** `dotnet test` — all 20 tests pass
- **Committed in:** `12177a8` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug in test design)
**Impact on plan:** No scope change. Test corrected to match Minimax's correct behavior on an unblockable double-threat position.

## Issues Encountered

None — the Minimax implementation itself was correct. Only the test scenario was wrong.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `chooseMoveAB` is ready to be used as the Minimax opponent in Plan 03 Q-Learning competition
- `evaluateBoard` can be reused or replaced by neural network in Phase 4 if needed
- No blockers for Plan 03 (Q-Learning self-play training loop)

---
*Phase: 03-connect-four-q-learning-minimax*
*Completed: 2026-02-20*
