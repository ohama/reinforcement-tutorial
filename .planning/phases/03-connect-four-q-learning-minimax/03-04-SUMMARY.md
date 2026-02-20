---
phase: 03-connect-four-q-learning-minimax
plan: "04"
subsystem: console
tags: [fsharp, serilog, minimax, q-learning, connect-four, mdbook, korean-tutorial]

# Dependency graph
requires:
  - phase: 03-01
    provides: Domain.fs + Rules.fs — Board type, applyMove, isGameOver
  - phase: 03-02
    provides: Minimax.fs — chooseMoveAB with Alpha-Beta pruning
  - phase: 03-03
    provides: QAgent.fs + Training.fs — QTable, chooseAction, trainQLearning
provides:
  - Full console application with Serilog + menu + AI vs AI + Human vs AI modes
  - Korean mdBook chapter for Connect Four phase with 4 {{#include}} directives
  - Q-table coverage analysis demonstrating why DQN is needed (0.000004% coverage)
  - Phase 3 complete
affects: [04-gomoku-dqn, 05-gomoku-alphazero, tutorial-readers]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Program.fs is sole impure file — all logic in Training.fs, agents in library"
    - "Serilog structured logging with Episode={N}, QTableSize={N}, Winner={W} fields"
    - "totalPrunes ref cell threaded through AI vs AI game loop for Alpha-Beta stats"
    - "Korean mdBook chapters with {{#include}} embedding source files"

key-files:
  created:
    - tutorial/src/03-connect-four/README.md
  modified:
    - 03-connect-four/src/ConnectFour.Console/Program.fs

key-decisions:
  - "Program.fs pattern follows 02-03: sole impure file, delegates to Training.fs"
  - "AI vs AI depth=6 for matchup; Human vs Minimax depth=7 for challenge"
  - "totalPossibleStates hardcoded as 4_531_985_219_092L — explicit limitation demonstration"
  - "SUMMARY.md already had 03-connect-four entry — no change needed"

patterns-established:
  - "Phase complete pattern: Domain → Rules → Agent → Training → Program (impure)"
  - "Q-table coverage analysis: print visited vs total states in every Phase 3+ console app"
  - "Korean tutorial structure: prior phase limits → types → rules → algorithms → Q-table limits → next phase preview"

# Metrics
duration: 3min
completed: 2026-02-20
---

# Phase 3 Plan 04: Console Integration + mdBook Chapter Summary

**Full Program.fs console with Serilog/menu/AI-vs-AI/Human-vs-AI and Korean mdBook chapter showing Q-table covers only 0.000004% of 4.5T Connect Four states**

## Performance

- **Duration:** 3m 17s
- **Started:** 2026-02-20T00:17:16Z
- **Completed:** 2026-02-20T00:20:33Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Program.fs fully implemented: Serilog Console + File sinks, 50k episode Q-learning training, Q-table coverage stats, AI vs AI matchup (20 games Minimax depth 6 vs Q-agent), Human vs AI (Minimax depth 7 or Q-agent ε=0), menu-driven UI
- Korean mdBook chapter written with 4 `{{#include}}` directives embedding Domain.fs, Rules.fs, Minimax.fs, QAgent.fs
- Q-table 한계 section demonstrates 0.000004% state coverage as motivation for Phase 4 DQN
- `mdbook build tutorial/` succeeds; `dotnet test ConnectFour.sln` passes all 20 tests

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement Program.fs — Serilog + AI vs AI + Human vs AI console** - `05033bc` (feat)
2. **Task 2: Write Korean mdBook chapter for 03-connect-four** - `7f3e8f2` (docs)

**Plan metadata:** (to be committed with this summary)

## Files Created/Modified
- `03-connect-four/src/ConnectFour.Console/Program.fs` - Full console app: Serilog setup, training call, Q-table stats, AI vs AI matchup, Human vs AI modes, menu loop
- `tutorial/src/03-connect-four/README.md` - Full Korean tutorial chapter with Phase 2 limits table, Domain/Rules/Minimax/QAgent includes, Q-table 한계 section, Phase 4 DQN preview

## Decisions Made
- AI vs AI uses depth=6 for Minimax; Human vs Minimax uses depth=7 for a proper challenge level
- `totalPossibleStates = 4_531_985_219_092L` hardcoded to make the limitation explicit (not computed)
- `tutorial/src/SUMMARY.md` already had 03-connect-four entry — no modification needed
- Pattern matches Phase 2: Program.fs as sole impure file, delegates training to Training.fs

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
- `timeout` command not available on macOS by default (coreutils not installed); used direct `dotnet run` with bash timeout parameter instead. No impact on execution.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Phase 3 complete: Domain, Rules, Minimax, QAgent, Training, Program all implemented and tested
- All 20 tests pass
- mdBook chapter complete with source includes
- Phase 4 (DQN): TorchSharp Conv2D on Apple Silicon ARM64 needs research before implementation
- Concern: TorchSharp-cpu ARM64 support to verify before Phase 4 starts
