module TicTacToe.Training

open TicTacToe.Domain
open TicTacToe.Rules
open TicTacToe.Agent

/// 한 에피소드 자가 대국 실행.
/// 두 TD 에이전트가 동일 ValueTable(공유)으로 경쟁.
/// 반환: (업데이트된 ValueTable, 게임 결과)
let playEpisode
    (rng: System.Random)
    (alpha: float)
    (epsilon: float)
    (vTable: ValueTable) : ValueTable * GameResult =

    let rec loop (state: GameState) (vTable: ValueTable) (prevBoard: Board option) =
        match isGameOver state.Board with
        | Some result ->
            // 종단 상태: 최종 값 고정 + 마지막 백업
            let terminalValue =
                match result with
                | XWins -> 1.0
                | OWins -> 0.0
                | Draw  -> 0.5
            let vTable' = Map.add state.Board terminalValue vTable
            let vTable'' =
                match prevBoard with
                | Some prev -> tdUpdate alpha vTable' prev state.Board
                | None -> vTable'
            vTable'', result
        | None ->
            let move = tdAgent rng epsilon vTable state
            let nextBoard = applyMove state.Board state.CurrentPlayer move
            let nextState = { Board = nextBoard; CurrentPlayer = otherPlayer state.CurrentPlayer }
            // TD 백업: 현재 플레이어의 이전 보드를 현재 보드 방향으로 업데이트
            let vTable' =
                match prevBoard with
                | Some prev -> tdUpdate alpha vTable prev state.Board
                | None -> vTable
            loop nextState vTable' (Some state.Board)

    loop (initialState ()) vTable None

/// N 에피소드 자가 대국 학습 (TICT-05).
/// 반환: (최종 ValueTable, win rate history [(episode, xWinRate)])
/// logInterval마다 현재까지의 X 승률을 history에 기록
let trainAgent
    (rng: System.Random)
    (episodes: int)
    (alpha: float)
    (epsilon: float)
    (logInterval: int) : ValueTable * (int * float) list =

    let rec loop ep vTable wins history =
        if ep > episodes then
            vTable, List.rev history
        else
            let vTable', result = playEpisode rng alpha epsilon vTable
            let wins' = if result = XWins then wins + 1 else wins
            let history' =
                if ep % logInterval = 0 && ep > 0 then
                    let rate = float wins' / float ep
                    (ep, rate) :: history
                else history
            loop (ep + 1) vTable' wins' history'

    loop 1 Map.empty 0 []

/// TD 에이전트(X, epsilon=0) vs 랜덤 에이전트(O) 승률 측정 (TICT-08)
/// games 판 중 X 승리 비율 반환
let winRateVsRandom
    (rng: System.Random)
    (vTable: ValueTable)
    (games: int) : float =
    let wins =
        [ 1..games ]
        |> List.sumBy (fun _ ->
            let rec play state =
                match isGameOver state.Board with
                | Some XWins -> 1
                | Some _ -> 0
                | None ->
                    let move =
                        if state.CurrentPlayer = X then
                            tdAgent rng 0.0 vTable state  // 평가 시 epsilon=0 (순수 탐욕)
                        else
                            randomAgent rng state
                    let next = {
                        Board = applyMove state.Board state.CurrentPlayer move
                        CurrentPlayer = otherPlayer state.CurrentPlayer
                    }
                    play next
            play (initialState ()))
    float wins / float games
