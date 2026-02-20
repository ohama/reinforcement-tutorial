module Gomoku.Mcts

open Gomoku.Domain
open Gomoku.Rules
open Gomoku.MctsNode

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
