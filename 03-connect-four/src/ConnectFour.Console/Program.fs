module ConnectFour.Console.Program

open Serilog
open ConnectFour.Domain
open ConnectFour.Rules
open ConnectFour.Minimax
open ConnectFour.QAgent
open ConnectFour.Console.Training

let private cellChar (c: Cell) =
    match c with
    | Empty  -> "."
    | Red    -> "R"
    | Yellow -> "Y"

let private printBoard (board: Board) =
    printfn " 1 2 3 4 5 6 7"
    for r in 0 .. rows - 1 do
        for c in 0 .. cols - 1 do
            printf " %s" (cellChar board.[idx r c])
        printfn ""
    printfn ""

let private getHumanMove (board: Board) : int =
    let legal = legalMoves board
    let rec ask () =
        printf "열을 입력하세요 (1-7): "
        match System.Console.ReadLine() |> System.Int32.TryParse with
        | true, n when n >= 1 && n <= 7 && List.contains (n - 1) legal ->
            n - 1
        | true, n when n >= 1 && n <= 7 ->
            printfn "열 %d는 가득 찼습니다. 다시 선택하세요." n
            ask ()
        | _ ->
            printfn "1에서 7 사이의 숫자를 입력하세요."
            ask ()
    ask ()

let private runHumanVsMinimax (depth: int) =
    printfn "\n=== 사람(R) vs Minimax AI(Y) ==="
    let rec loop board currentPlayer =
        printBoard board
        match isGameOver board with
        | Some RedWins    -> printfn "사람(R) 승리!"
        | Some YellowWins -> printfn "Minimax AI(Y) 승리!"
        | Some Draw       -> printfn "무승부!"
        | None ->
            let col =
                if currentPlayer = Red then
                    getHumanMove board
                else
                    let c, pruned = chooseMoveAB board currentPlayer depth
                    printfn "AI가 열 %d를 선택했습니다 (가지치기: %d회)" (c + 1) pruned
                    c
            loop (applyMove board currentPlayer col) (opponent currentPlayer)
    loop (emptyBoard ()) Red

let private runHumanVsQAgent (rng: System.Random) (table: QTable) =
    printfn "\n=== 사람(R) vs Q-Learning AI(Y) ==="
    let rec loop board currentPlayer =
        printBoard board
        match isGameOver board with
        | Some RedWins    -> printfn "사람(R) 승리!"
        | Some YellowWins -> printfn "Q-Learning AI(Y) 승리!"
        | Some Draw       -> printfn "무승부!"
        | None ->
            let col =
                if currentPlayer = Red then
                    getHumanMove board
                else
                    chooseAction rng table board 0.0
            loop (applyMove board currentPlayer col) (opponent currentPlayer)
    loop (emptyBoard ()) Red

let private runAIvsAI (rng: System.Random) (redTable: QTable) (depth: int) (numGames: int) =
    printfn "\n=== AI vs AI: Minimax(R) vs Q-Learning(Y) — %d게임 ===" numGames
    let mutable minimaxWins = 0
    let mutable qWins       = 0
    let mutable draws       = 0
    let totalPrunes         = ref 0

    for gameNum in 1 .. numGames do
        let rec loop board currentPlayer moves =
            match isGameOver board with
            | Some RedWins ->
                minimaxWins <- minimaxWins + 1
                Log.Information("Game {GameNum}: Minimax(R) wins in {Moves} moves | PruneCount={PruneCount}",
                    gameNum, moves, totalPrunes.Value)
            | Some YellowWins ->
                qWins <- qWins + 1
                Log.Information("Game {GameNum}: Q-Learning(Y) wins in {Moves} moves | PruneCount={PruneCount}",
                    gameNum, moves, totalPrunes.Value)
            | Some Draw ->
                draws <- draws + 1
                Log.Information("Game {GameNum}: Draw in {Moves} moves | PruneCount={PruneCount}",
                    gameNum, moves, totalPrunes.Value)
            | None ->
                let col =
                    if currentPlayer = Red then
                        let c, pruned = chooseMoveAB board Red depth
                        totalPrunes.Value <- totalPrunes.Value + pruned
                        c
                    else
                        chooseAction rng redTable board 0.0
                loop (applyMove board currentPlayer col) (opponent currentPlayer) (moves + 1)
        loop (emptyBoard ()) Red 0

    printfn "\n=== AI vs AI 결과 (총 %d게임) ===" numGames
    printfn "Minimax(R) 승: %d / %d (%.1f%%)" minimaxWins numGames (float minimaxWins / float numGames * 100.0)
    printfn "Q-Learning(Y) 승: %d / %d (%.1f%%)" qWins numGames (float qWins / float numGames * 100.0)
    printfn "무승부: %d / %d (%.1f%%)" draws numGames (float draws / float numGames * 100.0)
    printfn "Alpha-Beta 누적 가지치기: %d회" totalPrunes.Value
    Log.Information("AIvsAI Summary: Minimax={MinimaxWins} QAgent={QWins} Draws={Draws} TotalPrunes={TotalPrunes}",
        minimaxWins, qWins, draws, totalPrunes.Value)

[<EntryPoint>]
let main _argv =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File("logs/connectfour-.log",
                rollingInterval = RollingInterval.Day,
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger()

    let rng = System.Random()

    printfn "Q-Learning 에이전트 학습 중 (50,000 에피소드)..."
    let result = trainQLearning rng 50_000 0.1 0.95 0.15 0.05 10_000

    for (ep, qSize, winRate) in result.History do
        Log.Information("Episode={Episode} QTableSize={QTableSize} RedWinRate={RedWinRate:P1}",
            ep, qSize, winRate)

    let totalPossibleStates = 4_531_985_219_092L
    printfn "\n=== Q-Table 크기 분석 ==="
    printfn "학습 후 방문한 상태: %d" result.RedTable.Count
    printfn "전체 가능한 상태:    %d" totalPossibleStates
    printfn "커버율:              %.6f%%" (float result.RedTable.Count / float totalPossibleStates * 100.0)
    printfn "(이것이 Phase 4 DQN이 필요한 이유입니다)"
    Log.Information("QTable Analysis: Visited={Visited} TotalPossible={Total} Coverage={Coverage:P8}",
        result.RedTable.Count, totalPossibleStates,
        float result.RedTable.Count / float totalPossibleStates)

    let rec menu () =
        printfn "\n=== Connect Four Phase 3 ==="
        printfn "1. AI vs AI (Minimax vs Q-Learning, 20게임)"
        printfn "2. 사람 vs Minimax AI"
        printfn "3. 사람 vs Q-Learning AI"
        printfn "0. 종료"
        printf "선택: "
        match System.Console.ReadLine() with
        | "1" -> runAIvsAI rng result.RedTable 6 20; menu ()
        | "2" -> runHumanVsMinimax 7; menu ()
        | "3" -> runHumanVsQAgent rng result.RedTable; menu ()
        | "0" -> ()
        | _   -> printfn "잘못된 입력입니다."; menu ()

    menu ()
    Log.CloseAndFlush()
    0
