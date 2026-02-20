---
phase: 04-connect-four-dqn
plan: "01"
subsystem: neural-network
tags: [torchsharp, fsharp, dqn, connect-four, fsharp-testing, expecto, fscheck, tensor]

# Dependency graph
requires:
  - phase: 03-connect-four-q-learning-minimax
    provides: Domain.fs, Rules.fs, Minimax.fs (Connect Four game logic, isGameOver API)
provides:
  - DQN.sln solution with ConnectFourDQN library and ConnectFourDQN.Console projects
  - TorchSharp-cpu 0.106.0 integration with ARM64 macOS NativeLoader
  - boardToTensor function: converts Board to [3,6,7] float32 tensor (myPiece/oppPiece/empty channels)
  - FsCheck property tests verifying tensor invariants (sum=42, shape=[3,6,7])
affects:
  - 04-connect-four-dqn/02 (DQN network architecture, training loop)
  - 04-connect-four-dqn/03 (self-play, evaluation)

# Tech tracking
tech-stack:
  added:
    - TorchSharp-cpu 0.106.0 (Apple Silicon ARM64 CPU-only PyTorch bindings)
    - Serilog 4.3.1 + Sinks.Console 6.1.1 + Sinks.File 7.0.0
    - Expecto 10.2.3 + Expecto.FsCheck 10.2.3 + FsCheck 2.16.5
    - YoloDev.Expecto.TestSdk 0.15.5 + Microsoft.NET.Test.Sdk 18.0.1
  patterns:
    - NativeLoader module (module-level do load()) for ARM64 dylib preloading before any TorchSharp call
    - torch.NewDisposeScope() in every test for deterministic tensor memory management
    - [3,6,7] tensor encoding: ch0=myPiece, ch1=oppPiece, ch2=empty — standard DQN board representation
    - Arb.fromGen for FsCheck custom generators in Expecto property tests
    - genValidBoard plays 0-30 random plies using System.Random and isGameOver: GameResult option check

key-files:
  created:
    - 04-connect-four-dqn/DQN.sln
    - 04-connect-four-dqn/src/ConnectFourDQN/ConnectFourDQN.fsproj
    - 04-connect-four-dqn/src/ConnectFourDQN/Domain.fs
    - 04-connect-four-dqn/src/ConnectFourDQN/Rules.fs
    - 04-connect-four-dqn/src/ConnectFourDQN/Minimax.fs
    - 04-connect-four-dqn/src/ConnectFourDQN/NativeLoader.fs
    - 04-connect-four-dqn/src/ConnectFourDQN/DQNAgent.fs
    - 04-connect-four-dqn/src/ConnectFourDQN.Console/ConnectFourDQN.Console.fsproj
    - 04-connect-four-dqn/src/ConnectFourDQN.Console/Program.fs
    - 04-connect-four-dqn/tests/ConnectFourDQN.Tests/ConnectFourDQN.Tests.fsproj
    - 04-connect-four-dqn/tests/ConnectFourDQN.Tests/TensorTests.fs
    - 04-connect-four-dqn/tests/ConnectFourDQN.Tests/Main.fs
  modified: []

key-decisions:
  - "NativeLoader uses module-level `do load()` so dylibs preload before any TorchSharp call — prevents SIGSEGV on ARM64"
  - "boardToTensor: [3,6,7] encoding ch0=myPiece, ch1=oppPiece, ch2=empty — each cell contributes exactly 1.0f (sum invariant = 42.0f)"
  - "genValidBoard uses fixed System.Random(42) seed for reproducibility — plays 0-30 random plies, stops if isGameOver returns Some"
  - "torch.NewDisposeScope() in all tests — prevents tensor memory leaks in test runs"
  - "FsCheck 2.16.5 required (not 3.x) — confirmed from Phase 1 decision: StdGen removed in FsCheck 3.x causes TypeLoadException with Expecto.FsCheck 10.2.3"

patterns-established:
  - "NativeLoader pattern: load dylibs in module-level do block, check Directory.Exists before loading"
  - "boardToTensor: Array.init (3*rows*cols) to build flat float32 array, then reshape to [3L;6L;7L]"
  - "Test scope pattern: use _scope = torch.NewDisposeScope() wrapping all tensor ops in each test"

# Metrics
duration: 3min
completed: 2026-02-20
---

# Phase 4 Plan 01: Bootstrap + boardToTensor Summary

**TorchSharp-cpu 0.106.0 DQN project bootstrapped on ARM64 macOS with NativeLoader, boardToTensor [3,6,7] tensor encoding, and 3 FsCheck/Expecto property tests all passing.**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-20T01:49:34Z
- **Completed:** 2026-02-20T01:52:51Z
- **Tasks:** 2
- **Files modified:** 12 created

## Accomplishments

- Created DQN.sln with ConnectFourDQN library (TorchSharp-cpu 0.106.0 + Serilog), ConnectFourDQN.Console, and ConnectFourDQN.Tests projects
- Implemented NativeLoader.fs with ARM64 macOS dylib preloading (libomp, libc10, libtorch_cpu, libtorch, libLibTorchSharp)
- Implemented boardToTensor: [3,6,7] float32 tensor where ch0=myPiece, ch1=oppPiece, ch2=empty with sum invariant sum=42.0f
- All 3 FsCheck/Expecto tests pass: tensorSumInvariant (property), tensorShapeTest (property), emptyBoardAllEmpty (unit)

## Task Commits

Each task was committed atomically:

1. **Task 1: Bootstrap DQN.sln + ConnectFourDQN library with TorchSharp + NativeLoader** - `1a8e723` (feat)
2. **Task 2: Implement boardToTensor + FsCheck TensorTests** - `63c7104` (feat)

## Files Created/Modified

- `04-connect-four-dqn/DQN.sln` - Solution with 3 projects
- `04-connect-four-dqn/src/ConnectFourDQN/ConnectFourDQN.fsproj` - Library project with TorchSharp-cpu 0.106.0 + Serilog, compile order: Domain→Rules→Minimax→NativeLoader→DQNAgent
- `04-connect-four-dqn/src/ConnectFourDQN/Domain.fs` - Copied from Phase 3, module renamed to ConnectFourDQN.Domain
- `04-connect-four-dqn/src/ConnectFourDQN/Rules.fs` - Copied from Phase 3, module renamed to ConnectFourDQN.Rules, isGameOver returns GameResult option
- `04-connect-four-dqn/src/ConnectFourDQN/Minimax.fs` - Copied from Phase 3, module renamed to ConnectFourDQN.Minimax
- `04-connect-four-dqn/src/ConnectFourDQN/NativeLoader.fs` - ARM64 macOS dylib preloader using NativeLibrary.Load
- `04-connect-four-dqn/src/ConnectFourDQN/DQNAgent.fs` - boardToTensor implementation using TorchSharp tensor API
- `04-connect-four-dqn/src/ConnectFourDQN.Console/Program.fs` - Minimal entry point (main _ = 0)
- `04-connect-four-dqn/tests/ConnectFourDQN.Tests/TensorTests.fs` - FsCheck property tests + unit test for boardToTensor
- `04-connect-four-dqn/tests/ConnectFourDQN.Tests/Main.fs` - runTestsInAssemblyWithCLIArgs entry point

## Decisions Made

- NativeLoader uses module-level `do load()` so dylibs preload before any TorchSharp call — prevents SIGSEGV on ARM64 macOS
- boardToTensor encoding: ch0=myPiece, ch1=oppPiece, ch2=empty — each cell contributes exactly 1.0f ensuring sum invariant = 42.0f
- genValidBoard uses fixed System.Random(42) seed for reproducibility — plays 0-30 random plies, stops at isGameOver Some result
- torch.NewDisposeScope() in all tests — deterministic tensor memory management prevents leaks
- FsCheck 2.16.5 (not 3.x) — confirmed required; StdGen removed in FsCheck 3.x causes TypeLoadException with Expecto.FsCheck 10.2.3
- libomp confirmed present at /opt/homebrew/opt/libomp/lib/libomp.dylib — no brew install needed

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all packages resolved, build succeeded 0 warnings/errors, all 3 tests passed on first run.

## User Setup Required

None - no external service configuration required. libomp was already installed on the system.

## Next Phase Readiness

- boardToTensor ready for DQN network input pipeline
- NativeLoader pattern established for safe TorchSharp initialization
- Test infrastructure ready for DQN network architecture tests
- Phase 4 Plan 02 can begin: DQN network (Conv2D/Linear layers), replay buffer, training loop

---
*Phase: 04-connect-four-dqn*
*Completed: 2026-02-20*
