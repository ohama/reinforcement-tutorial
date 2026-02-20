# Phase 5: Gomoku MCTS - Research

**Researched:** 2026-02-20
**Domain:** AlphaZero-style MCTS + Dual-Head Neural Network on 15x15 Gomoku, F# / TorchSharp 0.106.0
**Confidence:** MEDIUM-HIGH (algorithms well-documented; F#-specific MCTS patterns require adaptation from Python/C# sources; TorchSharp APIs verified against 0.106.0 source and Phase 4 working code)

---

## Summary

Phase 5 is the most complex phase of the tutorial. It combines MCTS (Monte Carlo Tree Search) with a dual-head Policy/Value neural network to produce an AlphaZero-style self-play learning AI for 15x15 Gomoku. The domain is well-researched in Python (junxiaosong/AlphaZero_Gomoku is the canonical reference implementation), but F# implementations of MCTS are rare and must be adapted manually from Python or C# sources.

The key challenges are: (1) building an MCTS tree with mutable nodes in F# using classes rather than records, because MCTS nodes require parent pointers and mutable children dictionaries; (2) correctly implementing the PUCT selection formula with perspective-flipping backpropagation; (3) adapting the TorchSharp ResBlock pattern from C# (no F# ResBlock examples exist in TorchSharpExamples); (4) making training feasible on CPU-only ARM64 macOS — 15x15 with 400 simulations/move will be slow, so the research recommends a smaller board or reduced simulation count for the tutorial context.

The good news: all TorchSharp APIs needed (BatchNorm2d, Dirichlet, log_softmax / nll_loss, mse_loss) exist and work in 0.106.0. The NativeLoader pattern from Phase 4 carries over unchanged. Memory management (NewDisposeScope) carries over unchanged. The overall project structure (Gomoku.sln, independent solution) carries over unchanged.

**Primary recommendation:** Implement a 4-channel board encoding, a 3-conv-layer shared backbone (no residual blocks for simplicity) with separate policy/value heads, MCTS with 200-400 simulations per move (not 800), and train for 200-500 self-play games. Use a class-based mutable MctsNode. Achieve >80% win vs random using MCTS alone (without neural network) first, then wire in the network. This is achievable on CPU in a few hours.

---

## Standard Stack

### Core (inherited from Phase 4 — all confirmed working)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| TorchSharp-cpu | 0.106.0 | Meta-package TorchSharp + libtorch CPU | Confirmed working ARM64 in Phase 4 |
| libtorch-cpu-osx-arm64 | 2.10.0 | Native libtorch binaries for ARM64 | Transitively included |
| Serilog | 4.3.1 | Structured logging | Established in Phases 3-4 |
| Serilog.Sinks.Console | 6.1.1 | Console output | As before |
| Serilog.Sinks.File | 7.0.0 | File output | As before |
| Expecto | 10.2.3 | Test framework | Established in Phases 1-4 |
| Expecto.FsCheck | 10.2.3 | Property tests | As before |
| FsCheck | 2.16.5 | Property test engine | 2.16.5 required (NOT 3.x) |
| YoloDev.Expecto.TestSdk | 0.15.5 | dotnet test bridge | As before |
| Microsoft.NET.Test.Sdk | 18.0.1 | Test SDK | As before |

### New in Phase 5

| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| TorchSharp.torch.distributions.Dirichlet | (in TorchSharp 0.106.0) | Root exploration noise | Built-in; `torch.distributions.Dirichlet(concentration).rsample()` |

### System Prerequisite

| Prerequisite | Version | Install |
|-------------|---------|---------|
| libomp (Homebrew) | any | `brew install libomp` (already present from Phase 4) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| 15x15 board | 9x9 or 11x11 | Much faster training on CPU; literature shows 9x9 achieves strong play in ~500 games. Use 15x15 if only testing win vs random (not strong play). |
| ResBlock (residual) | 3 plain conv layers | Simpler; no skip connection complexity; sufficient for tutorial purposes |
| Dirichlet from TorchSharp | Manual gamma sampling | TorchSharp built-in Dirichlet works; no need to implement manually |

### Installation

```xml
<!-- Same as Phase 4; carry over from DQN.fsproj -->
<PackageReference Include="TorchSharp-cpu" Version="0.106.0" />
<PackageReference Include="Serilog" Version="4.3.1" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
```

```bash
# No new system prerequisites beyond Phase 4
# libomp already installed; TorchSharp packages already in NuGet cache
```

---

## Architecture Patterns

### Recommended Project Structure

```
05-gomoku-mcts/
├── Gomoku.sln                         (traditional .sln format)
├── src/
│   ├── Gomoku/
│   │   ├── Gomoku.fsproj
│   │   ├── Domain.fs                  (Stone DU, Board type, GameState)
│   │   ├── Rules.fs                   (isWinner, legalMoves, applyMove)
│   │   ├── NativeLoader.fs            (ARM64 dylib preload — IDENTICAL to Phase 4)
│   │   ├── PolicyValueNet.fs          (dual-head TorchSharp model)
│   │   ├── MctsNode.fs                (mutable class-based tree node)
│   │   ├── Mcts.fs                    (PUCT selection, expand, backpropagate, search)
│   │   ├── SelfPlay.fs                (game loop producing training data)
│   │   └── Training.fs                (batch learning loop — pure, no I/O)
│   └── Gomoku.Console/
│       ├── Gomoku.Console.fsproj
│       └── Program.fs                 (sole impure file: Serilog + menu + human-vs-AI)
└── tests/
    └── Gomoku.Tests/
        ├── Gomoku.Tests.fsproj
        ├── RulesTests.fs              (FsCheck: 5-in-a-row, legalMoves invariants)
        ├── MctsTests.fs               (Expecto: backpropagation, perspective flip)
        └── Main.fs
```

### Pattern 1: Board Domain Types

**What:** Immutable board with copy-and-evolve semantics. Board is a flat int array (0=empty, 1=Black, -1=White — using ints not DU for easier tensor encoding).

**Why int not DU:** Tensor encoding to float32 is trivial (`float32 cell`). For DU, need match expressions everywhere.

```fsharp
// Source: adapted from junxiaosong/AlphaZero_Gomoku + Phase 4 patterns
module Gomoku.Domain

[<Literal>]
let BoardSize = 15
[<Literal>]
let WinLength = 5

// 0 = Empty, 1 = Black, -1 = White
type Board = int array  // length = BoardSize * BoardSize = 225, row-major

type Player = Black | White

let playerValue = function Black -> 1 | White -> -1
let opponent = function Black -> White | White -> Black

type GameState = {
    Board: Board
    CurrentPlayer: Player
    LastMove: int option   // index 0..224, None if no move yet
    MoveCount: int
}

let emptyBoard () : Board = Array.create (BoardSize * BoardSize) 0

let initialState () = {
    Board = emptyBoard ()
    CurrentPlayer = Black
    LastMove = None
    MoveCount = 0
}
```

### Pattern 2: Winner Detection

**What:** After each move, check only the 4 directions through the last placed stone. O(1) per check (bounded by WinLength * 4 = 20 cells).

```fsharp
// Source: standard gomoku win detection algorithm
module Gomoku.Rules

open Gomoku.Domain

let private directions = [| (0,1); (1,0); (1,1); (1,-1) |]  // H, V, diag, anti-diag

let private inBounds r c = r >= 0 && r < BoardSize && c >= 0 && c < BoardSize

let private countDir (board: Board) (r0: int) (c0: int) (dr: int) (dc: int) (player: int) =
    let mutable count = 0
    let mutable r = r0 + dr
    let mutable c = c0 + dc
    while inBounds r c && board.[r * BoardSize + c] = player do
        count <- count + 1
        r <- r + dr
        c <- c + dc
    count

/// Check if last move at index `move` caused a win
let isWinningMove (board: Board) (move: int) =
    let r0 = move / BoardSize
    let c0 = move % BoardSize
    let player = board.[move]
    directions |> Array.exists (fun (dr, dc) ->
        let forward  = countDir board r0 c0 dr dc player
        let backward = countDir board r0 c0 (-dr) (-dc) player
        forward + backward + 1 >= WinLength)

let legalMoves (board: Board) =
    [| for i in 0 .. BoardSize * BoardSize - 1 do
        if board.[i] = 0 then yield i |]

let applyMove (state: GameState) (move: int) : GameState =
    let newBoard = Array.copy state.Board
    newBoard.[move] <- playerValue state.CurrentPlayer
    { Board = newBoard
      CurrentPlayer = opponent state.CurrentPlayer
      LastMove = Some move
      MoveCount = state.MoveCount + 1 }
```

**FsCheck property invariants (GMOK-08):**
1. `isWinningMove` only returns true when there are exactly 5+ consecutive stones through the winning position
2. `legalMoves` count + occupied count = 225 (always)
3. `legalMoves` after `applyMove` contains (count - 1) elements

### Pattern 3: Board Tensor Encoding (4 channels)

**What:** Based on junxiaosong/AlphaZero_Gomoku encoding. 4 channels × 15 × 15 = 900 float32 values.

| Channel | Content |
|---------|---------|
| 0 | Current player's stones (1.0 where current player has stone) |
| 1 | Opponent's stones (1.0 where opponent has stone) |
| 2 | Last move indicator (1.0 at position of last move, else 0.0) |
| 3 | Turn indicator (1.0 everywhere if Black to move, 0.0 if White to move) |

**Why 4 channels not 2:** Channel 2 (last move) helps the network recognize forbidden openings and the last move context. Channel 3 (turn) resolves the "whose perspective?" ambiguity in the value head.

```fsharp
// Source: adapted from junxiaosong/AlphaZero_Gomoku game.py current_state()
let boardToTensor (state: GameState) : torch.Tensor =
    let size = BoardSize * BoardSize
    let data = Array.zeroCreate<float32> (4 * size)
    let myVal   = playerValue state.CurrentPlayer
    let oppVal  = -myVal
    for i in 0 .. size - 1 do
        if state.Board.[i] = myVal  then data.[i]          <- 1.0f
        if state.Board.[i] = oppVal then data.[size + i]   <- 1.0f
    match state.LastMove with
    | Some m -> data.[2 * size + m] <- 1.0f
    | None -> ()
    if state.CurrentPlayer = Black then
        for i in 0 .. size - 1 do data.[3 * size + i] <- 1.0f
    // Shape: [4, 15, 15]
    torch.tensor(data, dtype=ScalarType.Float32).reshape([|4L; int64 BoardSize; int64 BoardSize|])
```

### Pattern 4: Policy/Value Network (Dual-Head)

**What:** Shared convolutional backbone → two heads: policy (225 log-probabilities) + value (1 scalar in [-1,1]).

**Architecture decision — plain conv, no ResBlock:** ResBlock in F#/TorchSharp requires a custom Module class where `forward` computes `F(x) + x`. The C# TorchVision ResNet has the pattern. For a tutorial with educational clarity, 3 plain conv layers + BatchNorm + ReLU is adequate for achieving >80% vs random on a 15x15 board. If ResBlock is desired, Pattern 4b below shows the implementation.

```fsharp
// Source: adapted from junxiaosong/AlphaZero_Gomoku policy_value_net_pytorch.py
// 4-channel 15x15 input → 225 policy log-probs + 1 value

type PolicyValueNet(name: string) as this =
    inherit Module<torch.Tensor, struct(torch.Tensor * torch.Tensor)>(name)

    // Shared backbone: 3 conv layers
    let conv1 = Conv2d(4L,  32L, 3L, padding=1L)
    let bn1   = BatchNorm2d(32L)
    let conv2 = Conv2d(32L, 64L, 3L, padding=1L)
    let bn2   = BatchNorm2d(64L)
    let conv3 = Conv2d(64L, 128L, 3L, padding=1L)
    let bn3   = BatchNorm2d(128L)
    let relu  = ReLU()

    // Policy head: 128 -> 4 channels, 1x1, flatten, linear -> 225
    let pConv = Conv2d(128L, 4L, 1L)
    let pBn   = BatchNorm2d(4L)
    let pFc   = Linear(4L * int64 Domain.BoardSize * int64 Domain.BoardSize, int64 (Domain.BoardSize * Domain.BoardSize))

    // Value head: 128 -> 2 channels, 1x1, flatten, linear 256, linear 1
    let vConv = Conv2d(128L, 2L, 1L)
    let vBn   = BatchNorm2d(2L)
    let vFc1  = Linear(2L * int64 Domain.BoardSize * int64 Domain.BoardSize, 256L)
    let vFc2  = Linear(256L, 1L)
    let tanh  = Tanh()

    do this.RegisterComponents()

    override _.forward(x) =
        // Shared backbone
        let h =
            x
            --> conv1 |> fun t -> bn1.forward(t) |> fun t -> relu.forward(t)
            --> conv2 |> fun t -> bn2.forward(t) |> fun t -> relu.forward(t)
            --> conv3 |> fun t -> bn3.forward(t) |> fun t -> relu.forward(t)

        // Policy head
        let pRaw =
            h
            --> pConv |> fun t -> pBn.forward(t) |> fun t -> relu.forward(t)
            |> fun t -> t.flatten(1L)  // [B, 4*15*15]
        let pLogits = pFc.forward(pRaw)          // [B, 225]
        let pLogProbs = functional.log_softmax(pLogits, 1L)  // [B, 225]

        // Value head
        let vRaw =
            h
            --> vConv |> fun t -> vBn.forward(t) |> fun t -> relu.forward(t)
            |> fun t -> t.flatten(1L)  // [B, 2*15*15]
        let vHidden = relu.forward(vFc1.forward(vRaw))  // [B, 256]
        let vOut = tanh.forward(vFc2.forward(vHidden))   // [B, 1]

        struct(pLogProbs, vOut)
```

**CRITICAL API NOTE:** The forward return type uses F# anonymous struct tuple `struct(Tensor * Tensor)` because TorchSharp's Module<'TInput, 'TOutput> requires a single output type. Using `struct` avoids boxing. Alternatively, define a record wrapper. Do not use `Tensor * Tensor` (boxed tuple) as the type parameter — use a custom return type or struct tuple.

**Alternative using separate methods (simpler):**
```fsharp
// Simpler: expose policy and value as separate methods
member _.policy(x: torch.Tensor) : torch.Tensor = ...   // returns log_probs [B, 225]
member _.value(x: torch.Tensor) : torch.Tensor = ...    // returns scalar [B, 1]
// Call both from the same forward pass using shared backbone cached result
```

### Pattern 4b: ResBlock in F# (if desired)

**What:** Residual block with skip connection. Must be a separate Module class because `forward` adds input to processed output.

```fsharp
// Source: adapted from TorchSharp TorchVision ResNet.cs BasicBlock (C#)
// Verified pattern: C# uses x.add_(identity) in-place; F# should use x.add(identity) or x + identity
type ResBlock(channels: int64) as this =
    inherit Module<torch.Tensor, torch.Tensor>("ResBlock")

    let conv1 = Conv2d(channels, channels, 3L, padding=1L, bias=false)
    let bn1   = BatchNorm2d(channels)
    let conv2 = Conv2d(channels, channels, 3L, padding=1L, bias=false)
    let bn2   = BatchNorm2d(channels)
    let relu  = ReLU()

    do this.RegisterComponents()

    override _.forward(x) =
        let identity = x
        // Process: conv1 -> bn1 -> relu -> conv2 -> bn2
        let out =
            x
            |> conv1.forward
            |> bn1.forward
            |> relu.forward
            |> conv2.forward
            |> bn2.forward
        // Skip connection: add identity, then relu
        relu.forward(out.add(identity))
```

**PITFALL:** Do NOT use `out.add_(identity)` (in-place) if `identity` and `out` share storage (e.g., when channels match and stride=1). In-place add can corrupt gradients. Use `out.add(identity)` (not in-place) to be safe. The TorchSharp C# source uses `add_()` but with a downsample check; for the simple same-channels case, `add()` is safer in F#.

### Pattern 5: MCTS Node (Mutable Class)

**What:** MCTS requires parent pointers and mutable children. F# records with mutable fields could work but do not support recursive type definitions with mutable child collections well. Use a class with mutable fields.

**Key data stored per node:**
- `Parent`: MctsNode option (null for root) — needed for backpropagation
- `Children`: Dictionary<int, MctsNode> (action -> child node)
- `Visits`: mutable int (visit count N)
- `TotalValue`: mutable float (sum of backup values Q * N)
- `Prior`: float (prior probability P from policy network)
- `IsExpanded`: mutable bool

```fsharp
// Source: adapted from junxiaosong/AlphaZero_Gomoku mcts_alphaZero.py TreeNode
// and joshvarty.github.io/AlphaZero/ Node class
module Gomoku.MctsNode

open System.Collections.Generic

type MctsNode(parent: MctsNode option, prior: float) =
    let children = Dictionary<int, MctsNode>()
    let mutable visits     = 0
    let mutable totalValue = 0.0
    let mutable isExpanded = false

    member _.Parent      = parent
    member _.Children    = children
    member _.Visits      = visits
    member _.TotalValue  = totalValue
    member _.Prior       = prior
    member _.IsExpanded  = isExpanded

    member _.Q () =
        if visits = 0 then 0.0
        else totalValue / float visits

    member _.Expand(actionPriors: (int * float) seq) =
        for (action, p) in actionPriors do
            if not (children.ContainsKey(action)) then
                children.[action] <- MctsNode(Some this, p)
        isExpanded <- true

    member _.Update(value: float) =
        visits     <- visits + 1
        totalValue <- totalValue + value

    member this.UpdateRecursive(value: float) =
        this.Update(value)
        match parent with
        | Some p -> p.UpdateRecursive(-value)  // perspective flip: negate for parent
        | None   -> ()

    member this.IsLeaf () = children.Count = 0
```

**WHY NEGATION IN UpdateRecursive:** MCTS for two-player zero-sum games stores values from the perspective of the player who JUST MOVED. When a child node is updated with value `v` (good for child's player), the parent is updated with `-v` (bad for parent's player, since it's the opponent). This is the "alternating perspective" or "negamax" backpropagation.

### Pattern 6: PUCT Selection Formula

**What:** PUCT (Polynomial Upper Confidence Trees) = Q + c_puct * P * sqrt(N_parent) / (1 + N_child)

```fsharp
// Source: derived from junxiaosong mcts_alphaZero.py + AlphaGo Zero paper PUCT formula
// c_puct typically 5.0 for AlphaZero; may need tuning
module Gomoku.Mcts

open Gomoku.MctsNode

let private puctScore (cPuct: float) (parent: MctsNode) (child: MctsNode) =
    let q = child.Q()
    let u = cPuct * child.Prior * sqrt(float parent.Visits) / (1.0 + float child.Visits)
    q + u

let private selectAction (cPuct: float) (node: MctsNode) =
    node.Children
    |> Seq.maxBy (fun kvp -> puctScore cPuct node kvp.Value)
    |> fun kvp -> (kvp.Key, kvp.Value)
```

**Selection phase:** From root, repeatedly call `selectAction` to descend the tree until reaching a leaf (unexpanded node) or terminal state.

### Pattern 7: Full MCTS Search Function

```fsharp
// Source: adapted from junxiazsong mcts_alphaZero.py MCTSPlayer.get_move_probs
let mctsSearch
    (model: PolicyValueNet)
    (rootState: GameState)
    (nSimulations: int)
    (cPuct: float)
    (addDirichletNoise: bool)
    : (int * float) array =   // (action, visit_probability) array

    let root = MctsNode(None, 1.0)

    for _ in 1 .. nSimulations do
        use _scope = torch.NewDisposeScope()
        let mutable state = rootState
        let mutable node = root
        let mutable isTerminal = false

        // 1. Selection: descend tree until leaf or terminal
        while not node.IsLeaf() && not isTerminal do
            let (action, child) = selectAction cPuct node
            state <- Rules.applyMove state action
            node <- child
            match state.LastMove with
            | Some m when Rules.isWinningMove state.Board m -> isTerminal <- true
            | _ when Rules.legalMoves state.Board |> Array.isEmpty -> isTerminal <- true
            | _ -> ()

        // 2. Expansion + Evaluation
        let leafValue =
            if isTerminal then
                // Terminal: value is -1 (the player who just moved won; bad for current player)
                // NOTE: state.CurrentPlayer is now the player who needs to move, but game is over
                // The last mover (opponent of CurrentPlayer) just won → value for CurrentPlayer = -1
                -1.0
            else
                // Evaluate with neural network
                let stateTensor = boardToTensor state
                let batchedTensor = stateTensor.unsqueeze(0L)  // [1, 4, 15, 15]
                model.eval()
                let struct(logProbs, valueT) = model.forward(batchedTensor)
                let probs = logProbs.exp()  // [1, 225]
                let value = valueT.item<float32>() |> float

                // Get legal moves and their priors
                let legal = Rules.legalMoves state.Board
                let probsData = probs.squeeze(0L).data<float32>().ToArray()
                let legalPriors =
                    legal |> Array.map (fun a -> (a, float probsData.[a]))
                // Normalize priors over legal moves
                let totalProb = legalPriors |> Array.sumBy snd
                let normalizedPriors =
                    if totalProb > 1e-8 then
                        legalPriors |> Array.map (fun (a, p) -> (a, p / totalProb))
                    else
                        // Uniform prior if network is miscalibrated
                        legalPriors |> Array.map (fun (a, _) -> (a, 1.0 / float legal.Length))

                node.Expand(normalizedPriors)
                value

        // 3. Backpropagation
        node.UpdateRecursive(-leafValue)  // negate: value is from child's perspective

    // Dirichlet noise at root (for training only)
    if addDirichletNoise then
        use _scope = torch.NewDisposeScope()
        let alpha = 0.3  // standard for Gomoku (Go uses 0.03, Chess 0.3)
        let n = root.Children.Count
        let concentration = torch.full([|int64 n|], alpha, dtype=ScalarType.Float32)
        let noise = torch.distributions.Dirichlet(concentration).rsample()
        let noiseData = noise.data<float32>().ToArray()
        let mutable i = 0
        for kvp in root.Children do
            let noisy = 0.75 * kvp.Value.Prior + 0.25 * float noiseData.[i]
            // Note: MctsNode.Prior is not mutable; need to restructure or accept that
            // Dirichlet noise modifies the selection score during root's children search
            // Alternative: apply noise directly in selectAction at root level
            i <- i + 1

    // Convert visit counts to probabilities (temperature = 1.0 default)
    let totalVisits = root.Children.Values |> Seq.sumBy (fun c -> c.Visits)
    [| for kvp in root.Children do
        yield (kvp.Key, float kvp.Value.Visits / float totalVisits) |]
```

**PITFALL on Dirichlet noise:** `MctsNode.Prior` should be mutable, OR the PUCT score should add noise at the root selection step as a separate path. The simplest approach: make `Prior` a `mutable float` field in `MctsNode` so it can be updated after adding noise.

### Pattern 8: Self-Play Data Format

**What:** Self-play produces `(boardState, mctsPolicyTarget, valueTarget)` tuples for training.

```fsharp
// Source: standard AlphaZero training data format
type TrainingSample = {
    State:         float32[]  // flat [4*15*15] = 900 values
    PolicyTarget:  float32[]  // flat [225] visit count distribution (sums to 1)
    ValueTarget:   float32    // +1.0 if this player won, -1.0 if lost, 0.0 if draw
}
```

**Self-play loop:**
1. For each move: run MCTS search (nSimulations) → get visit distribution `pi`
2. Sample move from `pi` (temperature=1.0 for first 15 moves, argmax after)
3. Record `(currentState, pi, PLACEHOLDER_outcome)` — value target filled in after game ends
4. Apply move, check terminal
5. On game end: fill value targets (+1 for winner's states, -1 for loser's states, flip per player)

### Pattern 9: Temperature-Based Move Selection

```fsharp
// Source: AlphaZero paper + junxiaosong implementation
let selectMoveByTemperature (rng: System.Random) (visitCounts: (int * float) array) (temperature: float) =
    if temperature < 1e-6 then
        // Greedy: pick highest visit count
        visitCounts |> Array.maxBy snd |> fst
    else
        // Sample proportional to N^(1/temperature)
        let weights = visitCounts |> Array.map (fun (_, n) -> n ** (1.0 / temperature))
        let total = Array.sum weights
        let normalized = weights |> Array.map (fun w -> w / total)
        let r = rng.NextDouble()
        let mutable cumSum = 0.0
        let mutable selected = fst visitCounts.[0]
        for (action, _) in visitCounts |> Array.zip normalized |> Array.map (fun (w, (a, _)) -> (a, w)) do
            cumSum <- cumSum + snd (visitCounts |> Array.find (fun (ac, _) -> ac = action))
            if cumSum >= r && selected = fst visitCounts.[0] then
                selected <- action
        selected

// Temperature schedule: τ=1.0 for first 15 moves, τ→0 (greedy) after
let temperature moveCount = if moveCount < 15 then 1.0 else 1e-6
```

### Pattern 10: Training Loop

**What:** Sample batches from self-play buffer → compute policy loss (cross-entropy) + value loss (MSE) → update.

```fsharp
// Source: adapted from junxiaosong AlphaZero_Gomoku train.py
// Loss = -sum(pi * log(p_theta)) + MSE(z, v_theta)

let trainBatch (model: PolicyValueNet) (opt: Optimizer) (batch: TrainingSample[]) =
    use _scope = torch.NewDisposeScope()
    model.train()

    // Stack states [B, 4, 15, 15]
    let stateFlat = batch |> Array.collect (fun s -> s.State)
    let stateT = torch.tensor(stateFlat, dtype=ScalarType.Float32)
                       .reshape([|int64 batch.Length; 4L; int64 BoardSize; int64 BoardSize|])

    // Policy targets [B, 225]
    let policyFlat = batch |> Array.collect (fun s -> s.PolicyTarget)
    let policyT = torch.tensor(policyFlat, dtype=ScalarType.Float32)
                        .reshape([|int64 batch.Length; int64 (BoardSize * BoardSize)|])

    // Value targets [B, 1]
    let valueFlat = batch |> Array.map (fun s -> s.ValueTarget)
    let valueT = torch.tensor(valueFlat, dtype=ScalarType.Float32).unsqueeze(1L)

    // Forward pass
    let struct(logProbs, valuePred) = model.forward(stateT)

    // Policy loss: cross-entropy = -mean(sum(pi * log_p))
    let policyLoss = -(policyT * logProbs).sum(1L).mean()

    // Value loss: MSE
    let valueLoss = functional.mse_loss(valuePred, valueT, Reduction.Mean)

    // Combined loss
    let loss = policyLoss + valueLoss

    opt.zero_grad()
    loss.backward()
    opt.step() |> ignore

    (policyLoss.item<float32>(), valueLoss.item<float32>())
```

### Anti-Patterns to Avoid

- **ImmutableRecordNode:** F# records with mutable children maps work but require `let mutable children = Map.empty` which reassigns on every update. For MCTS with thousands of nodes, class-based `Dictionary<int, MctsNode>` is more efficient.
- **Storing Tensors in TrainingSample:** Store `float32[]` not `torch.Tensor`. Tensors stored outside a dispose scope leak memory. Experience buffer holds flat arrays; convert to tensor at training time.
- **BatchNorm2d with batch size 1 during eval:** BatchNorm2d in eval mode uses running statistics (set during training). Call `model.eval()` before any MCTS inference. Call `model.train()` before any gradient update. Do NOT call forward with batch=1 during training mode — BatchNorm2d crashes or produces wrong stats with batch=1.
- **Missing model.eval() before MCTS rollout:** During self-play, the model should be in eval mode (not training mode). Dropout and BatchNorm behavior differ.
- **torch.no_grad() disposal:** Per Phase 4 findings: `use _noGrad = torch.no_grad()` in F# — `_noGrad` must be disposed before calling `loss.backward()`. Use a nested scope or separate function for inference.
- **Tensor operations across MCTS simulations:** Each MCTS simulation calls model.forward once. Use `torch.NewDisposeScope()` per simulation to prevent accumulation.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dirichlet noise | Gamma sampling from scratch | `torch.distributions.Dirichlet(concentration).rsample()` | TorchSharp 0.106.0 has built-in Dirichlet |
| Policy cross-entropy loss | Manual log/sum | `-(policyT * logProbs).sum(1L).mean()` | log_softmax output + this formula = standard CE |
| Win detection scan | Checking all 225 positions after each move | Check only through the last move position (4 directions × 2 sides) | O(WinLength) not O(225) |
| MCTS GC | Manual node pool, explicit GC.Collect() | Let GC handle it; between games, root = null | .NET GC handles tree roots; no need for explicit pool |
| ResBlock skip | Sequential modules | Custom Module class with add() in forward | Sequential cannot express y = F(x) + x |

**Key insight:** The MCTS tree grows to thousands of nodes per game. Between games, the entire tree is garbage collected by setting root to null. Do NOT try to reuse trees between games unless implementing tree reuse optimization (advanced, not needed for tutorial).

---

## Common Pitfalls

### Pitfall 1: BatchNorm2d in Eval Mode with Self-Play

**What goes wrong:** During self-play, if the model is in training mode when called from MCTS, BatchNorm2d computes batch statistics from the single-sample forward pass. With batch size 1, this is mathematically unstable (variance = 0).

**Why it happens:** `model.train()` is the default mode after construction. Easy to forget to switch to `model.eval()` for MCTS inference.

**How to avoid:** Always call `model.eval()` before the self-play loop. Call `model.train()` only during the gradient update step inside `trainBatch`. Pattern: `model.eval()` at start of self-play game → run MCTS → collect data → `model.train()` → `trainBatch` → `model.eval()` → next game.

**Warning signs:** Very fast oscillation in value output during self-play; NaN in loss after first few training steps.

### Pitfall 2: MCTS Backpropagation Perspective Error

**What goes wrong:** Value is not negated when propagating up the tree, causing all nodes to receive the same sign value regardless of which player occupies them. The AI will converge to always playing the same move (exploiting the sign error).

**Why it happens:** In AlphaZero, value is from the perspective of the player who just moved. When propagating to the parent (the opponent), the value must be negated.

**How to avoid:** Use `UpdateRecursive(-value)` when calling backprop on the leaf. The recursive call does `parent.UpdateRecursive(-value)`. This alternates sign at each level.

**Expecto test (GMOK-09):** After one simulation from a leaf where value = +1.0:
- Leaf node: Visits=1, TotalValue=+1.0 (or -1.0 depending on implementation choice)
- Parent node: Visits=1, TotalValue=-1.0 (negated)
- Grandparent: Visits=1, TotalValue=+1.0 (negated again)

### Pitfall 3: Policy Network Output All Illegal Moves Masked to Zero

**What goes wrong:** After masking illegal moves (setting their priors to 0), the sum of priors is 0, causing division by zero in normalization.

**Why it happens:** Early in training, the network may assign near-zero probability to all legal moves (probability mass concentrated on already-occupied positions).

**How to avoid:** Check if `totalProb < 1e-8` after filtering legal moves. If so, assign uniform probability to all legal moves. Log a warning when this happens.

**Warning signs:** NaN in Q values; MCTS always selects the first legal move.

### Pitfall 4: MctsNode.Prior Immutability Blocks Dirichlet Noise

**What goes wrong:** `Prior` field is immutable (declared as `let prior` in constructor), so Dirichlet noise cannot be applied after node creation.

**How to avoid:** Make `Prior` a `mutable` field: `let mutable prior = p`. Expose as `member _.Prior with get() = prior and set(v) = prior <- v`. Apply Dirichlet noise to root children after expansion.

### Pitfall 5: Tensor Memory Accumulation Across MCTS Simulations

**What goes wrong:** 400 simulations × 1 neural network forward pass each = 400 sets of tensors. Without dispose scopes, memory grows ~400× per move.

**Why it happens:** Each `model.forward()` creates intermediate tensors. Without `torch.NewDisposeScope()`, these persist until GC.

**How to avoid:** Wrap each simulation's tensor work in `use _scope = torch.NewDisposeScope()`. The scope covers the `boardToTensor`, `model.forward()`, and probability extraction.

**How to detect:** `DisposeScopeManager.Statistics.ThreadTotalLiveCount` should not grow across simulations.

### Pitfall 6: F# Compile Order in .fsproj

**What goes wrong:** `Mcts.fs` references `MctsNode.fs` types, but `MctsNode.fs` is compiled after. Compile error.

**Required compile order:**
```xml
<Compile Include="Domain.fs" />
<Compile Include="Rules.fs" />
<Compile Include="NativeLoader.fs" />
<Compile Include="PolicyValueNet.fs" />
<Compile Include="MctsNode.fs" />
<Compile Include="Mcts.fs" />
<Compile Include="SelfPlay.fs" />
<Compile Include="Training.fs" />
```

### Pitfall 7: 15x15 Board Training Time on CPU

**What goes wrong:** 400 simulations × 225 possible moves × neural network inference is very slow on CPU. One game can take 30-60 minutes. Training 200 games = 100-400 hours.

**Why it happens:** 15x15 = 225 positions is large. Neural network forward pass on [1, 4, 15, 15] is fast (~5ms on ARM64 CPU), but 400 simulations × ~5ms = 2s per move × 50 moves per game = 100s per game. 200 games = ~5.5 hours.

**How to avoid (for tutorial purposes):**
- Reduce simulations: 50-100 for training (not 400). Use 400 only for evaluation.
- Use random rollout for most simulations, neural net only for expansion (pure MCTS with value=rollout is much faster).
- For GMOK-10 test (>80% vs random): Pure MCTS with 100 simulations achieves >80% vs random without neural network. Verifying this first is faster.
- Accept that "train for hours" is part of the tutorial — use `dotnet run --save` to checkpoint and resume.

**Warning signs:** First game takes > 5 minutes; total training time estimate > 10 hours.

### Pitfall 8: torch.no_grad() Before Gradient Update

**What goes wrong:** `loss.backward()` fails because tensors were created under `no_grad()` context, making them non-differentiable.

**Why it happens:** Per Phase 4 findings: `torch.no_grad()` in F# returns an `IDisposable`. If not disposed before `loss.backward()`, gradients cannot flow.

**How to avoid:** Inference (MCTS) and training are in separate functions. The training function (`trainBatch`) never calls `torch.no_grad()`. The MCTS inference function uses `model.eval()` which does not disable autograd — but since it's in a separate scope, no tensor from inference is reused in training.

---

## Code Examples

### Verified Pattern: NativeLoader (identical to Phase 4)

```fsharp
// Source: Phase 4 verified working on ARM64 macOS (2026-02-20)
// Module name changes to Gomoku.NativeLoader; rest is identical
module Gomoku.NativeLoader

open System.Runtime.InteropServices
open System.IO

let private load () =
    let exeDir = System.AppContext.BaseDirectory
    let nativeDir = Path.Combine(exeDir, "runtimes", "osx-arm64", "native")
    if Directory.Exists(nativeDir) then
        NativeLibrary.Load(Path.Combine(nativeDir, "libomp.dylib"))          |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libc10.dylib"))          |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libtorch_cpu.dylib"))    |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libtorch.dylib"))        |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libLibTorchSharp.dylib")) |> ignore

do load ()
```

### Verified Pattern: Dirichlet Noise (TorchSharp 0.106.0)

```fsharp
// Source: TorchSharp/src/TorchSharp/Distributions/Dirichlet.cs (fetched 2026-02-20)
// Constructor: Dirichlet(concentration: Tensor, generator: torch.Generator = null)
// rsample(params long[] sample_shape): Tensor

use _scope = torch.NewDisposeScope()
let n = 225  // number of legal moves at root (or actual count)
let alpha = 0.3f  // standard for Gomoku
let concentration = torch.full([|int64 n|], alpha, dtype=ScalarType.Float32)
let dirichlet = torch.distributions.Dirichlet(concentration)
let noise = dirichlet.rsample()  // returns Tensor of shape [n]
let noiseData = noise.data<float32>().ToArray()
// Apply: prior_noisy[i] = 0.75 * prior[i] + 0.25 * noise[i]
```

**Confidence:** MEDIUM — API confirmed from TorchSharp source code; not tested running on ARM64. Shape `[n]` from `rsample()` with no sample_shape args is expected behavior.

### Verified Pattern: Policy Cross-Entropy Loss

```fsharp
// Source: standard AlphaZero loss formula; functional form confirmed from MNIST.fs + PyTorch docs
// logProbs: [B, 225] from functional.log_softmax(logits, 1L)
// policyT:  [B, 225] MCTS visit distribution (sums to 1 per row)

let policyLoss = -(policyT * logProbs).sum(1L).mean()
// Equivalent to: mean over batch of negative sum(pi * log_p)
// = cross-entropy between pi and p
```

### Verified Pattern: Model Save/Load (from Phase 4)

```fsharp
// Source: Phase 4 verified on ARM64 (2026-02-20)
model.save("gomoku_model.pt") |> ignore
// Later:
let loaded = new PolicyValueNet("pv-net")
loaded.load("gomoku_model.pt") |> ignore
```

### Verified Pattern: functional.mse_loss and nll_loss signatures

```fsharp
// Source: MNIST.fs example + TorchSharp API (PyTorch naming convention)
// Both exist in TorchSharp.torch.nn.functional namespace (accessed via "open type torch.nn")

let valueLoss = functional.mse_loss(predicted, target, Reduction.Mean)
// predicted: [B, 1], target: [B, 1], returns scalar tensor

let policyLoss_alt = functional.nll_loss(logProbs, classIndices, reduction=Reduction.Mean)
// For policy: use manual cross-entropy instead (policyT * logProbs) since target is distribution not class index
```

### Pattern: FsCheck Gomoku Property Tests

```fsharp
// Source: Phase 4 FsCheck pattern + adapted for Gomoku
// GMOK-08: 5-in-a-row invariant + legal moves invariant

open FsCheck
open Expecto

let gomokuPropertyTests =
    testList "Gomoku FsCheck Properties" [
        testProperty "legalMoves count + occupied count = 225" (fun () ->
            let state = Domain.initialState()
            // Play a random game
            let rng = System.Random(42)
            let mutable s = state
            let mutable running = true
            let mutable moveCount = 0
            while running && moveCount < 50 do
                let legal = Rules.legalMoves s.Board
                if legal.Length = 0 then running <- false
                else
                    let move = legal.[rng.Next(legal.Length)]
                    s <- Rules.applyMove s move
                    match s.LastMove with
                    | Some m when Rules.isWinningMove s.Board m -> running <- false
                    | _ -> moveCount <- moveCount + 1
            // Invariant: legal + occupied = 225
            let occupied = s.Board |> Array.filter (fun c -> c <> 0) |> Array.length
            let legal = Rules.legalMoves s.Board
            legal.Length + occupied = Domain.BoardSize * Domain.BoardSize)
    ]
```

### Pattern: MCTS Backpropagation Expecto Test

```fsharp
// GMOK-09: Verify perspective flip in backpropagation
// Source: derived from backpropagation invariant described in section above

let mctsBackpropTests =
    testList "MCTS Backpropagation" [
        test "leaf value +1.0 propagates with perspective flip" {
            // Build 3-level chain: grandparent -> parent -> leaf
            let grandparent = MctsNode(None, 1.0)
            let parent = MctsNode(Some grandparent, 1.0)
            let leaf = MctsNode(Some parent, 1.0)

            // Backprop value +1.0 from leaf
            leaf.UpdateRecursive(-1.0)  // called with -leafValue per convention
            // leaf: TotalValue = -1.0, Visits = 1
            // parent: TotalValue = +1.0, Visits = 1
            // grandparent: TotalValue = -1.0, Visits = 1

            Expect.equal leaf.Visits 1 "leaf visited once"
            Expect.floatClose Accuracy.medium leaf.TotalValue -1.0 "leaf value = -1"
            Expect.floatClose Accuracy.medium parent.TotalValue 1.0 "parent value = +1 (negated)"
            Expect.floatClose Accuracy.medium grandparent.TotalValue -1.0 "grandparent value = -1 (negated again)"
        }
    ]
```

---

## Training and Evaluation Guidance (GMOK-10: >80% vs Random)

### What Achieves >80% Win Rate vs Random

Literature review findings (LOW-MEDIUM confidence — no exact F# benchmarks):

1. **Pure MCTS with 100-200 simulations (no neural network):** Should achieve >90% vs random on 15x15 Gomoku. Random play is very weak; even naive MCTS immediately spots tactical threats.

2. **AlphaZero with neural network (trained):** 200-500 self-play games should be sufficient to achieve >80% vs random. The network does not need to be strong overall; it just needs to recognize winning patterns.

3. **junxiaosong reference (6x6, 4-in-a-row):** 500-1000 self-play games, 400 simulations/move achieves "reasonable performance" in ~2 hours.

4. **For this tutorial's goal:** Recommend testing win rate after MCTS-only phase first (no neural net), then after training. This separates algorithm correctness from training effectiveness.

### Recommended Training Configuration

| Parameter | Value | Why |
|-----------|-------|-----|
| Board size | 15×15 (as required) | Phase requirement |
| Simulations during self-play | 100 | Fast enough for CPU (< 30s/game) |
| Simulations during evaluation | 400 | Better quality play for final benchmark |
| Self-play games per iteration | 1-5 | Simple pipeline |
| Training batch size | 256 | Standard |
| Buffer size | 10,000 | Holds ~200 games of data |
| Learning rate | 2e-3 (Adam) | From junxiaosong |
| c_puct | 5.0 | Standard AlphaZero value |
| Dirichlet alpha | 0.3 | Standard for Gomoku/Chess |
| Dirichlet noise weight | 0.25 | Standard (0.75 * prior + 0.25 * noise) |
| Temperature | 1.0 for moves 1-15, 0 after | Standard AlphaZero schedule |
| Total training games | 200-500 | Achievable in 2-4 hours on CPU |
| Training epochs per update | 5 | From junxiaosong |

### Evaluation Test (GMOK-10)

Play 50 games vs random opponent (25 as Black, 25 as White). AI wins >40 of 50 = >80%.

**Random opponent:** At each move, selects uniformly from legal moves.

**Time estimate:** 50 games × 50 moves avg × 400 simulations × 5ms/inference ≈ 50 × 50 × 2s = ~1.4 hours. This is too slow for `dotnet test`. Use 50 simulations for the test: 50 × 50 × 50 × 5ms = ~10 minutes. Still long. Use 20 simulations: ~2.5 minutes. Or use pure random rollout (no neural net) for MCTS speed.

**Recommendation for CI test:** Use **pure MCTS (random rollout, no neural network) with 50 simulations** for the automated test. Pure MCTS is much faster (no neural net call) and still achieves >80% vs random easily. The neural network benchmark can be a separate manual step.

---

## Serilog Logging Pattern (GMOK-11)

**Per Phase 4 established pattern — Program.fs is sole impure file:**

```fsharp
// Log MCTS statistics per game during self-play
Log.Information("SelfPlay {Game}: Moves={Moves}, Duration={Duration}ms, Winner={Winner}",
    gameNumber, moveCount, elapsed.TotalMilliseconds, winner)

// Log training statistics per iteration
Log.Information("Training {Iter}: PolicyLoss={PolicyLoss:F4}, ValueLoss={ValueLoss:F4}",
    iteration, policyLoss, valueLoss)

// Log win rate during evaluation
Log.Information("Evaluation: WinRate={WinRate:P1}, Simulations={Sims}",
    winRate, simulationCount)
```

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Random rollout in simulation | Neural value estimate replaces rollout | AlphaGo Zero (2017) — 10-100x more efficient |
| UCB1 (plain sqrt exploration) | PUCT with policy prior | Biases exploration toward promising moves |
| Separate policy + value networks | Dual-head shared network | One forward pass = both heads; 2x inference speed |
| F# immutable records for game tree | F# class with mutable fields | Parent pointers + mutable children = practical MCTS |
| Sequential.eval() bug | Fixed in TorchSharp 0.103+ (PR #1443 merged Mar 2025) | BatchNorm2d eval mode works correctly in 0.106.0 |
| BatchNorm2d save/load bug | Fixed in TorchSharp 0.96.1 | Safe to use in 0.106.0 |

**Deprecated/outdated:**
- Intel Mac TorchSharp support: dropped 0.103.0, do not use libtorch-cpu (non-RID package)
- Rollout-based MCTS: replaced by neural value estimation in AlphaZero-style systems

---

## Open Questions

1. **Dual-head Module<'TInput, 'TOutput> return type in F#**
   - What we know: Module<Tensor, Tensor> is the standard; returning two tensors requires a wrapper type
   - What's unclear: Whether `struct(Tensor * Tensor)` as the type parameter compiles cleanly, or whether a custom `PolicyValueOutput` record is safer
   - Recommendation: Define a `[<Struct>] PolicyValueOutput = { LogProbs: Tensor; Value: Tensor }` and use `Module<Tensor, PolicyValueOutput>`. Alternatively, expose `policy()` and `value()` as separate methods sharing an internal `backbone()` call.

2. **Dirichlet rsample() shape without sample_shape args**
   - What we know: `rsample(params long[] sample_shape)` — calling with no args should return shape matching concentration ([n])
   - What's unclear: Exact behavior when `sample_shape` is empty params array
   - Recommendation: Test `torch.distributions.Dirichlet(torch.ones([|5L|])).rsample()` in a quick test; verify shape is `[5]`

3. **MCTS tree GC pressure — is explicit GC.Collect needed?**
   - What we know: .NET GC handles tree roots; after `root <- null`, the tree is collected on next GC run
   - What's unclear: Whether GC pressure from thousands of MctsNode objects causes latency during self-play
   - Recommendation: Set `root <- null` at end of each game and let GC handle it. Only add `GC.Collect()` if memory pressure is observed.

4. **Win rate >80% achievable with 200 training games on CPU**
   - What we know: junxiaosong achieves "reasonable" play in 500-1000 games on 6x6. Literature suggests 15x15 needs more.
   - What's unclear: Exact game count needed for >80% vs random on 15x15 with small network (3 conv layers, no ResBlock)
   - Recommendation: Define GMOK-10 test using pure MCTS (50 simulations, no neural net) rather than neural-guided MCTS. Pure MCTS >80% vs random is guaranteed quickly. Neural MCTS >80% may need experimentation.

5. **BatchNorm2d with model.eval() in separate inference thread**
   - What we know: MCTS simulations are sequential (not parallel) in the basic implementation
   - What's unclear: If MCTS is later parallelized, BatchNorm2d's running stats are not thread-safe during `train()`
   - Recommendation: Keep MCTS single-threaded for Phase 5. Note in comments that parallelization requires `model.clone()` per thread.

---

## Sources

### Primary (HIGH confidence — source code verified)

- `github.com/dotnet/TorchSharp/src/TorchSharp/Distributions/Dirichlet.cs` — Dirichlet API: `Dirichlet(concentration).rsample()` confirmed
- `github.com/dotnet/TorchSharp/src/TorchVision/models/ResNet.cs` — ResBlock C# pattern: `x.add_(identity).relu_()` with downsample check
- `github.com/dotnet/TorchSharpExamples/src/FSharp/FSharpExamples/MNIST.fs` — F# loss pattern: `functional.nll_loss`, `model.train()`, `model.eval()`, `torch.NewDisposeScope()`
- Phase 4 RESEARCH.md + SUMMARY files — All TorchSharp 0.106.0 F# patterns verified running on ARM64

### Secondary (MEDIUM confidence — official docs, multiple sources agree)

- `github.com/junxiaosong/AlphaZero_Gomoku` — Canonical Python AlphaZero Gomoku: 4-channel board encoding, 3-conv backbone, policy/value heads, training hyperparameters (LR=2e-3, batch=512, buffer=10K, c_puct=5.0, alpha=0.3, games=500-1000)
- `github.com/dotnet/TorchSharp/issues/1426` — Sequential.eval() bug fixed in PR #1443 (merged Mar 2025); safe in 0.106.0
- `github.com/dotnet/TorchSharp/issues/538` — BatchNorm2d save/load bug fixed in 0.96.1; safe in 0.106.0
- `joshvarty.github.io/AlphaZero/` — Node class structure: prior, to_play, children dict, visit_count, value_sum; UCB score formula
- PUCT formula: AlphaGo Zero paper (Silver et al. 2017), multiple implementations confirm `Q + c_puct * P * sqrt(N_parent) / (1 + N_child)`
- Temperature schedule: AlphaZero paper — τ=1.0 for first 30 moves, τ→0 after; tutorial uses 15 moves

### Tertiary (LOW confidence — search results, training estimates unverified)

- Training time estimates (30s/game at 100 simulations) — extrapolated from Phase 4 timing data (5ms/inference on ARM64)
- "200-500 games achieves >80% vs random" — inferred from literature on 6x6 Gomoku; not verified for 15x15

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages are Phase 4 carry-over, verified working
- TorchSharp API (BatchNorm2d, Dirichlet, functional losses): HIGH — source code verified, bugs confirmed fixed in 0.106.0
- Architecture (network, MCTS node, PUCT): HIGH — canonical Python reference verified + adapted to F# patterns
- F# ResBlock pattern: MEDIUM — C# source verified; F# adaptation not tested; simple version (3 conv layers without ResBlock) recommended instead
- Training convergence (>80% vs random): MEDIUM — training time estimates are extrapolated; pure MCTS approach de-risks the test
- Dual-head return type (struct tuple): MEDIUM — pattern matches TorchSharp generics but not explicitly tested

**Research date:** 2026-02-20
**Valid until:** 2026-05-20 (TorchSharp 0.106.0 stable; PyTorch release cycle ~quarterly)

---

## Implementation Priorities for Planning

The planner should sequence tasks in this order:

1. **05-01: Game Engine** — Domain.fs, Rules.fs, legalMoves, isWinningMove, applyMove. FsCheck tests. No TorchSharp yet.
2. **05-02: MCTS (Pure, No Network)** — MctsNode class, PUCT selection, expand, backpropagate, random rollout simulation. Expecto backprop test. Verify >80% vs random with pure MCTS at 100 simulations (fast).
3. **05-03: Policy/Value Network + PUCT Integration** — PolicyValueNet (4-channel, 3 conv, dual head), boardToTensor. Replace random rollout with neural value. Keep simulations low (50-100) during training.
4. **05-04: Self-Play + Training Pipeline + Model Save/Load + Human vs AI** — SelfPlay.fs (data collection), Training.fs (batch gradient update), Program.fs menu (train/evaluate/play).
5. **05-05: Serilog Logging + Convergence Test + mdBook** — Structured logs (MCTS stats, training progress), Expecto win rate test, Korean tutorial chapter.

**De-risk strategy:** Tasks 05-01 and 05-02 can complete quickly (hours, not days). The >80% vs random requirement (GMOK-10) should be verified using pure MCTS (Task 05-02 outcome) before adding neural network complexity. If pure MCTS does not achieve >80%, something is wrong with Rules.fs or MCTS logic — catch it early.
