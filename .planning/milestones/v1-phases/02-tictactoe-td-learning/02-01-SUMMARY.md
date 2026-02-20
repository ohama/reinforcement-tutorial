---
phase: 02-tictactoe-td-learning
plan: 01
subsystem: game-engine
tags: [fsharp, tictactoe, domain-modeling, fscheck, expecto, property-testing, pure-functions]

# Dependency graph
requires:
  - phase: 01-bandit-mdbook
    provides: "Established test infrastructure pattern: FsCheck 2.16.5 + Expecto + YoloDev.Expecto.TestSdk + GenerateProgramFile=false"
provides:
  - TicTacToe.sln (traditional .sln format, 3-project solution)
  - Domain.fs: Cell/Board/GameState/ValueTable types + emptyBoard/initialState/otherPlayer pure functions
  - Rules.fs: 8 winLines + checkWinner/GameResult/isGameOver/legalMoves/applyMove pure functions
  - TicTacToe.Tests: 5 FsCheck board invariant property tests passing
  - TicTacToe.Console: minimal stub (to be completed in 02-03)
affects:
  - 02-02 (TD learning agent builds on Domain.fs + Rules.fs)
  - 02-03 (Console training loop uses game engine)

# Tech tracking
tech-stack:
  added:
    - FsCheck 2.16.5 (property-based testing)
    - Expecto 10.2.3 (test framework)
    - Expecto.FsCheck 10.2.3 (FsCheck integration)
    - YoloDev.Expecto.TestSdk 0.15.5 (dotnet test adapter)
    - Microsoft.NET.Test.Sdk 18.0.1 (VSTest integration)
    - Serilog 4.3.1 + Sinks.Console 6.1.1 + Sinks.File 7.0.0 (structured logging)
  patterns:
    - "Flat 9-element Board array (row-major 0-8) for tic-tac-toe state"
    - "Cell DU (Empty|X|O) as board element type — Array.create 9 Empty, never Array.zeroCreate"
    - "Pure game functions: checkWinner/legalMoves/applyMove/isGameOver all I/O-free"
    - "8-line win condition check via Array.tryPick over winLines"
    - "FsCheck testProperty with deterministic function bodies (no random args needed for these invariants)"

key-files:
  created:
    - TicTacToe/TicTacToe.sln
    - TicTacToe/src/TicTacToe/TicTacToe.fsproj
    - TicTacToe/src/TicTacToe/Domain.fs
    - TicTacToe/src/TicTacToe/Rules.fs
    - TicTacToe/src/TicTacToe.Console/TicTacToe.Console.fsproj
    - TicTacToe/src/TicTacToe.Console/Program.fs
    - TicTacToe/tests/TicTacToe.Tests/TicTacToe.Tests.fsproj
    - TicTacToe/tests/TicTacToe.Tests/PropertyTests.fs
    - TicTacToe/tests/TicTacToe.Tests/Main.fs
  modified: []

key-decisions:
  - "dotnet new sln --format sln required on .NET 10 (defaults to .slnx)"
  - "FsCheck 2.16.5 required (not 3.x) — same pattern confirmed from Phase 1"
  - "Array.create 9 Empty for board init (Array.zeroCreate returns null for DU types)"
  - "ValueTable = Map<Board, float> — board state to X win probability [0.0, 1.0]"

patterns-established:
  - "Game engine split: Domain.fs (types) -> Rules.fs (logic) — F# compile order enforced"
  - "All game logic pure: no I/O, no mutable state, returns new values"
  - "Property tests use testProperty with deterministic function bodies (no FsCheck generators needed for fixed-input invariants)"

# Metrics
duration: 3min
completed: 2026-02-19
---

# Phase 2 Plan 01: TicTacToe Solution Bootstrap Summary

**Pure functional TicTacToe game engine (Domain.fs + Rules.fs) with 5 FsCheck board invariant tests passing in a 3-project .NET 10 F# solution**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-19T05:49:22Z
- **Completed:** 2026-02-19T05:52:36Z
- **Tasks:** 2
- **Files modified:** 9 created

## Accomplishments

- TicTacToe.sln (traditional .sln format, not .slnx) with 3 projects scaffolded
- Domain.fs: Cell/Board/GameState/ValueTable types + emptyBoard/initialState/otherPlayer pure functions
- Rules.fs: 8 win lines, checkWinner, GameResult (XWins|OWins|Draw), isGameOver, legalMoves, applyMove — all pure
- PropertyTests.fs: 5 FsCheck board invariant tests all passing (dotnet test: Failed=0, Passed=5)

## Task Commits

Each task was committed atomically:

1. **Task 1: TicTacToe.sln bootstrap + Domain.fs + Rules.fs** - `57d7508` (feat)
2. **Task 2: FsCheck property tests + dotnet test pass** - `4b2df82` (feat)

## Files Created/Modified

- `TicTacToe/TicTacToe.sln` - Traditional .sln format solution file with 3 projects
- `TicTacToe/src/TicTacToe/TicTacToe.fsproj` - Library project (Domain.fs -> Rules.fs compile order)
- `TicTacToe/src/TicTacToe/Domain.fs` - Cell/Board/GameState/ValueTable types + pure functions
- `TicTacToe/src/TicTacToe/Rules.fs` - 8 winLines + game logic pure functions
- `TicTacToe/src/TicTacToe.Console/TicTacToe.Console.fsproj` - Console project with Serilog
- `TicTacToe/src/TicTacToe.Console/Program.fs` - Minimal stub (completed in 02-03)
- `TicTacToe/tests/TicTacToe.Tests/TicTacToe.Tests.fsproj` - Test project with GenerateProgramFile=false, FsCheck 2.16.5
- `TicTacToe/tests/TicTacToe.Tests/PropertyTests.fs` - 5 FsCheck board invariant tests with [<Tests>] attribute
- `TicTacToe/tests/TicTacToe.Tests/Main.fs` - Expecto entry point

## Decisions Made

- **dotnet new sln --format sln**: .NET 10 defaults to .slnx; must explicitly pass `--format sln` to get traditional format
- **FsCheck 2.16.5 confirmed**: Same constraint as Phase 1 — 3.x breaks Expecto.FsCheck with TypeLoadException
- **Array.create 9 Empty**: DU types cannot use Array.zeroCreate (returns null); documented in Domain.fs comment
- **ValueTable = Map<Board, float>**: Board state to X's win probability — immutable Map for functional purity

## Deviations from Plan

None - plan executed exactly as written.

The one deviation noted is that `dotnet new sln` creates .slnx by default on .NET 10, but the plan already documented this and the fix (`--format sln` flag). Handled as specified.

## Issues Encountered

- `.NET 10 defaults to .slnx` format: First `dotnet new sln` created TicTacToe.slnx. Deleted it and re-ran with `--format sln` flag to get traditional TicTacToe.sln. Already documented in STATE.md accumulated context.

## Next Phase Readiness

- Game engine (Domain.fs + Rules.fs) complete and verified — ready for 02-02 TD agent
- Console project stub in place — ready for 02-03 training loop
- Test infrastructure working — FsCheck 2.16.5 + Expecto pattern confirmed for Phase 2
- No blockers for 02-02 (TD learning implementation)

---
*Phase: 02-tictactoe-td-learning*
*Completed: 2026-02-19*
