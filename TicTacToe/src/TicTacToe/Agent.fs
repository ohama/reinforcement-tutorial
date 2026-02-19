module TicTacToe.Agent

open TicTacToe.Domain
open TicTacToe.Rules

/// TD(0) 업데이트: V(s) <- V(s) + alpha * (V(s') - V(s))
/// alpha: 학습률 (전형적으로 0.1-0.3)
/// 모든 값은 X의 관점 (V=1.0: X 확실 승, V=0.0: O 확실 승, V=0.5: 불명/무승부)
let tdUpdate (alpha: float) (vTable: ValueTable) (state: Board) (nextState: Board) : ValueTable =
    let vCurrent = Map.tryFind state vTable |> Option.defaultValue 0.5
    let vNext    = Map.tryFind nextState vTable |> Option.defaultValue 0.5
    let vNew     = vCurrent + alpha * (vNext - vCurrent)
    Map.add state vNew vTable

/// 랜덤 에이전트 (TICT-03): 합법 수 중 무작위 선택
let randomAgent (rng: System.Random) (state: GameState) : int =
    let moves = legalMoves state.Board
    moves.[rng.Next(moves.Length)]

/// TD 에이전트 (TICT-04): epsilon-greedy 수 선택
/// X는 V(후계)를 최대화, O는 V(후계)를 최소화
/// epsilon 확률로 무작위, 그 외에는 탐욕적 선택
let tdAgent (rng: System.Random) (epsilon: float) (vTable: ValueTable) (state: GameState) : int =
    let moves = legalMoves state.Board
    if rng.NextDouble() < epsilon then
        moves.[rng.Next(moves.Length)]  // 탐험
    else
        let getBoardValue board =
            Map.tryFind board vTable |> Option.defaultValue 0.5
        let scoredMoves =
            moves
            |> List.map (fun move ->
                let nextBoard = applyMove state.Board state.CurrentPlayer move
                let v = getBoardValue nextBoard
                move, v)
        match state.CurrentPlayer with
        | X -> scoredMoves |> List.maxBy snd |> fst  // X: V 최대화
        | O -> scoredMoves |> List.minBy snd |> fst  // O: V 최소화
        | Empty -> failwith "tdAgent: Empty는 유효한 플레이어가 아닙니다"
