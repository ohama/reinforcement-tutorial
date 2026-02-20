---
phase: "05"
plan: "05"
name: "Serilog Structured Logging + Korean mdBook Chapter"
subsystem: tutorial-and-logging
tags: [serilog, mdbook, korean, mcts, puct, policy-value-network, self-play]

dependency-graph:
  requires: ["05-04"]
  provides:
    - Serilog structured logging in Program.fs (GMOK-11)
    - Korean mdBook chapter for Phase 5 (TUTR-03, TUTR-04, TUTR-05, TUTR-06)
    - Complete tutorial with all 5 phases linked
  affects: []

tech-stack:
  added: []
  patterns:
    - "Program.fs sole impure file: Serilog setup → call pure functions → log results"
    - "Korean mdBook chapter structure: Phase N 한계 → 개념 → 구현 → {{#include}} → 결과 → 총정리"
    - "{{#include}} paths from tutorial/src/05-gomoku/: ../../../05-gomoku-mcts/src/Gomoku/..."

file-tracking:
  created:
    - path: "tutorial/src/05-gomoku/README.md"
      description: "Korean mdBook chapter — DQN 한계 → MCTS/PUCT → PolicyValueNet → 자가 대국 → Phase 5 총정리"
  modified:
    - path: "05-gomoku-mcts/src/Gomoku.Console/Program.fs"
      description: "Added Serilog setup in main, Log.Information for training/MCTS stats, Log.CloseAndFlush() before exit"

decisions:
  - id: "05-05-01"
    decision: "SUMMARY.md already had 05-gomoku entry — no update needed"
    rationale: "05-04 plan had already added the chapter entry to SUMMARY.md when the stub was created"
  - id: "05-05-02"
    decision: "Include NativeLoader, MctsNode, PolicyValueNet, Mcts, SelfPlay via {{#include}} (5 files)"
    rationale: "Training.fs omitted from {{#include}} as the pipeline explanation is in prose; too repetitive with SelfPlay"
  - id: "05-05-03"
    decision: "Keep printfn for interactive output (board, menu, prompts); Serilog only for structured stats"
    rationale: "Serilog is for machine-readable training metrics; user-facing text stays as printfn"

metrics:
  duration: "3m 14s"
  tasks-completed: 2
  tests-passing: 14
  completed: "2026-02-20"
---

# Phase 05 Plan 05: Serilog Structured Logging + Korean mdBook Chapter Summary

**One-liner:** Serilog structured training/MCTS logs in Program.fs + Korean AlphaZero tutorial chapter with 5 {{#include}} directives; `mdbook build tutorial/` exits 0 with all 5 phases.

## What Was Done

### Task 1: Serilog Structured Logging (GMOK-11)

Updated `05-gomoku-mcts/src/Gomoku.Console/Program.fs`:

- Added `open Serilog` import
- Added Serilog setup at the start of `main`:
  - Console sink with `[HH:mm:ss LVL] Message` template
  - File sink to `logs/gomoku-training.log` with daily rolling
- Added structured `Log.Information` calls:
  - `SelfPlay training started: Iterations, Simulations, LR`
  - `Training Iter, Games, PolicyLoss, ValueLoss` per iteration
  - `Model saved to {Path}` on completion
  - `Benchmark: WinRate, Wins, Games, Simulations`
  - `AI move ({Row},{Col}) Simulations` in human-vs-AI mode
- Added `Log.CloseAndFlush()` before exit
- Kept `printfn` for interactive output (board display, menu, prompts, game results)

### Task 2: Korean mdBook Chapter + SUMMARY.md (TUTR-03-06)

Created `tutorial/src/05-gomoku/README.md` with:

- **Phase 4의 한계**: DQN탐색 없음, 보상 희소성, 오목으로 확장 불가 → MCTS 동기
- **MCTS 4단계**: 선택(Selection) → 확장(Expansion) → 평가(Evaluation) → 역전파(Backprop)
- **PUCT 공식 설명**: Q(a) + c_puct × P(a) × √N_parent / (1 + N_child) table로 각 항 설명
- **ARM64 네이티브 로딩**: NativeLoader.fs {{#include}}
- **MCTS 트리 노드**: MctsNode.fs {{#include}} + 설계 결정 (mutable Prior, UpdateRecursive 규약)
- **Policy/Value 신경망**: PolicyValueNet.fs {{#include}} + 아키텍처 다이어그램 (4채널 입력, 이중 헤드)
- **신경망 연동 MCTS**: Mcts.fs {{#include}} + Dirichlet 노이즈 설명
- **자가 대국**: SelfPlay.fs {{#include}} + 온도 기반 수 선택, 훈련 샘플 메모리 관리
- **Phase 5 총정리**: 구성 요소 표, 검증 전략, 핵심 교훈 4가지

SUMMARY.md already had the 05-gomoku entry (added in 05-04 as stub). No update needed.

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build Gomoku.sln` | 0 errors, 0 warnings |
| `dotnet test Gomoku.sln` | 14/14 tests pass |
| `mdbook build tutorial/` | exits 0, INFO "Book successfully built" |
| `tutorial/book/05-gomoku/index.html` | exists |
| SUMMARY.md has all 5 chapters | confirmed (01-bandit through 05-gomoku) |
| Program.fs has `open Serilog` + `Log.Information` | confirmed (5 Log.Information calls) |
| 05-gomoku/README.md has 5 {{#include}} directives | confirmed |

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 9ac722c | feat(05-05) | Wire Serilog structured logging into Program.fs (GMOK-11) |
| 77423a8 | docs(05-05) | Korean mdBook 05-gomoku chapter with MCTS/PUCT explanation (TUTR-03-06) |

## Deviations from Plan

### Auto-fixed Issues

None.

### Observations

- SUMMARY.md already had the 05-gomoku entry from the stub created in 05-04. The plan noted to append it but it was already there — no change needed, documented as deviation-free execution.
- The 05-gomoku/ directory and stub README.md already existed from plan 05-04. The chapter was written fresh over the stub.

## Next Phase Readiness

This is the **final plan of the final phase** (05-05 of Phase 5).

All 5 tutorial phases are complete:
- 01-bandit: UCB1, ε-greedy, FsCheck properties
- 02-tictactoe: TD learning, value table
- 03-connect-four: Q-Learning + Minimax, Serilog
- 04-dqn: DQN, TorchSharp, target network, curriculum learning
- 05-gomoku: MCTS, PUCT, PolicyValueNet, AlphaZero self-play

The entire reinforcement tutorial is complete. `mdbook build tutorial/` exits 0 with all 5 Korean chapters linked in SUMMARY.md.
