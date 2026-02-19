# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-19)

**Core value:** 각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.
**Current focus:** Phase 2 — Tictactoe + mdBook

## Current Position

Phase: 1 of 5 complete (Bandit + mdBook 기반) — Phase 2 ready to start
Plan: 3 of 3 in Phase 1 (Phase 1 COMPLETE)
Status: Phase complete
Last activity: 2026-02-19 — Completed 01-03-PLAN.md (FsCheck property tests, Expecto convergence tests, Korean mdBook chapter)

Progress: [███░░░░░░░] 20% (3/15 plans total)

## Performance Metrics

**Velocity:**
- Total plans completed: 3
- Average duration: 3.7 min
- Total execution time: ~11 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-bandit-mdbook | 3/3 COMPLETE | ~11 min | 3.7 min |
| 02-tictactoe-mdbook | 0/3 | - | - |
| 03-gomoku-minimax | 0/3 | - | - |
| 04-gomoku-dqn | 0/3 | - | - |
| 05-gomoku-alphazero | 0/3 | - | - |

**Recent Trend:**
- Last 5 plans: 01-01 (5 min), 01-02 (2 min), 01-03 (4 min)
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
- [01-01]: Traditional .sln format required — .NET 10 defaults to .slnx; must use `dotnet new sln` twice or delete .slnx to get .sln
- [01-01]: mdBook v0.5.2 installed via Homebrew (research specified 0.4.52) — backward-compatible
- [01-02]: selectArm signature in runEpisode is `AgentState -> Arm` (rng captured in closure by caller) — idiomatic F#, avoids threading rng through signature
- [01-02]: Local mutable totalPulls in runEpisodeUcb1 is acceptable — function-local, not exposed externally
- [01-02]: compareEpsilons creates child RNG per epsilon via rng.Next() seed — statistical independence with determinism
- [01-03]: FsCheck 2.16.5 required (not 3.x) — StdGen removed in FsCheck 3.x causes TypeLoadException with Expecto.FsCheck 10.2.3; NU1608 warning is NOT benign
- [01-03]: YoloDev.Expecto.TestSdk 0.15.5 + [<Tests>] attribute + GenerateProgramFile=false required for dotnet test to discover Expecto tests
- [01-03]: dotnet test with Expecto = Microsoft.NET.Test.Sdk + YoloDev.Expecto.TestSdk + [<Tests>] on let bindings + GenerateProgramFile=false

### Pending Todos

None — Phase 1 complete.

### Blockers/Concerns

- [Phase 2 planning]: Use FsCheck 2.16.5 (not 3.x) — confirmed requirement from Phase 1
- [Phase 2 planning]: Same test infrastructure pattern applies: [<Tests>] + YoloDev adapter + GenerateProgramFile=false
- [Phase 3 planning]: Alpha-Beta 평가 함수 설계 — F# 전용 예제 부족, planning 시 research-phase 고려
- [Phase 4 planning]: TorchSharp Conv2D + Sequential F# API 패턴 — C# 예제와 다름, research-phase 권장
- [Phase 4]: Apple Silicon ARM64 TorchSharp-cpu 지원 여부 — Phase 4 시작 전 확인 필요
- [Phase 5 planning]: AlphaZero 스타일 self-play + dual-head MCTS in F# — 문서 희소, research-phase 강력 권장

## Session Continuity

Last session: 2026-02-19T02:47:42Z
Stopped at: Completed 01-03-PLAN.md (FsCheck property tests + Expecto convergence tests + Korean mdBook chapter — Phase 1 COMPLETE)
Resume file: None
