module ConnectFourDQN.Console.Program

open Serilog
open ConnectFourDQN.Domain
open ConnectFourDQN.Rules
open ConnectFourDQN.Minimax
open ConnectFourDQN.DQNModel
open ConnectFourDQN.DQNAgent
open ConnectFourDQN.Console.Training

// ── Serilog Setup ────────────────────────────────────────────────────────────
let private setupLogging () =
    let logDir = "logs"
    System.IO.Directory.CreateDirectory(logDir) |> ignore
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                System.IO.Path.Combine(logDir, "dqn-training.log"),
                outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger()

// ── Board Rendering ──────────────────────────────────────────────────────────
let private cellChar = function
    | Empty  -> '.'
    | Red    -> 'R'
    | Yellow -> 'Y'

let private printBoard (board: Board) =
    printfn " 0 1 2 3 4 5 6"
    for r in 0 .. rows - 1 do
        let row = String.init cols (fun c -> sprintf "%c " (cellChar board.[r * cols + c]))
        printfn "%s" row
    printfn ""

// ── Training Mode ─────────────────────────────────────────────────────────────
let private runTraining () =
    Log.Information("DQN 학습 시작 — 50K 에피소드, 커리큘럼 학습 (random → Minimax depth 2 → depth 4)")
    Log.Information("학습 중... (완료까지 수 분 소요)")

    let result = trainDQN defaultConfig

    // Log loss and win rate history as structured properties
    for (episode, avgLoss) in result.LossHistory do
        Log.Information("Episode={Episode:D5} AvgLoss={AvgLoss:F6}", episode, avgLoss)

    for (episode, winRate) in result.WinRateHistory do
        Log.Information("Episode={Episode:D5} WinRate={WinRate:P1}", episode, winRate)

    Log.Information("학습 완료. 모델 저장: {ModelPath}", result.ModelPath)
    printfn "\n학습 완료! 모델이 '%s'에 저장됐습니다." result.ModelPath

// ── Benchmark Mode ───────────────────────────────────────────────────────────
let private runBenchmark (modelPath: string) =
    if not (System.IO.File.Exists(modelPath)) then
        printfn "모델 파일이 없습니다: %s — 먼저 학습을 실행하세요." modelPath
    else
        Log.Information("벤치마크 시작 — {ModelPath} vs Minimax depth 4, 100 게임", modelPath)
        let model = new DQNModel("bench")
        model.load(modelPath) |> ignore
        let rng = System.Random(0)
        let mutable wins = 0
        let mutable losses = 0
        let mutable draws = 0
        for _ in 1 .. 100 do
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
                            chooseMove rng model board Red Yellow 0.0
                        else
                            fst (chooseMoveAB board Yellow 4)
                    board   <- applyMove board current col
                    current <- if current = Red then Yellow else Red
                    match isGameOver board with
                    | Some RedWins    -> wins   <- wins   + 1; running <- false
                    | Some YellowWins -> losses <- losses + 1; running <- false
                    | Some Draw       -> draws  <- draws  + 1; running <- false
                    | None            -> ()
        let total = wins + losses + draws
        let winRate = float wins / float total
        Log.Information("벤치마크 결과: W={Wins} L={Losses} D={Draws} WinRate={WinRate:P1}", wins, losses, draws, winRate)
        printfn "\n벤치마크 결과 (100 게임 vs Minimax depth 4):"
        printfn "  승: %d  패: %d  무: %d  승률: %.1f%%" wins losses draws (winRate * 100.0)

// ── Human vs AI Mode ─────────────────────────────────────────────────────────
let private runHumanVsAI (modelPath: string) =
    if not (System.IO.File.Exists(modelPath)) then
        printfn "모델 파일이 없습니다: %s — 먼저 학습을 실행하세요." modelPath
    else
        let model = new DQNModel("play")
        model.load(modelPath) |> ignore
        let rng = System.Random()
        printfn "\n사람(Y) vs DQN(R). 열 번호(0~6)를 입력하세요."
        let mutable board   = Array.create (rows * cols) Empty
        let mutable current = Yellow  // Human goes first as Yellow
        let mutable running = true
        while running do
            printBoard board
            let legal = legalMoves board
            if List.isEmpty legal then
                printfn "무승부!"
                running <- false
            else
                let col =
                    if current = Yellow then
                        printf "열 선택 (합법: %s): " (legal |> List.map string |> String.concat ",")
                        let mutable input = -1
                        while not (List.contains input legal) do
                            match System.Int32.TryParse(System.Console.ReadLine()) with
                            | true, n when List.contains n legal -> input <- n
                            | _ -> printfn "유효하지 않은 열. 다시 입력하세요."
                        input
                    else
                        let c = chooseMove rng model board Red Yellow 0.0
                        printfn "DQN이 %d열을 선택했습니다." c
                        c
                board   <- applyMove board current col
                current <- if current = Red then Yellow else Red
                match isGameOver board with
                | Some RedWins ->
                    printBoard board
                    printfn "DQN 승리!"
                    running <- false
                | Some YellowWins ->
                    printBoard board
                    printfn "사람 승리!"
                    running <- false
                | Some Draw ->
                    printBoard board
                    printfn "무승부!"
                    running <- false
                | None -> ()

// ── Main ──────────────────────────────────────────────────────────────────────
[<EntryPoint>]
let main _ =
    setupLogging ()
    printfn "=== F# DQN Connect Four ==="

    let rec menu () =
        printfn "\n1. 학습 (50K 에피소드, 커리큘럼)"
        printfn "2. 벤치마크 (DQN vs Minimax depth 4, 100 게임)"
        printfn "3. 사람 vs DQN"
        printfn "0. 종료"
        printf "선택: "
        match System.Console.ReadLine() with
        | "1" -> runTraining ();                             menu ()
        | "2" -> runBenchmark defaultConfig.ModelSavePath;  menu ()
        | "3" -> runHumanVsAI defaultConfig.ModelSavePath;  menu ()
        | "0" -> ()
        | _   -> printfn "잘못된 입력.";                    menu ()

    menu ()
    Log.CloseAndFlush()
    0
