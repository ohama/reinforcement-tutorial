---
phase: 04-connect-four-dqn
plan: "02"
subsystem: neural-network
tags: [TorchSharp, DQN, Conv2d, replay-buffer, epsilon-greedy, bellman-update, F#]

# Dependency graph
requires:
  - phase: 04-connect-four-dqn plan 01
    provides: DQN.sln scaffold, boardToTensor, NativeLoader, TorchSharp-cpu 0.106.0 confirmed working
provides:
  - DQNModel: Conv2d(3→64)→relu→Conv2d(64→128)→relu→Flatten→Linear(5376→256)→relu→Linear(256→7) with RegisterComponents
  - ReplayBuffer: fixed-capacity circular buffer with float32[] Experience storage (no tensor leaks)
  - DQNAgent: boardToTensor, boardToArray, chooseMove (epsilon-greedy + illegal mask), trainStep, syncTargetNetwork
  - 7 passing tests: 3 TensorTests + 4 ReplayBufferTests
affects: [04-03-training-loop, 05-gomoku-alphazero]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "open type TorchSharp.torch shadows F# int/int64 conversion functions — use Operators.int and Operators.int64"
    - "Tensor.max(dim) returns struct tuple in TorchSharp 0.106.0 — use struct(vals, _) destructuring, NOT .values"
    - "index_fill_(0L, idxTensor, Scalar) for in-place masking — qVec.[int64 col] <- does not compile due to shadowing"
    - "use _scope = torch.NewDisposeScope() in trainStep wraps all tensor ops — no tensor escapes function"
    - "Experience.StateData: float32[] not Tensor — plain .NET heap objects safe to hold across training steps"
    - "RegisterComponents() in do block — mandatory for gradient tracking and model serialization"
    - "F# compile order: Domain → Rules → Minimax → NativeLoader → DQNModel → ReplayBuffer → DQNAgent"

key-files:
  created:
    - 04-connect-four-dqn/src/ConnectFourDQN/DQNModel.fs
    - 04-connect-four-dqn/src/ConnectFourDQN/ReplayBuffer.fs
    - 04-connect-four-dqn/tests/ConnectFourDQN.Tests/ReplayBufferTests.fs
  modified:
    - 04-connect-four-dqn/src/ConnectFourDQN/DQNAgent.fs
    - 04-connect-four-dqn/src/ConnectFourDQN/ConnectFourDQN.fsproj
    - 04-connect-four-dqn/tests/ConnectFourDQN.Tests/ConnectFourDQN.Tests.fsproj

key-decisions:
  - "Tensor.max(1L) returns struct ValueTuple<Tensor,Tensor> — destructure with let struct(vals, _) = t.max(1L)"
  - "open type TorchSharp.torch shadows int/int64 as ScalarType enum values — Operators.int/Operators.int64 required"
  - "index_fill_ with System.Single.NegativeInfinity cast to Scalar for illegal move masking (implicit conversion FS3391 warning is benign)"
  - "Experience uses float32[] arrays not Tensor fields — tensors outside dispose scope cause memory leaks in long training loops"

patterns-established:
  - "TorchSharp shadowing: Any F# module using open type TorchSharp.torch must use Operators.int / Operators.int64 for numeric conversions"
  - "Struct tuple max: let struct(maxVals, _) = tensor.max(dimL) — .values property does not exist in TorchSharp 0.106.0"
  - "ReplayBuffer pattern: Push circular-overwrites, Sample requires size >= batchSize, Experience holds float32[] not Tensor"

# Metrics
duration: 6min
completed: 2026-02-20
---

# Phase 4 Plan 02: DQN Network Architecture + Replay Buffer Summary

**Conv2d DQN model + circular replay buffer + epsilon-greedy agent with Bellman trainStep, all memory-safe via NewDisposeScope and float32[] Experience fields, 7/7 tests passing**

## Performance

- **Duration:** 6 min
- **Started:** 2026-02-20T01:58:45Z
- **Completed:** 2026-02-20T02:04:17Z
- **Tasks:** 2
- **Files modified:** 5 (3 created, 2 updated)

## Accomplishments
- DQNModel: Conv2d(3→64)→relu→Conv2d(64→128)→relu→Flatten→Linear(5376→256)→relu→Linear(256→7) with RegisterComponents() for gradient tracking
- ReplayBuffer: fixed-capacity circular buffer with Push/Sample/Size — Experience stores float32[] arrays (not tensors) preventing memory leaks
- DQNAgent: complete epsilon-greedy action selection with illegal move masking via index_fill_, Bellman trainStep wrapped in NewDisposeScope, hard-copy syncTargetNetwork
- All 7 tests pass: 3 TensorTests (shape/sum invariant/empty board) + 4 ReplayBufferTests (capacity, sample size, error-on-empty, done-flag retrieval)

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement DQNModel.fs + ReplayBuffer.fs** - `824f102` (feat)
2. **Task 2: Implement full DQNAgent.fs + ReplayBufferTests** - `f1df8af` (feat)

## Files Created/Modified
- `04-connect-four-dqn/src/ConnectFourDQN/DQNModel.fs` - Neural network: Conv2d→relu→Conv2d→relu→Flatten→Linear→relu→Linear with RegisterComponents
- `04-connect-four-dqn/src/ConnectFourDQN/ReplayBuffer.fs` - Experience type (float32[]) + circular replay buffer
- `04-connect-four-dqn/src/ConnectFourDQN/DQNAgent.fs` - boardToTensor, boardToArray, chooseMove, trainStep, syncTargetNetwork
- `04-connect-four-dqn/src/ConnectFourDQN/ConnectFourDQN.fsproj` - Updated compile order: Domain→Rules→Minimax→NativeLoader→DQNModel→ReplayBuffer→DQNAgent
- `04-connect-four-dqn/tests/ConnectFourDQN.Tests/ConnectFourDQN.Tests.fsproj` - Added ReplayBufferTests.fs
- `04-connect-four-dqn/tests/ConnectFourDQN.Tests/ReplayBufferTests.fs` - 4 buffer tests

## Decisions Made

- **Tensor.max(1L) struct tuple:** TorchSharp 0.106.0 returns a struct ValueTuple, not an object with `.values`. Fixed with `let struct(nextQMax, _) = target.forward(nextStates).max(1L)`.
- **int/int64 shadowing:** `open type TorchSharp.torch` brings `int64` and `int` into scope as `ScalarType.Int64`/`ScalarType.Int32` enum values, shadowing F# conversion functions. Solution: use `Operators.int64` and `Operators.int` throughout DQNAgent.
- **Illegal move masking approach:** `qVec.[int64 col] <- torch.tensor(-infinityf)` fails to compile due to shadowing. Replaced with `index_fill_(0L, idxTensor, Scalar)` with `System.Single.NegativeInfinity` — triggers benign FS3391 implicit conversion warning.
- **float32[] in Experience:** Storing Tensor fields in Experience would leak memory outside NewDisposeScope. float32[] arrays are plain .NET heap objects — safe across training steps.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] TorchSharp API incompatibilities required three fixes**
- **Found during:** Task 2 (DQNAgent.fs implementation)
- **Issue 1:** `.max(1L).values` — `.values` property does not exist in TorchSharp 0.106.0; max returns struct ValueTuple
- **Issue 2:** `qVec.[int64 col] <- ...` — `int64` function shadowed by `ScalarType.Int64`
- **Issue 3:** `item<int64>() |> int` — `int` function shadowed by `ScalarType.Int32`
- **Fix:** Struct tuple destructuring for max, `index_fill_` for masking, `Operators.int`/`Operators.int64` for conversions
- **Files modified:** `DQNAgent.fs`
- **Verification:** `dotnet test` — 7/7 passing
- **Committed in:** `f1df8af` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - TorchSharp API incompatibilities with plan's pseudocode)
**Impact on plan:** All fixes necessary for correctness. No scope creep. Core algorithm unchanged.

## Issues Encountered
- The plan's code template used `.values` on Tensor.max result and `|> int` after `item<int64>()`, but TorchSharp 0.106.0 on .NET 10 / ARM64 macOS has a struct-returning max and `open type torch` shadows F# numeric conversion functions. All three issues were fixed in a single iteration via fsi testing.

## Next Phase Readiness
- DQNModel, ReplayBuffer, DQNAgent are all complete and tested
- Ready for Plan 03: full 50K episode DQN training loop with exploration decay, periodic target sync, and Serilog progress logging
- No blockers

---
*Phase: 04-connect-four-dqn*
*Completed: 2026-02-20*
