module ConnectFour.Minimax

open ConnectFour.Domain
open ConnectFour.Rules

let [<Literal>] private NegInf = -1_000_000
let [<Literal>] private PosInf =  1_000_000

let private scoreWindow (window: Cell list) (player: Cell) : int =
    let opp = opponent player
    let p = window |> List.filter ((=) player) |> List.length
    let o = window |> List.filter ((=) opp) |> List.length
    if o > 0 then 0
    else
        match p with
        | 4 -> 10000
        | 3 -> 50
        | 2 -> 3
        | 1 -> 1
        | _ -> 0

let private allWindows (board: Board) : Cell list list =
    [ for r in 0 .. rows-1 do
        for c in 0 .. cols-4 do
          yield [ board.[idx r c]; board.[idx r (c+1)]; board.[idx r (c+2)]; board.[idx r (c+3)] ]
      for c in 0 .. cols-1 do
        for r in 0 .. rows-4 do
          yield [ board.[idx r c]; board.[idx (r+1) c]; board.[idx (r+2) c]; board.[idx (r+3) c] ]
      for r in 0 .. rows-4 do
        for c in 0 .. cols-4 do
          yield [ board.[idx r c]; board.[idx (r+1) (c+1)]; board.[idx (r+2) (c+2)]; board.[idx (r+3) (c+3)] ]
      for r in 3 .. rows-1 do
        for c in 0 .. cols-4 do
          yield [ board.[idx r c]; board.[idx (r-1) (c+1)]; board.[idx (r-2) (c+2)]; board.[idx (r-3) (c+3)] ]
    ]

let evaluateBoard (board: Board) (player: Cell) : int =
    let opp = opponent player
    let windows = allWindows board
    let netWindowScore =
        windows |> List.sumBy (fun w ->
            scoreWindow w player - scoreWindow w opp)
    let centerBonus =
        [ 0 .. rows-1 ]
        |> List.filter (fun r -> board.[idx r 3] = player)
        |> List.length
        |> (*) 3
    netWindowScore + centerBonus

let rec minimaxAB
    (board: Board) (player: Cell) (depth: int)
    (alpha: int) (beta: int) (pruneCount: int ref) : int =
    match isGameOver board with
    | Some result ->
        match result with
        | RedWins    -> if player = Red    then 10000 + depth else -(10000 + depth)
        | YellowWins -> if player = Yellow then 10000 + depth else -(10000 + depth)
        | Draw       -> 0
    | None when depth = 0 ->
        evaluateBoard board player
    | None ->
        let opp = opponent player
        let moves = legalMoves board |> List.sortBy (fun c -> abs (c - 3))
        let rec loop moves alpha bestScore =
            match moves with
            | [] -> bestScore
            | col :: rest ->
                let nextBoard = applyMove board player col
                let childScore = -(minimaxAB nextBoard opp (depth - 1) (-beta) (-alpha) pruneCount)
                let newBest = max bestScore childScore
                let newAlpha = max alpha newBest
                if newAlpha >= beta then
                    pruneCount.Value <- pruneCount.Value + 1
                    newBest
                else
                    loop rest newAlpha newBest
        loop moves alpha NegInf

let chooseMoveAB (board: Board) (player: Cell) (depth: int) : int * int =
    let pruneCount = ref 0
    let opp = opponent player
    let moves = legalMoves board |> List.sortBy (fun c -> abs (c - 3))
    let scored =
        moves |> List.map (fun col ->
            let nextBoard = applyMove board player col
            let score = -(minimaxAB nextBoard opp (depth - 1) NegInf PosInf pruneCount)
            col, score)
    let bestCol = scored |> List.maxBy snd |> fst
    bestCol, pruneCount.Value

let rec private naiveMinimax (board: Board) (player: Cell) (depth: int) : int =
    match isGameOver board with
    | Some result ->
        match result with
        | RedWins    -> if player = Red    then 10000 + depth else -(10000 + depth)
        | YellowWins -> if player = Yellow then 10000 + depth else -(10000 + depth)
        | Draw       -> 0
    | None when depth = 0 ->
        evaluateBoard board player
    | None ->
        let opp = opponent player
        let moves = legalMoves board |> List.sortBy (fun c -> abs (c - 3))
        moves |> List.map (fun col ->
            let nextBoard = applyMove board player col
            -(naiveMinimax nextBoard opp (depth - 1)))
        |> List.max

let chooseMoveNaive (board: Board) (player: Cell) (depth: int) : int =
    let opp = opponent player
    let moves = legalMoves board |> List.sortBy (fun c -> abs (c - 3))
    moves |> List.map (fun col ->
        let nextBoard = applyMove board player col
        col, -(naiveMinimax nextBoard opp (depth - 1)))
    |> List.maxBy snd |> fst
