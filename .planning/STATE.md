# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-19)

**Core value:** 각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.
**Current focus:** Phase 1 — Bandit + mdBook 기반

## Current Position

Phase: 1 of 5 (Bandit + mdBook 기반)
Plan: 1 of 3 in current phase
Status: In progress
Last activity: 2026-02-19 — Completed 01-01-PLAN.md (bootstrap skeleton)

Progress: [█░░░░░░░░░] 7% (1/15 plans total)

## Performance Metrics

**Velocity:**
- Total plans completed: 1
- Average duration: 5 min
- Total execution time: ~5 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-bandit-mdbook | 1/3 | ~5 min | 5 min |
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: 01-01 (5 min)
- Trend: -

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
- [01-01]: FsCheck 3.3.2 with NU1608 warning — monitor in Plan 01-02 when property tests added; downgrade to 2.16.5 if runtime errors occur
- [01-01]: mdBook v0.5.2 installed via Homebrew (research specified 0.4.52) — backward-compatible

### Pending Todos

- [01-02]: Monitor FsCheck 3.3.2 NU1608 warning — downgrade to 2.16.5 if property test runtime errors occur

### Blockers/Concerns

- [Phase 3 planning]: Alpha-Beta 평가 함수 설계 — F# 전용 예제 부족, planning 시 research-phase 고려
- [Phase 4 planning]: TorchSharp Conv2D + Sequential F# API 패턴 — C# 예제와 다름, research-phase 권장
- [Phase 4]: Apple Silicon ARM64 TorchSharp-cpu 지원 여부 — Phase 4 시작 전 확인 필요
- [Phase 5 planning]: AlphaZero 스타일 self-play + dual-head MCTS in F# — 문서 희소, research-phase 강력 권장

## Session Continuity

Last session: 2026-02-19T02:33:52Z
Stopped at: Completed 01-01-PLAN.md (bootstrap skeleton — Bandit.sln + mdBook)
Resume file: None
