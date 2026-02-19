---
phase: 01-bandit-mdbook
plan: 02
subsystem: rl-core
tags: [fsharp, dotnet, reinforcement-learning, bandit, epsilon-greedy, ucb1, serilog, pure-functional]

# Dependency graph
requires:
  - phase: 01-bandit-mdbook/01-01
    provides: Bandit F# three-project solution skeleton with stub implementations
provides:
  - Pure Bandit RL library: Domain types + validation, Environment (pullArm), Agent (epsilonGreedy, ucb1, incrementalMean), Training (runEpisode, compareEpsilons, compareStrategies, totalReward)
  - Working console application printing ε=0.01/0.10/0.30 comparison and ε-greedy vs UCB1 winner
  - .gitignore excluding bin/, obj/, tutorial/book/, logs/
affects:
  - 01-03: tutorial content writing — can now reference working algorithm code
  - 02-tictactoe and later: three-project pattern confirmed with real RL content

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Functional Core / Imperative Shell enforced: pure library (Bandit.fsproj) has zero I/O; all I/O in Bandit.Console/Program.fs only
    - selectArm: AgentState -> Arm (rng captured in closure by caller, not passed through runEpisode)
    - Result type for validation (validateEpsilon, validateEnv) — no exceptions thrown in pure modules
    - UCB1 unvisited-arm initialization via Array.tryFindIndex before UCB formula
    - compareEpsilons creates child rng per epsilon via rng.Next() seed for reproducibility
    - mutable totalPulls in runEpisodeUcb1: local mutable acceptable in pure functional context (fold boundary)

key-files:
  created:
    - .gitignore
  modified:
    - Bandit/src/Bandit/Domain.fs
    - Bandit/src/Bandit/Environment.fs
    - Bandit/src/Bandit/Agent.fs
    - Bandit/src/Bandit/Training.fs
    - Bandit/src/Bandit.Console/Program.fs

key-decisions:
  - "selectArm signature in runEpisode is AgentState -> Arm (not System.Random -> AgentState -> Arm): rng captured in closure by caller avoids threading rng through signature unnecessarily"
  - "runEpisodeUcb1 uses local mutable totalPulls counter: required because UCB1 needs cumulative pull count; mutable is local to function, not exposed externally"
  - "compareEpsilons creates child System.Random per epsilon (rng.Next() seed): ensures statistical independence between epsilon runs without global RNG sharing"

patterns-established:
  - "Zero-I/O pure library: grep -n 'printfn|Console.|Log.' on all four library files returns empty"
  - "Closure-captured rng: caller binds rng to selectArm closure before passing to runEpisode"
  - "Serilog structured logging in console shell: {Epsilon:F2} format specifiers, rollingInterval file sink"

# Metrics
duration: 2min
completed: 2026-02-19
---

# Phase 1 Plan 02: Bandit RL Engine Summary

**10-arm ε-greedy vs UCB1 bandit engine in pure F# with Serilog console shell — prints ε=0.01/0.10/0.30 comparison and declares strategy winner**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-19T02:37:25Z
- **Completed:** 2026-02-19T02:39:52Z
- **Tasks:** 2/2
- **Files modified:** 5 modified, 1 created

## Accomplishments

- Pure functional RL library: Domain types with Result validation, pullArm, epsilonGreedy, ucb1, incrementalMean, runEpisode, compareEpsilons, compareStrategies, totalReward — zero I/O
- Working Bandit console app: Serilog logs ε=0.01/0.10/0.30 arm estimates and declares ε-greedy vs UCB1 winner with reward delta
- Full solution builds clean: `dotnet build Bandit.sln` exits 0, 0 errors (2 known FsCheck NU1608 warnings from Plan 01-01)

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement pure Bandit library (Domain, Environment, Agent, Training)** - `1c816a4` (feat)
2. **Task 2: Implement impure console shell with Serilog logging and comparison output** - `db07a1b` (feat)

**Plan metadata:** (pending — this commit)

## Files Created/Modified

- `Bandit/src/Bandit/Domain.fs` - Core types (Arm, AgentState, BanditEnv) + validateEpsilon/validateEnv using Result
- `Bandit/src/Bandit/Environment.fs` - pullArm pure function with rng parameter
- `Bandit/src/Bandit/Agent.fs` - incrementalMean, epsilonGreedy, ucb1 with unvisited-arm initialization
- `Bandit/src/Bandit/Training.fs` - runEpisode, runEpisodeUcb1, compareEpsilons, compareStrategies, totalReward
- `Bandit/src/Bandit.Console/Program.fs` - Serilog console+file logging, epsilon comparison, strategy winner, Log.CloseAndFlush()
- `.gitignore` - Excludes bin/, obj/, tutorial/book/, logs/, .vs/

## Decisions Made

- **selectArm as `AgentState -> Arm` not `System.Random -> AgentState -> int`:** The plan's provided code had a type mismatch — `epsilonGreedy rng eps` returns `AgentState -> Arm` but `runEpisode` expected `System.Random -> AgentState -> int`. Fixed by simplifying `runEpisode` to take `AgentState -> Arm` since caller always captures rng in the closure. This is cleaner design.
- **Local mutable in runEpisodeUcb1:** UCB1 requires cumulative step count. A local mutable `totalPulls` inside the function is acceptable; it is never exposed externally and doesn't compromise purity of the module interface.
- **Child RNG seeding in compareEpsilons:** Each epsilon run gets its own `System.Random(rng.Next())` for statistical independence while remaining deterministic from the root seed.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed type mismatch in selectArm signature**

- **Found during:** Task 1 (building Bandit.fsproj)
- **Issue:** Plan provided code used `selectArm: System.Random -> AgentState -> int` in `runEpisode` but called it as `runEpisode rng2 env (epsilonGreedy rng2 eps)` where `epsilonGreedy rng2 eps : AgentState -> Arm`. Type mismatch caused 4 compiler errors.
- **Fix:** Changed `runEpisode` signature to `selectArm: AgentState -> Arm`. Caller captures rng in closure before passing. No behavioral change — same rng is used, just bound earlier.
- **Files modified:** `Bandit/src/Bandit/Training.fs`
- **Verification:** `dotnet build src/Bandit/Bandit.fsproj` exits 0 after fix
- **Committed in:** 1c816a4 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug — type mismatch in plan-provided code)
**Impact on plan:** Fix was necessary and correct. No scope change. The simpler signature (closure over rng) is idiomatic F#.

## Issues Encountered

None beyond the type mismatch auto-fix documented above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 01-03 (tutorial content) can start immediately: working algorithm code is available to reference
- FsCheck 3.3.2 NU1608 warning unchanged from Plan 01-01 — no runtime errors observed, no action needed
- Property tests in Bandit.Tests can now be filled in with real algorithm property assertions (Plan 01-02 scope did not include test implementation)

---
*Phase: 01-bandit-mdbook*
*Completed: 2026-02-19*
