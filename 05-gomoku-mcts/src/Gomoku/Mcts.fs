module Gomoku.Mcts

open Gomoku.Domain
open Gomoku.Rules
open Gomoku.MctsNode
open Gomoku.PolicyValueNet
open TorchSharp

// NOTE: We intentionally do NOT use `open type TorchSharp.torch` at module level
// because it shadows F# built-ins: float, int, int64, sqrt, log, cos, etc.
// All torch types are accessed via fully qualified names: torch.Tensor, torch.NewDisposeScope(), etc.

/// PUCT score for a child node during selection.
/// Q + c_puct * P * sqrt(N_parent) / (1 + N_child)
let private puctScore (cPuct: float) (parentVisits: int) (child: MctsNode) =
    let u = cPuct * child.Prior * sqrt(float parentVisits) / (1.0 + float child.Visits)
    child.Q() + u

/// Select the child with highest PUCT score.
let private selectAction (cPuct: float) (node: MctsNode) =
    node.Children
    |> Seq.maxBy (fun kvp -> puctScore cPuct node.Visits kvp.Value)
    |> fun kvp -> (kvp.Key, kvp.Value)

/// Random rollout from state until terminal. Returns +1 if the CURRENT player at start wins,
/// -1 if they lose, 0 for draw. Uses simple random play (no neural network).
let private rollout (rng: System.Random) (state: GameState) : float =
    let mutable s = state
    let mutable running = true
    let mutable result = 0.0
    // Track starting player to assign result correctly
    let startPlayer = state.CurrentPlayer
    while running do
        match s.LastMove with
        | Some m when isWinningMove s.Board m ->
            // The player who just moved (opponent of s.CurrentPlayer) won
            let winner = opponent s.CurrentPlayer
            result <- if winner = startPlayer then 1.0 else -1.0
            running <- false
        | _ ->
            let legal = legalMoves s.Board
            if legal.Length = 0 then
                result <- 0.0  // draw (full board)
                running <- false
            else
                let move = legal.[rng.Next(legal.Length)]
                s <- applyMove s move
    result

/// Full MCTS search using pure random rollout (no neural network).
/// Returns array of (action, visit_probability) for the root's children.
/// cPuct: exploration constant (5.0 typical); addDirichletNoise: false for pure MCTS.
let mctsSearch
    (rng: System.Random)
    (rootState: GameState)
    (nSimulations: int)
    (cPuct: float)
    : (int * float) array =

    let root = MctsNode(None, 1.0)

    for _ in 1 .. nSimulations do
        let mutable state = rootState
        let mutable node = root
        let mutable isTerminal = false

        // 1. SELECTION: descend until leaf or terminal
        while not (node.IsLeaf()) && not isTerminal do
            let (action, child) = selectAction cPuct node
            state <- applyMove state action
            node <- child
            match state.LastMove with
            | Some m when isWinningMove state.Board m -> isTerminal <- true
            | _ when (legalMoves state.Board).Length = 0 -> isTerminal <- true
            | _ -> ()

        // 2. EXPANSION + EVALUATION
        let leafValue =
            if isTerminal then
                // The last move was made by the player who is now NOT the current player
                // That player just won → bad for current player at this node
                -1.0
            else
                // Expand with uniform priors
                let legal = legalMoves state.Board
                let uniformPrior = 1.0 / float legal.Length
                node.Expand(legal |> Array.map (fun a -> (a, uniformPrior)))
                // 3. ROLLOUT (random simulation from expanded node)
                rollout rng state

        // 4. BACKPROPAGATION
        // Convention: UpdateRecursive(-leafValue) — negate because leafValue is from leaf's perspective,
        // and UpdateRecursive negates again at each parent, alternating perspective correctly.
        node.UpdateRecursive(-leafValue)

    // Convert visit counts to probabilities
    if root.Children.Count = 0 then
        // Root was never expanded (0 simulations or already terminal)
        let legal = legalMoves rootState.Board
        legal |> Array.map (fun a -> (a, 1.0 / float legal.Length))
    else
        let totalVisits = root.Children.Values |> Seq.sumBy (fun c -> c.Visits) |> float
        [| for kvp in root.Children do
            yield (kvp.Key, float kvp.Value.Visits / totalVisits) |]

/// Select the best move (highest visit count) from MCTS results.
let bestMove (visitProbs: (int * float) array) : int =
    visitProbs |> Array.maxBy snd |> fst

// ── Neural-Guided MCTS (GMOK-04: PUCT + PolicyValueNet) ──────────────────────

/// Sample Dirichlet(alpha) noise for n values using Gamma(alpha, 1) normalization.
/// Pure F# implementation using Box-Muller + Marsaglia-Tsang Gamma sampler.
/// alpha=0.3 is the standard for Gomoku; result sums to 1.0.
let private sampleDirichlet (rng: System.Random) (n: int) (alpha: float) : float array =
    let rec sampleGamma (a: float) : float =
        if a >= 1.0 then
            // Marsaglia-Tsang (2000) algorithm
            let d = a - 1.0 / 3.0
            let c = 1.0 / System.Math.Sqrt(9.0 * d)
            let mutable result = 0.0
            let mutable found = false
            while not found do
                let u1 = rng.NextDouble()
                let u2 = rng.NextDouble()
                // Box-Muller transform: standard normal sample
                let x = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2)
                let v = (1.0 + c * x) ** 3.0
                if v > 0.0 then
                    let u = rng.NextDouble()
                    if u < 1.0 - 0.0331 * (x * x) * (x * x) then
                        result <- d * v
                        found <- true
                    elif System.Math.Log(u) < 0.5 * x * x + d * (1.0 - v + System.Math.Log(v)) then
                        result <- d * v
                        found <- true
            result
        else
            // Boost for alpha < 1: Gamma(alpha, 1) = Gamma(alpha+1, 1) * U^(1/alpha)
            let g = sampleGamma (alpha + 1.0)
            g * (rng.NextDouble() ** (1.0 / alpha))
    let samples = Array.init n (fun _ -> sampleGamma alpha)
    let total = Array.sum samples
    if total > 1e-10 then
        samples |> Array.map (fun s -> s / total)
    else
        Array.create n (1.0 / float n)

/// Neural-guided MCTS search using PolicyValueNet for expansion (PUCT formula).
/// model.eval() should be set BEFORE calling this function.
/// Each simulation wraps tensor ops in torch.NewDisposeScope() to prevent memory accumulation.
///
/// addDirichletNoise: true during self-play training (root exploration), false during evaluation.
/// Returns array of (action, visit_probability) for move selection.
let mctsSearchWithNet
    (model: PolicyValueNet)
    (rng: System.Random)
    (rootState: GameState)
    (nSimulations: int)
    (cPuct: float)
    (addDirichletNoise: bool)
    : (int * float) array =

    let root = MctsNode(None, 1.0)

    for _ in 1 .. nSimulations do
        use _scope = torch.NewDisposeScope()
        let mutable state = rootState
        let mutable node = root
        let mutable isTerminal = false

        // 1. SELECTION: descend until leaf or terminal
        while not (node.IsLeaf()) && not isTerminal do
            let (action, child) = selectAction cPuct node
            state <- applyMove state action
            node <- child
            match state.LastMove with
            | Some m when isWinningMove state.Board m -> isTerminal <- true
            | _ when (legalMoves state.Board).Length = 0 -> isTerminal <- true
            | _ -> ()

        // 2. EXPANSION + EVALUATION
        let leafValue =
            if isTerminal then
                -1.0  // Player who just moved won; bad for current player at this node
            else
                // Evaluate with neural network
                let stateTensor = boardToTensor state
                let batchedTensor = stateTensor.unsqueeze(0L)  // [1, 4, 15, 15]

                // Policy: log-probs [1, 225] → exp → probs
                let logProbs = model.policy(batchedTensor)
                let probs = logProbs.exp()               // [1, 225]

                // Value: [1, 1] → scalar float
                let valueT = model.value(batchedTensor)  // [1, 1]
                let value = float (valueT.item<float32>())

                // Extract legal move priors
                let legal = legalMoves state.Board
                let probsData = probs.squeeze(0L).data<float32>().ToArray()
                let legalPriors =
                    legal |> Array.map (fun a -> (a, float probsData.[a]))
                let totalProb = legalPriors |> Array.sumBy snd

                // Normalize; fall back to uniform if network assigns ~0 to all legal moves
                let normalizedPriors =
                    if totalProb > 1e-8 then
                        legalPriors |> Array.map (fun (a, p) -> (a, p / totalProb))
                    else
                        legal |> Array.map (fun a -> (a, 1.0 / float legal.Length))

                node.Expand(normalizedPriors)
                value

        // 3. BACKPROPAGATION (perspective flip)
        node.UpdateRecursive(-leafValue)

    // Apply Dirichlet noise to root children (training only)
    if addDirichletNoise && root.Children.Count > 0 then
        let n = root.Children.Count
        let noise = sampleDirichlet rng n 0.3  // alpha=0.3 standard for Gomoku
        let mutable i = 0
        for kvp in root.Children do
            kvp.Value.Prior <- 0.75 * kvp.Value.Prior + 0.25 * noise.[i]
            i <- i + 1

    // Convert visit counts to probabilities
    if root.Children.Count = 0 then
        let legal = legalMoves rootState.Board
        legal |> Array.map (fun a -> (a, 1.0 / float legal.Length))
    else
        let totalVisits = root.Children.Values |> Seq.sumBy (fun c -> c.Visits) |> float
        [| for kvp in root.Children do
            yield (kvp.Key, float kvp.Value.Visits / totalVisits) |]
