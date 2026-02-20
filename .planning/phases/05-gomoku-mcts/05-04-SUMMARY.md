---
phase: 05-gomoku-mcts
plan: "04"
subsystem: training
tags: [fsharp, torchsharp, mcts, alphazero, self-play, policy-value-net, gomoku]

# Dependency graph
requires:
  - phase: 05-01
    provides: Domain.fs (GameState, Board, Player), Rules.fs (isWinningMove, legalMoves, applyMove)
  - phase: 05-02
    provides: MctsNode.fs (MctsNode mutable class), Mcts.fs (mctsSearch, bestMove)
  - phase: 05-03
    provides: PolicyValueNet.fs (boardToTensor, forwardBoth, policy, value), mctsSearchWithNet in Mcts.fs
provides:
  - "SelfPlay.fs: TrainingSample type (float32[] arrays), playSelfPlayGame returning training samples with perspective-correct value targets"
  - "Training.fs: trainBatch (pure, returns (policyLoss, valueLoss)), runSelfPlayTraining (full AlphaZero loop), evaluateVsRandom"
  - "Program.fs: 4-option console menu — train/benchmark/human-vs-AI/quit, model save/load, sole impure file"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TrainingSample stores float32[] not Tensor — avoids memory leaks in long replay buffer loops"
    - "Temperature schedule: τ=1.0 for first 15 moves (sample), τ→0 after (argmax)"
    - "Perspective-correct value targets: winner's states get +1.0f, loser's get -1.0f, draw 0.0f"
    - "trainBatch is pure (no I/O, no side effects) — returns (float * float) tuple"
    - "Program.fs is sole impure file — all console I/O and model save/load here"
    - "open type TorchSharp.torch used in Training.fs — Operators.int64/Operators.float for shadowed conversions"

key-files:
  created:
    - 05-gomoku-mcts/src/Gomoku/SelfPlay.fs
    - 05-gomoku-mcts/src/Gomoku/Training.fs
  modified:
    - 05-gomoku-mcts/src/Gomoku/Gomoku.fsproj
    - 05-gomoku-mcts/src/Gomoku.Console/Program.fs

key-decisions:
  - "Training.fs needs open Gomoku.Rules for isWinningMove/legalMoves/applyMove in evaluateVsRandom"
  - "Program.fs uses open Gomoku.Mcts then unqualified mctsSearchWithNet/bestMove (not Mcts.X)"
  - "mctsSearchWithNet signature has rng: System.Random as second param (from 05-03 Dirichlet noise design)"
  - "Human-vs-AI creates its own Random() for AI rng — no seed, so each game is different"

patterns-established:
  - "Full AlphaZero self-play loop: model.eval() → playSelfPlayGame → model.train() → trainBatch → model.eval()"
  - "Replay buffer as List<TrainingSample> with MaxBufferSize trim — O(n) remove but simple and correct"
  - "Deviations auto-fixed: missing open statements caught at build time, not requiring plan changes"

# Metrics
duration: 3min
completed: 2026-02-20
---

# Phase 5 Plan 04: Self-Play Training Pipeline Summary

**AlphaZero-style self-play pipeline: SelfPlay.fs produces perspective-correct TrainingSample[], Training.fs runs pure batch gradient updates (CE + MSE loss), Program.fs provides 4-option menu with model save/load and human-vs-AI play**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-20T04:24:20Z
- **Completed:** 2026-02-20T04:27:22Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- SelfPlay.fs: complete self-play game loop with temperature-based move selection and perspective-correct value target assignment (+1.0f winner, -1.0f loser, 0.0f draw)
- Training.fs: pure trainBatch (policy cross-entropy + value MSE, returns (float * float)), full runSelfPlayTraining loop with replay buffer, evaluateVsRandom benchmark
- Program.fs: 4-option menu (train/benchmark/human-vs-AI/quit), model.save()/model.load() on gomoku_model.pt, console board display
- All 14 existing tests continue to pass after adding 234 lines across two new files

## Task Commits

Each task was committed atomically:

1. **Task 1: SelfPlay.fs + Training.fs (pure self-play pipeline)** - `76a27b8` (feat)
2. **Task 2: Program.fs — menu + model save/load + human-vs-AI** - `6c62d05` (feat)

**Plan metadata:** (committed with SUMMARY.md below)

## Files Created/Modified

- `05-gomoku-mcts/src/Gomoku/SelfPlay.fs` - TrainingSample type; playSelfPlayGame: self-play game loop with temperature schedule and value target assignment
- `05-gomoku-mcts/src/Gomoku/Training.fs` - trainBatch (pure gradient update), runSelfPlayTraining (full AlphaZero loop), evaluateVsRandom (benchmark)
- `05-gomoku-mcts/src/Gomoku/Gomoku.fsproj` - Added SelfPlay.fs and Training.fs after Mcts.fs in compile order
- `05-gomoku-mcts/src/Gomoku.Console/Program.fs` - Replaced stub with full 4-option menu, board display, model save/load, human-vs-AI loop

## Decisions Made

- **Training.fs missing Gomoku.Rules open:** evaluateVsRandom uses isWinningMove/legalMoves/applyMove — added `open Gomoku.Rules` (auto-fix, Rule 3 blocking)
- **Program.fs uses open Gomoku.Mcts:** mctsSearchWithNet/bestMove are called unqualified after opening the module, not `Mcts.X` pattern (auto-fix, Rule 3 blocking)
- **mctsSearchWithNet signature includes rng:** The 05-03 design added `rng: System.Random` as second param for Dirichlet noise — SelfPlay.fs passes the game-level rng through; Program.fs creates its own `Random()` for AI moves in human-vs-AI mode

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Missing `open Gomoku.Rules` in Training.fs**
- **Found during:** Task 1 (Training.fs build)
- **Issue:** evaluateVsRandom called isWinningMove, legalMoves, applyMove but Rules module not opened
- **Fix:** Added `open Gomoku.Rules` after `open Gomoku.Domain` in Training.fs
- **Files modified:** 05-gomoku-mcts/src/Gomoku/Training.fs
- **Verification:** Build passed with 0 errors after fix
- **Committed in:** 76a27b8 (Task 1 commit)

**2. [Rule 3 - Blocking] Missing `open Gomoku.Mcts` in Program.fs**
- **Found during:** Task 2 (Program.fs build)
- **Issue:** Program.fs used `Mcts.mctsSearchWithNet` / `Mcts.bestMove` but module not opened, causing FS0039 errors
- **Fix:** Added `open Gomoku.Mcts` and changed calls to unqualified `mctsSearchWithNet` / `bestMove`
- **Files modified:** 05-gomoku-mcts/src/Gomoku.Console/Program.fs
- **Verification:** Build passed with 0 errors after fix
- **Committed in:** 6c62d05 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 3 - blocking missing open statements)
**Impact on plan:** Both fixes necessary for compilation. No scope creep. Logic and architecture unchanged from plan.

## Issues Encountered

None beyond the two missing `open` statements caught at build time.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 5 is now fully complete (plans 05-01 through 05-04 done):
- 05-01: Domain, Rules, 8 tests
- 05-02: MCTS (pure random rollout), 6 tests (14 total)
- 05-03: PolicyValueNet (TorchSharp), mctsSearchWithNet
- 05-04: SelfPlay, Training, Program.fs (full AlphaZero loop)

The AlphaZero learning loop is closed. `dotnet run` from Gomoku.Console presents the 4-option menu. No blockers.

---
*Phase: 05-gomoku-mcts*
*Completed: 2026-02-20*
