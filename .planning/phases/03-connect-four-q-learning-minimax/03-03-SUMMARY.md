---
phase: 03-connect-four-q-learning-minimax
plan: "03"
subsystem: reinforcement-learning
tags: [fsharp, q-learning, q-table, epsilon-greedy, self-play, connect-four, tabular-rl]

# Dependency graph
requires:
  - phase: 03-connect-four-q-learning-minimax/03-01
    provides: ConnectFour game engine (Domain.fs, Rules.fs) — Board, Cell, applyMove, isGameOver, legalMoves
affects:
  - 03-connect-four-q-learning-minimax/03-04 (Program.fs — will call trainQLearning, playQAgentVsRandom)

provides:
  - QAgent.fs: QTable type, encodeState (42-char key), getQ (lazy init), chooseAction (epsilon-greedy), updateQ (Q-Learning update)
  - Training.fs: playEpisode (self-play), trainQLearning (epsilon-decay loop), playQAgentVsRandom (evaluation)
  - Separate Q-tables for Red and Yellow players to avoid perspective confusion

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Dictionary<string, float[]> as Q-table: string key from encodeState, float[] indexed by column"
    - "Lazy Q-value initialization: getQ creates zero-array on first access for any state"
    - "Separate Q-tables per player: eliminates perspective confusion in self-play"
    - "Epsilon-linear decay: (epsilonStart - epsilonEnd) / episodes, clamped at epsilonEnd"
    - "Terminal reward propagation: isTerminal=true passes 0.0 as nextMax in updateQ"

key-files:
  created:
    - 03-connect-four/src/ConnectFour/QAgent.fs
    - 03-connect-four/src/ConnectFour.Console/Training.fs
  modified:
    - 03-connect-four/src/ConnectFour/ConnectFour.fsproj
    - 03-connect-four/src/ConnectFour.Console/ConnectFour.Console.fsproj

key-decisions:
  - "QAgent.fs placed after Rules.fs and before Minimax.fs in fsproj (Minimax.fs was already present from Plan 02)"
  - "encodeState maps Empty->'.', Red->'R', Yellow->'Y' for 42-char string key from flat Board array"
  - "Separate Q-tables for Red and Yellow: each player learns from its own perspective"
  - "RewardDraw = 0.3 (positive but less than win) to encourage draws over losses"
  - "RewardStep = 0.0 — no intermediate reward shaping, only terminal rewards drive learning"

patterns-established:
  - "Q-Learning pattern: encodeState -> getQ -> chooseAction -> applyMove -> updateQ"
  - "Self-play loop: playEpisode runs full game, both players update their own tables"
  - "Evaluation separation: playQAgentVsRandom uses epsilon=0 (greedy) for fair evaluation"

# Metrics
duration: 2min
completed: 2026-02-20
---

# Phase 3 Plan 03: Q-Learning Agent Summary

**Tabular Q-Learning agent with Dictionary<string, float[]> backend, epsilon-greedy policy, and self-play training loop using separate Q-tables per player**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-20T00:09:47Z
- **Completed:** 2026-02-20T00:11:08Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- QAgent.fs: complete Q-table module with encodeState (42-char board key), lazy getQ, epsilon-greedy chooseAction, Q-Learning updateQ
- Training.fs: self-play training loop with linear epsilon decay, per-episode win-rate tracking, and Q-table size logging
- Separate Q-tables for Red and Yellow players — each learns from its own perspective, eliminating state aliasing
- `dotnet build ConnectFour.sln` passes with 0 warnings and 0 errors

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement QAgent.fs — Q-table module with epsilon-greedy policy** - `b3fd8a5` (feat)
2. **Task 2: Implement Training.fs — Q-Learning self-play training loop** - `9d3622c` (feat)

## Files Created/Modified
- `03-connect-four/src/ConnectFour/QAgent.fs` - QTable, encodeState, getQ, chooseAction, updateQ, reward constants
- `03-connect-four/src/ConnectFour/ConnectFour.fsproj` - Added QAgent.fs between Rules.fs and Minimax.fs
- `03-connect-four/src/ConnectFour.Console/Training.fs` - EpisodeOutcome, TrainingResult, playEpisode, trainQLearning, playQAgentVsRandom
- `03-connect-four/src/ConnectFour.Console/ConnectFour.Console.fsproj` - Added RootNamespace property

## Decisions Made
- QAgent.fs placed after Rules.fs and before Minimax.fs in ConnectFour.fsproj (Minimax.fs was already present from Plan 02's parallel execution)
- encodeState produces exactly 42 characters: Empty->'.', Red->'R', Yellow->'Y' mapped over the flat 42-element board array
- Separate Q-tables per player: Red trains redTable, Yellow trains yellowTable — avoids perspective confusion where the same board looks different from each player's viewpoint
- RewardDraw = 0.3 (small positive): encourages draws over losses while keeping win as the primary goal
- RewardStep = 0.0: no intermediate reward shaping — only terminal game outcomes drive Q-Learning

## Deviations from Plan

None - plan executed exactly as written.

The pre-existing Minimax test failure ("Minimax blocks opponent's winning move (depth 2)") was present before this plan and is not caused by these changes — it is a Plan 02 issue with the Minimax evaluation heuristic at depth 2.

## Issues Encountered
- Pre-existing test failure in MinimaxTests.fs: "Minimax blocks opponent's winning move (depth 2)" was already failing before Task 2 changes. This is a Plan 02 regression (Minimax heuristic does not reliably block at depth 2). Not introduced by Q-Learning changes.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- QAgent.fs and Training.fs are ready for Plan 04 (Program.fs — Console integration)
- Plan 04 can call `trainQLearning` and `playQAgentVsRandom` directly
- The pre-existing Minimax depth-2 blocking test failure should be addressed in Plan 02 or before Phase 3 completion

---
*Phase: 03-connect-four-q-learning-minimax*
*Completed: 2026-02-20*
