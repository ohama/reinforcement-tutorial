module ConnectFourDQN.Tests.BenchmarkTests

open Expecto
open TorchSharp
open type TorchSharp.torch
open ConnectFourDQN.Domain
open ConnectFourDQN.Rules
open ConnectFourDQN.Minimax
open ConnectFourDQN.DQNModel
open ConnectFourDQN.DQNAgent
open ConnectFourDQN.Console.Training

// NOTE: `open type TorchSharp.torch` shadows F# int/float conversion functions.
// Use `Operators.int` and `Operators.float` where needed.

// ── Play N games: DQN (Red, greedy) vs opponent move function ────────────────
// opponentMove: Board -> Cell -> int   (returns chosen column)
// Returns (wins, losses, draws) from Red's perspective.
let private playGames (model: DQNModel) (rng: System.Random)
                       (opponentMove: Board -> Cell -> int) (n: int)
                       : int * int * int =
    let mutable wins   = 0
    let mutable losses = 0
    let mutable draws  = 0
    for _ in 1 .. n do
        let mutable board   = Array.create (rows * cols) Empty
        let mutable current = Red
        let mutable running = true
        while running do
            let legal = legalMoves board
            if List.isEmpty legal then
                draws   <- draws + 1
                running <- false
            else
                let col =
                    if current = Red then
                        chooseMove rng model board Red Yellow 0.0  // greedy (epsilon=0)
                    else
                        opponentMove board Yellow
                board   <- applyMove board current col
                current <- if current = Red then Yellow else Red
                match isGameOver board with
                | Some RedWins    -> wins   <- wins   + 1; running <- false
                | Some YellowWins -> losses <- losses + 1; running <- false
                | Some Draw       -> draws  <- draws  + 1; running <- false
                | None            -> ()
    wins, losses, draws

// ── Test 1: Model save/load roundtrip ───────────────────────────────────────
[<Tests>]
let modelSaveLoadTest =
    testCase "Model save/load: loaded model produces same output as original" (fun () ->
        use _scope = NewDisposeScope()
        let m1 = new DQNModel("m1")
        let tmpPath = System.IO.Path.GetTempFileName()
        m1.save(tmpPath) |> ignore
        let m2 = new DQNModel("m2")
        m2.load(tmpPath) |> ignore
        // Both models should produce identical output for same input
        use input = zeros([| 1L; 3L; 6L; 7L |])
        use out1  = m1.forward(input)
        use out2  = m2.forward(input)
        use diff  = (out1 - out2).abs().max()
        Expect.isTrue (diff.item<float32>() < 1e-5f)
            "Loaded model output must match saved model output")

// ── Test 2: DQN benchmark — 3K episode training vs Minimax depth 1 ──────────
// CI-safe short run. Full 50K episode vs depth 4 is the production target
// (run via `dotnet run` in ConnectFourDQN.Console).
// Decision (DQN-CI): use depth 1 / 3K episodes / > 30% threshold for fast CI.
// Production goal: > 50% win rate vs Minimax depth 2 after 50K episodes.
[<Tests>]
let dqnVsMinimaxBenchmark =
    testCase "DQN (3K episodes, curriculum) beats Minimax depth 1 at > 30% win rate" (fun () ->
        // Short training run for CI speed (3K episodes, not 50K)
        let config = { defaultConfig with
                         TotalEpisodes   = 3_000
                         MinReplaySize   = 500
                         ModelSavePath   =
                             System.IO.Path.Combine(
                                 System.IO.Path.GetTempPath(), "dqn_benchmark_test.pt") }
        let _result = trainDQN config

        // Load the saved model
        let loaded = new DQNModel("bench")
        loaded.load(config.ModelSavePath) |> ignore

        let rng = System.Random(99)
        // `chooseMoveAB` returns (col, pruneCount) — take fst for column
        let wins, losses, draws =
            playGames loaded rng (fun board opp -> fst (chooseMoveAB board opp 1)) 50

        let total   = wins + losses + draws
        let winRate = Operators.float wins / Operators.float total

        // DQN-CI decision: 3K episodes vs depth 1 → assert > 30% win rate as smoke test.
        // Production target (50K episodes vs depth 4): > 50% vs Minimax depth 2.
        // Run `dotnet run --project src/ConnectFourDQN.Console` for full benchmark.
        Expect.isGreaterThan winRate 0.30
            (sprintf "Expected win rate > 30%% vs Minimax depth 1, got %.1f%% (%d W / %d L / %d D)"
                (winRate * 100.0) wins losses draws))
