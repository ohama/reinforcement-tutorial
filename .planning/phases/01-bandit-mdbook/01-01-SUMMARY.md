---
phase: 01-bandit-mdbook
plan: 01
subsystem: infra
tags: [fsharp, dotnet, expecto, fscheck, serilog, mdbook, bandit, reinforcement-learning]

# Dependency graph
requires: []
provides:
  - Bandit F# three-project solution (Bandit.sln) with Bandit library, Bandit.Console, Bandit.Tests
  - Core domain types: Arm, AgentState, BanditEnv in Domain.fs
  - Functional stubs: pullArm, epsilonGreedy, ucb1, incrementalMean, runEpisode
  - mdBook tutorial site with Korean book.toml and 5-chapter SUMMARY.md
  - All 5 chapter stub READMEs (01-bandit through 05-gomoku)
affects:
  - 01-02: implements full RL algorithms on top of this skeleton
  - 01-03: writes tutorial content into existing mdBook structure
  - 02-tictactoe through 05-gomoku: each phase follows same three-project layout pattern

# Tech tracking
tech-stack:
  added:
    - .NET 10 / F# 9 (net10.0 — system only had .NET 10 SDK, not net9.0 as planned)
    - Expecto 10.2.3 (test runner, values-as-tests)
    - Expecto.FsCheck 10.2.3 (FsCheck bridge for property tests)
    - FsCheck 3.3.2 (property-based test generation)
    - Serilog 4.3.1 (structured logging core)
    - Serilog.Sinks.Console 6.1.1 (console output)
    - Serilog.Sinks.File 7.0.0 (file output)
    - mdBook v0.5.2 via Homebrew (research specified 0.4.52; 0.5.2 installed — fully compatible)
  patterns:
    - Functional Core / Imperative Shell: pure library (Bandit.fsproj) vs impure shell (Bandit.Console)
    - F# file ordering in .fsproj: Domain.fs → Environment.fs → Agent.fs → Training.fs (dependency order)
    - System.Random passed as parameter to all environment/agent functions (no global mutable state)
    - Expecto console runner pattern with [<EntryPoint>] in Main.fs
    - Traditional .sln format (not new .slnx — .NET 10 defaults to .slnx, overridden)

key-files:
  created:
    - Bandit/Bandit.sln
    - Bandit/src/Bandit/Bandit.fsproj
    - Bandit/src/Bandit/Domain.fs
    - Bandit/src/Bandit/Environment.fs
    - Bandit/src/Bandit/Agent.fs
    - Bandit/src/Bandit/Training.fs
    - Bandit/src/Bandit.Console/Bandit.Console.fsproj
    - Bandit/src/Bandit.Console/Program.fs
    - Bandit/tests/Bandit.Tests/Bandit.Tests.fsproj
    - Bandit/tests/Bandit.Tests/PropertyTests.fs
    - Bandit/tests/Bandit.Tests/ConvergenceTests.fs
    - Bandit/tests/Bandit.Tests/Main.fs
    - tutorial/book.toml
    - tutorial/src/SUMMARY.md
    - tutorial/src/README.md
    - tutorial/src/01-bandit/README.md
    - tutorial/src/02-tictactoe/README.md
    - tutorial/src/03-connect-four/README.md
    - tutorial/src/04-dqn/README.md
    - tutorial/src/05-gomoku/README.md
  modified: []

key-decisions:
  - "net10.0 used instead of net9.0: system only has .NET 10 SDK installed; fully compatible"
  - "Traditional .sln format forced: dotnet new sln in .NET 10 defaults to .slnx; deleted .slnx and recreated with --format sln equivalent (second dotnet new sln call produced .sln)"
  - "mdBook v0.5.2 installed via Homebrew: research specified 0.4.52 but 0.5.2 is backward-compatible"
  - "FsCheck 3.3.2 retained despite NU1608 warning: Expecto.FsCheck 10.2.3 declares <3.0.0 requirement but runtime works correctly — placeholder tests pass"
  - "Serilog NOT added to Bandit.Tests: logging belongs only in Bandit.Console per research decision"

patterns-established:
  - "Three-project layout: {Name}.fsproj (classlib) + {Name}.Console.fsproj (exe) + {Name}.Tests.fsproj (exe)"
  - "F# compile order: Domain → Environment → Agent → Training (no forward references)"
  - "Expecto entry point: Main.fs with [<EntryPoint>] calling runTestsWithCLIArgs"
  - "mdBook structure: tutorial/src/SUMMARY.md as authoritative TOC, one folder per chapter"

# Metrics
duration: 5min
completed: 2026-02-19
---

# Phase 1 Plan 01: Bootstrap Skeleton Summary

**Three-project F# Bandit solution (net10.0) with Expecto/FsCheck/Serilog stubs, plus 5-chapter Korean mdBook tutorial site — both build cleanly from repo root**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-19T02:29:08Z
- **Completed:** 2026-02-19T02:33:52Z
- **Tasks:** 2/2
- **Files modified:** 20 created, 0 modified

## Accomplishments

- Bandit.sln with three projects in correct layout; `dotnet build` exits 0 with 0 errors
- Functional stubs for Domain types, Environment (pullArm), Agent (epsilonGreedy, ucb1, incrementalMean), Training (runEpisode) in correct F# file order
- Expecto test runner with 2 placeholder tests passing; Serilog console logger in Bandit.Console printing correctly
- mdBook tutorial site with Korean title, 5-chapter SUMMARY.md, all chapter stub READMEs; `mdbook build` exits 0

## Task Commits

Each task was committed atomically:

1. **Task 1: Bootstrap Bandit F# solution** - `d789ac4` (feat)
2. **Task 2: Initialize mdBook tutorial site** - `678d3ce` (feat)

**Plan metadata:** (pending — this commit)

## Files Created/Modified

- `Bandit/Bandit.sln` - Traditional solution file referencing all three projects
- `Bandit/src/Bandit/Bandit.fsproj` - Pure classlib, net10.0, file order: Domain→Environment→Agent→Training
- `Bandit/src/Bandit/Domain.fs` - Core types: Arm, AgentState, BanditEnv
- `Bandit/src/Bandit/Environment.fs` - pullArm (pure, rng parameter)
- `Bandit/src/Bandit/Agent.fs` - incrementalMean, epsilonGreedy, ucb1 stubs
- `Bandit/src/Bandit/Training.fs` - runEpisode (pure fold-based episode runner)
- `Bandit/src/Bandit.Console/Bandit.Console.fsproj` - Exe with Serilog packages + ProjectReference to Bandit
- `Bandit/src/Bandit.Console/Program.fs` - Serilog logger config, prints phase 1 placeholder, exits 0
- `Bandit/tests/Bandit.Tests/Bandit.Tests.fsproj` - Exe with Expecto + FsCheck + ProjectReference to Bandit
- `Bandit/tests/Bandit.Tests/PropertyTests.fs` - Placeholder property test list
- `Bandit/tests/Bandit.Tests/ConvergenceTests.fs` - Placeholder convergence test list
- `Bandit/tests/Bandit.Tests/Main.fs` - [<EntryPoint>] combining all test lists
- `tutorial/book.toml` - mdBook config, Korean title, language=ko
- `tutorial/src/SUMMARY.md` - TOC linking all 5 chapters in 기초/심화 structure
- `tutorial/src/README.md` - Intro page with 5-chapter overview table
- `tutorial/src/01-bandit/README.md` - Bandit problem intro, F# types, algorithm stub descriptions
- `tutorial/src/02-05/README.md` - Stub placeholders for future phases

## Decisions Made

- **net10.0 vs net9.0:** System has .NET 10 SDK; used net10.0 throughout. All tooling (Expecto, Serilog, FsCheck) works identically.
- **Traditional .sln format:** .NET 10 `dotnet new sln` defaults to `.slnx` (new XML format). Deleted it, ran `dotnet new sln` again which produced traditional `.sln`. Required because plan specifies `Bandit.sln`.
- **FsCheck 3.3.2 with NU1608 warning:** Expecto.FsCheck 10.2.3 declares FsCheck dependency as `>= 2.16.5 && < 3.0.0` but FsCheck 3.3.2 is required per research. Warning appears but tests run correctly. Will monitor if property tests fail in Plan 02.
- **mdBook v0.5.2:** Homebrew installed 0.5.2 (research specified 0.4.52). API-compatible, no issues.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] net10.0 substituted for net9.0**
- **Found during:** Task 1 (creating classlib project)
- **Issue:** `dotnet new classlib --framework net9.0` failed — system only has .NET 10 SDK
- **Fix:** Used `--framework net10.0` for all three projects
- **Files modified:** All three .fsproj files target net10.0
- **Verification:** `dotnet build Bandit.sln` exits 0, 0 errors
- **Committed in:** d789ac4 (Task 1 commit)

**2. [Rule 3 - Blocking] Recreated .sln from .slnx**
- **Found during:** Task 1 (build verification)
- **Issue:** `dotnet new sln` in .NET 10 creates `.slnx` format; `dotnet build Bandit.sln` failed (file not found)
- **Fix:** Deleted `Bandit.slnx`, ran `dotnet new sln -n Bandit` again which produced traditional `.sln`; re-added all three projects
- **Files modified:** Bandit/Bandit.sln (traditional format)
- **Verification:** `dotnet build Bandit.sln` exits 0
- **Committed in:** d789ac4 (Task 1 commit)

**3. [Rule 3 - Blocking] Installed mdBook via Homebrew**
- **Found during:** Pre-execution check
- **Issue:** `mdbook` not installed on system
- **Fix:** `brew install mdbook` — installed v0.5.2
- **Files modified:** None (system tool)
- **Verification:** `mdbook build tutorial/` exits 0
- **Committed in:** N/A (system tool install)

---

**Total deviations:** 3 auto-fixed (3 blocking)
**Impact on plan:** All fixes were necessary to unblock execution. No scope creep. net10.0 is fully forward-compatible with all planned libraries.

## Issues Encountered

- Auto-generated `Program.fs` in `tests/Bandit.Tests/` from `dotnet new console` template — removed it since `Main.fs` is the custom entry point
- Auto-generated `Library.fs` in `src/Bandit/` from `dotnet new classlib` template — removed it since Domain.fs replaces it

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 01-02 (RL algorithm implementation) can start immediately: skeleton exists, builds cleanly
- Plan 01-03 (tutorial content) can start after Plan 01-02: mdBook structure is in place
- FsCheck 3.3.2 / NU1608 warning should be monitored when Plan 01-02 adds property tests — if runtime errors occur, downgrade to FsCheck 2.16.5

---
*Phase: 01-bandit-mdbook*
*Completed: 2026-02-19*
