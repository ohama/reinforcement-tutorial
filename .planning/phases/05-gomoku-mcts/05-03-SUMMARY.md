---
phase: 05-gomoku-mcts
plan: "03"
subsystem: neural-network
tags: [torchsharp, pytorch, mcts, neural-network, policy-value-net, arm64, conv2d, batchnorm, alphazero]

# Dependency graph
requires:
  - phase: 05-01
    provides: Domain.fs (Board, GameState, Player), Rules.fs (legalMoves, applyMove, isWinningMove)
  - phase: 05-02
    provides: MctsNode.fs (mutable MCTS tree node with Prior), Mcts.fs (pure MCTS, puctScore, selectAction)
  - phase: 04-connect-four-dqn
    provides: NativeLoader pattern (ARM64 macOS dylib preload), TorchSharp-cpu 0.106.0 in NuGet cache
provides:
  - NativeLoader.fs — ARM64 macOS dylib preload (identical to Phase 4 pattern)
  - PolicyValueNet.fs — boardToTensor [4,15,15] + dual-head network (3-conv backbone, policy log-probs + value tanh)
  - Mcts.fs updated — mctsSearchWithNet (PUCT + PolicyValueNet expansion) + sampleDirichlet (pure F#)
affects: [05-04 (self-play training uses PolicyValueNet.forwardBoth and mctsSearchWithNet)]

# Tech tracking
tech-stack:
  added: [TorchSharp-cpu 0.106.0, Conv2d, BatchNorm2d, Linear, ReLU, Tanh, log_softmax]
  patterns:
    - NativeLoader module-level do load() prevents SIGSEGV on ARM64 macOS
    - Dual-head network with separate policy() and value() methods sharing backbone
    - boardToTensor 4-channel encoding (current, opponent, lastMove, turnIndicator)
    - No open type TorchSharp.torch at module level — avoids shadowing F# built-ins (float, sqrt, log, etc.)
    - Pure F# Dirichlet sampler via Marsaglia-Tsang Gamma distribution (no torch.distributions needed)
    - torch.NewDisposeScope() in every simulation loop iteration for deterministic tensor memory management

key-files:
  created:
    - 05-gomoku-mcts/src/Gomoku/NativeLoader.fs
    - 05-gomoku-mcts/src/Gomoku/PolicyValueNet.fs
  modified:
    - 05-gomoku-mcts/src/Gomoku/Gomoku.fsproj
    - 05-gomoku-mcts/src/Gomoku/Mcts.fs

key-decisions:
  - "Do NOT use open type TorchSharp.torch at module level in Mcts.fs — shadows float, sqrt, log, cos, int, int64 and breaks pure-MCTS code"
  - "Use ReLU() and Tanh() as nn module instances (not functional relu/tanh) — same pattern as Phase 4 DQNModel"
  - "Use torch.nn.functional.log_softmax (fully qualified) to avoid namespace ambiguity"
  - "Implement Dirichlet noise in pure F# using Marsaglia-Tsang Gamma sampler — torch.distributions.Dirichlet not reliably accessible in 0.106.0 via F# open type"
  - "mctsSearchWithNet takes rng parameter alongside model — pure-MCTS rollout removed but rng needed for Dirichlet noise"
  - "PolicyValueNet uses ReLU() module instances registered via RegisterComponents() for gradient tracking and serialization"

patterns-established:
  - "Pattern: TorchSharp module classes use let-bound module instances (not properties) so RegisterComponents() can discover them via reflection"
  - "Pattern: boardToTensor produces [4,15,15] float32 — always called inside NewDisposeScope() in simulation loop"
  - "Pattern: model.eval() before mctsSearchWithNet, model.train() only during trainBatch"

# Metrics
duration: 4min
completed: 2026-02-20
---

# Phase 5 Plan 03: TorchSharp + PolicyValueNet + Neural MCTS Summary

**TorchSharp 0.106.0 integrated with 3-conv dual-head PolicyValueNet (boardToTensor [4,15,15]) and neural MCTS replacing random rollout with PUCT-guided expansion using network priors and leaf value**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-20T04:14:36Z
- **Completed:** 2026-02-20T04:19:24Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- PolicyValueNet: 3-conv shared backbone → dual heads (policy [B,225] log-probs + value [B,1] tanh), registers all sub-modules for gradient tracking
- boardToTensor: 4-channel [4,15,15] float32 encoding — ch0=current player stones, ch1=opponent stones, ch2=last move, ch3=turn indicator (Black=1.0)
- mctsSearchWithNet: PUCT-guided expansion using PolicyValueNet priors and leaf value, Dirichlet noise support for self-play training
- NativeLoader.fs: module-level `do load()` preloads ARM64 macOS dylibs (prevents SIGSEGV)
- All 14 existing tests (8 rule + 6 MCTS) still pass

## Task Commits

Each task was committed atomically:

1. **Task 1: NativeLoader.fs + PolicyValueNet.fs + Gomoku.fsproj** - `4a47a6c` (feat)
2. **Task 2: Update Mcts.fs with neural-guided mctsSearchWithNet** - `24b4aed` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `05-gomoku-mcts/src/Gomoku/NativeLoader.fs` — ARM64 macOS dylib preload, module-level `do load()`
- `05-gomoku-mcts/src/Gomoku/PolicyValueNet.fs` — boardToTensor [4,15,15], PolicyValueNet class with policy()/value()/forwardBoth()
- `05-gomoku-mcts/src/Gomoku/Gomoku.fsproj` — added TorchSharp-cpu 0.106.0, Serilog packages, updated compile order
- `05-gomoku-mcts/src/Gomoku/Mcts.fs` — added mctsSearchWithNet, sampleDirichlet; preserved original mctsSearch unchanged

## Decisions Made

1. **No `open type TorchSharp.torch` at Mcts.fs module level** — The existing `mctsSearch` uses `float`, `sqrt`, `log`, `cos` which are all shadowed by `open type TorchSharp.torch`. Used `open TorchSharp` only (no type open) and fully qualified all torch calls (`torch.NewDisposeScope()`, `torch.nn.functional.log_softmax`).

2. **ReLU()/Tanh() module instances instead of functional relu/tanh** — Phase 4 established this pattern. `nn.functional.relu` might not be accessible in all contexts; module instances are unambiguous and integrate cleanly with `RegisterComponents()`.

3. **Pure F# Dirichlet sampler** — `torch.distributions.Dirichlet` class exists in 0.106.0 but the F# API path (`torch.distributions.Dirichlet`) was uncertain. Implemented Marsaglia-Tsang Gamma sampler in pure F# — deterministic, dependency-free, correct.

4. **mctsSearchWithNet signature includes `rng: System.Random`** — Dirichlet noise at root requires randomness outside tensor scope. Passing rng explicitly maintains functional purity and testability.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed `open type TorchSharp.torch` from Mcts.fs module level**
- **Found during:** Task 2 (first build attempt)
- **Issue:** `open type TorchSharp.torch` shadows `float`, `int`, `sqrt`, `log`, `cos` — caused 24 compiler errors in existing `mctsSearch` code
- **Fix:** Used `open TorchSharp` only; access torch types via fully qualified names (`torch.NewDisposeScope()`, `torch.Tensor`)
- **Files modified:** `05-gomoku-mcts/src/Gomoku/Mcts.fs`
- **Verification:** `dotnet build Gomoku.sln` — 0 errors, 0 warnings
- **Committed in:** `24b4aed` (Task 2 commit)

**2. [Rule 2 - Missing Critical] Implemented Dirichlet noise in pure F# instead of torch.distributions**
- **Found during:** Task 2 (implementation)
- **Issue:** `torch.distributions.Dirichlet` class exists in 0.106.0 but F# API path uncertain; plan said "If not, implement Dirichlet manually"
- **Fix:** Implemented Marsaglia-Tsang Gamma sampler in pure F# (`sampleDirichlet` function)
- **Files modified:** `05-gomoku-mcts/src/Gomoku/Mcts.fs`
- **Verification:** Function produces normalized samples summing to 1.0; correct alpha behavior
- **Committed in:** `24b4aed` (Task 2 commit)

**3. [Rule 2 - Missing Critical] Added `rng: System.Random` parameter to mctsSearchWithNet**
- **Found during:** Task 2 (Dirichlet implementation)
- **Issue:** Dirichlet sampler requires randomness; plan signature didn't include rng but it's needed
- **Fix:** Added `rng: System.Random` parameter to `mctsSearchWithNet` signature
- **Files modified:** `05-gomoku-mcts/src/Gomoku/Mcts.fs`
- **Verification:** Build succeeds; caller provides rng as with `mctsSearch`
- **Committed in:** `24b4aed` (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (1 blocking, 2 missing critical)
**Impact on plan:** All fixes necessary for compilation and correctness. The rng parameter addition is a minor signature adjustment; the shadow avoidance is a known gotcha documented in accumulated decisions.

## Issues Encountered

- TorchSharp open type shadowing is more pervasive than expected — affects the ENTIRE module, not just functions using torch. Future plans should document this as a first-class concern when adding TorchSharp opens to files with existing F# math code.

## User Setup Required

None - no external service configuration required. TorchSharp-cpu 0.106.0 was already in NuGet cache from Phase 4.

## Next Phase Readiness
- PolicyValueNet.forwardBoth() ready for Plan 04 self-play training loop
- mctsSearchWithNet with addDirichletNoise=true ready for training, addDirichletNoise=false for evaluation
- All 14 existing tests still passing — neural additions are additive, no regressions
- Blockers: None

---
*Phase: 05-gomoku-mcts*
*Completed: 2026-02-20*
