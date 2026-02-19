# Pitfalls Research

**Domain:** F# Reinforcement Learning Tutorial (mdBook + Console Projects)
**Researched:** 2026-02-19
**Confidence:** HIGH

---

## Critical Pitfalls

### Pitfall 1: TorchSharp Tensor Memory Leak — No DisposeScope

**What goes wrong:**
TorchSharp tensors are native (C++) objects. The .NET GC does not know the size of native tensor memory, so it does not trigger collection in time. In a training loop that creates thousands of tensors per episode, memory grows unboundedly until the process crashes with an out-of-memory error or becomes extremely slow.

**Why it happens:**
F# developers expect GC to handle cleanup. When a tensor goes out of scope in F#, the managed wrapper is collected eventually, but not the native memory behind it. The .NET GC sees only a tiny wrapper object, not 100 MB of tensor data. In a tight DQN training loop with batch_size=64 and thousands of steps, unreleased tensors accumulate rapidly.

**How to avoid:**
Wrap every computation-heavy block with `torch.NewDisposeScope()`. All tensors created inside the scope are disposed when the scope ends.

```fsharp
// WRONG — tensors leak in training loop
let trainStep (policy: Module) (batch: Experience[]) =
    let states = batch |> Array.map boardToTensor |> torch.stack
    let qValues = policy.forward(states)
    let loss = computeLoss qValues batch
    optimizer.step()
    // states, qValues, loss tensors NOT disposed

// CORRECT — DisposeScope handles all temporaries
let trainStep (policy: Module) (batch: Experience[]) =
    use _ = torch.NewDisposeScope()
    let states = batch |> Array.map boardToTensor |> torch.stack
    let qValues = policy.forward(states)
    let loss = computeLoss qValues batch
    optimizer.step()
    // everything disposed at end of scope
```

Add `GC.Collect()` after each mini-batch to let the GC reclaim the managed wrappers promptly. Note: Adam optimizer creates internal tensors that are DETACHED from DisposeScope — these are managed by the optimizer's lifetime.

**Warning signs:**
- Process memory grows steadily during training and never decreases
- Training slows down over time without change in model size
- `System.OutOfMemoryException` after many episodes
- Task Manager shows dotnet process consuming gigabytes of RAM after Phase 4 training starts

**Phase to address:** Phase 4 (DQN). Phase 5 (MCTS + Policy/Value Net) inherits this risk.

---

### Pitfall 2: MCTS Backpropagation Perspective Bug — Value Not Flipped

**What goes wrong:**
In a two-player zero-sum game (Connect Four, Gomoku), the MCTS node stores win statistics from the perspective of the player who was to move at that node. If the value from a simulation is not flipped when propagating up the tree, every node accumulates value from the WRONG perspective. The tree gives high UCT scores to moves that are good for the opponent, causing the AI to play as if trying to lose.

**Why it happens:**
The bug is subtle: when you back-propagate a value of +1 (win for current player at leaf), the parent node represents the opponent's turn. From the opponent's viewpoint, that +1 is a -1. Developers write a simple add-to-sum without thinking about whose perspective each level represents.

**How to avoid:**
Negate the value at each step of backpropagation:

```fsharp
// WRONG — accumulates from leaf player's perspective always
let rec backpropagate (node: MctsNode) (value: float) =
    node.Visits <- node.Visits + 1
    node.TotalValue <- node.TotalValue + value  // BUG: not flipped
    match node.Parent with
    | Some parent -> backpropagate parent value  // same value, wrong at parent
    | None -> ()

// CORRECT — flip value at each level
let rec backpropagate (node: MctsNode) (value: float) =
    node.Visits <- node.Visits + 1
    node.TotalValue <- node.TotalValue + value
    match node.Parent with
    | Some parent -> backpropagate parent (-value)  // flip: opponent's view
    | None -> ()
```

**Warning signs:**
- AI consistently makes obviously losing moves (e.g., ignoring winning line, blocking nothing)
- Higher MCTS simulation count makes performance WORSE instead of better
- AI loses reliably to random play
- When printing Q-values, winning terminal states show negative values

**Phase to address:** Phase 5 (Gomoku + MCTS). This is the hardest bug to detect because training still "runs" without errors.

---

### Pitfall 3: DQN Terminal State Target Bug — Bootstrapping from Done State

**What goes wrong:**
When computing the DQN target: `r + γ * max Q(s', a')`, if the game is over (Done=true), you must NOT add the discounted next-Q-value — there is no next state. Using `max Q(terminal_state)` instead of `0.0` makes the Q-value target nonsensical, causing the network to overestimate values at game-ending positions and potentially training toward incorrect behavior.

**Why it happens:**
The done mask is easy to forget when vectorizing the target computation across a batch. One missing element-wise multiplication corrupts every terminal transition in training.

**How to avoid:**
```fsharp
// CORRECT target computation with done mask
let computeTargets (batch: Experience[]) (targetNet: Module) (gamma: float) =
    use _ = torch.NewDisposeScope()
    let nextStates = batch |> Array.map (fun e -> boardToTensor e.NextState)
    let nextQValues = targetNet.forward(torch.stack nextStates)
    let maxNextQ = nextQValues.max(1).values
    // done mask: 0.0 if terminal, 1.0 if ongoing
    let doneMask =
        batch
        |> Array.map (fun e -> if e.Done then 0.0f else 1.0f)
        |> torch.tensor
    let rewards = batch |> Array.map (fun e -> float32 e.Reward) |> torch.tensor
    rewards + (float32 gamma) * maxNextQ * doneMask  // mask prevents bootstrapping
```

**Warning signs:**
- Q-values for winning/losing moves are near-identical (no differentiation)
- Agent learns some policy but wins plateau far below expected against Minimax
- Loss decreases initially but oscillates without converging

**Phase to address:** Phase 4 (DQN). Include a specific Expecto test that checks target computation on a terminal batch.

---

### Pitfall 4: Sparse Reward Convergence Failure — Win/Loss Only Reward

**What goes wrong:**
Using only terminal rewards (+1 win, -1 loss, 0 draw) with no intermediate shaping makes TD Learning and Q-Learning fail to converge in reasonable time for Phase 2 and Phase 3. The agent gets no learning signal for most of the game and only receives feedback at the end, making credit assignment extremely hard for longer games (Connect Four can last 42 moves).

**Why it happens:**
This is the textbook implementation described in papers, which works in theory with infinite samples. In a tutorial context with limited training time (10K-100K episodes), pure terminal rewards are insufficient for games beyond 3x3 Tic-Tac-Toe. Authors implement the "correct" algorithm but see no convergence and assume bugs.

**How to avoid:**
Add small intermediate rewards for game-relevant signals:
- For Phase 3 Connect Four: +0.5 for creating a 3-in-a-row threat, -0.5 for allowing one
- For Phase 2 Tic-Tac-Toe: terminal rewards alone ARE sufficient (short game, 5478 states)
- Do NOT use heavily engineered reward functions — they teach the agent to exploit the reward rather than play well. Keep shaping signals small compared to terminal rewards.

Be explicit in the tutorial about the tradeoff: "more reward shaping = faster convergence but less 'pure' RL."

**Warning signs:**
- After 50K episodes, win rate vs random opponent is below 60%
- Q-values or value function are nearly uniform across all states
- Learning curve shows no upward trend after first 10K episodes

**Phase to address:** Phase 2 and Phase 3. Phase 4 is less affected because neural net generalizes from correlated states.

---

### Pitfall 5: F# `Map<Board, float>` as Value Table — Performance Collapse at Scale

**What goes wrong:**
Using F# `Map` (AVL tree, O(log n) insert/lookup) as the Q-table or value table works fine for Phase 2 Tic-Tac-Toe (~5K states) but becomes a severe bottleneck in Phase 3 Connect Four (4.5 trillion possible states, even with a feature-reduced representation). The immutable `Map` creates garbage on every insert, thrashing the GC during 100K-episode training loops.

**Why it happens:**
F# `Map` is the idiomatic immutable dictionary. It is correct and convenient. But with 1M+ lookups per training run, AVL tree overhead and GC pressure compound. One benchmark showed F# Map taking 4× longer than `Dictionary` for high-frequency operations.

**How to avoid:**
- Phase 2: F# `Map<Board, float>` is fine — state space is small enough
- Phase 3 Q-Learning: Use `System.Collections.Generic.Dictionary<string, float>` with a board hash as key. Wrap in a module to keep the F# API clean:

```fsharp
// Use mutable Dictionary with a hashed key for Q-table
type QTable() =
    let table = System.Collections.Generic.Dictionary<string, float>()
    member _.Get(board: Board, action: int) =
        let key = sprintf "%A_%d" board action
        match table.TryGetValue(key) with
        | true, v -> v
        | _ -> 0.0
    member _.Set(board: Board, action: int, value: float) =
        table[sprintf "%A_%d" board action] <- value
```

- Phase 4+: Replace Q-table with neural network (the point of DQN), so this pitfall dissolves naturally.
- Board hashing: Using `sprintf "%A"` is slow. For performance, encode the board as a compact byte array or integer for hashing.

**Warning signs:**
- Phase 3 training loop takes >5 minutes for 10K episodes
- GC pause spikes visible in profiler during Q-table updates
- Memory usage grows steadily during Q-Learning training (immutable map copies)

**Phase to address:** Phase 3. Design the Q-table abstraction to be swappable so Phase 4 can replace it cleanly.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Using `Array.copy` for immutable board in tight loop | Simple correctness | Allocation pressure in 100K episode training | Phase 1-2 (acceptable); Phase 3+ use preallocated mutable buffers for copy |
| `sprintf "%A"` for board hashing in Q-table | Zero implementation effort | ~10× slower than custom hash; bottleneck at 100K lookups/sec | Phase 2 only (tiny state space); never in Phase 3+ |
| No gradient zeroing (`optimizer.zero_grad()`) before backward pass | One less line of code | Gradients accumulate across batches, model diverges silently | Never acceptable |
| Single shared model for self-play (no opponent pool) | Simpler code | Strategy cycling: learns to beat one strategy, forgets others | Acceptable for Phase 4 (DQN) tutorial; document the limitation |
| Hard-coded hyperparameters inline | Fast to write | Tutorial readers can't experiment without finding magic numbers | Never — always define as named constants or config record |
| Skipping target network, using single DQN | 30% less code | Oscillating Q-values, non-convergence; tutorial fails to demonstrate DQN | Never — target network is the critical DQN contribution |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| TorchSharp + F# | Calling `tensor.item<float32>()` on CPU tensor without prior `.cpu()` call when tensor might be on GPU in future | Always add `.cpu()` before `.item<T>()` even if currently CPU-only; makes code GPU-ready |
| TorchSharp optimizer | Forgetting `optimizer.zero_grad()` before `loss.backward()` | The training loop pattern must be: zero_grad → forward → loss → backward → step. Make this a named function. |
| TorchSharp Adam | Adam optimizer creates internal state tensors that bypass DisposeScope | Accept this: Adam tensors live with the optimizer. Do not try to dispose them manually. |
| FsCheck + Expecto | Using `Check.Quick` without seed makes failures non-reproducible | Use `Check.One(config, property)` with a fixed seed in CI; use `Check.Quick` only during local development |
| FsCheck generators for boards | Generating arbitrary board states that are not reachable through legal play — these states may be invalid (wrong piece counts, impossible configurations) | Write custom `Arb` that generates boards by replaying legal moves from start, not by random cell assignment |
| Serilog + console apps | Logger not flushed before process exit causes dropped log lines | Call `Log.CloseAndFlush()` at the end of every console app's `main` |
| mdBook + code blocks | F# code in mdBook rendered without syntax highlighting (shows as plain text) | Explicitly mark code fences as `\`\`\`fsharp`, not `\`\`\`fs`. Verify in `book.toml` that the F# highlighting theme is configured. |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| `List.map` inside episode loop for board operations | Training loop is 10-100× slower than expected | Use `Array.map` or `Array` operations exclusively in hot paths; F# lists are linked lists, not arrays | Phase 2+ with 10K+ episodes |
| Creating new `Random()` instance per call | Statistical bias, `Random` not thread-safe, and constructor is expensive | Create one `let rng = Random()` module-level or pass as argument | Any phase with exploration |
| `Map.add` for value-table update in hot loop | GC pressure, allocation spike every episode | Switch to `Dictionary` for Phase 3+ Q-table; keep `Map` for small config data only | Phase 3 (10K+ Q-table entries) |
| TorchSharp: `torch.tensor()` inside batch loop | Tensor allocation per element instead of per batch; 64× slowdown | Pre-build full batch tensor outside the per-element loop using `torch.stack` or preallocation | Phase 4 batch training |
| MCTS node expansion with F# `Map` for children | Significant overhead per expansion in high-simulation MCTS | Use `Dictionary<Move, MctsNode>` for `Children`; MCTS creates millions of nodes per search | Phase 5 with >100 simulations per move |

---

## Tutorial Structure Pitfalls

These are specific to the mdBook tutorial format, not the code.

| Pitfall | Impact on Reader | Better Approach |
|---------|-----------------|-----------------|
| Explaining Q-Learning math before showing a working example | Reader disengages before implementation | Show working code first, then derive the math from the code's behavior |
| Each phase is a monolith (single 500-line file) | Reader cannot navigate, cannot run partial implementations | Structure each Phase as: Types → Environment → Agent → Training Loop → Visualization, each in separate files with clear module boundaries |
| Tutorial assumes reader will read all phases in order | Phase 4 reader does not know why they need TorchSharp | Begin each Phase chapter with a "Why this phase?" section that references the limitation exposed in the previous phase |
| No runnable intermediate checkpoints | Reader gets stuck when implementation doesn't work | Provide a `checkpoint/` directory per phase with the completed code; reader can diff against their implementation |
| Printing convergence as raw numbers without visual | Win-rate "0.51, 0.53, 0.49..." means nothing | Include ASCII chart in console output (e.g., Plotly via Expecto or simple histogram print) even for console-only apps |
| Serilog structured logging not explained | Reader doesn't know what to look for in logs | Include a "Reading the Logs" section per Phase explaining what normal vs. abnormal log output looks like |

---

## "Looks Done But Isn't" Checklist

- [ ] **ε-greedy Phase 1:** ε=0 is not tested separately — verify agent converges to optimal arm with ε=0 after initial training phase. Often, pure exploitation (ε=0) is confused with pure greed — make sure the final policy can be extracted cleanly.
- [ ] **TD Learning Phase 2:** `ValueTable` is trained, but symmetry of the Tic-Tac-Toe board is NOT exploited — 8 symmetric board states are stored separately. This is fine for tutorial correctness but should be documented as a known limitation.
- [ ] **Minimax Phase 3:** Alpha-beta pruning is implemented, but the evaluation function is not tested for symmetry — same board rotated may return a different score. Property-based test needed.
- [ ] **DQN Phase 4:** Model trains and loss decreases, but the agent is never benchmarked against the Phase 3 Minimax AI — the stated success criterion "70% win rate vs Minimax depth 6" is never verified.
- [ ] **FsCheck properties:** Properties are written, but the generator only creates small/simple boards. Add `Gen.sized` to test on large boards too.
- [ ] **Serilog:** Logging is added but only at INFO level — DQN debugging requires DEBUG-level output (Q-values per step, loss per batch). Add a `--verbose` flag or config.
- [ ] **MCTS Phase 5:** MCTS runs N simulations but the time budget is uncapped — on slow machines, N=800 simulations may take 30+ seconds per move. Add a time limit or simulation count parameter.
- [ ] **Phase independence:** Each phase is declared independent, but the tutorial text may reference "as we implemented in Phase 3..." — ensure Phase N code is self-contained and compilable without Phase N-1.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Memory leak discovered in Phase 4 training | MEDIUM | Add `use _ = torch.NewDisposeScope()` around training step. No architectural change. Add integration test that runs 1000 steps and checks memory stays flat. |
| MCTS backprop bug discovered late in Phase 5 | HIGH | Single-line fix (add negation), but requires full retrain from scratch. Prevention via unit test in Phase 5 milestone is critical. |
| Q-table performance bottleneck in Phase 3 | LOW | Replace `Map` with `Dictionary` wrapper behind a module interface — no change to training logic. |
| Terminal state bug in DQN | MEDIUM | Fix target computation, retrain. Write regression test with a minimal 2-step episode that verifies done=true gives `r + 0`, done=false gives `r + γ*maxQ`. |
| Sparse reward non-convergence in Phase 3 | MEDIUM | Add minimal reward shaping (+0.01 per step to discourage long games, or +0.1 for 3-in-row). Document the change in tutorial as a deliberate design decision. |
| FsCheck generator producing invalid boards | LOW | Replace `Arb.generate<Board>` with a custom generator that replays legal moves. Existing tests still pass; some edge cases are removed. |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| TorchSharp tensor memory leak | Phase 4, milestone start | Run 10K training steps; measure process memory before and after; assert < 10% growth |
| MCTS backprop perspective bug | Phase 5, before integration with neural net | Unit test: single MCTS search on terminal board should produce correct visit counts with correct sign |
| DQN terminal state target bug | Phase 4, during training loop implementation | Expecto test: compute targets on batch where Done=true; assert target equals reward exactly |
| Sparse reward convergence failure | Phase 2 (document), Phase 3 (mitigate) | Learning curve test: after 100K episodes, win rate vs. random must exceed 75% |
| F# Map Q-table performance | Phase 3, before training loop | Benchmark 1M Q-table lookups; must complete under 5 seconds |
| FsCheck invalid board generator | Phase 2, test setup | Property: generated boards have correct piece count parity (X = O or X = O+1) |
| MCTS strategy cycling (self-play) | Phase 5, training design | After 500 iterations, evaluate vs. random baseline — should not regress below 95% win rate |
| Tutorial code coupling across phases | Each phase, at completion | Compile and run each phase solution in isolation (CI matrix build per phase) |
| Serilog not flushed | Phase 1, boilerplate setup | Integration test that runs console app and checks log file completeness |
| Gradient not zeroed in DQN | Phase 4, first training step | Expecto test: run 2 backward passes, assert gradients reset between them |

---

## Sources

- [TorchSharp Memory Management Wiki](https://github.com/dotnet/TorchSharp/wiki/Memory-Management) — DisposeScope, GC interaction, Adam optimizer caveat
- [TorchSharp Memory Leak Troubleshooting](https://github.com/dotnet/TorchSharp/wiki/Memory-Leak-Troubleshooting) — diagnosis patterns
- [Deep Reinforcement Learning Doesn't Work Yet — Alex Irpan](https://www.alexirpan.com/2018/02/14/rl-hard.html) — DQN convergence failure modes, reward shaping tradeoffs
- [Techniques to Improve DQN Performance — Towards Data Science](https://towardsdatascience.com/techniques-to-improve-the-performance-of-a-dqn-agent-29da8a7a0a7e/) — target network importance, overestimation bias
- [MCTS Survey — JAIR 2017](https://www.jair.org/index.php/jair/article/download/11099/26289/20632) — backpropagation correctness, UCB1 in two-player games
- [Writing High Performance F# Code — Bartosz Sypytkowski](https://www.bartoszsypytkowski.com/writing-high-performance-f-code/) — F# Map vs Dictionary, list vs array, hot loop patterns
- [FsCheck Documentation — Properties](https://fscheck.github.io/FsCheck/Properties.html) — generator correctness, reproducible seeds
- [Self-Play in RL — HuggingFace Deep RL Course](https://huggingface.co/learn/deep-rl-course/en/unit7/self-play) — strategy cycling, opponent pool design
- [F# for Fun and Profit — Result Anti-Patterns](https://fsharpforfunandprofit.com/fppatterns/) — overuse of Result/Option in non-error contexts
- [ResearchGate — Q-loss non-convergence](https://www.researchgate.net/post/What-are-possible-reasons-why-Q-loss-is-not-converging-in-Deep-Q-Learning-algorithm) — practical DQN debugging

---
*Pitfalls research for: F# Reinforcement Learning Tutorial (Bandit → Gomoku)*
*Researched: 2026-02-19*
