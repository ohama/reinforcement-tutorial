# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-19)

**Core value:** 각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.
**Current focus:** Phase 3 COMPLETE — Phase 4 (DQN) next. Verify TorchSharp ARM64 support before starting.

## Current Position

Phase: 3 of 5 COMPLETE (Connect Four + Q-Learning + Minimax)
Plan: 4 of 4 in Phase 3 complete (03-01, 03-02, 03-03, 03-04 all done)
Status: Phase 3 complete — ready for Phase 4
Last activity: 2026-02-20 — Completed 03-04-PLAN.md (Program.fs console + Korean mdBook chapter)

Progress: [██████████░░] 67% (10/15 plans total)

## Performance Metrics

**Velocity:**
- Total plans completed: 10
- Average duration: ~2.3 min
- Total execution time: ~26 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-bandit-mdbook | 3/3 COMPLETE | ~11 min | 3.7 min |
| 02-tictactoe-td-learning | 3/3 COMPLETE | ~7 min | 2.3 min |
| 03-connect-four-q-learning-minimax | 4/4 COMPLETE | ~8 min | 2.0 min |
| 04-gomoku-dqn | 0/3 | - | - |
| 05-gomoku-alphazero | 0/3 | - | - |

**Recent Trend:**
- Last 5 plans: 03-01 (3 min), 03-02 (2 min), 03-03 (2 min), 03-04 (3 min)
- Trend: fast

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Setup]: mdBook for tutorial site — 정적 사이트 생성, Markdown 기반
- [Setup]: Phase별 독립 solution — 각 Phase가 자체 완결적, copy-and-evolve (no shared library)
- [Setup]: TorchSharp for neural nets — Phase 4, 5에서만 도입 (Phases 1-3은 zero NN dependency)
- [Setup]: Console only — RL 학습에 집중, Web/GUI 없음
- [01-01]: net10.0 used (system has .NET 10 SDK only, not net9.0) — all future phases use net10.0
- [01-01]: Traditional .sln format required — .NET 10 defaults to .slnx; must use `dotnet new sln --format sln` to get .sln
- [01-01]: mdBook v0.5.2 installed via Homebrew (research specified 0.4.52) — backward-compatible
- [01-02]: selectArm signature in runEpisode is `AgentState -> Arm` (rng captured in closure by caller) — idiomatic F#, avoids threading rng through signature
- [01-02]: Local mutable totalPulls in runEpisodeUcb1 is acceptable — function-local, not exposed externally
- [01-02]: compareEpsilons creates child RNG per epsilon via rng.Next() seed — statistical independence with determinism
- [01-03]: FsCheck 2.16.5 required (not 3.x) — StdGen removed in FsCheck 3.x causes TypeLoadException with Expecto.FsCheck 10.2.3; NU1608 warning is NOT benign
- [01-03]: YoloDev.Expecto.TestSdk 0.15.5 + [<Tests>] attribute + GenerateProgramFile=false required for dotnet test to discover Expecto tests
- [01-03]: dotnet test with Expecto = Microsoft.NET.Test.Sdk + YoloDev.Expecto.TestSdk + [<Tests>] on let bindings + GenerateProgramFile=false
- [02-01]: dotnet new sln --format sln required — confirmed .NET 10 creates .slnx by default; `--format sln` flag is the fix
- [02-01]: Array.create 9 Empty required for Board init — Array.zeroCreate returns null for DU types; documented in Domain.fs
- [02-01]: ValueTable = Map<Board, float> — board state to X win probability [0.0, 1.0]; pure functional approach
- [02-02]: prevBoard tracks current player's previous board for TD backup (not opponent's board)
- [02-02]: Both X and O share same ValueTable in self-play — X maximizes V, O minimizes V
- [02-02]: winRateVsRandom hardcodes epsilon=0 — evaluation is always greedy by design
- [02-02]: Main.fs needs fully qualified module path: TicTacToe.Tests.ConvergenceTests.convergenceTests (F# scoping rule)
- [02-03]: Program.fs is sole impure file — Training.fs returns history list; Program.fs iterates and logs via Serilog
- [02-03]: runHumanVsAI uses recursive F# loop (not while loop) — consistent with codebase style
- [02-03]: AI plays epsilon=0 in human-vs-AI mode — user faces optimally trained policy, not random noise
- [03-01]: Connect Four Board = flat 42-element array, row-major, row 0 = top, row 5 = bottom (gravity anchor)
- [03-01]: dropRow iterates rows-1 downto 0 — gravity: first empty found from bottom = landing row
- [03-01]: genValidBoard plays 0-30 random plies via System.Random to produce realistic board states for FsCheck
- [03-01]: Array.create (rows*cols) Empty for Board init (not Array.zeroCreate — returns null for DU types)
- [03-02]: NegInf/PosInf = +-1_000_000 (not Int32.MinValue/MaxValue) — negating MinValue overflows in .NET
- [03-02]: chooseMoveNaive inside Minimax.fs as reference for equivalence testing (not separate module)
- [03-02]: Blocking test uses 1-sided threat (Yellow cols 0,1,2) — double-sided threat is unblockable so only col 3 blocks
- [03-03]: QAgent.fs placed after Rules.fs and before Minimax.fs — QAgent has no dependency on Minimax
- [03-03]: Separate Q-tables per player (redTable, yellowTable) — avoids perspective confusion in self-play
- [03-03]: encodeState: Empty->'.', Red->'R', Yellow->'Y' — 42-char string key from flat Board array
- [03-03]: RewardDraw=0.3, RewardStep=0.0 — terminal-only rewards, no intermediate shaping
- [03-04]: Program.fs is sole impure file for Phase 3 — Training.fs returns result; Program.fs logs via Serilog
- [03-04]: AI vs AI depth=6 for matchup; Human vs Minimax depth=7 for player challenge
- [03-04]: Q-table covers only 0.000004% of 4.5T possible states — explicit DQN motivation
- [03-04]: Korean mdBook {{#include}} path pattern: ../../../03-connect-four/src/ConnectFour/...

### Pending Todos

None — Phase 3 complete. Phase 4 (DQN with TorchSharp) is next.

### Blockers/Concerns

- [Phase 4 planning]: TorchSharp Conv2D + Sequential F# API 패턴 — C# 예제와 다름, research-phase 권장
- [Phase 4]: Apple Silicon ARM64 TorchSharp-cpu 지원 여부 — Phase 4 시작 전 확인 필요
- [Phase 5 planning]: AlphaZero 스타일 self-play + dual-head MCTS in F# — 문서 희소, research-phase 강력 권장

## Session Continuity

Last session: 2026-02-20
Stopped at: Phase 3 complete — verified 4/4 must-haves, all requirements marked Complete
Resume file: None
