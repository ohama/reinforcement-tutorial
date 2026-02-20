module ConnectFourDQN.Console.Training

open TorchSharp
open type TorchSharp.torch
open ConnectFourDQN.Domain
open ConnectFourDQN.Rules
open ConnectFourDQN.Minimax
open ConnectFourDQN.DQNModel
open ConnectFourDQN.ReplayBuffer
open ConnectFourDQN.DQNAgent

// NOTE: `open type TorchSharp.torch` shadows many F# conversion functions:
//   - `int`     → use `Operators.int`
//   - `float`   → use `Operators.float`
//   - `float32` → use `Operators.float32`
//   - `int64`   → use `Operators.int64`
//   - `float64` type does NOT exist in F# (it's `float`); TorchSharp adds ScalarType.Float64

// ── Hyperparameters ─────────────────────────────────────────────────────────
[<Struct>]
type TrainingConfig = {
    TotalEpisodes:   int
    ReplayCapacity:  int
    MinReplaySize:   int    // experiences before training starts
    BatchSize:       int
    Gamma:           float32
    EpsilonStart:    float
    EpsilonEnd:      float
    EpsilonDecayEnd: int    // episode at which epsilon reaches EpsilonEnd
    TargetSyncFreq:  int    // steps between target network hard updates
    LearningRate:    float  // Adam lr (passed as named arg; TorchSharp optim.Adam takes float)
    ModelSavePath:   string
}

let defaultConfig = {
    TotalEpisodes   = 50_000
    ReplayCapacity  = 50_000
    MinReplaySize   = 1_000
    BatchSize       = 128
    Gamma           = 0.99f
    EpsilonStart    = 1.0
    EpsilonEnd      = 0.05
    EpsilonDecayEnd = 40_000
    TargetSyncFreq  = 1_000
    LearningRate    = 1e-4
    ModelSavePath   = "connect_four_dqn.pt"
}

// ── Training Result ──────────────────────────────────────────────────────────
type TrainingResult = {
    LossHistory:    (int * float32) list   // (episode, avg loss in window)
    WinRateHistory: (int * float) list     // (episode, win rate in window)
    FinalEpsilon:   float
    ModelPath:      string
}

// ── Opponent Selection (Curriculum) ─────────────────────────────────────────
// Episodes 0..20K:   random opponent
// Episodes 20K..35K: Minimax depth 2
// Episodes 35K..50K: Minimax depth 4
let private chooseOpponentMove (rng: System.Random) (board: Board) (opp: Cell) (episode: int) : int =
    let legal = legalMoves board
    if episode < 20_000 then
        legal.[rng.Next(legal.Length)]
    elif episode < 35_000 then
        fst (chooseMoveAB board opp 2)
    else
        fst (chooseMoveAB board opp 4)

// ── Single Episode ────────────────────────────────────────────────────────────
// Play one episode. DQN always plays as Red; opponent plays as Yellow.
// Only Red's experiences are pushed into the replay buffer.
// Returns whether Red won or it was a draw.
let private runEpisode (rng: System.Random) (model: DQNModel) (buf: ReplayBuffer)
                        (epsilon: float) (episode: int) : {| Win: bool; Draw: bool |} =
    let mutable board   = Array.create (rows * cols) Empty
    let mutable current = Red
    let mutable result  = {| Win = false; Draw = false |}
    let mutable running = true

    while running do
        let legal = legalMoves board
        if List.isEmpty legal then
            // No legal moves left — draw
            result  <- {| Win = false; Draw = true |}
            running <- false
        else
            let action =
                if current = Red then
                    chooseMove rng model board Red Yellow epsilon
                else
                    chooseOpponentMove rng board Yellow episode

            let stateData     = boardToArray board Red Yellow
            let nextBoard     = applyMove board current action
            let nextStateData = boardToArray nextBoard Red Yellow

            let gameResult = isGameOver nextBoard
            let gameOver   = Option.isSome gameResult

            let redWon =
                match gameResult with
                | Some RedWins -> true
                | _            -> false

            let reward =
                if not gameOver then 0.0f
                elif redWon     then 1.0f
                else
                    match gameResult with
                    | Some YellowWins -> -1.0f
                    | _               -> 0.3f   // draw

            // Only push Red's perspective into replay buffer
            if current = Red then
                buf.Push {
                    StateData     = stateData
                    Action        = action
                    Reward        = reward
                    NextStateData = nextStateData
                    Done          = gameOver
                }

            board   <- nextBoard
            current <- if current = Red then Yellow else Red

            if gameOver then
                let isDraw =
                    match gameResult with
                    | Some Draw -> true
                    | _         -> false
                result  <- {| Win = redWon; Draw = isDraw |}
                running <- false

    result

// ── Training Loop ─────────────────────────────────────────────────────────────
// Runs totalEpisodes of curriculum DQN training.
// Returns TrainingResult with loss/win-rate history for Serilog logging in Program.fs.
// PURE (no I/O): caller (Program.fs) handles all logging.
let trainDQN (config: TrainingConfig) : TrainingResult =
    let rng    = System.Random(42)
    let model  = new DQNModel("policy")
    let target = new DQNModel("target")

    // Initialise target weights = policy weights
    model.save(config.ModelSavePath) |> ignore
    target.load(config.ModelSavePath) |> ignore

    // `open type TorchSharp.torch` shadows `float`, so we pass the named argument
    // using the raw F# float value (which TorchSharp resolves via optional param).
    let opt = optim.Adam(model.parameters(), lr = config.LearningRate)
    let buf = ReplayBuffer(config.ReplayCapacity)

    let mutable step        = 0
    let mutable lossAcc     = 0.0f
    let mutable lossCount   = 0
    let mutable wins        = 0
    let mutable windowTotal = 0

    let logInterval = 1_000   // log every 1K episodes

    let mutable lossHistory    : (int * float32) list = []
    let mutable winRateHistory : (int * float) list   = []

    for episode in 0 .. config.TotalEpisodes - 1 do
        // Epsilon: linear decay from EpsilonStart to EpsilonEnd over EpsilonDecayEnd episodes
        let epsilon =
            if episode >= config.EpsilonDecayEnd then config.EpsilonEnd
            else
                config.EpsilonStart -
                  (config.EpsilonStart - config.EpsilonEnd) *
                  Operators.float episode / Operators.float config.EpsilonDecayEnd

        let episodeResult = runEpisode rng model buf epsilon episode
        if episodeResult.Win then wins <- wins + 1
        windowTotal <- windowTotal + 1
        step        <- step + 1

        // Train when buffer has enough experiences
        if buf.Size >= config.MinReplaySize then
            let batch = buf.Sample config.BatchSize rng
            let loss  = trainStep model target opt batch config.Gamma
            lossAcc   <- lossAcc + loss
            lossCount <- lossCount + 1

            // Hard sync target network every targetSyncFreq steps
            if step % config.TargetSyncFreq = 0 then
                let tmpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_dqn_sync_tmp.dat")
                syncTargetNetwork model target tmpPath

        // Collect history snapshot every logInterval episodes
        if (episode + 1) % logInterval = 0 then
            let avgLoss =
                if lossCount > 0
                then lossAcc / Operators.float32 lossCount
                else 0.0f
            let winRate = Operators.float wins / Operators.float windowTotal
            lossHistory    <- lossHistory    @ [(episode + 1, avgLoss)]
            winRateHistory <- winRateHistory @ [(episode + 1, winRate)]
            lossAcc     <- 0.0f
            lossCount   <- 0
            wins        <- 0
            windowTotal <- 0

    // Save final trained model
    model.save(config.ModelSavePath) |> ignore

    {
        LossHistory    = lossHistory
        WinRateHistory = winRateHistory
        FinalEpsilon   = config.EpsilonEnd
        ModelPath      = config.ModelSavePath
    }
