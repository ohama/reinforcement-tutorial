---
phase: 04-connect-four-dqn
plan: "04"
subsystem: console-entrypoint
tags: [dqn, serilog, torchsharp, mdbook, korean-tutorial, curriculum-learning, fsharp]

requires:
  - phase: 04-03
    provides: "trainDQN pure training loop, TrainingResult with LossHistory/WinRateHistory, defaultConfig with ModelSavePath"
  - phase: 04-02
    provides: "DQNModel, DQNAgent (chooseMove, boardToArray), ReplayBuffer"
  - phase: 04-01
    provides: "NativeLoader ARM64 workaround, Domain/Rules/Minimax modules"

provides:
  - "Program.fs: sole impure file — Serilog setup + trainDQN call + structured logging + menu (train/benchmark/human-vs-AI)"
  - "Serilog logs Episode/AvgLoss/WinRate as structured properties to console + logs/dqn-training.log"
  - "tutorial/src/04-dqn/README.md: Korean DQN chapter with 5x {{#include}} directives"
  - "mdBook build succeeds; tutorial book includes 04-dqn chapter"
  - "Phase 4 complete: 9/9 tests pass, console compiles, mdBook builds"

affects:
  - "05-gomoku: MCTS tutorial chapter follows same Korean structure"

tech-stack:
  added: []
  patterns:
    - "Program.fs sole-impure pattern: Serilog setup → call pure Training function → log result → menu loop"
    - "isGameOver pattern: match on GameResult option (RedWins/YellowWins/Draw/None) for game loop termination"
    - "Korean mdBook chapter: Phase N 한계 → 개념 → 핵심 구현 → {{#include}} → 결과 → Phase N+1 예고"

key-files:
  created:
    - "tutorial/src/04-dqn/README.md"
    - ".planning/phases/04-connect-four-dqn/04-04-SUMMARY.md"
  modified:
    - "04-connect-four-dqn/src/ConnectFourDQN.Console/Program.fs"

key-decisions:
  - "04-04-WIN: winner() function does not exist in Rules.fs — used isGameOver with GameResult match (RedWins/YellowWins/Draw)"
  - "04-04-SERILOG: Serilog packages available transitively via ProjectReference to ConnectFourDQN.fsproj; no direct package reference needed in Console.fsproj"
  - "04-04-SUMMARY: SUMMARY.md already had 04-dqn entry from initial tutorial setup; no modification required"

patterns-established:
  - "Program.fs impure boundary: all I/O (Serilog, printfn, Console.ReadLine) isolated in Program.fs; Training.fs is pure"
  - "Game loop termination: match isGameOver board with | Some X -> ... | None -> () (not winner() helper)"

duration: ~8min
completed: 2026-02-20
---

# Phase 4 Plan 04: Program.fs + Korean mdBook Chapter Summary

**Serilog-wired console entry point (train/benchmark/human-vs-AI menu) plus Korean DQN tutorial chapter with 5x {{#include}} directives covering NativeLoader, DQNAgent, DQNModel, ReplayBuffer, and Training.fs — Phase 4 complete with 9/9 tests passing**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-02-20T04:05:27Z
- **Completed:** 2026-02-20T04:13:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- `Program.fs`: full console entry point with Serilog (console + file sinks), trainDQN call from menu option 1 only, benchmark mode (DQN vs Minimax depth 4, 100 games), human-vs-AI interactive mode
- `tutorial/src/04-dqn/README.md`: Korean chapter with 5 `{{#include}}` directives — NativeLoader.fs, DQNAgent.fs, DQNModel.fs, ReplayBuffer.fs, Training.fs — covering Q-table failure, DQN concepts, curriculum learning, Phase 5 MCTS preview
- Phase 4 fully complete: `dotnet test DQN.sln` passes 9/9, `mdbook build tutorial/` exits 0

## Task Commits

1. **Task 1: Program.fs — Serilog + training menu + win rate logging** - `5fcfad0` (feat)
2. **Task 2: Korean mdBook chapter 04-dqn/README.md** - `09f87f0` (feat)

## Files Created/Modified

- `04-connect-four-dqn/src/ConnectFourDQN.Console/Program.fs` — Serilog setup, menu loop (train/benchmark/human-vs-AI/quit), trainDQN call with structured logging of Episode/AvgLoss/WinRate
- `tutorial/src/04-dqn/README.md` — Korean DQN chapter: Q-table 한계 → 경험 재플레이 + 타겟 네트워크 + 커리큘럼 → 5x {{#include}} → 학습 결과 해석 → Phase 5 MCTS 예고

## Decisions Made

**04-04-WIN: Use `isGameOver` pattern (not `winner()` helper)**
Rules.fs exposes `isGameOver board : GameResult option` returning `RedWins | YellowWins | Draw | None`. There is no `winner()` function in the codebase. The benchmark and human-vs-AI modes use `match isGameOver board with | Some RedWins -> ... | Some YellowWins -> ... | Some Draw -> ... | None -> ()`.

**04-04-SERILOG: Transitive Serilog reference from library**
Serilog 4.3.1 + Console 6.1.1 + File 7.0.0 are referenced in `ConnectFourDQN.fsproj` (the library). The Console project accesses them transitively via `ProjectReference` — no additional package references needed in `ConnectFourDQN.Console.fsproj`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Plan template used `winner board` which does not exist in Rules.fs**
- **Found during:** Task 1 (reviewing Minimax.fs and Rules.fs before writing Program.fs)
- **Issue:** The plan's Program.fs code snippet called `winner board` returning `Cell option`, but Rules.fs has no such function. `isGameOver` returns `GameResult option` (RedWins/YellowWins/Draw).
- **Fix:** Used `match isGameOver board with | Some RedWins -> ... | Some YellowWins -> ... | Some Draw -> ... | None -> ()` throughout benchmark and human-vs-AI modes.
- **Files modified:** `04-connect-four-dqn/src/ConnectFourDQN.Console/Program.fs`
- **Verification:** `dotnet build` 0 errors; 9/9 tests pass
- **Committed in:** `5fcfad0` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (plan template bug)
**Impact on plan:** Fix essential for compilation. No scope change.

## Issues Encountered

None beyond the deviation documented above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 4 is complete: all success criteria met
  - 9/9 Expecto tests pass (tensor shape, replay capacity, done-mask, model save/load, DQN vs random benchmark)
  - `Program.fs` compiles with Serilog; menu provides train/benchmark/human-vs-AI
  - `tutorial/src/04-dqn/README.md` Korean chapter with 5x `{{#include}}` directives
  - `mdbook build tutorial/` exits 0
- Phase 5 (MCTS / AlphaZero-style): builds on DQN Phase 4; will add Policy/Value dual-head network and MCTS tree search. Korean tutorial chapter will explain why DQN needs tree search.

---
*Phase: 04-connect-four-dqn*
*Completed: 2026-02-20*
