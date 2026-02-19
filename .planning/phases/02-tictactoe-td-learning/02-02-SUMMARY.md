---
phase: 02-tictactoe-td-learning
plan: "02"
subsystem: reinforcement-learning
tags: [fsharp, td-learning, epsilon-greedy, self-play, expecto, value-table]

# Dependency graph
requires:
  - phase: 02-01
    provides: Domain.fs (Cell, Board, GameState, ValueTable), Rules.fs (isGameOver, legalMoves, applyMove), FsCheck property tests infrastructure

provides:
  - TD(0) learning agent with epsilon-greedy action selection (Agent.fs)
  - Self-play training loop with prevBoard TD backup (Training.fs)
  - Expecto convergence test: >90% win rate vs random after 100k episodes (ConvergenceTests.fs)
  - trainAgent returning ValueTable + win-rate history
  - winRateVsRandom with epsilon=0 greedy evaluation

affects: [02-03, tutorial-docs]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TD(0) backup: V(s) += alpha * (V(s') - V(s)) applied to previous board after each move"
    - "X maximizes V, O minimizes V (opponent is adversarial, same ValueTable)"
    - "Terminal state values hardcoded: XWins=1.0, OWins=0.0, Draw=0.5"
    - "Default V for unseen states = 0.5 (Option.defaultValue 0.5)"
    - "Evaluation uses epsilon=0 (pure greedy); training uses epsilon=0.1"

key-files:
  created:
    - TicTacToe/src/TicTacToe/Agent.fs
    - TicTacToe/src/TicTacToe/Training.fs
    - TicTacToe/tests/TicTacToe.Tests/ConvergenceTests.fs
  modified:
    - TicTacToe/src/TicTacToe/TicTacToe.fsproj
    - TicTacToe/tests/TicTacToe.Tests/TicTacToe.Tests.fsproj
    - TicTacToe/tests/TicTacToe.Tests/Main.fs

key-decisions:
  - "prevBoard tracks the current player's previous board for TD backup (not the opponent's)"
  - "Both X and O use the same ValueTable (self-play): X maximizes, O minimizes"
  - "winRateVsRandom uses epsilon=0 internally — no caller configuration needed"
  - "Main.fs uses fully qualified TicTacToe.Tests.ConvergenceTests.convergenceTests (F# module scoping)"

patterns-established:
  - "Self-play: single ValueTable shared between both agents"
  - "Tail-recursive loop for episode playback (no stack overflow risk)"
  - "Convergence test: fixed seed (System.Random(42)) for determinism + 1000-game evaluation"

# Metrics
duration: 2min
completed: 2026-02-19
---

# Phase 2 Plan 02: TD Learning Agent + Convergence Test Summary

**epsilon-greedy TD(0) agent with shared-ValueTable self-play achieves >90% win rate vs random after 100k episodes, verified by Expecto convergence test in ~4 seconds**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-19T05:56:14Z
- **Completed:** 2026-02-19T05:57:56Z
- **Tasks:** 2/2
- **Files modified:** 6

## Accomplishments

- Implemented TD(0) update rule (V(s) += alpha * (V(s') - V(s))) in Agent.fs
- Self-play training loop (playEpisode) with prevBoard tracking for correct TD backup
- 100k episode trainAgent converges to >90% win rate vs random opponent
- Expecto convergence test passes in ~4 seconds (3 testCase assertions, all pass)
- Total test count: 8 (FsCheck 5 + Expecto 3), Failed: 0

## Task Commits

Each task was committed atomically:

1. **Task 1: Agent.fs + Training.fs (TD(0) core)** - `cd8e1d8` (feat)
2. **Task 2: Expecto convergence tests** - `c9fd1c1` (feat)

## Files Created/Modified

- `TicTacToe/src/TicTacToe/Agent.fs` - tdUpdate, randomAgent, tdAgent (epsilon-greedy)
- `TicTacToe/src/TicTacToe/Training.fs` - playEpisode, trainAgent, winRateVsRandom
- `TicTacToe/tests/TicTacToe.Tests/ConvergenceTests.fs` - 3 Expecto testCase tests including >90% convergence
- `TicTacToe/src/TicTacToe/TicTacToe.fsproj` - added Agent.fs, Training.fs in dependency order
- `TicTacToe/tests/TicTacToe.Tests/TicTacToe.Tests.fsproj` - added ConvergenceTests.fs before Main.fs
- `TicTacToe/tests/TicTacToe.Tests/Main.fs` - runs both propertyTests and convergenceTests

## Decisions Made

- **prevBoard tracks current player's last board:** In the loop, when a move is made, we backup the previous board (which belonged to the current player before their move). This correctly bootstraps TD learning for each player's perspective within the shared ValueTable.
- **Same ValueTable for both X and O:** During self-play, X maximizes V(successsor) while O minimizes it. No separate tables needed.
- **winRateVsRandom hardcodes epsilon=0:** Caller doesn't need to remember to pass 0.0; evaluation is always greedy by design.
- **Fully qualified module path in Main.fs:** `TicTacToe.Tests.ConvergenceTests.convergenceTests` required because F# module access in the same namespace needs explicit qualification when not opened.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed unqualified `convergenceTests` reference in Main.fs**

- **Found during:** Task 2 (Expecto tests)
- **Issue:** Main.fs used bare `convergenceTests` which F# couldn't resolve at module scope
- **Fix:** Changed to fully qualified `TicTacToe.Tests.ConvergenceTests.convergenceTests`
- **Files modified:** TicTacToe/tests/TicTacToe.Tests/Main.fs
- **Verification:** Build succeeded, all 8 tests pass
- **Committed in:** c9fd1c1 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - compile error in Main.fs module reference)
**Impact on plan:** Trivial fix, no scope change.

## Issues Encountered

- Main.fs module reference needed full qualification (`TicTacToe.Tests.ConvergenceTests.convergenceTests` not just `convergenceTests`) — fixed immediately.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- TD(0) learning core complete. ValueTable trains to >90% vs random in 100k episodes (~4s).
- Ready for Phase 2 Plan 03 (tutorial documentation / mdBook integration for TicTacToe chapter).
- No blockers.

---
*Phase: 02-tictactoe-td-learning*
*Completed: 2026-02-19*
