# Project Research Summary

**Project:** F# Reinforcement Learning Tutorial (Korean, Console + mdBook)
**Domain:** Educational tutorial — progressive RL game-playing agents in F#
**Researched:** 2026-02-19
**Confidence:** HIGH

## Executive Summary

This project is a Korean-language, F#-native reinforcement learning tutorial delivered as an mdBook documentation site paired with five independent, runnable console solutions. The recommended pattern — established confidently across all research dimensions — is a strict separation between a pure functional game engine (types and rules) and an impure training shell (episode orchestration, logging, output), with each of the five phases building conceptually on the previous one without creating cross-phase code dependencies. The target audience is experienced F# practitioners who need no language hand-holding; the differentiating value is idiomatic F# RL code (immutable agent state, discriminated unions for game results, FsCheck property tests for game rules) combined with convergence tests that actually verify the agent learns.

The recommended technical approach uses .NET 9 / F# 9 throughout, with Expecto + FsCheck for testing, Serilog for structured training-loop logging, and Spectre.Console for readable terminal output. TorchSharp-cpu (0.105.2) is introduced only at Phase 4 (DQN) and Phase 5 (MCTS + Policy/Value Network). Phases 1–3 have zero neural-network dependencies, keeping the early phases accessible and fast to compile. The mdBook site uses `{{#include}}` directives to embed real, runnable code from the phase directories rather than hypothetical snippets — this is critical for tutorial credibility. Each phase is an independent `.sln` with no project references crossing phase boundaries (copy-and-evolve, not shared library).

The critical risks are concentrated in Phases 4–5: TorchSharp tensor memory leaks (no DisposeScope), DQN terminal-state target bugs (bootstrapping from done states), and MCTS backpropagation perspective bugs (value not negated at each level). All three have silent failure modes where training appears to run but the agent does not learn. The mitigation is to write Expecto regression tests for each before integration. An additional risk applies to Phases 2–3: sparse reward convergence failure for longer games — this is manageable with minimal reward shaping documented explicitly in the tutorial prose.

## Key Findings

### Recommended Stack

The stack is lean and well-verified. .NET 9 / F# 9 is the right runtime (stable STS, forward-compatible with TorchSharp, provides F# 9 language features). TorchSharp-cpu 0.105.2 is the CPU convenience package for Phases 4–5; using the convenience package avoids version-mismatch errors between TorchSharp core and libtorch native binaries. All testing uses Expecto 10.2.3 + FsCheck 3.3.2 with matching major.minor versions — mixing versions causes test discovery failures. mdBook 0.4.52 is the documentation engine; it requires no JavaScript pipeline and deploys to GitHub Pages as a static site.

**Core technologies:**
- **.NET 9 / F# 9**: Runtime and primary language — stable, TorchSharp-compatible, F# 9 features
- **Expecto 10.2.3 + FsCheck 3.3.2**: F#-native test runner with property-based testing — mandatory per project constraints
- **Serilog 4.3.1**: Structured logging for training loops — tracks episode counts, reward curves, epsilon decay
- **Spectre.Console 0.54.0**: Rich console output — progress bars and tables make 10K-episode training loops readable
- **TorchSharp-cpu 0.105.2**: PyTorch .NET bindings for DQN and MCTS phases — introduces at Phase 4 only
- **mdBook 0.4.52**: Tutorial documentation site — Rust binary, no Node.js, GitHub Pages-ready

**Critical version constraint:** Expecto and Expecto.FsCheck must share the same major.minor (both 10.2.3). TorchSharp-cpu 0.105.2 bundles libtorch internally — do not add a separate libtorch-cpu reference. On Apple Silicon, verify ARM64 native support before Phase 4 starts.

See `.planning/research/STACK.md` for full version compatibility matrix and installation commands.

### Expected Features

**Must have (table stakes):**
- Progressive difficulty curve: Bandit → TicTacToe → Connect Four → DQN → MCTS, each phase building on the previous
- Working, runnable F# console app per phase — tutorial credibility depends on compilable code
- RL concept + math explanation per chapter in Korean — wrong language = wrong audience
- Expecto unit tests per phase — F# community expects this
- FsCheck property-based tests for game rules — natural fit for board game invariants
- Serilog structured logging in every training loop — makes RL training observable
- Option/Result error handling throughout — no exceptions; stated constraint
- Phase-bridging "Why this matters" sections — explains why each next technique is needed

**Should have (competitive differentiators):**
- F# functional type system as first-class RL modeling tool — immutable records for state, pure functions for transitions
- Convergence tests that verify the agent actually learns (Expecto: win rate > threshold after N episodes)
- Dual-agent comparison output within each phase (e.g., epsilon-greedy vs UCB1, Q-Learning vs Minimax)
- Math formula → F# function correspondence in each chapter
- mdBook `{{#include}}` directives to embed real code from phase directories
- Self-contained `.sln` per phase — any phase openable independently

**Defer (v2+):**
- ASCII training curve visualization — nice-to-have after Phase 1 validation
- GPU/CUDA support — tutorial-scale DQN runs fine on CPU; document expected CPU training times
- Persistent model storage — appropriate in Phase 4–5 only (.pt via TorchSharp)
- Web UI / Fable / SignalR — explicitly out of scope per PROJECT.md (Phase 5 is console MCTS)

**Anti-features to explicitly exclude:** F# language basics (audience are experts), Gym integration (F#-only constraint), hyperparameter sweep tooling (scope creep), Renzu Gomoku rules (PROJECT.md exclusion), RLHF (entirely wrong domain).

See `.planning/research/FEATURES.md` for full prioritization matrix and MVP definition.

### Architecture Approach

The architecture has two parallel layers: a **documentation layer** (mdBook site at `book/`) and a **code layer** (five independent F# solutions at `phases/`). These are siblings in the repository root. Within each phase, the module ordering follows F#'s top-to-bottom compilation requirement: `Domain.fs` (types only) → `Rules.fs` (pure game logic) → `Agent.fs` (RL policy and update) → `Training.fs` (episode loop, impure) → `Logging.fs` (Serilog setup) → `Program.fs` (entry point). The test project per phase references the source project and exercises both game rules (FsCheck properties) and learning convergence (Expecto integration tests). The fundamental rule is that pure game logic never touches I/O — `Rules.fs` has no Serilog calls, no printfn, no random number generation.

**Major components:**
1. **mdBook site** — tutorial narrative, math explanations, Korean prose, references phase code via `{{#include}}`
2. **Game Engine (Domain.fs + Rules.fs per phase)** — board state, legal moves, win detection; pure functions only
3. **RL Agent (Agent.fs per phase)** — policy function, value/Q-table or neural network, learning update
4. **Training Loop (Training.fs per phase)** — episode orchestration, impure shell, calls pure modules
5. **Test project (per phase)** — FsCheck game rule properties + Expecto convergence tests

**Key patterns:**
- Pattern 1: Pure game engine + impure shell (foundational to all phases)
- Pattern 2: Agent state as immutable record, updated via return value (Phases 1–3; partial in Phase 4 due to TorchSharp)
- Pattern 3: Agent as function type `Policy = GameState -> int` — composable, swappable for evaluation
- Pattern 4: Result/Option at the input boundary; assume valid inside pure logic
- Pattern 5: Training loop as observable pipeline with Serilog structured events

**Cross-phase code strategy:** Copy-and-evolve, not shared library. Phase 4 copies Phase 3's `Domain.fs` and `Rules.fs` into its own project. This preserves phase independence and makes type evolution visible to the reader.

See `.planning/research/ARCHITECTURE.md` for detailed file layouts per phase and anti-pattern list.

### Critical Pitfalls

1. **TorchSharp tensor memory leak (no DisposeScope)** — Wrap every DQN/MCTS training step with `use _ = torch.NewDisposeScope()`. Symptoms: memory grows steadily, process crashes after extended training. Prevention: add an integration test that runs 1,000 steps and asserts process memory stays flat. Affects Phases 4–5.

2. **MCTS backpropagation perspective bug (value not negated)** — In two-player MCTS, negate the value at each backprop step (`backpropagate parent (-value)`). Symptoms: AI plays as if trying to lose; higher simulation count makes performance worse. This is the hardest bug to detect because training completes without errors. Write a unit test before integrating with the neural network. Affects Phase 5.

3. **DQN terminal state target bug (bootstrapping from done state)** — Apply a done mask in target computation: `reward + gamma * maxNextQ * (if done then 0.0f else 1.0f)`. Symptoms: Q-values plateau below expected win rate, loss oscillates without converging. Verify with an Expecto test on a terminal batch before writing the full training loop. Affects Phase 4.

4. **Sparse reward convergence failure** — Pure win/loss terminal rewards are insufficient for Connect Four (42-move game with sparse feedback). Add minimal intermediate reward shaping (+0.5 for 3-in-a-row threat). Document the shaping explicitly in tutorial prose as a deliberate design choice. Phase 2 (TicTacToe) is short enough that terminal rewards work. Affects Phase 3 primarily.

5. **F# Map as Q-table at scale** — F# `Map<Board, float>` is correct for Phase 2 (5K TicTacToe states) but causes GC pressure and 4× slowdown at Phase 3 scale. Replace with `Dictionary<string, float>` wrapped behind a `QTable` module before Phase 3 training. The abstraction boundary makes it a simple swap. Affects Phase 3.

See `.planning/research/PITFALLS.md` for recovery strategies, integration gotchas (Serilog flush, FsCheck seed reproducibility, optimizer.zero_grad), and the "looks done but isn't" checklist.

## Implications for Roadmap

Based on the combined research, the project maps naturally to five implementation phases that mirror the tutorial's conceptual phases. Each roadmap phase should produce both working code AND the corresponding mdBook chapter content — shipping them together prevents the tutorial from falling behind the code.

### Phase 1: Foundation — Multi-Armed Bandit + mdBook Scaffold

**Rationale:** Lowest complexity, no game board, no opponent. Establishes the entire project structure: mdBook site scaffold (all five phases stubbed), the pure-engine/impure-shell pattern, Expecto + FsCheck test harness, Serilog training loop logging, and Spectre.Console output. Getting all tooling working on the simplest problem prevents debugging toolchain issues during conceptually complex later phases.

**Delivers:** Working mdBook site with SUMMARY.md + placeholder chapters for all 5 phases; Phase 1 Bandit console app (epsilon-greedy + UCB1, 1000 episodes, Spectre.Console comparison output); Expecto tests; FsCheck property (counts sum = episodes); Serilog training log.

**Addresses features:** Progressive phase structure, working console app per phase, Korean chapter content (Phase 1), Expecto tests, FsCheck properties, Serilog logging, Option/Result patterns, self-contained `.sln`.

**Avoids:** No pitfalls are critical in Phase 1 — it has no game board, no Q-table, no neural network. Main risk is establishing incorrect conventions early (missing Serilog flush, mutable agent state) that propagate to later phases.

**Research flag:** Well-documented patterns. Skip `/gsd:research-phase`.

### Phase 2: TicTacToe — MDP + TD Learning + Game Engine Pattern

**Rationale:** Introduces the game engine pattern (Domain.fs + Rules.fs + Agent.fs) that all subsequent phases inherit. TicTacToe is small enough (5,478 states) that F# `Map<Board, float>` is performant and a `ValueTable` fits in memory. Self-play convergence is achievable in 10K episodes, making convergence tests fast. This phase validates the full tutorial format (concept → math → F# types → algorithm → tests → chapter).

**Delivers:** TicTacToe console app (TD(0) self-play, epsilon-greedy policy, value table); FsCheck properties (valid board invariant, no-move-after-terminal, legal-moves non-empty pre-terminal); Expecto convergence test (TD agent wins >85% vs random after 5K episodes); human-vs-AI console mode; Phase 2 mdBook chapter.

**Addresses features:** MDP state modeling, TD update, convergence verification, dual-agent comparison (TD vs random).

**Avoids:** Mutable agent state anti-pattern (establish immutable AgentState record pattern here). Sparse reward is not an issue for TicTacToe — document it here as a preview of Phase 3's challenge.

**Research flag:** Well-documented patterns. Skip `/gsd:research-phase`.

### Phase 3: Connect Four — Q-Learning + Minimax + Alpha-Beta

**Rationale:** Introduces 2D board state, column-gravity rules, and the state-space explosion that motivates Phase 4's neural network. The key architectural addition is the `QTable` module using `Dictionary<string, float>` — this must be designed as a clean abstraction before writing the training loop so Phase 4 can swap it for a neural network without touching training logic. Minimax with alpha-beta is implemented alongside Q-Learning for explicit comparison.

**Delivers:** Connect Four console app (Q-Learning + Minimax depth 6 + alpha-beta); `QTable` module with Dictionary backend; Minimax vs Q-Learning head-to-head console output with alpha-beta pruning stats; FsCheck properties (gravity invariant, 4-in-a-row detection, column-full handling); Phase 3 mdBook chapter with "Why Q-Table breaks here" section that motivates DQN.

**Addresses features:** Feature-based Q-Learning, Minimax + alpha-beta, dual-agent comparison, state-space limitation narrative.

**Avoids:** F# Map Q-table performance pitfall (use Dictionary from the start); sparse reward convergence failure (add minimal reward shaping for 3-in-a-row, document it explicitly).

**Research flag:** Minimax evaluation heuristic design may need domain-specific research during planning. Consider `/gsd:research-phase` for the alpha-beta scoring function.

### Phase 4: DQN Connect Four — Neural Q-Network with TorchSharp

**Rationale:** First phase introducing TorchSharp. The game engine is copied from Phase 3 (maintaining phase independence), then extended with tensor conversion (`boardToTensor`), a Conv2D DQN model, experience replay buffer, and target network. The pitfall surface is highest here: tensor memory leaks, done-mask bugs, gradient zeroing. All three require Expecto regression tests written before the full training loop.

**Delivers:** DQN Connect Four console app (Conv2D policy net, target network, CircularBuffer replay, epsilon decay); `boardToTensor` conversion with Expecto shape tests; `ReplayBuffer` with Expecto capacity tests; Expecto test for done-mask correctness; DQN vs Phase 3 Minimax win-rate benchmark after 50K episodes; model checkpoint save/load (.pt); Phase 4 mdBook chapter.

**Addresses features:** TorchSharp DQN training, experience replay, target network, model persistence.

**Avoids:** Tensor memory leak (DisposeScope wrapping every train step); DQN terminal state target bug (done-mask Expecto test); gradient not zeroed (training step helper function enforcing zero_grad → forward → loss → backward → step order); Apple Silicon ARM64 TorchSharp compatibility verified before this phase starts.

**Research flag:** TorchSharp F# API patterns for Conv2D + Sequential in F# 9 may need validation during planning. Consider `/gsd:research-phase` for TorchSharp-specific F# API surface.

### Phase 5: Gomoku MCTS — Monte Carlo Tree Search + Policy/Value Network

**Rationale:** Highest complexity phase. Research recommends building in sub-phases: (1) Gomoku game engine + MCTS in console mode first, (2) add Policy/Value neural network, (3) self-play training pipeline. This order lets each component be verified independently before integration. The MCTS backpropagation perspective bug is the highest-recovery-cost pitfall in the entire project — a unit test must be written and pass before neural net integration.

**Delivers:** Gomoku console app (15×15 board, 5-in-a-row win detection); MCTS tree (MctsNode, PUCT selection, expand, simulate, backpropagate); Expecto unit test for backprop sign correctness; Policy + Value network (shared trunk, dual heads); self-play data generation pipeline; MCTS simulation count parameter (no uncapped loops); Expecto: MCTS with 400 simulations beats random Gomoku >80%; Phase 5 mdBook chapter.

**Addresses features:** MCTS algorithm, PUCT selection, self-play pipeline, Policy/Value network design.

**Avoids:** MCTS perspective bug (unit test before neural net integration); strategy cycling (evaluate vs random baseline every 500 self-play iterations, assert no regression below 95%); uncapped MCTS simulation time (add simulation count or time-budget parameter from the start).

**Research flag:** AlphaZero-style self-play pipeline design in F# has sparse documentation. `/gsd:research-phase` is recommended for Phase 5 planning, specifically for MCTS + PolicyValueNet integration patterns and self-play data pipeline design.

### Phase Ordering Rationale

- **Dependency chain:** Each phase's game engine builds on the previous conceptually (Bandit → stateless; TicTacToe → small state space; Connect Four → large state space + Minimax; DQN → neural generalization; MCTS → tree search + neural guidance). The tutorial's own narrative structure mandates this order.
- **Pitfall surface concentration:** The most dangerous pitfalls (TorchSharp leaks, MCTS perspective bug, DQN done-mask) are isolated to Phases 4–5. Getting Phases 1–3 solid first means the codebase conventions (pure engine, immutable agent state, Expecto convergence tests) are battle-tested before the complexity spike.
- **Toolchain validation:** Establishing the complete toolchain (mdBook, Expecto, FsCheck, Serilog, Spectre.Console) in Phase 1 means no new tooling is introduced after Phase 3 except TorchSharp — which is introduced in one go in Phase 4.
- **Content and code together:** Each phase should ship both the working console app and the corresponding mdBook chapter. Splitting them creates tutorial debt.

### Research Flags

**Phases needing deeper research during planning:**
- **Phase 3:** Alpha-beta evaluation heuristic design for Connect Four — the scoring function significantly impacts Minimax quality and Q-Learning comparison fairness. Moderate documentation exists but F#-specific examples are sparse.
- **Phase 4:** TorchSharp Conv2D + Sequential API in F# 9 — the F# API surface for model definition differs from C# examples in the TorchSharp docs. Worth a targeted research pass before writing `Model.fs`.
- **Phase 5:** AlphaZero-style self-play pipeline and MCTS integration with a dual-head network in F# — this combination has very sparse documentation in F#. High complexity, novel implementation territory.

**Phases with standard patterns (skip research-phase):**
- **Phase 1:** Multi-armed bandit epsilon-greedy and UCB1 are textbook algorithms; mdBook setup is well-documented.
- **Phase 2:** TD(0) learning for TicTacToe is thoroughly documented; F# `Map` value table is standard.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All package versions verified on NuGet; TorchSharp-cpu convenience package behavior confirmed via GitHub wiki and release notes; version compatibility matrix cross-checked |
| Features | HIGH | Feature set derived from PROJECT.md constraints plus authoritative RL tutorial references (HuggingFace Deep RL Course, F# for Fun and Profit); anti-features grounded in explicit PROJECT.md out-of-scope list |
| Architecture | HIGH | Per-phase .sln layout is standard .NET practice; pure engine / impure shell is established F# functional pattern; mdBook `{{#include}}` is official mdBook feature; TorchSharp DQN data flow mirrors PyTorch official tutorial |
| Pitfalls | HIGH | TorchSharp memory management sourced from official TorchSharp wiki; MCTS backprop bug sourced from JAIR survey; DQN target bug from PyTorch DQN tutorial; performance traps from F# high-performance code reference |

**Overall confidence:** HIGH

### Gaps to Address

- **TorchSharp ARM64 native support on Apple Silicon:** As of TorchSharp 0.106, the -cpu convenience package may not include ARM64 natives. Verify this before Phase 4 planning; if ARM64 support is absent, document the Rosetta 2 fallback or the manual `TorchSharp` + `libtorch-cpu` with `osx-arm64` RID approach.
- **Phase 5 web layer scope:** Architecture research includes Giraffe + Fable + SignalR in the Phase 5 structure. However, PROJECT.md explicitly states web is out of scope and Phase 5 is console MCTS only. Resolve this contradiction at requirements definition: either Phase 5 is console-only (simpler) or a web layer is in scope (significant additional complexity). The roadmap should not include the Giraffe/Fable components unless this is confirmed in scope.
- **Convergence test episode counts:** Research notes that convergence tests can be slow. The recommended episode counts for passing tests (5K–10K episodes per test) need benchmarking against actual training speed before being locked into CI. These may need a separate "slow tests" category in Expecto from the start.
- **mdBook syntax highlighting for F#:** Use `` ```fsharp `` not `` ```fs `` in code fences. Verify that the chosen mdBook theme renders F# highlighting correctly in `book.toml` before Phase 1 content is written.

## Sources

### Primary (HIGH confidence)
- [NuGet Gallery — TorchSharp 0.106.0](https://www.nuget.org/packages/TorchSharp/) — version, .NET targets
- [NuGet Gallery — TorchSharp-cpu 0.105.2](https://www.nuget.org/packages/TorchSharp-cpu/) — CPU convenience package behavior
- [GitHub — dotnet/TorchSharp](https://github.com/dotnet/TorchSharp) — F# API, memory management wiki
- [NuGet Gallery — Expecto 10.2.3](https://www.nuget.org/packages/Expecto/) — current stable, FsCheck integration
- [NuGet Gallery — FsCheck 3.3.2](https://www.nuget.org/packages/FsCheck) — current stable
- [NuGet Gallery — Serilog 4.3.1](https://www.nuget.org/packages/serilog/) — current stable, sink compatibility
- [mdBook Documentation](https://rust-lang.github.io/mdBook/) — SUMMARY.md format, `{{#include}}`, syntax highlighting
- [F# for Fun and Profit — Property-based testing](https://fsharpforfunandprofit.com/posts/property-based-testing-2/) — FsCheck patterns for F#
- [F# for Fun and Profit — Railway Oriented Programming](https://fsharpforfunandprofit.com/posts/recipe-part2/) — Result/Option at boundaries
- [PyTorch DQN Tutorial](https://docs.pytorch.org/tutorials/intermediate/reinforcement_q_learning.html) — Experience replay + target network pattern (TorchSharp mirrors this API)
- [TorchSharp Memory Management Wiki](https://github.com/dotnet/TorchSharp/wiki/Memory-Management) — DisposeScope, Adam optimizer caveat
- [MCTS Survey — JAIR 2017](https://www.jair.org/index.php/jair/article/download/11099/26289/20632) — backpropagation correctness, UCB1 in two-player games

### Secondary (MEDIUM confidence)
- [Hugging Face Deep RL Course](https://huggingface.co/learn/deep-rl-course/en/unit0/introduction) — chapter structure pattern, self-play design
- [Deep RL and MCTS with Connect 4 — Medium/TDS](https://medium.com/data-science/deep-reinforcement-learning-and-monte-carlo-tree-search-with-connect-4-ba22a4713e7a) — DQN + MCTS game pipeline example
- [Writing High Performance F# Code — Bartosz Sypytkowski](https://www.bartoszsypytkowski.com/writing-high-performance-f-code/) — F# Map vs Dictionary, array vs list in hot loops
- [F# game development patterns — softwarepatternslexicon.com](https://softwarepatternslexicon.com/f-sharp/case-studies/game-development-with-f/) — pure engine pattern examples

### Tertiary (LOW confidence, needs validation)
- [Deep RL Doesn't Work Yet — Alex Irpan](https://www.alexirpan.com/2018/02/14/rl-hard.html) — convergence failure modes; dated (2018) but conceptually still valid for tutorial-scale DQN

---
*Research completed: 2026-02-19*
*Ready for roadmap: yes*
