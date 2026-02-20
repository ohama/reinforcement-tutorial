module ConnectFourDQN.DQNAgent

open TorchSharp
open type TorchSharp.torch
open ConnectFourDQN.Domain
open ConnectFourDQN.Rules
open ConnectFourDQN.DQNModel
open ConnectFourDQN.ReplayBuffer

// NOTE: `open type TorchSharp.torch` shadows the F# `int` and `int64` conversion
// functions (they resolve to ScalarType.Int32 / ScalarType.Int64 instead).
// Use `Operators.int` and `Operators.int64` for numeric conversions throughout.

// ── Board → Tensor ─────────────────────────────────────────────────────────
// Encode board as 3-channel float32 tensor [3, 6, 7].
// Channel 0: my pieces, Channel 1: opponent pieces, Channel 2: empty cells.
// Invariant: sum of all values = 42.0 (exactly one channel = 1.0 per cell).
let boardToTensor (board: Board) (myPiece: Cell) (oppPiece: Cell) : Tensor =
    let flat = Array.init (3 * 6 * 7) (fun i ->
        let ch   = i / (6 * 7)
        let rem  = i % (6 * 7)
        let cell = board.[rem / 7 * 7 + rem % 7]
        match ch, cell with
        | 0, c when c = myPiece  -> 1.0f
        | 1, c when c = oppPiece -> 1.0f
        | 2, Empty               -> 1.0f
        | _                      -> 0.0f)
    torch.tensor(flat, dtype = ScalarType.Float32).reshape([| 3L; 6L; 7L |])

// Extract float32[] from boardToTensor result (for storing in ReplayBuffer without tensor leak).
// Must be called inside a dispose scope — the tensor is freed when scope exits.
let boardToArray (board: Board) (myPiece: Cell) (oppPiece: Cell) : float32[] =
    use _scope = torch.NewDisposeScope()
    use t = boardToTensor board myPiece oppPiece
    t.data<float32>().ToArray()

// ── Action Selection ────────────────────────────────────────────────────────
// Epsilon-greedy with illegal move masking.
// Illegal columns (full) are masked with -infinity before argmax so the network
// never selects an invalid action.
let chooseMove (rng: System.Random) (model: DQNModel) (board: Board)
               (myPiece: Cell) (oppPiece: Cell) (epsilon: float) : int =
    let legal = legalMoves board
    if rng.NextDouble() < epsilon then
        // Exploration: random legal move
        legal.[rng.Next(legal.Length)]
    else
        // Exploitation: greedy Q-value with illegal move masking
        use _scope = torch.NewDisposeScope()
        use stateTensor = boardToTensor board myPiece oppPiece
        use input = stateTensor.unsqueeze(0L)   // [1,3,6,7]
        use qAll  = model.forward(input)         // [1,7]
        use qVec  = qAll.squeeze(0L)             // [7]
        // Mask illegal columns with -infinity using index_fill_
        // (avoids shadowed `int64` conversion function)
        let illegalCols : int64[] =
            [| 0 .. cols - 1 |]
            |> Array.filter (fun c -> not (List.contains c legal))
            |> Array.map Operators.int64
        if illegalCols.Length > 0 then
            use idxTensor = torch.tensor(illegalCols)
            let negInf : Scalar = System.Single.NegativeInfinity
            qVec.index_fill_(0L, idxTensor, negInf) |> ignore
        // Operators.int to convert int64 result (avoids shadowed `int` function)
        let v : int64 = qVec.argmax().item<int64>()
        Operators.int v

// ── Training Step ────────────────────────────────────────────────────────────
// One gradient update step using a batch from the replay buffer.
// Wraps ALL tensor operations in NewDisposeScope — no tensor escapes this function.
// Returns the loss value for logging.
let trainStep (model: DQNModel) (target: DQNModel)
              (opt: TorchSharp.torch.optim.Optimizer)
              (experiences: Experience[]) (gamma: float32) : float32 =
    use _scope = torch.NewDisposeScope()

    let n = experiences.Length

    // Reconstruct state/nextState tensors from stored float32[] arrays
    let toBatch (getData: Experience -> float32[]) =
        let concat = experiences |> Array.collect getData
        torch.tensor(concat, dtype = ScalarType.Float32)
              .reshape([| Operators.int64 n; 3L; 6L; 7L |])

    use states     = toBatch (fun e -> e.StateData)
    use nextStates = toBatch (fun e -> e.NextStateData)
    use actions    = torch.tensor(experiences |> Array.map (fun e -> Operators.int64 e.Action))
    use rewards    = torch.tensor(experiences |> Array.map (fun e -> e.Reward) : float32[])
    use dones      = torch.tensor(experiences |> Array.map (fun e -> if e.Done then 1.0f else 0.0f) : float32[])

    // Current Q-values for the taken actions: Q(s, a)
    use qAll   = model.forward(states)                                  // [N, 7]
    use qTaken = qAll.gather(1L, actions.unsqueeze(1L)).squeeze(1L)    // [N]

    // Target Q-values: r + gamma * max_a'(Q_target(s', a')) * (1 - done)
    use _noGrad = torch.no_grad()
    let struct(nextQMax, _nextQIdx) = target.forward(nextStates).max(1L)  // [N]
    use nextQMaxDisposable = nextQMax
    use targetQ = rewards + gamma * nextQMax * (1.0f - dones)             // [N]

    opt.zero_grad()
    use loss = nn.functional.smooth_l1_loss(qTaken, targetQ, reduction = nn.Reduction.Mean)
    loss.backward()
    opt.step() |> ignore

    loss.item<float32>()

// ── Target Network Sync ──────────────────────────────────────────────────────
// Hard copy: save policy weights to temp file, load into target.
// Called every targetSyncFreq steps.
let syncTargetNetwork (policy: DQNModel) (target: DQNModel) (tmpPath: string) =
    policy.save(tmpPath) |> ignore
    target.load(tmpPath) |> ignore
