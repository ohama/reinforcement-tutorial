# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-19)

**Core value:** 각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.
**Current focus:** Phase 5 IN PROGRESS — 05-01 done. Next: 05-02 (MCTS).

## Current Position

Phase: 5 of 5 IN PROGRESS (Gomoku MCTS / AlphaZero-style)
Plan: 1 of 3 in Phase 5 complete (05-01 done)
Status: In progress

Last activity: 2026-02-20 — Completed 05-01-PLAN.md (Gomoku.sln bootstrap + Domain.fs + Rules.fs + 8/8 FsCheck/Expecto tests passing)

Progress: [███████████████] 100% (15/15 plans total — Phase 5 base plan count TBD)

## Performance Metrics

**Velocity:**
- Total plans completed: 14
- Average duration: ~2.8 min (non-DQN) / ~8 min (DQN)
- Total execution time: ~69 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-bandit-mdbook | 3/3 COMPLETE | ~11 min | 3.7 min |
| 02-tictactoe-td-learning | 3/3 COMPLETE | ~7 min | 2.3 min |
| 03-connect-four-q-learning-minimax | 4/4 COMPLETE | ~8 min | 2.0 min |
| 04-connect-four-dqn | 4/4 COMPLETE | ~43 min | 10.8 min |
| 05-gomoku-mcts | 1/3 | ~2 min | 2 min |

**Recent Trend:**
- Last 5 plans: 04-02 (6 min), 04-03 (26 min), 04-04 (~8 min), 05-01 (2 min)

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Setup]: mdBook for tutorial site — 정적 사이트 생성, Markdown 기반
- [Setup]: Phase별 독립 solution — 각 Phase가 자체 완결적, copy-and-evolve (no shared library)
- [Setup]: TorchSharp for neural nets — Phase 4, 5에서만 도입 (Phases 1-3은 zero NN dependency)
- [Setup]: Console only — RL 학습에 집중, Web/GUI 없음
- [01-01]: net10.0 used (system has .NET 10 SDK only, not net9.0) — all future phases use net10.0
- [01-01]: Traditional .sln format required — .NET 10 defaults to .slnx; must use `dotnet new sln --format sln` to get .sln
- [01-01]: mdBook v0.5.2 installed via Homebrew (research specified 0.4.52) — backward-compatible
- [01-02]: selectArm signature in runEpisode is `AgentState -> Arm` (rng captured in closure by caller) — idiomatic F#, avoids threading rng through signature
- [01-02]: Local mutable totalPulls in runEpisodeUcb1 is acceptable — function-local, not exposed externally
- [01-02]: compareEpsilons creates child RNG per epsilon via rng.Next() seed — statistical independence with determinism
- [01-03]: FsCheck 2.16.5 required (not 3.x) — StdGen removed in FsCheck 3.x causes TypeLoadException with Expecto.FsCheck 10.2.3; NU1608 warning is NOT benign
- [01-03]: YoloDev.Expecto.TestSdk 0.15.5 + [<Tests>] attribute + GenerateProgramFile=false required for dotnet test to discover Expecto tests
- [01-03]: dotnet test with Expecto = Microsoft.NET.Test.Sdk + YoloDev.Expecto.TestSdk + [<Tests>] on let bindings + GenerateProgramFile=false
- [02-01]: dotnet new sln --format sln required — confirmed .NET 10 creates .slnx by default; `--format sln` flag is the fix
- [02-01]: Array.create 9 Empty required for Board init — Array.zeroCreate returns null for DU types; documented in Domain.fs
- [02-01]: ValueTable = Map<Board, float> — board state to X win probability [0.0, 1.0]; pure functional approach
- [02-02]: prevBoard tracks current player's previous board for TD backup (not opponent's board)
- [02-02]: Both X and O share same ValueTable in self-play — X maximizes V, O minimizes V
- [02-02]: winRateVsRandom hardcodes epsilon=0 — evaluation is always greedy by design
- [02-02]: Main.fs needs fully qualified module path: TicTacToe.Tests.ConvergenceTests.convergenceTests (F# scoping rule)
- [02-03]: Program.fs is sole impure file — Training.fs returns history list; Program.fs iterates and logs via Serilog
- [02-03]: runHumanVsAI uses recursive F# loop (not while loop) — consistent with codebase style
- [02-03]: AI plays epsilon=0 in human-vs-AI mode — user faces optimally trained policy, not random noise
- [03-01]: Connect Four Board = flat 42-element array, row-major, row 0 = top, row 5 = bottom (gravity anchor)
- [03-01]: dropRow iterates rows-1 downto 0 — gravity: first empty found from bottom = landing row
- [03-01]: genValidBoard plays 0-30 random plies via System.Random to produce realistic board states for FsCheck
- [03-01]: Array.create (rows*cols) Empty for Board init (not Array.zeroCreate — returns null for DU types)
- [03-02]: NegInf/PosInf = +-1_000_000 (not Int32.MinValue/MaxValue) — negating MinValue overflows in .NET
- [03-02]: chooseMoveNaive inside Minimax.fs as reference for equivalence testing (not separate module)
- [03-02]: Blocking test uses 1-sided threat (Yellow cols 0,1,2) — double-sided threat is unblockable so only col 3 blocks
- [03-03]: QAgent.fs placed after Rules.fs and before Minimax.fs — QAgent has no dependency on Minimax
- [03-03]: Separate Q-tables per player (redTable, yellowTable) — avoids perspective confusion in self-play
- [03-03]: encodeState: Empty->'.', Red->'R', Yellow->'Y' — 42-char string key from flat Board array
- [03-03]: RewardDraw=0.3, RewardStep=0.0 — terminal-only rewards, no intermediate shaping
- [03-04]: Program.fs is sole impure file for Phase 3 — Training.fs returns result; Program.fs logs via Serilog
- [03-04]: AI vs AI depth=6 for matchup; Human vs Minimax depth=7 for player challenge
- [03-04]: Q-table covers only 0.000004% of 4.5T possible states — explicit DQN motivation
- [03-04]: Korean mdBook {{#include}} path pattern: ../../../03-connect-four/src/ConnectFour/...
- [04-01]: NativeLoader uses module-level `do load()` — dylibs preload before any TorchSharp call, prevents SIGSEGV on ARM64 macOS
- [04-01]: boardToTensor [3,6,7] encoding: ch0=myPiece, ch1=oppPiece, ch2=empty — each cell contributes 1.0f (sum invariant = 42.0f)
- [04-01]: genValidBoard in tests uses System.Random(42) fixed seed + isGameOver: GameResult option check — consistent with Phase 3 API
- [04-01]: torch.NewDisposeScope() in every test — deterministic tensor memory management, no leaks
- [04-01]: TorchSharp-cpu 0.106.0 confirmed working on Apple Silicon ARM64 macOS with NativeLoader pattern
- [04-02]: open type TorchSharp.torch shadows F# int/int64 conversion functions — use Operators.int and Operators.int64 throughout DQNAgent
- [04-02]: Tensor.max(dim) returns struct ValueTuple<Tensor,Tensor> in TorchSharp 0.106.0 — use let struct(vals, _) = t.max(1L); .values property does NOT exist
- [04-02]: index_fill_(0L, idxTensor, Scalar) for in-place illegal move masking — qVec.[int64 col] <- fails to compile due to shadowing
- [04-02]: Experience stores float32[] not Tensor fields — tensors outside NewDisposeScope cause memory leaks in 50K episode loops
- [04-03]: torch.no_grad() in F# requires explicit .Dispose() before gradient computation — `use _noGrad` spans the entire function scope, disabling autograd for loss.backward() if placed mid-function
- [04-03]: DQN alternating-player transition design: Red experience = (s_red, a_red, r, s_after_yellow, done) — stores board AFTER Yellow responds as nextState so Red receives terminal rewards when Yellow wins
- [04-03]: torch.manual_seed(42L) before DQNModel() for deterministic weight init in CI tests
- [04-03]: DQN CI benchmark: test vs random opponent (not Minimax) when training distribution is purely random (< 20K episode curriculum threshold). Production 50K uses full curriculum → test vs Minimax depth 2 (run via dotnet run)
- [04-03]: float64 is not a valid F# type (use float); open type TorchSharp.torch also shadows float32 — use Operators.float32 for numeric conversion
- [04-04]: isGameOver returns GameResult option (RedWins/YellowWins/Draw/None) — no winner() function exists in Rules.fs; all game loop termination must match isGameOver pattern
- [04-04]: Serilog packages available transitively in Console project via ProjectReference to ConnectFourDQN.fsproj — no direct package reference needed in Console.fsproj
- [05-01]: Board = int array (0=empty, 1=Black, -1=White) — flat 225-element row-major array; Array.zeroCreate safe for int arrays (unsafe only for DU arrays)
- [05-01]: isWinningMove scans 4 directions through placed stone only — O(WinLength) not O(225), critical for MCTS simulation speed
- [05-01]: playRandomGame FsCheck helper: plays random plies stopping on win/draw to generate varied board states for property tests

### Pending Todos

None — Phase 5 in progress. Next: 05-02 (MCTS implementation).

### Blockers/Concerns

None — 05-01 complete cleanly, no blockers for 05-02.

## Session Continuity

Last session: 2026-02-20
Stopped at: Completed 05-01-PLAN.md — Gomoku.sln bootstrap + Domain.fs + Rules.fs + 8/8 tests passing
Resume file: None
