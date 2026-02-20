---
phase: 03-connect-four-q-learning-minimax
plan: "01"
subsystem: game-engine
tags: [fsharp, connect-four, property-based-testing, fscheck, expecto, game-rules, pure-functions]

# Dependency graph
requires:
  - phase: 02-tictactoe-td-learning
    provides: FsCheck 2.16.5 + Expecto 10.2.3 + YoloDev.Expecto.TestSdk 0.15.5 test stack patterns
  - phase: 01-bandit-mdbook
    provides: net10.0 + traditional .sln format decisions

provides:
  - ConnectFour.sln with three F# projects (game engine, console, tests)
  - Domain.fs: Cell DU, Board type, GameState, rows/cols/idx/emptyBoard/initialState/opponent
  - Rules.fs: dropRow, legalMoves, applyMove, checkWinner, isGameOver, GameResult DU
  - FsCheck property tests: gravity invariants + 4-in-a-row detection (14 tests passing)

affects:
  - 03-connect-four-q-learning-minimax/plan-02 (Minimax uses Domain + Rules directly)
  - 03-connect-four-q-learning-minimax/plan-03 (Q-Learning uses Domain + Rules directly)

# Tech tracking
tech-stack:
  added:
    - Expecto 10.2.3
    - Expecto.FsCheck 10.2.3
    - FsCheck 2.16.5
    - Microsoft.NET.Test.Sdk 18.0.1
    - YoloDev.Expecto.TestSdk 0.15.5
    - Serilog 4.3.1 (stub, Console project)
    - Serilog.Sinks.Console 6.1.1 (stub)
    - Serilog.Sinks.File 7.0.0 (stub)
  patterns:
    - Pure functional game engine: Domain.fs (types) + Rules.fs (logic), no I/O
    - Flat array board representation: row-major, row 0 = top, row 5 = bottom
    - Custom FsCheck generator (genValidBoard): simulates random games for property-based tests
    - Gravity-respecting applyMove: always drops to lowest empty row in column
    - Array.create N Empty for Board init (not Array.zeroCreate — returns null for DU types)

key-files:
  created:
    - 03-connect-four/ConnectFour.sln
    - 03-connect-four/src/ConnectFour/ConnectFour.fsproj
    - 03-connect-four/src/ConnectFour/Domain.fs
    - 03-connect-four/src/ConnectFour/Rules.fs
    - 03-connect-four/src/ConnectFour.Console/ConnectFour.Console.fsproj
    - 03-connect-four/src/ConnectFour.Console/Training.fs
    - 03-connect-four/src/ConnectFour.Console/Program.fs
    - 03-connect-four/tests/ConnectFour.Tests/ConnectFour.Tests.fsproj
    - 03-connect-four/tests/ConnectFour.Tests/PropertyTests.fs
    - 03-connect-four/tests/ConnectFour.Tests/Main.fs
  modified: []

key-decisions:
  - "Board as flat 42-element array (row-major): row 0 = top, row 5 = bottom"
  - "dropRow iterates rows-1 downto 0 to enforce gravity (bottom-first search)"
  - "checkWinner scans all positions in 4 directions: horizontal, vertical, diagonal, anti-diagonal"
  - "genValidBoard plays 0-30 random plies to generate realistic board states for property tests"
  - "isGameOver = checkWinner union full-board Draw detection"

patterns-established:
  - "Domain.fs exports pure types only, Rules.fs imports Domain and exports pure functions"
  - "All game logic is pure (no mutation, no I/O) — enables safe use in Minimax and Q-Learning"
  - "FsCheck property tests use custom generator wrapping random game simulation"

# Metrics
duration: 3min
completed: 2026-02-20
---

# Phase 03 Plan 01: Bootstrap ConnectFour Game Engine Summary

**Pure Connect Four game engine in F# (Domain + Rules) with 14 FsCheck property tests verifying gravity invariants and 4-in-a-row detection across all four directions**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-20T00:02:45Z
- **Completed:** 2026-02-20T00:05:56Z
- **Tasks:** 3
- **Files modified:** 10

## Accomplishments

- Bootstrapped ConnectFour.sln with three F# projects using traditional .sln format (decision [01-01])
- Implemented pure game engine: Domain.fs (Cell/Board/GameState types) + Rules.fs (dropRow/legalMoves/applyMove/checkWinner/isGameOver)
- 14 FsCheck property tests pass: 6 gravity invariants + 8 winner detection tests including all four win directions

## Task Commits

1. **Task 1: Bootstrap ConnectFour.sln** - `da8531f` (feat)
2. **Task 2: Implement Domain.fs + Rules.fs** - `9cf55bb` (feat)
3. **Task 3: FsCheck property tests** - `6cf6351` (feat)

## Files Created/Modified

- `03-connect-four/ConnectFour.sln` - Traditional .sln solution with three projects
- `03-connect-four/src/ConnectFour/ConnectFour.fsproj` - Game engine library (net10.0)
- `03-connect-four/src/ConnectFour/Domain.fs` - Cell DU, Board, GameState, rows, cols, idx, emptyBoard, initialState, opponent
- `03-connect-four/src/ConnectFour/Rules.fs` - dropRow, legalMoves, applyMove, checkWinner, GameResult, isGameOver
- `03-connect-four/src/ConnectFour.Console/ConnectFour.Console.fsproj` - Console Exe with Serilog (stub)
- `03-connect-four/src/ConnectFour.Console/Training.fs` - Stub (Plan 03 will implement Q-Learning)
- `03-connect-four/src/ConnectFour.Console/Program.fs` - Stub entry point
- `03-connect-four/tests/ConnectFour.Tests/ConnectFour.Tests.fsproj` - Expecto+FsCheck test runner
- `03-connect-four/tests/ConnectFour.Tests/PropertyTests.fs` - 14 property/unit tests (all passing)
- `03-connect-four/tests/ConnectFour.Tests/Main.fs` - EntryPoint combining gravityTests + winnerTests

## Decisions Made

- Board representation: flat 42-element array, row-major order, row 0 = top (visual), row 5 = bottom (gravity anchor)
- `dropRow` scans from `rows-1 downto 0` — this is the gravity implementation; first empty found = landing row
- `checkWinner` uses `List.tryPick` over all cells and 4 directions; short-circuits on first win found
- `genValidBoard` simulates random games (0-30 plies) with `System.Random` to produce realistic board states
- Console project has stub Training.fs + Program.fs so it builds from the start; real implementation in Plan 03

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Domain.fs and Rules.fs are pure modules with no I/O — ready for Minimax (Plan 02) and Q-Learning (Plan 03)
- ConnectFour.Console project stubs are in place; Training.fs and Program.fs need implementation in Plan 03
- FsCheck test infrastructure proven: genValidBoard generator produces diverse board states for property tests

---
*Phase: 03-connect-four-q-learning-minimax*
*Completed: 2026-02-20*
