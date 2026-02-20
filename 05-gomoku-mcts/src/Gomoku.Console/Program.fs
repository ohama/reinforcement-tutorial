module Gomoku.Console.Program

open System
open Gomoku.Domain
open Gomoku.Rules
open Gomoku.PolicyValueNet
open Gomoku.Mcts
open Gomoku.Training

let private printBoard (board: Board) =
    printfn ""
    printf "   "
    for c in 0 .. BoardSize - 1 do printf "%2d" c
    printfn ""
    for r in 0 .. BoardSize - 1 do
        printf "%2d " r
        for c in 0 .. BoardSize - 1 do
            let cell = board.[r * BoardSize + c]
            let ch = if cell = 1 then "X" elif cell = -1 then "O" else "."
            printf " %s" ch
        printfn ""
    printfn ""

let private parseHumanMove (input: string) (legal: int[]) : int option =
    let parts = input.Trim().Split([| ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    match parts with
    | [| r; c |] ->
        match Int32.TryParse(r), Int32.TryParse(c) with
        | (true, row), (true, col) when row >= 0 && row < BoardSize && col >= 0 && col < BoardSize ->
            let idx = row * BoardSize + col
            if legal |> Array.contains idx then Some idx else None
        | _ -> None
    | [| idx |] ->
        match Int32.TryParse(idx) with
        | true, i when i >= 0 && i < BoardSize * BoardSize && legal |> Array.contains i -> Some i
        | _ -> None
    | _ -> None

let private runTraining (config: TrainingConfig) =
    printfn "\n=== Gomoku MCTS Training ==="
    printfn "Config: %d iterations, %d simulations/move, %d games/iter"
        config.NTrainingIter config.NSimulations config.NSelfPlayGames

    let model = new PolicyValueNet("pv-net")
    let rng   = Random(42)
    let results = runSelfPlayTraining model config rng

    printfn "\n=== Training Complete ==="
    for r in results |> List.filter (fun r -> r.Iteration % 10 = 0 || r.Iteration = 1) do
        printfn "  Iter %3d | Games=%4d | PolicyLoss=%.4f | ValueLoss=%.4f"
            r.Iteration r.GamesPlayed r.AvgPolicyLoss r.AvgValueLoss

    model.save(config.ModelSavePath) |> ignore
    printfn "\nModel saved to: %s" config.ModelSavePath
    model

let private runBenchmark (modelPath: string) (nGames: int) (nSimulations: int) =
    printfn "\n=== Benchmark: AI vs Random (%d games, %d simulations/move) ===" nGames nSimulations

    let model = new PolicyValueNet("pv-net")
    if IO.File.Exists(modelPath) then
        model.load(modelPath) |> ignore
        printfn "Model loaded from: %s" modelPath
    else
        printfn "No saved model found at %s — using random weights." modelPath

    let rng  = Random(123)
    let wins = evaluateVsRandom model nGames nSimulations 5.0 rng
    let rate = float wins / float nGames
    printfn "Result: %d/%d wins (%.1f%%)" wins nGames (rate * 100.0)

let private runHumanVsAI (modelPath: string) (nSimulations: int) =
    printfn "\n=== Human vs AI (difficulty = %d simulations/move) ===" nSimulations

    let model = new PolicyValueNet("pv-net")
    if IO.File.Exists(modelPath) then
        model.load(modelPath) |> ignore
        printfn "Model loaded from: %s\n" modelPath
    else
        printfn "No saved model found at %s — AI uses random weights.\n" modelPath

    printfn "You are O (White). AI is X (Black)."
    printfn "Enter moves as 'row,col' (0-indexed).\n"

    let mutable state  = initialState ()
    let mutable running = true
    let humanPlayer = White
    let rng = Random()

    while running do
        printBoard state.Board
        match state.LastMove with
        | Some m when isWinningMove state.Board m ->
            let winner = opponent state.CurrentPlayer
            if winner = humanPlayer then printfn "You win!"
            else printfn "AI wins!"
            running <- false
        | _ when (legalMoves state.Board).Length = 0 ->
            printfn "Draw!"
            running <- false
        | _ ->
            if state.CurrentPlayer = humanPlayer then
                printf "Your move (row,col): "
                let input = Console.ReadLine()
                let legal = legalMoves state.Board
                match parseHumanMove input legal with
                | Some move -> state <- applyMove state move
                | None      -> printfn "Invalid move."
            else
                printfn "AI thinking..."
                let visitProbs = mctsSearchWithNet model rng state nSimulations 5.0 false
                let move = bestMove visitProbs
                printfn "AI plays: (%d, %d)" (move / BoardSize) (move % BoardSize)
                state <- applyMove state move

let private showMenu () =
    printfn "\n=== Gomoku MCTS ==="
    printfn "  1. Train (self-play)"
    printfn "  2. Benchmark vs random"
    printfn "  3. Human vs AI"
    printfn "  4. Quit"
    printf "Choice: "

[<EntryPoint>]
let main _ =
    let modelPath    = "gomoku_model.pt"
    let trainConfig  = { defaultConfig with ModelSavePath = modelPath }

    let mutable running = true
    while running do
        showMenu ()
        match Console.ReadLine() with
        | "1" -> runTraining trainConfig |> ignore
        | "2" ->
            printf "Simulations per move (default 100): "
            let sims =
                match Int32.TryParse(Console.ReadLine()) with
                | true, n when n > 0 -> n
                | _ -> 100
            runBenchmark modelPath 50 sims
        | "3" ->
            printf "Simulations per move / difficulty (default 100): "
            let sims =
                match Int32.TryParse(Console.ReadLine()) with
                | true, n when n > 0 -> n
                | _ -> 100
            runHumanVsAI modelPath sims
        | "4" | "q" | "Q" -> running <- false
        | other ->
            if not (String.IsNullOrEmpty other) then printfn "Unknown option: %s" other

    0
