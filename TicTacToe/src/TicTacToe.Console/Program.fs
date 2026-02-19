module TicTacToe.Console.Program

open Serilog
open TicTacToe.Domain
open TicTacToe.Rules
open TicTacToe.Agent
open TicTacToe.Training

/// 보드를 콘솔에 출력
let printBoard (board: Board) =
    let cellChar = function X -> "X" | O -> "O" | Empty -> "."
    for row in 0..2 do
        let line =
            [0..2]
            |> List.map (fun col -> cellChar board.[row * 3 + col])
            |> String.concat " | "
        printfn " %s " line
        if row < 2 then printfn " ---------"

/// 사람(X) vs AI(O) 콘솔 대전 루프 (TICT-06)
/// epsilon=0: AI는 순수 탐욕적 플레이 (최선 수 선택)
let runHumanVsAI (rng: System.Random) (vTable: ValueTable) =
    printfn "\n============================="
    printfn " 사람(X) vs AI(O) 대전 시작! "
    printfn "============================="
    printfn "위치 안내: 1|2|3 / 4|5|6 / 7|8|9 (1=왼쪽 위, 9=오른쪽 아래)\n"

    let rec loop state =
        printBoard state.Board
        match isGameOver state.Board with
        | Some XWins -> printfn "\n사람(X) 승리!"
        | Some OWins -> printfn "\nAI(O) 승리!"
        | Some Draw  -> printfn "\n무승부!"
        | None ->
            if state.CurrentPlayer = X then
                // 사람 차례
                printf "위치 입력 (1-9): "
                let input = System.Console.ReadLine()
                match System.Int32.TryParse(input) with
                | true, n when n >= 1 && n <= 9 ->
                    let idx = n - 1  // 0-indexed로 변환
                    if state.Board.[idx] <> Empty then
                        printfn "이미 둔 자리입니다. 다시 선택하세요."
                        loop state
                    else
                        let nextBoard = applyMove state.Board state.CurrentPlayer idx
                        let nextState = { Board = nextBoard; CurrentPlayer = otherPlayer state.CurrentPlayer }
                        loop nextState
                | _ ->
                    printfn "잘못된 입력입니다. 1-9 사이 숫자를 입력하세요."
                    loop state
            else
                // AI 차례 (epsilon=0: 순수 탐욕)
                let move = tdAgent rng 0.0 vTable state
                printfn "AI(O)가 %d번 위치에 두었습니다." (move + 1)
                let nextBoard = applyMove state.Board state.CurrentPlayer move
                let nextState = { Board = nextBoard; CurrentPlayer = otherPlayer state.CurrentPlayer }
                loop nextState

    loop (initialState ())

[<EntryPoint>]
let main _args =
    // Serilog 설정 (TICT-09)
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File(
                "logs/tictactoe-.log",
                rollingInterval = RollingInterval.Day,
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger()

    let rng = System.Random(42)

    Log.Information("=== TicTacToe TD Learning 시작 ===")
    Log.Information("학습 설정: 에피소드=100,000 alpha=0.1 epsilon=0.1 로그간격=1,000")

    // 학습 실행 (TICT-05): trainAgent는 순수 함수로 history 반환
    let vTable, history = trainAgent rng 100_000 0.1 0.1 1_000

    // 학습 곡선 로깅 (TICT-09): 매 1,000 에피소드 승률 기록
    for (ep, rate) in history do
        Log.Information("Episode={Episode} WinRate={WinRate:P1}", ep, rate)

    let finalRate = if history.IsEmpty then 0.0 else snd (List.last history)
    Log.Information("학습 완료. 최종 승률: {WinRate:P1}", finalRate)
    Log.Information("ValueTable 크기: {Size}개 상태", Map.count vTable)

    // 사람 vs AI 대전 (TICT-06)
    runHumanVsAI rng vTable

    Log.CloseAndFlush()
    0
