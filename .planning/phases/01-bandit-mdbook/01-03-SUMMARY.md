---
phase: 01-bandit-mdbook
plan: "03"
subsystem: testing
tags: [fsharp, expecto, fscheck, property-based-testing, mdbook, korean, bandit, ucb1, epsilon-greedy]

# Dependency graph
requires:
  - phase: 01-bandit-mdbook/01-02
    provides: pure Bandit library with runEpisode, runEpisodeUcb1, epsilonGreedy, ucb1 functions

provides:
  - FsCheck property tests: Counts sum invariant for ε-greedy and UCB1 (BAND-07)
  - Expecto convergence tests: UCB1 and ε-greedy identify best arm after 1000 steps (BAND-08)
  - Full Korean mdBook chapter: RL concepts, F# types, incremental mean, UCB1 formula (TUTR-03, TUTR-06)
  - dotnet test integration via YoloDev.Expecto.TestSdk + Microsoft.NET.Test.Sdk

affects:
  - 02-tictactoe-mdbook (same test pattern: property tests + convergence tests + Korean chapter)
  - all future phases (FsCheck 2.16.5 + Expecto.FsCheck 10.2.3 compatibility constraint documented)

# Tech tracking
tech-stack:
  added:
    - FsCheck 2.16.5 (downgraded from 3.3.2 — required by Expecto.FsCheck 10.2.3)
    - Expecto.FsCheck 10.2.3 (testProperty via ExpectoFsCheck module)
    - Microsoft.NET.Test.Sdk 18.0.1 (dotnet test VSTest integration)
    - YoloDev.Expecto.TestSdk 0.15.5 (Expecto test adapter for VSTest discovery)
  patterns:
    - "[<Tests>] attribute on test list values for YoloDev adapter discovery"
    - "GenerateProgramFile=false to preserve [<EntryPoint>] with Microsoft.NET.Test.Sdk"
    - "Property tests use fixed seeds (no FsCheck generators) — deterministic properties"
    - "Convergence tests use fixed seed 42 — reproducible stochastic results"

key-files:
  created: []
  modified:
    - Bandit/tests/Bandit.Tests/PropertyTests.fs
    - Bandit/tests/Bandit.Tests/ConvergenceTests.fs
    - Bandit/tests/Bandit.Tests/Main.fs
    - Bandit/tests/Bandit.Tests/Bandit.Tests.fsproj
    - tutorial/src/01-bandit/README.md

key-decisions:
  - "FsCheck 3.3.2 → 2.16.5 downgrade: StdGen type removed in FsCheck 3.x breaks Expecto.FsCheck 10.2.3 at runtime"
  - "YoloDev.Expecto.TestSdk + [<Tests>] attributes: required for dotnet test discovery with Expecto"
  - "GenerateProgramFile=false: prevents Microsoft.NET.Test.Sdk from injecting Program.fs that conflicts with our [<EntryPoint>]"
  - "Fixed-seed property tests: FsCheck generators not needed — properties are deterministic given fixed env and steps"

patterns-established:
  - "Pattern 1: Expecto tests use [<Tests>] attribute on let bindings for YoloDev adapter discovery"
  - "Pattern 2: Property tests call domain functions directly with fixed Random seeds — no FsCheck Arbitrary needed"
  - "Pattern 3: Convergence tests pair fixed seed + ≥1000 steps to make stochastic outcomes reproducible"
  - "Pattern 4: Korean mdBook chapter structure: problem framing → types → algorithm 1 → algorithm 2 → comparison → design principles → next chapter"

# Metrics
duration: 4min
completed: 2026-02-19
---

# Phase 1 Plan 03: Tests and Korean Chapter Summary

**FsCheck property tests (Counts invariant) + Expecto convergence tests (UCB1/ε-greedy best-arm) + 173-line Korean mdBook chapter covering MAB, incremental mean, and UCB1 formula**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-19T02:43:42Z
- **Completed:** 2026-02-19T02:47:42Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- BAND-07: 4 FsCheck properties verify Counts sum equals total steps for ε-greedy and UCB1
- BAND-08: 4 Expecto convergence tests confirm UCB1 and ε-greedy identify arm with p=0.90 as best after 1000 steps
- TUTR-03/TUTR-06: 173-line Korean chapter covers MAB problem framing, exploration/exploitation table, AgentState/BanditEnv types, epsilonGreedy code, incremental mean formula, UCB1 formula, algorithm comparison table, and design principles
- All 8 tests pass via `dotnet test` with 0 failures, 0 errors

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement FsCheck property tests and Expecto convergence tests** - `b90782e` (test)
2. **Task 2: Write full Korean mdBook chapter for 01-bandit** - `b26653c` (docs)

**Plan metadata:** (this commit)

## Files Created/Modified

- `Bandit/tests/Bandit.Tests/PropertyTests.fs` — 4 FsCheck properties (Counts sum invariant, value ranges, non-negative counts; ε-greedy and UCB1)
- `Bandit/tests/Bandit.Tests/ConvergenceTests.fs` — 4 Expecto testCases (UCB1/ε-greedy converge to arm 2 p=0.90 after 1000 steps; all arms visited)
- `Bandit/tests/Bandit.Tests/Main.fs` — Expecto entry point aggregating propertyTests + convergenceTests
- `Bandit/tests/Bandit.Tests/Bandit.Tests.fsproj` — Added FsCheck 2.16.5 (downgraded), Microsoft.NET.Test.Sdk 18.0.1, YoloDev.Expecto.TestSdk 0.15.5, GenerateProgramFile=false
- `tutorial/src/01-bandit/README.md` — Full 173-line Korean chapter replacing 37-line stub

## Decisions Made

- **FsCheck 3.3.2 → 2.16.5 downgrade**: `StdGen` type was removed in FsCheck 3.x but Expecto.FsCheck 10.2.3 internally uses it at runtime. The NU1608 warning was not benign — it caused `TypeLoadException` at runtime. Downgraded to the version Expecto.FsCheck declares as its dependency.
- **[<Tests>] attribute required**: YoloDev.Expecto.TestSdk discovers tests by scanning for the `[<Tests>]` attribute on `let` bindings. Without it, `dotnet test` reports "No test is available."
- **GenerateProgramFile=false**: When Microsoft.NET.Test.Sdk is present it appends `Microsoft.NET.Test.Sdk.Program.fs` after our `Main.fs`, causing F# error FS0433 (EntryPoint not last in sequence). Setting `GenerateProgramFile=false` prevents injection.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] FsCheck 3.x incompatibility with Expecto.FsCheck caused runtime TypeLoadException**

- **Found during:** Task 1 (first test run via `dotnet run`)
- **Issue:** `StdGen` type removed in FsCheck 3.3.2; Expecto.FsCheck 10.2.3 still references it internally. All 4 FsCheck property tests errored with `System.TypeLoadException: Could not load type 'StdGen'`.
- **Fix:** Downgraded `FsCheck` from 3.3.2 to 2.16.5 in `Bandit.Tests.fsproj` — the exact version Expecto.FsCheck 10.2.3 declares as its requirement.
- **Files modified:** `Bandit/tests/Bandit.Tests/Bandit.Tests.fsproj`
- **Verification:** All 8 tests pass via `dotnet run` and `dotnet test`
- **Committed in:** `b90782e` (Task 1 commit)

**2. [Rule 3 - Blocking] dotnet test required YoloDev adapter + [<Tests>] attribute + GenerateProgramFile=false**

- **Found during:** Task 1 (plan verify step uses `dotnet test`)
- **Issue:** `dotnet test` reported "No test is available" without test adapter; adding Microsoft.NET.Test.Sdk caused FS0433 compile error (injected Program.fs conflicts with [<EntryPoint>]); adapter required `[<Tests>]` attribute for discovery.
- **Fix:** Added `Microsoft.NET.Test.Sdk 18.0.1` + `YoloDev.Expecto.TestSdk 0.15.5` packages; added `[<Tests>]` to `propertyTests` and `convergenceTests`; set `GenerateProgramFile=false`.
- **Files modified:** `Bandit/tests/Bandit.Tests/Bandit.Tests.fsproj`, `PropertyTests.fs`, `ConvergenceTests.fs`
- **Verification:** `dotnet test` shows "Test Run Successful. Total tests: 8 Passed: 8"
- **Committed in:** `b90782e` (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 3 — blocking issues)
**Impact on plan:** Both fixes necessary for `dotnet test` to work as plan's verify command requires. No scope creep.

## Issues Encountered

- FsCheck version incompatibility was pre-warned in STATE.md ("FsCheck 3.3.2 has a NU1608 warning... works at runtime") — this turned out to be incorrect. It does NOT work at runtime; the NU1608 warning is real and causes a runtime exception. Future phases should use FsCheck 2.16.5 when using Expecto.FsCheck.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 1 is fully complete: all 4 success criteria met
  - Criterion 1: `dotnet run` shows ε comparison and UCB1 winner output
  - Criterion 2: `dotnet test` passes 8 tests (4 FsCheck properties + 4 Expecto cases)
  - Criterion 3: `mdbook build tutorial/` succeeds with Korean chapter
  - Criterion 4: No I/O in pure modules (grep returns empty)
- Phase 2 (Tictactoe + mdBook) can begin; same test infrastructure pattern applies
- Key constraint for future phases: use FsCheck 2.16.5, not 3.x, when pairing with Expecto.FsCheck

---
*Phase: 01-bandit-mdbook*
*Completed: 2026-02-19*
