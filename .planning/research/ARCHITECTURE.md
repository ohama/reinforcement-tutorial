# Architecture Research

**Domain:** F# Reinforcement Learning Tutorial — mdBook site + phase-based console projects
**Researched:** 2026-02-19
**Confidence:** HIGH (F# project layout), HIGH (RL component design), MEDIUM (mdBook site structure), HIGH (testing patterns)

---

## Standard Architecture

### System Overview

The project has two interlocking structural dimensions: a **documentation layer** (mdBook tutorial site) and a **code layer** (five independent F# solutions). These are siblings in the repository, not nested.

```
reinforcement-tutorial/
├── book/                    # mdBook source (tutorial text)
│   ├── book.toml
│   └── src/
│       ├── SUMMARY.md
│       ├── intro/
│       ├── phase1-bandit/
│       ├── phase2-tictactoe/
│       ├── phase3-connect4/
│       ├── phase4-dqn/
│       └── phase5-gomoku/
├── phases/                  # F# code, one .sln per phase
│   ├── phase1-bandit/
│   ├── phase2-tictactoe/
│   ├── phase3-connect4/
│   ├── phase4-dqn/
│   └── phase5-gomoku/
├── .planning/
└── README.md
```

**Key principle:** Each phase's code lives beside its chapter text. The book references code snippets that are actually runnable from the corresponding phase directory.

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| mdBook site | Tutorial narrative, explanations, diagrams | Markdown files, book.toml config |
| Phase F# solution | Runnable, self-contained RL implementation | .sln + .fsproj files, NuGet refs |
| Game Engine (per phase) | Board state, rules, legal moves, win detection | Pure F# module — no side effects |
| RL Agent (per phase) | Policy, value estimation, learning update | Pure functions operating on agent state |
| Training Loop (per phase) | Episode orchestration, logging, checkpoint | Impure entry point; calls pure modules |
| Test project (per phase) | Game rule properties, learning convergence | Expecto + FsCheck |

---

## mdBook Site Structure

### Chapter Organization Pattern

mdBook's SUMMARY.md drives all navigation. For a five-phase progression, use **parts** (unnumbered headings in SUMMARY.md) to group chapters per phase, with sub-chapters for concepts within each phase.

```markdown
# SUMMARY.md

[Introduction](intro/introduction.md)
[How to Use This Book](intro/how-to-use.md)

---

# Phase 1: Multi-Armed Bandit

- [Overview](phase1-bandit/overview.md)
- [Core Concepts](phase1-bandit/concepts.md)
  - [Exploration vs Exploitation](phase1-bandit/exploration.md)
  - [ε-greedy Strategy](phase1-bandit/epsilon-greedy.md)
- [Implementation](phase1-bandit/implementation.md)
- [Exercises](phase1-bandit/exercises.md)
- [Summary & Next Phase](phase1-bandit/summary.md)

---

# Phase 2: Tic-Tac-Toe (MDP + TD Learning)

- [Overview](phase2-tictactoe/overview.md)
- [Core Concepts](phase2-tictactoe/concepts.md)
  - [MDP and Value Functions](phase2-tictactoe/mdp.md)
  - [TD Learning](phase2-tictactoe/td-learning.md)
- [Implementation](phase2-tictactoe/implementation.md)
- [Exercises](phase2-tictactoe/exercises.md)
- [Summary & Next Phase](phase2-tictactoe/summary.md)

---

# Phase 3: Connect Four (Q-Learning + Minimax)
...

---

# Phase 4: Connect Four + DQN (Neural Q-Networks)
...

---

# Phase 5: Gomoku Web (MCTS + Policy/Value Net)
...

---

[Appendix: F# Primer](appendix/fsharp-primer.md)
[Appendix: RL Glossary](appendix/rl-glossary.md)
[Appendix: References](appendix/references.md)
```

### Cross-Reference Pattern

Each phase's "Summary & Next Phase" chapter explicitly previews the problem the next phase solves. This creates the narrative bridge: "We just saw that 5,478 TicTacToe states fit in a Map. Connect Four has 4.5 trillion states. Here's why that matters..."

Use mdBook's `[link text](../phase2-tictactoe/concepts.md)` syntax for forward/backward references. Keep references deliberate and sparse — readers read phases linearly.

### File Naming Convention

```
phase1-bandit/
  overview.md          # What we build, what you'll learn
  concepts.md          # Theory chapter (with math)
  implementation.md    # Walk through the F# code, snippets inline
  exercises.md         # Things to try/modify
  summary.md           # Recap, what broke, why Phase 2 is needed
```

---

## F# Solution Structure (Per Phase)

### Recommended Layout

Each phase is a **fully self-contained .sln** with no cross-phase project references. This allows readers to open any phase independently.

```
phases/phase2-tictactoe/
├── TicTacToe.sln
├── src/
│   └── TicTacToe/
│       ├── TicTacToe.fsproj
│       ├── Domain.fs          # Types only: Board, Cell, Player, GameState
│       ├── Rules.fs           # Pure game logic: legalMoves, applyMove, checkWin
│       ├── Agent.fs           # RL agent: AgentState, ValueTable, policy functions
│       ├── Training.fs        # Training loop: runEpisode, trainNEpisodes
│       ├── Logging.fs         # Serilog setup, metric emission
│       └── Program.fs         # Entry point: parse args, run, print results
└── tests/
    └── TicTacToe.Tests/
        ├── TicTacToe.Tests.fsproj
        ├── RulesTests.fs      # Expecto + FsCheck: game rule properties
        ├── AgentTests.fs      # Convergence tests, sanity checks
        └── Main.fs            # Expecto entry point
```

**File ordering within .fsproj matters in F#.** Types must come before functions that use them. The ordering above (Domain → Rules → Agent → Training → Logging → Program) respects this constraint.

### Phase Complexity Progression

| Phase | State Type | Agent State | Key New Dependency |
|-------|-----------|-------------|-------------------|
| 1 Bandit | None (stateless env) | `int array * float array` | None (stdlib only) |
| 2 TicTacToe | `Cell array` (9 cells) | `Map<Board, float>` | None |
| 3 Connect Four | `Player option array2d` | `Map<Board*int, float>` | None |
| 4 DQN Connect Four | `Player option array2d` | `torch.nn.Module` | TorchSharp |
| 5 Gomoku | `Stone option array2d` | Policy+Value network | TorchSharp, Giraffe, Fable |

### Shared Code Strategy: Copy-and-Evolve, Not Shared Library

**Do not** create a shared F# library referenced across phases. Instead:

- Each phase copies and potentially simplifies/modifies the types it needs
- The `Domain.fs` in Phase 3 is related to Phase 2 but is not the same module
- This makes each phase independently openable in an IDE without needing the whole repo
- It also makes the pedagogical progression explicit: readers see types evolve

The only shared artifact is the **mdBook site** — the code is deliberately not DRY across phases.

---

## Architectural Patterns

### Pattern 1: Pure Game Engine + Impure Shell

**What:** All game state transitions are pure functions (no side effects, no mutation). The impure shell (Program.fs, Training.fs) calls into these pure functions and handles I/O.

**When to use:** Every phase. This is the foundational pattern for the whole project.

**Trade-offs:** Pure functions are easily testable with FsCheck. The impure boundary is explicit and easy to find.

**Example:**

```fsharp
// Domain.fs — types only
type Cell = Empty | X | O
type Board = Cell array
type GameState = { Board: Board; CurrentPlayer: Cell }
type GameResult = Winner of Cell | Draw | InProgress

// Rules.fs — pure functions, no I/O
let legalMoves (state: GameState) : int list =
    state.Board
    |> Array.indexed
    |> Array.choose (fun (i, c) -> if c = Empty then Some i else None)
    |> Array.toList

let applyMove (state: GameState) (pos: int) : GameState =
    let newBoard = Array.copy state.Board
    newBoard.[pos] <- state.CurrentPlayer
    { Board = newBoard
      CurrentPlayer = if state.CurrentPlayer = X then O else X }

// Training.fs — impure, calls pure modules
let runEpisode (alpha: float) (epsilon: float) (vTable: ValueTable) =
    // Has I/O: random number gen, loop, returns new state
    ...
```

### Pattern 2: Agent State as Immutable Record, Updated via Return Value

**What:** Agent state (value table, Q-table, network weights) is never mutated in place. Functions take state in and return new state out.

**When to use:** Phases 1-3 (tabular methods). Phase 4 partially breaks this for TorchSharp tensors.

**Trade-offs:** Clean and testable. In Phase 4, TorchSharp modules are inherently mutable objects (PyTorch paradigm) — wrap them in a record and treat the record as "the agent" to maintain the functional façade.

**Example:**

```fsharp
// Phases 1-3: fully immutable
type AgentState = { Counts: int array; Values: float array }

let updateValue (state: AgentState) (arm: int) (reward: float) : AgentState =
    let n = state.Counts.[arm] + 1
    let delta = (reward - state.Values.[arm]) / float n
    { Counts = state.Counts |> Array.mapi (fun i c -> if i = arm then n else c)
      Values = state.Values |> Array.mapi (fun i v -> if i = arm then v + delta else v) }

// Phase 4: TorchSharp is mutable internally, but the record is our boundary
type DQNAgent = {
    PolicyNet: Sequential   // mutable internally
    TargetNet: Sequential   // mutable internally
    Optimizer: Adam
    ReplayBuffer: CircularBuffer<Experience>
    Epsilon: float
    StepCount: int
}
```

### Pattern 3: Environment/Agent Separation via Interface Types

**What:** Define the environment and agent as function signatures (not OOP interfaces), passed as parameters. This enables substituting random agents, human input agents, or trained agents at the same call site.

**When to use:** Training loop, evaluation, self-play.

**Trade-offs:** Very composable. The training loop doesn't care what kind of agent it's calling. Makes it easy to swap agents for evaluation (e.g., Minimax vs DQN).

**Example:**

```fsharp
// Define agent as a function type
type Policy = GameState -> int   // state → chosen action

// Training loop works with any Policy
let runEpisode (player1: Policy) (player2: Policy) (initialState: GameState) =
    let rec loop state history =
        match checkResult state with
        | InProgress ->
            let action =
                if state.CurrentPlayer = X then player1 state
                else player2 state
            let next = applyMove state action
            loop next ((state, action) :: history)
        | result -> (result, history)
    loop initialState []

// Compose any agents freely
let randomPolicy (state: GameState) =
    legalMoves state |> List.item (Random().Next(legalMoves state |> List.length))

let trainedTDPolicy (vTable: ValueTable) (epsilon: float) (state: GameState) =
    if Random().NextDouble() < epsilon then randomPolicy state
    else bestAction vTable state
```

### Pattern 4: Result/Option for Rule Validation at Boundaries

**What:** Use `Result<'T, string>` when parsing user input or validating moves at the boundary between impure and pure code. Inside pure game logic, assume valid state (enforced by the type system).

**When to use:** Anywhere user input enters the system (console input in human-vs-AI modes).

**Example:**

```fsharp
// Parse and validate at the boundary
let parseMove (state: GameState) (input: string) : Result<int, string> =
    match System.Int32.TryParse(input) with
    | false, _ -> Error "Input must be a number"
    | true, n when n < 0 || n > 8 -> Error "Position must be 0-8"
    | true, n when state.Board.[n] <> Empty -> Error "Cell already occupied"
    | true, n -> Ok n

// Pure game logic never validates, always assumes valid input
let applyMove (state: GameState) (pos: int) : GameState = ...
```

### Pattern 5: Training Loop as Observable Pipeline with Serilog

**What:** The training loop emits structured log events at each meaningful point (episode start, episode end, evaluation checkpoint). Serilog captures these with context attached.

**When to use:** All training phases. Makes it possible to analyze training runs after the fact.

**Example:**

```fsharp
open Serilog

let log = Log.Logger  // configured in Program.fs

let trainNEpisodes (n: int) (config: TrainingConfig) =
    let mutable agent = initialAgent config
    for episode in 1..n do
        let (result, history) = runEpisode (policy agent) (randomPolicy) initialState
        agent <- updateAgent agent history result
        if episode % 1000 = 0 then
            log.Information("Episode {Episode} complete. WinRate={WinRate:P2} Epsilon={Epsilon}",
                            episode, winRate agent, agent.Epsilon)
    agent
```

---

## Data Flow

### Training Data Flow (Phases 1-3: Tabular)

```
Environment (game rules)
    ↓  produces  ↓
GameState  →  Agent (Policy function)  →  Action
    ↓                                        ↓
applyMove(state, action) → new GameState → (loop)
    ↓  on episode end  ↓
History: [(state, action, reward)]
    ↓
updateAgent(agent, history) → new AgentState (updated value table)
    ↓
Serilog → console / log file
```

### Training Data Flow (Phase 4: DQN)

```
Environment (game rules)
    ↓
GameState → boardToTensor → torch.Tensor
    ↓
PolicyNet.forward(tensor) → Q values [7]
    ↓  ε-greedy  ↓
Action → applyMove → new GameState
    ↓
Experience {state, action, reward, next_state, done}
    ↓
ReplayBuffer.push(experience)          ← circular FIFO buffer

[every N steps]
ReplayBuffer.sample(batch_size)
    ↓
Batch → PolicyNet.forward (current Q)
         TargetNet.forward (target Q)
         MSELoss → Optimizer.step()    ← mutation happens here
    ↓
[every M steps]
TargetNet.load_state_dict(PolicyNet.state_dict())
    ↓
Serilog → metrics (loss, win rate, epsilon)
```

### Model Persistence Data Flow (Phase 4+)

```
Trained model state
    ↓
PolicyNet.save("checkpoints/dqn-step-{N}.pt")   ← TorchSharp serialization
    ↓  later  ↓
PolicyNet.load("checkpoints/dqn-best.pt")
    ↓
Evaluation vs Minimax → win rate logged
```

### Tutorial Content Data Flow

```
Book source (book/src/**/*.md)
    ↓  mdbook build  ↓
Static HTML site (book/book/)
    ↓  deploy  ↓
GitHub Pages / static host

Code snippets in .md files reference
phases/{phaseN}/src/{Module}.fs
(by path convention, not automated tooling)
```

---

## Recommended Project Structure: Per-Phase Detail

### Phase 1 — Bandit (minimal, teaches the pattern)

```
phases/phase1-bandit/
├── Bandit.sln
├── src/
│   └── Bandit/
│       ├── Bandit.fsproj
│       ├── Environment.fs     # BanditArm type, pull function
│       ├── Agent.fs           # AgentState, epsilonGreedy, updateValue
│       ├── Training.fs        # runTrials, compareStrategies
│       └── Program.fs         # Main: run experiment, print ASCII chart
└── tests/
    └── Bandit.Tests/
        ├── Bandit.Tests.fsproj
        ├── AgentTests.fs      # Property: value estimates converge to true mean
        └── Main.fs
```

### Phase 2 — TicTacToe (introduces game engine pattern)

```
phases/phase2-tictactoe/
├── TicTacToe.sln
├── src/
│   └── TicTacToe/
│       ├── TicTacToe.fsproj
│       ├── Domain.fs          # Cell, Board, GameState, GameResult
│       ├── Rules.fs           # legalMoves, applyMove, checkWin, checkResult
│       ├── Agent.fs           # ValueTable, tdUpdate, epsilonGreedyPolicy
│       ├── Training.fs        # runEpisode, selfPlay, learnNGames
│       ├── Logging.fs         # Serilog config, metric helpers
│       └── Program.fs         # Main: train, then human vs AI loop
└── tests/
    └── TicTacToe.Tests/
        ├── TicTacToe.Tests.fsproj
        ├── RulesTests.fs      # legalMoves always non-empty, applyMove idempotent properties
        ├── AgentTests.fs      # Agent beats random > 90% after training
        └── Main.fs
```

### Phase 3 — Connect Four (adds 2D array, Minimax)

```
phases/phase3-connect4/
├── ConnectFour.sln
├── src/
│   └── ConnectFour/
│       ├── ConnectFour.fsproj
│       ├── Domain.fs          # Player, Board (array2d), GameState
│       ├── Rules.fs           # dropPiece, legalColumns, checkWin4, checkResult
│       ├── Evaluate.fs        # Board scoring heuristic for Minimax
│       ├── Minimax.fs         # minimax with alpha-beta pruning
│       ├── Agent.fs           # Q-Learning agent (feature-based)
│       ├── Training.fs        # self-play + Minimax evaluation
│       ├── Logging.fs
│       └── Program.fs
└── tests/
    └── ConnectFour.Tests/
        ├── ConnectFour.Tests.fsproj
        ├── RulesTests.fs      # gravity, win detection, column full
        ├── MinimaxTests.fs    # depth-1 wins when available, doesn't walk into loss
        └── Main.fs
```

### Phase 4 — DQN (adds TorchSharp)

```
phases/phase4-dqn/
├── DQNConnect4.sln
├── src/
│   └── DQNConnect4/
│       ├── DQNConnect4.fsproj
│       ├── Domain.fs          # (same as Phase 3)
│       ├── Rules.fs           # (same as Phase 3)
│       ├── Tensor.fs          # boardToTensor, tensorToQValues
│       ├── Model.fs           # DQNModel definition (Sequential, Conv2D, Dense)
│       ├── ReplayBuffer.fs    # CircularBuffer<Experience>
│       ├── Agent.fs           # DQNAgent record, selectAction, trainStep
│       ├── Training.fs        # selfPlay loop, target sync, evaluation
│       ├── Checkpoint.fs      # save/load model weights
│       ├── Logging.fs
│       └── Program.fs
└── tests/
    └── DQNConnect4.Tests/
        ├── DQNConnect4.Tests.fsproj
        ├── TensorTests.fs     # boardToTensor shape and channel values
        ├── BufferTests.fs     # ReplayBuffer wraps correctly at capacity
        └── Main.fs
```

### Phase 5 — Gomoku Web (full stack)

```
phases/phase5-gomoku/
├── Gomoku.sln
├── src/
│   ├── Gomoku.Core/           # Shared domain (both server and client use)
│   │   ├── Gomoku.Core.fsproj
│   │   ├── Domain.fs          # Stone, Board (15x15), Move, GameState
│   │   └── Rules.fs           # legalMoves, applyMove, checkWin5, checkResult
│   ├── Gomoku.AI/             # Server-side AI engine
│   │   ├── Gomoku.AI.fsproj
│   │   ├── Tensor.fs
│   │   ├── Model.fs           # PolicyValueNet (shared trunk + dual heads)
│   │   ├── MCTS.fs            # MctsNode, select, expand, simulate, backprop
│   │   ├── SelfPlay.fs        # Data generation loop
│   │   ├── Training.fs        # Network training from self-play data
│   │   └── Checkpoint.fs
│   ├── Gomoku.Server/         # Giraffe + SignalR backend
│   │   ├── Gomoku.Server.fsproj
│   │   ├── Hubs.fs            # SignalR hub (GameHub)
│   │   ├── GameSession.fs     # Session management, move handling
│   │   └── Program.fs         # ASP.NET Core startup
│   └── Gomoku.Client/         # Fable + Feliz frontend
│       ├── Gomoku.Client.fsproj  # targets netstandard2.0
│       ├── Board.fs           # Feliz board component (SVG)
│       ├── App.fs             # Elmish model/update/view
│       └── Main.fs
└── tests/
    └── Gomoku.Tests/
        ├── Gomoku.Tests.fsproj
        ├── RulesTests.fs
        ├── MCTSTests.fs       # MCTS produces legal moves, never illegal
        └── Main.fs
```

---

## Testing Architecture

### Expecto + FsCheck Pattern for Game Rules

Property-based testing is the right tool for game rules because it generates thousands of random valid boards automatically.

```fsharp
// RulesTests.fs
open Expecto
open FsCheck

// Custom generator: random valid board
let validBoardGen =
    gen {
        let! moves = Gen.listOf (Gen.choose (0, 8))
        return applyMoves moves initialState
    }

let tests =
    testList "Game Rules" [
        // Unit test: specific known cases
        testCase "Win on row detected" <| fun () ->
            let board = [| X; X; X; O; O; Empty; Empty; Empty; Empty |]
            let state = { Board = board; CurrentPlayer = O }
            Expect.equal (checkResult state) (Winner X) "Row 0 should be a win"

        // Property: legal moves are always valid positions
        testProperty "legalMoves returns only empty cells" <| fun (state: GameState) ->
            legalMoves state
            |> List.forall (fun i -> state.Board.[i] = Empty)

        // Property: applying a legal move produces valid state
        testProperty "applyMove changes exactly one cell" <| fun (state: GameState) ->
            match legalMoves state with
            | [] -> true  // terminal state, skip
            | moves ->
                let move = List.head moves
                let next = applyMove state move
                let changed = Array.zip state.Board next.Board
                              |> Array.filter (fun (a, b) -> a <> b)
                changed.Length = 1

        // Property: game terminates in finite moves
        testProperty "Random game always terminates" <| fun () ->
            let rec play state depth =
                if depth > 9 then false  // should not happen on 3x3
                else
                    match checkResult state with
                    | InProgress ->
                        let move = List.head (legalMoves state)
                        play (applyMove state move) (depth + 1)
                    | _ -> true
            play initialState 0
    ]
```

### Learning Convergence Tests

These are integration-style tests that train a small number of episodes and assert the agent improves. Keep them fast by using fewer episodes than production runs.

```fsharp
// AgentTests.fs
testCase "TD agent beats random agent after 5000 episodes" <| fun () ->
    let trainedAgent = trainNEpisodes 5000 { Alpha = 0.1; Epsilon = 0.1 }
    let winRate = evaluateVsRandom 200 trainedAgent
    Expect.isGreaterThan winRate 0.85 "Should win > 85% vs random after training"
```

**Confidence note:** Learning convergence tests are slow by nature. Mark them with Expecto's `testSequenced` or put them behind a separate test category so the default test run stays fast.

---

## Anti-Patterns

### Anti-Pattern 1: Shared Library Across Phases

**What people do:** Create a `Shared.fsproj` with common types (Board, Cell) and reference it from all phases.

**Why it's wrong:** Breaks phase independence. Readers can't open Phase 3 without building Phase 1/2. Forces a single type definition that doesn't let types evolve cleanly (e.g., TicTacToe Board is `Cell array`, Connect Four Board is `Player option array2d` — they should not share a type).

**Do this instead:** Copy-and-evolve. Each phase defines its own domain types. The tutorial explicitly teaches type evolution as a feature.

### Anti-Pattern 2: Mutable Agent State with Imperative Loop

**What people do:** Use `mutable valueTable = ...` at module level and mutate it in a while loop.

**Why it's wrong:** Contradicts the functional F# style the tutorial teaches. Makes testing harder (state leaks between tests). Obscures the "agent state is an input/output" model.

**Do this instead:** Return new agent state from every update function. The loop in Training.fs accumulates state via recursion or `Array.fold`.

### Anti-Pattern 3: Game Logic Mixed with I/O in Same Module

**What people do:** Put `printfn "Player X plays at position %d"` calls inside `applyMove` or `checkWin`.

**Why it's wrong:** Makes pure functions impure. Can't property-test them with FsCheck. Breaks the educational pattern.

**Do this instead:** Pure game logic in Rules.fs, logging/printing in Training.fs or Program.fs. Pass a logger into the training loop if needed.

### Anti-Pattern 4: ReplayBuffer as a Global Mutable List

**What people do:** In Phase 4, define `let replayBuffer = ResizeArray<Experience>()` as a top-level mutable.

**Why it's wrong:** Global mutable state makes testing and reuse impossible. Multiple training runs in one process session interfere.

**Do this instead:** Define `ReplayBuffer` as a record with a fixed-capacity circular array. Pass it explicitly. Phase 4 partially breaks functional purity (TorchSharp requires it), but keep the buffer as a scoped local, not global.

### Anti-Pattern 5: One Giant Program.fs

**What people do:** Put all game logic, agent logic, training loop, and entry point in a single file.

**Why it's wrong:** Hard to navigate for tutorial readers. Hard to reference specific sections from the book. FsCheck can't easily test functions buried in a module that has side effects at the top.

**Do this instead:** One file = one concept. Domain.fs, Rules.fs, Agent.fs, Training.fs, Program.fs. The file structure mirrors the book's conceptual hierarchy.

### Anti-Pattern 6: mdBook Chapters Without Runnable Code Counterpart

**What people do:** Write explanation chapters that reference hypothetical code snippets not actually present in the phase directory.

**Why it's wrong:** Readers lose trust when they can't run the code they're reading.

**Do this instead:** Every code snippet in the book is a verbatim extract from the corresponding phase's source files. Use mdBook's `{{#include}}` preprocessor directive to pull real code from the phase directories:

```markdown
<!-- In book/src/phase2-tictactoe/implementation.md -->
The core update function:

\```fsharp
{{#include ../../../phases/phase2-tictactoe/src/TicTacToe/Agent.fs:tdUpdate}}
\```
```

This requires annotating the source with `// ANCHOR: tdUpdate` ... `// ANCHOR_END: tdUpdate` comments.

---

## Build Order Implications

Within each phase, build order follows the F# compiler's requirement (types before functions):

```
Domain.fs → Rules.fs → [Evaluate.fs] → [Minimax.fs] → Agent.fs → Training.fs → Logging.fs → Program.fs
```

Across phases, there is no build dependency. Each phase builds independently. The recommended teaching order is also the natural build order:

```
Phase 1 (no deps)
  → Phase 2 (introduces game engine pattern)
    → Phase 3 (adds 2D state + Minimax; no new NuGet)
      → Phase 4 (adds TorchSharp; Phase 3's Rules.fs is copied in)
        → Phase 5 (adds Giraffe, Fable, SignalR; Phase 3 game engine + Phase 4 model design)
```

Phase 5 (Gomoku) is the most complex because it requires:
1. A working Giraffe server (F# web experience)
2. A working Fable client (F# → JS compilation)
3. TorchSharp model (from Phase 4 pattern)
4. MCTS algorithm (new, most complex RL component)

**Recommendation:** In Phase 5 roadmap, build the game engine + MCTS first (console mode), then add the neural network, then add the web layer. This order lets each sub-component be verified independently.

---

## Integration Points

### Internal Boundaries (Per Phase)

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Rules.fs ↔ Agent.fs | Direct function call: `legalMoves state` | Agent calls rules; rules never call agent |
| Agent.fs ↔ Training.fs | Agent state passed as parameter, returned as value | No global state |
| Training.fs ↔ Logging.fs | Serilog `Log.Information(...)` calls | Logging is always at the impure boundary |
| Program.fs ↔ Training.fs | Calls `trainNEpisodes` with config record | Config record carries all hyperparameters |

### Phase 5 Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Gomoku.Core ↔ Gomoku.AI | Direct project reference | AI imports Core types |
| Gomoku.Core ↔ Gomoku.Client | Fable-compatible types only | No TorchSharp types cross this boundary |
| Gomoku.AI ↔ Gomoku.Server | Direct project reference | Server calls AI.MCTS.search() |
| Gomoku.Server ↔ Gomoku.Client | SignalR messages (JSON) | Defined as discriminated union, serialized |

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| GitHub Pages | mdbook build + gh-pages deploy | Standard mdBook deployment |
| NuGet (TorchSharp) | NuGet package reference | Large binary; ~400MB native libs; add .gitignore rule |
| NuGet (Expecto, FsCheck) | NuGet package reference | Lightweight; no native deps |
| NuGet (Serilog) | NuGet package reference | Add Serilog.Sinks.Console minimum |

---

## Scalability Considerations

This is a tutorial project, not a production system. Scalability here means "scales to complete all 5 phases without architectural debt."

| Concern | Early Phases (1-3) | Later Phases (4-5) |
|---------|---------------------|---------------------|
| State space | Fits in Map; fast | TorchSharp handles; GPU optional |
| Training time | Seconds to minutes | Phase 4: 30-60 min on CPU; Phase 5: hours to days |
| Test suite speed | Sub-second | Add `ptimeout 5000` to convergence tests |
| Repo size | Small | TorchSharp model checkpoints need .gitignore; use Git LFS if sharing models |

---

## Sources

- [mdBook Documentation — SUMMARY.md format](https://rust-lang.github.io/mdBook/format/summary.html) [HIGH confidence]
- [.NET project structure — src/tests separation](https://learn.microsoft.com/en-us/dotnet/core/tutorials/testing-with-cli) [HIGH confidence]
- [Organizing modules in F# — F# for Fun and Profit](https://fsharpforfunandprofit.com/posts/recipe-part3/) [HIGH confidence]
- [Expecto testing library — GitHub](https://github.com/haf/expecto) [HIGH confidence]
- [FsCheck Quick Start](https://fscheck.github.io/FsCheck/QuickStart.html) [HIGH confidence]
- [F# game development patterns — softwarepatternslexicon.com](https://softwarepatternslexicon.com/f-sharp/case-studies/game-development-with-f/) [MEDIUM confidence]
- [TorchSharp DQN discussion — GitHub dotnet/TorchSharp #710](https://github.com/dotnet/TorchSharp/discussions/710) [MEDIUM confidence]
- [Railway Oriented Programming — F# for Fun and Profit](https://fsharpforfunandprofit.com/posts/recipe-part2/) [HIGH confidence]
- [Serilog structured logging](https://serilog.net/) [HIGH confidence]
- [DQN data flow — PyTorch official tutorial](https://docs.pytorch.org/tutorials/intermediate/reinforcement_q_learning.html) [HIGH confidence — translates directly to TorchSharp]

---

*Architecture research for: F# Reinforcement Learning Tutorial (mdBook + 5-phase console projects)*
*Researched: 2026-02-19*
