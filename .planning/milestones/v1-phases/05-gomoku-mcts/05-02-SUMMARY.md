---
phase: 05-gomoku-mcts
plan: "02"
subsystem: mcts
tags: [fsharp, mcts, alphazero, puct, backpropagation, gomoku, expecto]

# Dependency graph
requires:
  - phase: 05-01
    provides: Domain.fs (GameState, Player, Board), Rules.fs (legalMoves, applyMove, isWinningMove, initialState, opponent)
provides:
  - MctsNode class: mutable MCTS tree node with parent pointer, Dictionary children, mutable prior/visits/totalValue, Q()/Expand/Update/UpdateRecursive/IsLeaf
  - Mcts.fs: PUCT selection (Q + c_puct * P * sqrt(N_parent) / (1 + N_child)), random rollout, mctsSearch (SELECT→EXPAND→ROLLOUT→BACKPROP), bestMove
  - GMOK-09: UpdateRecursive perspective-flip unit test (3-level chain: leaf=-1.0, parent=+1.0, grandparent=-1.0)
  - GMOK-10: pure MCTS 50 simulations achieves 100% win rate vs random in 50-game benchmark (exceeds >80% requirement)
affects:
  - 05-03 (AlphaZero MCTS with neural network policy/value — extends MctsNode.Prior for Dirichlet noise, replaces rollout with NN value)
  - 05-04 (SelfPlay + Training — uses mctsSearch for data generation)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "MctsNode as mutable class (not record): enables parent pointer and Dictionary<int,MctsNode> children dict"
    - "UpdateRecursive(-leafValue) convention: call with negated value at leaf; recursive negation = perspective flip at each tree level"
    - "PUCT exploration: Q(s,a) + c_puct * P(s,a) * sqrt(N_parent) / (1 + N_child)"
    - "Pure MCTS rollout: uniform priors (1/n), random play to terminal, no neural network"
    - "MctsNode.Prior mutable: prepared for Dirichlet noise injection at root (Plan 03)"

key-files:
  created:
    - 05-gomoku-mcts/src/Gomoku/MctsNode.fs
    - 05-gomoku-mcts/src/Gomoku/Mcts.fs
    - 05-gomoku-mcts/tests/Gomoku.Tests/MctsTests.fs
  modified:
    - 05-gomoku-mcts/src/Gomoku/Gomoku.fsproj
    - 05-gomoku-mcts/tests/Gomoku.Tests/Gomoku.Tests.fsproj

key-decisions:
  - "MctsNode.Prior is mutable field (prior_) exposed via get/set property — enables Dirichlet noise application in Plan 03 without redesign"
  - "UpdateRecursive(-leafValue) call convention: caller negates at leaf, recursive calls negate at each ancestor — alternating sign = perspective flip"
  - "leafValue for terminal node in BACKPROP = -1.0 (not +1.0): the player who reached a terminal state is the CURRENT player who did NOT make the winning move, so it's bad for them"
  - "rollout uses startPlayer tracking (not s.CurrentPlayer) to assign result from the expanded node's perspective correctly"
  - "No TorchSharp in this plan — pure MCTS only; neural network integration deferred to Plan 03"

patterns-established:
  - "MctsNode class pattern: mutable class with private mutable backing fields, public properties, and methods (not F# record)"
  - "PUCT selection: Seq.maxBy over Dictionary KeyValuePairs, extract (key, value) tuple"
  - "MCTS loop: for _ in 1..nSimulations with mutable state/node/isTerminal inside"

# Metrics
duration: 2min
completed: 2026-02-20
---

# Phase 5 Plan 02: Pure MCTS with PUCT Selection and Random Rollout Summary

**MctsNode mutable class + Mcts.fs PUCT search achieve 100% win rate vs random (50/50 games) with uniform priors and random rollout in 4.4 seconds**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-20T04:08:35Z
- **Completed:** 2026-02-20T04:10:31Z
- **Tasks:** 2
- **Files modified:** 5 (2 created src, 1 created test, 2 updated fsproj)

## Accomplishments
- MctsNode mutable class with parent pointer, Dictionary children, mutable prior/visits/totalValue — ready for AlphaZero Dirichlet noise extension
- PUCT selection formula (Q + c_puct * P * sqrt(N_parent) / (1 + N_child)) implemented in Mcts.fs with clean SELECT→EXPAND→ROLLOUT→BACKPROP loop
- GMOK-09 perspective-flip test: verified UpdateRecursive(-1.0) produces leaf=-1.0, parent=+1.0, grandparent=-1.0 exactly
- GMOK-10 win rate benchmark: pure MCTS 50 simulations achieves 50/50 (100%) vs random opponent (far exceeds >80% requirement)
- Total test suite: 14/14 passing (8 RulesTests + 5 MctsNode unit tests + 1 win rate benchmark)

## Task Commits

Each task was committed atomically:

1. **Task 1: MctsNode.fs + Mcts.fs (pure MCTS with random rollout)** - `2fbeb18` (feat)
2. **Task 2: MctsTests.fs — backpropagation test + win rate benchmark** - `ffce181` (test)

**Plan metadata:** (this commit)

## Files Created/Modified
- `05-gomoku-mcts/src/Gomoku/MctsNode.fs` - Mutable MctsNode class: parent pointer, Dictionary children, mutable prior/visits/totalValue, Q()/Expand/Update/UpdateRecursive/IsLeaf
- `05-gomoku-mcts/src/Gomoku/Mcts.fs` - PUCT selection, random rollout, mctsSearch (SELECT→EXPAND→ROLLOUT→BACKPROP), bestMove
- `05-gomoku-mcts/src/Gomoku/Gomoku.fsproj` - Added MctsNode.fs and Mcts.fs in compile order after Rules.fs
- `05-gomoku-mcts/tests/Gomoku.Tests/MctsTests.fs` - GMOK-09 backprop perspective-flip (5 unit tests) + GMOK-10 win rate benchmark (1 test)
- `05-gomoku-mcts/tests/Gomoku.Tests/Gomoku.Tests.fsproj` - Added MctsTests.fs before RulesTests.fs

## Decisions Made
- MctsNode.Prior is a mutable field (private `prior_` backing, public get/set property) — enables Dirichlet noise injection at root in Plan 03 without class redesign
- UpdateRecursive(-leafValue) call convention: caller negates at leaf, recursive calls negate at each ancestor — alternating sign achieves perspective flip for zero-sum game
- leafValue for terminal node = -1.0 (not +1.0): current player at terminal node is the one who did NOT make the winning move, so it's bad for them
- rollout tracks startPlayer to assign result from the expanded node's perspective (handles the asymmetry between "who plays next" and "who we're evaluating for")
- No TorchSharp in this plan — neural network integration deferred to Plan 03 (de-risks GMOK-10 by proving pure MCTS works)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. Build and tests passed on first attempt. MCTS achieved 100% win rate (50/50) vs random, substantially exceeding the >80% benchmark requirement.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- MctsNode and Mcts.fs are ready for Plan 03 extension: replace uniform priors with PolicyValueNet output, add Dirichlet noise at root via MctsNode.Prior setter, add temperature-based move selection
- MctsNode.Prior is already mutable — Plan 03 can apply Dirichlet noise without class redesign
- rollout function will be replaced by neural network value prediction in Plan 03
- No blockers for Plan 03 (neural network + self-play)

---
*Phase: 05-gomoku-mcts*
*Completed: 2026-02-20*
