---
phase: 02-tictactoe-td-learning
plan: "03"
subsystem: console-interface-and-tutorial
tags: [fsharp, serilog, structured-logging, mdbook, human-vs-ai, td-learning, korean-tutorial]

dependency-graph:
  requires:
    - "02-01: TicTacToe domain, game engine, project structure"
    - "02-02: TD(0) agent, self-play training, convergence tests"
  provides:
    - "Program.fs: Serilog Console+File sink, trainAgent 100k, history logging, runHumanVsAI loop"
    - "tutorial/src/02-tictactoe/README.md: full Korean chapter with 4x include"
  affects:
    - "03-gomoku-minimax: tutorial structure pattern to follow"

tech-stack:
  added:
    - "Serilog 4.3.1 (structured logging)"
    - "Serilog.Sinks.Console 6.1.1"
    - "Serilog.Sinks.File 7.0.0"
  patterns:
    - "I/O isolation: Program.fs is the sole impure file; Training.fs stays pure"
    - "Structured logging: Episode={N} WinRate={X:P1} template variables"
    - "Human-AI loop: recursive function with retry on invalid/occupied input"
    - "mdBook {{#include}} for live source embedding in tutorial chapters"

key-files:
  created: []
  modified:
    - "TicTacToe/src/TicTacToe.Console/Program.fs"
    - "tutorial/src/02-tictactoe/README.md"

decisions:
  - id: "02-03-a"
    decision: "Program.fs is sole impure file; Training.fs returns history list, not logs"
    rationale: "Pure/impure boundary: keeps Training.fs testable, logging centralized in Program.fs"
  - id: "02-03-b"
    decision: "runHumanVsAI uses recursive loop (not while loop) for F# idiomatic style"
    rationale: "Consistent with rest of codebase; no mutable state needed"
  - id: "02-03-c"
    decision: "AI plays with epsilon=0 in human vs AI mode (pure greedy)"
    rationale: "User faces the best-trained policy; epsilon>0 would make AI randomly weaker"

metrics:
  duration: "~2 minutes"
  completed: "2026-02-19"
---

# Phase 2 Plan 03: Human vs AI Console + Serilog Logging + Korean mdBook Chapter Summary

**One-liner:** Serilog-instrumented 100k training loop with Episode/WinRate structured logs and recursive human-vs-AI console, plus full Korean mdBook chapter embedding all 4 source files via `{{#include}}`.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Program.fs: Serilog + training loop + human vs AI | bbe4225 | TicTacToe/src/TicTacToe.Console/Program.fs |
| 2 | Korean mdBook 02-tictactoe chapter | 1b5ba2a | tutorial/src/02-tictactoe/README.md |

## What Was Built

### Task 1: Program.fs

The stub `Program.fs` (6 lines from 02-01) was replaced with a full 91-line implementation:

- **Serilog setup**: Console + File sink, structured template `[{Timestamp} {Level}] {Message}`
- **Training call**: `trainAgent rng 100_000 0.1 0.1 1_000` returns `(vTable, history)`
- **History logging**: iterates `history` list, emits `Episode={N} WinRate={X:P1}` structured log per entry
- **`runHumanVsAI`**: recursive loop, human plays X (1-9 input with validation/retry), AI plays O (epsilon=0 greedy)
- **I/O isolation**: all side effects (Console, Serilog, Random init) confined to Program.fs

### Task 2: tutorial/src/02-tictactoe/README.md

Full Korean tutorial chapter replacing the 4-line placeholder:

- Phase 1 Bandit limitations vs MDP comparison table
- MDP concepts: state, action, transition, reward, value function
- TD(0) update formula with Korean explanation
- 4x `{{#include ../../../TicTacToe/src/TicTacToe/...}}` embedding Domain.fs, Rules.fs, Agent.fs, Training.fs
- Learning curve log output example showing convergence from ~61% to ~93%
- Phase 3 preview: state space explosion problem and two solution approaches

## Verification Results

```
dotnet build TicTacToe.sln     -> Build succeeded. 0 Warnings, 0 Errors
dotnet test TicTacToe.sln      -> Passed! Failed: 0, Passed: 8, Skipped: 0
mdbook build tutorial/         -> INFO HTML book written; exit code 0
grep Episode= Program.fs       -> Log.Information("Episode={Episode} WinRate={WinRate:P1}", ep, rate)
grep {{#include README.md      -> 4 includes (Domain.fs, Rules.fs, Agent.fs, Training.fs)
```

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Pure/impure boundary | Training.fs returns history list; Program.fs logs | Keeps Training.fs pure and testable |
| Human-AI loop style | Recursive `loop` function | Consistent with F# idiom in codebase |
| AI epsilon in human mode | epsilon=0 (pure greedy) | User faces optimally trained policy |

## Deviations from Plan

None - plan executed exactly as written.

## Phase 3 Readiness

Phase 2 is now complete. All three plans done:
- 02-01: Domain, game engine, project structure
- 02-02: TD agent, self-play training, convergence tests (>90% win rate)
- 02-03: Program.fs with Serilog, human vs AI console, Korean mdBook chapter

Phase 3 (Gomoku + Minimax Alpha-Beta) can begin. Key considerations:
- State space explosion: Gomoku 15x15 cannot use ValueTable approach
- Alpha-Beta pruning required for tractable search
- F# pattern matching suits recursive tree search well
