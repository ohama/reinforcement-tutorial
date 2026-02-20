module Gomoku.Training

open TorchSharp
open type TorchSharp.torch
open Gomoku.Domain
open Gomoku.Rules
open Gomoku.PolicyValueNet
open Gomoku.SelfPlay

type TrainingConfig = {
    NSelfPlayGames:   int
    NSimulations:     int
    BatchSize:        int
    MaxBufferSize:    int
    LearningRate:     float
    CPuct:            float
    NTrainingIter:    int
    NEpochsPerIter:   int
    ModelSavePath:    string
}

let defaultConfig = {
    NSelfPlayGames  = 1
    NSimulations    = 100
    BatchSize       = 256
    MaxBufferSize   = 10000
    LearningRate    = 2e-3
    CPuct           = 5.0
    NTrainingIter   = 200
    NEpochsPerIter  = 5
    ModelSavePath   = "gomoku_model.pt"
}

/// Single gradient update on one batch. Returns (policyLoss, valueLoss).
let trainBatch (model: PolicyValueNet) (opt: torch.optim.Optimizer) (batch: TrainingSample[]) : float * float =
    use _scope = torch.NewDisposeScope()
    let stateFlat  = batch |> Array.collect (fun s -> s.State)
    let stateT     = torch.tensor(stateFlat, dtype=ScalarType.Float32)
                           .reshape([| Operators.int64 batch.Length; 4L;
                                       Operators.int64 BoardSize; Operators.int64 BoardSize |])
    let policyFlat = batch |> Array.collect (fun s -> s.PolicyTarget)
    let policyT    = torch.tensor(policyFlat, dtype=ScalarType.Float32)
                           .reshape([| Operators.int64 batch.Length;
                                       Operators.int64 (BoardSize * BoardSize) |])
    let valueFlat  = batch |> Array.map (fun s -> s.ValueTarget)
    let valueT     = torch.tensor(valueFlat, dtype=ScalarType.Float32).unsqueeze(1L)

    let (logProbs, valuePred) = model.forwardBoth(stateT)
    let policyLoss = -(policyT * logProbs).sum(1L).mean()
    let valueLoss = nn.functional.mse_loss(valuePred, valueT, nn.Reduction.Mean)
    let loss = policyLoss + valueLoss
    opt.zero_grad()
    loss.backward()
    opt.step() |> ignore

    (Operators.float (policyLoss.item<float32>()),
     Operators.float (valueLoss.item<float32>()))

type IterationResult = {
    Iteration:   int
    GamesPlayed: int
    AvgPolicyLoss: float
    AvgValueLoss:  float
}

/// Full self-play training pipeline. Pure — returns results only, no I/O.
let runSelfPlayTraining
    (model: PolicyValueNet)
    (config: TrainingConfig)
    (rng: System.Random)
    : IterationResult list =

    let opt = torch.optim.Adam(model.parameters(), config.LearningRate)
    let buffer = System.Collections.Generic.List<TrainingSample>()
    let results = System.Collections.Generic.List<IterationResult>()

    for iter in 1 .. config.NTrainingIter do
        model.eval()
        for _ in 1 .. config.NSelfPlayGames do
            let samples = playSelfPlayGame model rng config.NSimulations config.CPuct
            buffer.AddRange(samples)
            while buffer.Count > config.MaxBufferSize do
                buffer.RemoveAt(0)

        model.train()
        let mutable totalPolicyLoss = 0.0
        let mutable totalValueLoss  = 0.0
        let mutable nBatches = 0

        if buffer.Count >= config.BatchSize then
            for _ in 1 .. config.NEpochsPerIter do
                let batchIndices = Array.init config.BatchSize (fun _ -> rng.Next(buffer.Count))
                let batch = batchIndices |> Array.map (fun i -> buffer.[i])
                let (pLoss, vLoss) = trainBatch model opt batch
                totalPolicyLoss <- totalPolicyLoss + pLoss
                totalValueLoss  <- totalValueLoss  + vLoss
                nBatches        <- nBatches + 1

        model.eval()

        let result = {
            Iteration      = iter
            GamesPlayed    = iter * config.NSelfPlayGames
            AvgPolicyLoss  = if nBatches > 0 then totalPolicyLoss / Operators.float nBatches else 0.0
            AvgValueLoss   = if nBatches > 0 then totalValueLoss  / Operators.float nBatches else 0.0
        }
        results.Add(result)

    results |> Seq.toList

/// Evaluate model win rate vs random opponent.
let evaluateVsRandom
    (model: PolicyValueNet)
    (nGames: int)
    (nSimulations: int)
    (cPuct: float)
    (rng: System.Random)
    : int =
    let mutable wins = 0
    model.eval()
    for i in 1 .. nGames do
        let mctsIsBlack = (i % 2 = 1)
        let mctsPlayer  = if mctsIsBlack then Black else White
        let mutable state  = initialState ()
        let mutable running = true
        while running do
            match state.LastMove with
            | Some m when isWinningMove state.Board m ->
                let winner = opponent state.CurrentPlayer
                if winner = mctsPlayer then wins <- wins + 1
                running <- false
            | _ when (legalMoves state.Board).Length = 0 ->
                running <- false
            | _ ->
                let legal = legalMoves state.Board
                let move =
                    if state.CurrentPlayer = mctsPlayer then
                        let vp = Mcts.mctsSearchWithNet model rng state nSimulations cPuct false
                        Mcts.bestMove vp
                    else
                        legal.[rng.Next(legal.Length)]
                state <- applyMove state move
    wins
