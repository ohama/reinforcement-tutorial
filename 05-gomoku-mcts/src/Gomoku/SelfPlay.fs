module Gomoku.SelfPlay

open TorchSharp
open Gomoku.Domain
open Gomoku.Rules
open Gomoku.PolicyValueNet
open Gomoku.Mcts

/// Training sample: stored as float32 arrays (NOT Tensors) to avoid memory leaks in replay buffer.
type TrainingSample = {
    State:        float32[]  // flat [4 * 15 * 15] = 900 values, from boardToTensor
    PolicyTarget: float32[]  // flat [225] MCTS visit distribution (sums to 1)
    ValueTarget:  float32    // +1.0f = current player won, -1.0f = lost, 0.0f = draw
}

/// Temperature-based move selection from MCTS visit counts.
let private selectMoveByTemp (rng: System.Random) (visitProbs: (int * float) array) (moveCount: int) : int =
    if moveCount >= 15 then
        visitProbs |> Array.maxBy snd |> fst
    else
        let weights = visitProbs |> Array.map snd
        let total   = Array.sum weights
        let r = rng.NextDouble() * total
        let mutable cumSum = 0.0
        let mutable chosen = fst visitProbs.[0]
        let mutable found  = false
        for (action, w) in visitProbs do
            if not found then
                cumSum <- cumSum + w
                if cumSum >= r then
                    chosen <- action
                    found  <- true
        chosen

/// Play one complete self-play game and return training samples with value targets filled.
/// model should be in eval() mode before calling this function.
let playSelfPlayGame
    (model: PolicyValueNet)
    (rng: System.Random)
    (nSimulations: int)
    (cPuct: float)
    : TrainingSample[] =

    let mutable state = initialState ()
    let stateHistory  = System.Collections.Generic.List<float32[]>()
    let policyHistory = System.Collections.Generic.List<float32[]>()
    let playerHistory = System.Collections.Generic.List<Player>()

    let mutable running = true

    while running do
        use _encScope = torch.NewDisposeScope()
        let stateTensor = boardToTensor state
        let stateData   = stateTensor.data<float32>().ToArray()
        stateHistory.Add(stateData)
        playerHistory.Add(state.CurrentPlayer)

        // NOTE: mctsSearchWithNet signature: model → rng → state → nSims → cPuct → addDirichlet
        let visitProbs = mctsSearchWithNet model rng state nSimulations cPuct true

        let piTarget = Array.zeroCreate<float32> (BoardSize * BoardSize)
        for (action, prob) in visitProbs do
            piTarget.[action] <- float32 prob
        policyHistory.Add(piTarget)

        let move = selectMoveByTemp rng visitProbs state.MoveCount
        state <- applyMove state move

        match state.LastMove with
        | Some m when isWinningMove state.Board m -> running <- false
        | _ when (legalMoves state.Board).Length = 0 -> running <- false
        | _ -> ()

    let gameOutcome =
        match state.LastMove with
        | Some m when isWinningMove state.Board m ->
            Some (opponent state.CurrentPlayer)
        | _ -> None

    let n = stateHistory.Count
    Array.init n (fun i ->
        let valueTarget =
            match gameOutcome with
            | Some winner ->
                if playerHistory.[i] = winner then 1.0f else -1.0f
            | None -> 0.0f
        { State        = stateHistory.[i]
          PolicyTarget = policyHistory.[i]
          ValueTarget  = valueTarget })
