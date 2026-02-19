# Feature Research

**Domain:** F# Reinforcement Learning Tutorial (Self-Learning, mdBook + Console Projects)
**Researched:** 2026-02-19
**Confidence:** HIGH

---

## Feature Landscape

### Table Stakes (Users Expect These)

Features learners assume exist. Missing these = tutorial feels incomplete or untrustworthy.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Progressive difficulty curve (Bandit → MCTS) | Learners need conceptual on-ramps; each phase must build on the previous one | LOW | Already planned: Phase 1 (⭐) → Phase 5 (⭐⭐⭐⭐⭐) |
| Concept-first, code-second structure per chapter | Self-learners arrive without context; dropping code first loses them | LOW | Each phase: intro → core concept → math → F# types → algorithm implementation |
| Working, runnable F# console app per phase | Tutorial credibility depends on code that actually compiles and produces output | MEDIUM | Each phase = independent `.sln` + `.fsproj`; must build from scratch |
| Clear RL concept mapping per phase | Learners must know *why* this game teaches this concept (e.g., TicTacToe → MDP) | LOW | Table in PROJECT.md maps this; each chapter intro must make this explicit |
| Expecto unit tests per phase | F# community expects Expecto; missing = looks incomplete | LOW | All game rules, algorithm outputs, boundary conditions |
| FsCheck property-based tests for game rules | Board game invariants are the natural fit for PBT; learners expect this pattern | MEDIUM | Invariants: valid-board, no-move-after-win, move-count-bounds, symmetry |
| Console output of training progress | RL training is invisible without output; learners need to see convergence | LOW | Reward curves, win-rate over episodes, epoch counters |
| Option/Result error handling throughout | Stated constraint; F# tutorial audience expects this; exceptions = bad example | LOW | All phase code must model errors with discriminated unions, not `try/catch` |
| Serilog structured logging for training loop | Stated constraint; enables learner to inspect what the training loop is doing | LOW | Log episode count, cumulative reward, epsilon decay, Q-value samples |
| Korean-language explanations | Target audience is Korean-speaking; English tutorial = wrong audience | LOW | All mdBook prose in Korean; code comments bilingual OK |
| mdBook site with navigable SUMMARY.md | Standard for Korean F# learners; Gitbook-style navigation expected | LOW | One `src/SUMMARY.md` for all 5 phases |

### Differentiators (Competitive Advantage)

Features that set this tutorial apart from generic Python RL tutorials or English-only resources.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| F# functional type system used to model RL concepts | Most RL tutorials use Python + mutable state; F# DUs + immutable records make RL state transitions explicit and type-safe | MEDIUM | `GameState`, `AgentState`, `Experience` as immutable records; transitions as pure functions |
| Convergence tests as first-class test suite | Few tutorials verify that the RL agent *actually learns*; testing win-rate improvement after N episodes validates the algorithm not just the code | HIGH | Expecto test: after 10k episodes, agent beats random baseline at >60% rate |
| Phase-bridging "Why this matters" sections | Tutors learners on *why* the next technique is needed (e.g., Q-Table breaks at Connect Four state space → need DQN) | LOW | Each phase ends with "한계: 왜 이것만으로는 부족한가?" section |
| FsCheck-based game rule verification (not just unit tests) | Property testing for board games catches edge cases unit tests miss (e.g., move after terminal state, column gravity for Connect Four) | MEDIUM | Properties: `applyMove . applyMove` composition, terminal-state idempotency, legal-moves non-empty before terminal |
| Dual-agent comparison within each phase | Running two agents (e.g., ε-greedy vs UCB1, Q-Learning vs Minimax) and comparing output in the same console session teaches contrast, not just isolated implementation | MEDIUM | Phase 1: three ε values; Phase 3: Minimax vs Q-Learning; Phase 4: DQN vs Phase 3 Minimax |
| Math formula → F# code correspondence | Every RL update rule (TD, Q-update, PUCT) shown in LaTeX-like prose and then directly translated to F#; bridges theory and implementation | LOW | Each core update as a named function matching the math symbol |
| Self-contained phase solutions | Each phase is a standalone `.sln`; learner can start from Phase 3 without running Phase 1 | LOW | Reduces friction for returning learners or those jumping to relevant phase |
| ASCII / console chart for training curve | No external visualization dependency; shows reward curve or win-rate directly in terminal output | MEDIUM | Could use Spectre.Console or simple `printfn` bar charts; avoids matplotlib dependency |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem valuable but should be deliberately excluded.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| F# language basics explanation | Beginners might request this for accessibility | Stated constraint: author and audience are F# experts; including it pads chapters, dilutes RL focus, and insults the audience | Link to external F# resources in a "Prerequisites" section of the mdBook introduction; do not inline |
| Web UI / Fable / SignalR for any phase before Phase 5 | Visually appealing; makes results shareable | Adds 3–4x implementation complexity per phase; derails RL learning with frontend concerns; PROJECT.md explicitly excludes this | Console-only output for Phases 1–4; Phase 5 console MCTS is the final milestone (web is explicitly out of scope per PROJECT.md) |
| GPU / CUDA optimization for TorchSharp | DQN/MCTS training performance pressure | Tutorial goal is understanding, not throughput; GPU setup complexity is a blocker for learners on varied hardware; CPU is sufficient for Connect Four DQN at tutorial scale | Document expected CPU training time clearly; cap episodes at a practical number (e.g., 50k DQN episodes) |
| OpenAI Gym / Gymnasium integration | Python RL standard; learners may expect familiar environments | F# tutorial must use F#-native environments; Gym requires Python interop which breaks the F#-only constraint and adds complexity | Implement game environments directly in F# (they are simple enough for Bandit/TicTacToe/Connect Four/Gomoku) |
| Hyperparameter sweep / grid search tooling | Nice for optimization | Scope creep; tutorial already teaches RL concepts; automated tuning is a separate concern | Show 2–3 hardcoded hyperparameter comparisons in console output (e.g., ε = 0.01, 0.1, 0.3 in Phase 1) |
| Persistent model storage beyond Phase 4–5 | Saving trained models for reuse | Premature for early phases; adds file I/O complexity that distracts from RL learning | Phases 1–3: train from scratch each run (fast enough); Phase 4–5: `.pt` model saving via TorchSharp is appropriate and expected |
| Detailed convergence proofs / theoretical depth | Academic completeness | Audience is practitioners; heavy theory without code loses learners; Sutton & Barto book is already the reference for this | Link Sutton & Barto for proof details; tutorial provides intuition + code |
| Reinforcement learning from human feedback (RLHF) | Hot topic in 2024–2026 | Completely out of domain scope; requires LLM infrastructure; not related to game-playing RL | Stay in classical RL + DRL game-playing domain as scoped |
| Renzu rules / forbidden moves for Gomoku | Completeness of game rules | Explicitly excluded in PROJECT.md; adds rule complexity that distracts from MCTS concept | Basic Gomoku rules only: first to 5-in-a-row wins |
| Multi-language (Python/C#) comparison sections | Show F# vs alternatives | Scope creep; tutorial is F#-only; comparisons dilute focus | Mention alternatives in footnotes if relevant (e.g., "PyTorch equivalent is torch.nn.Conv2d") |

---

## Feature Dependencies

```
[mdBook SUMMARY.md structure]
    └──requires──> [Each phase md chapter files]
                       └──requires──> [Working F# console project per phase]

[FsCheck game rule properties]
    └──requires──> [Game engine types (Board, GameState, Move)]
                       └──requires──> [Phase F# project builds]

[Convergence test (Expecto)]
    └──requires──> [RL agent runs N episodes]
                       └──requires──> [Game engine + Agent implementation]

[DQN training (Phase 4)]
    └──requires──> [TorchSharp installed + boardToTensor]
                       └──requires──> [Connect Four game engine (Phase 3)]

[MCTS + Policy/Value Network (Phase 5)]
    └──requires──> [TorchSharp (from Phase 4)]
                       └──requires──> [Gomoku game engine]
                       └──requires──> [MCTS tree structure (MctsNode)]

[Console training curve output]
    └──requires──> [Serilog configured + episode counter]

[Dual-agent comparison console output]
    └──requires──> [Both agents implemented in same phase project]
```

### Dependency Notes

- **Phase 4 requires Phase 3 game engine:** DQN trains on Connect Four; the board representation from Phase 3 is reused (but in an independent `.sln`, so copy-forward or shared library is needed — recommendation: copy game engine module into Phase 4 project to maintain phase independence).
- **Phase 5 requires Phase 4 TorchSharp pattern:** MCTS + neural net uses the same TorchSharp API patterns from DQN; learner must have internalized Phase 4 before tackling Phase 5.
- **FsCheck properties require stable type definitions:** Property generators must be written after the `Board`, `GameState`, `Move` types are finalized — define types first, write PBT second.
- **Convergence tests require tuned hyperparameters:** Do not write convergence tests until default hyperparameters are validated; otherwise tests become flaky. Run manually first, then encode successful parameters as the test's training config.

---

## MVP Definition

This project has 5 phases. MVP = Phase 1 complete and publishable. The mdBook site can be launched with just Phase 1 content and expanded iteratively.

### Launch With (Phase 1 MVP)

- [ ] mdBook scaffolding with `book.toml`, `SUMMARY.md`, placeholder chapters for all 5 phases — *establishes the full structure early*
- [ ] Phase 1: Multi-Armed Bandit console app — ε-greedy + UCB1, 1000 episodes, console reward comparison output — *validates the core tutorial format*
- [ ] Expecto tests for Bandit environment (reward is within expected range, action count matches episode count)
- [ ] FsCheck property for Bandit (agent state totals are consistent: `sum(Counts) = total episodes`)
- [ ] Serilog logging for training loop (episode number, cumulative reward per arm)
- [ ] Phase 1 mdBook chapter: concept intro → math → F# types → algorithm → test section

### Add After Validation (v1.x — Phases 2–3)

- [ ] Phase 2: TicTacToe (MDP, TD Learning, self-play) — trigger: Phase 1 feedback positive
- [ ] Phase 2 FsCheck properties: valid board invariant, no-move-after-win, legal moves before terminal state
- [ ] Phase 2 convergence test: after 10k self-play episodes, TD agent wins >60% vs random
- [ ] Phase 3: Connect Four (Q-Learning + Minimax) — trigger: Phase 2 complete

### Future Consideration (v2+ — Phases 4–5)

- [ ] Phase 4: DQN Connect Four with TorchSharp — defer until Phases 1–3 are solid; TorchSharp adds significant setup complexity
- [ ] Phase 5: Gomoku MCTS + Policy/Value Network — highest complexity; requires Phase 4 TorchSharp patterns to be familiar
- [ ] ASCII training curve visualization — nice-to-have; add if console output feels insufficient after Phase 1 feedback

---

## Feature Prioritization Matrix

| Feature | Learner Value | Implementation Cost | Priority |
|---------|---------------|---------------------|----------|
| Progressive phase structure (Bandit → MCTS) | HIGH | LOW | P1 |
| Working console app per phase | HIGH | LOW | P1 |
| RL concept + math per chapter in Korean | HIGH | LOW | P1 |
| Expecto unit tests per phase | HIGH | LOW | P1 |
| FsCheck game rule properties | HIGH | MEDIUM | P1 |
| Serilog training loop logging | HIGH | LOW | P1 |
| Option/Result error handling | HIGH | LOW | P1 |
| Phase-bridging "Why this matters" sections | HIGH | LOW | P1 |
| Convergence tests (agent beats baseline) | HIGH | HIGH | P2 |
| Dual-agent comparison console output | HIGH | MEDIUM | P2 |
| Math formula → F# function correspondence | HIGH | LOW | P2 |
| ASCII/console training curve chart | MEDIUM | MEDIUM | P2 |
| Self-contained phase `.sln` files | MEDIUM | LOW | P1 |
| TorchSharp DQN training (Phase 4) | HIGH | HIGH | P2 (Phase 4 only) |
| MCTS + Policy/Value Network (Phase 5) | HIGH | HIGH | P3 (Phase 5 only) |
| Model save/load (.pt files) | MEDIUM | LOW | P2 (Phase 4–5 only) |

**Priority key:**
- P1: Must have for launch (Phase 1 MVP)
- P2: Should have, add when possible (Phases 2–4)
- P3: Nice to have, future consideration (Phase 5)

---

## Competitor Feature Analysis

| Feature | Hugging Face Deep RL Course | Sutton & Barto Book | Our Approach |
|---------|------------------------------|---------------------|--------------|
| Language | Python (Colab notebooks) | Pseudocode | F# — differentiator |
| Chapter structure | Theory → Hands-on → Challenge | Theory only | Theory → Math → F# types → Algorithm → Tests → Tutorial doc |
| Testing | None | None | Expecto + FsCheck — differentiator |
| Visualization | Matplotlib charts | Static figures | Console output + Serilog logs — simpler but functional |
| Game progression | Atari → complex envs (not sequential) | Bandit → MDP abstraction | Bandit → TicTacToe → Connect Four → DQN → MCTS — clear progression |
| Audience | Python ML practitioners | Academics | F# practitioner who knows the language |
| Language of instruction | English | English | Korean — differentiator |
| Convergence verification | Manual inspection | None | Convergence tests in Expecto — differentiator |
| State space limitation explanation | Implicit | Mathematical | Explicit "why this breaks at Connect Four" narrative |

---

## Phase-Level Feature Mapping

| Phase | Core RL Features to Implement | Testing Features | Console Output Features |
|-------|-------------------------------|-----------------|------------------------|
| Phase 1: Bandit | ε-greedy strategy, UCB1, incremental mean update, episode loop | FsCheck: counts sum = episodes; Expecto: reward in bounds | Cumulative reward per arm, strategy comparison (3 ε values + UCB1) |
| Phase 2: TicTacToe | MDP state type, TD(0) update, self-play loop, value table (Map), random agent | FsCheck: valid board invariant, no-move after terminal, legal-moves non-empty pre-terminal; Expecto: convergence after 10k episodes | Win-rate vs random over training, value table size growth |
| Phase 3: Connect Four | Q-Learning (Q-table with feature extraction), Minimax + Alpha-Beta, legal-moves (gravity) | FsCheck: gravity invariant (cells fall to bottom), 4-in-a-row detection; Expecto: Minimax vs random baseline >90% | Minimax vs Q-Learning head-to-head, Alpha-Beta pruning stats (nodes pruned) |
| Phase 4: DQN Connect Four | TorchSharp Conv2D model, Experience Replay buffer, Target Network, boardToTensor | Expecto: loss decreasing over time, DQN vs Phase 3 Minimax win-rate after 50k episodes | Training loss curve, epsilon decay schedule, win-rate vs Minimax |
| Phase 5: Gomoku MCTS | MCTS tree (MctsNode), PUCT selection, Policy + Value Network, self-play pipeline | Expecto: MCTS with 400 simulations beats random Gomoku >80%; FsCheck: 5-in-a-row detection correctness | MCTS simulation count per move, win-rate improvement over self-play iterations |

---

## Sources

- [Hugging Face Deep RL Course — Unit structure](https://huggingface.co/learn/deep-rl-course/en/unit0/introduction) — HIGH confidence; authoritative reference for "theory + hands-on" chapter pattern
- [Reinforcement Learning: Theory and Python Implementation (GitHub)](https://github.com/ZhiqingXiao/rl-book) — HIGH confidence; exemplar of algorithm-per-chapter tutorial structure
- [FsCheck — Property-Based Testing for .NET](https://fscheck.github.io/FsCheck/) — HIGH confidence; official documentation
- [Expecto — F# Testing Library](https://github.com/haf/expecto) — HIGH confidence; official repository confirming FsCheck integration
- [Choosing properties for property-based testing — F# for Fun and Profit](https://fsharpforfunandprofit.com/posts/property-based-testing-2/) — HIGH confidence; Scott Wlaschin's authoritative F# PBT guide
- [Serilog — Simple .NET logging](https://serilog.net/) — HIGH confidence; official documentation confirming console sink and structured property logging
- [Deep Reinforcement Learning and MCTS with Connect 4 — Medium/TDS](https://medium.com/data-science/deep-reinforcement-learning-and-monte-carlo-tree-search-with-connect-4-ba22a4713e7a) — MEDIUM confidence; practitioner example of DQN + MCTS game pipeline
- [PyTorch DQN Tutorial](https://docs.pytorch.org/tutorials/intermediate/reinforcement_q_learning.html) — HIGH confidence; reference for Experience Replay + Target Network pattern (TorchSharp API mirrors this)
- PROJECT.md constraints and Out-of-Scope list — HIGH confidence; authoritative source for anti-features

---
*Feature research for: F# Reinforcement Learning Tutorial (Korean, console, mdBook)*
*Researched: 2026-02-19*
