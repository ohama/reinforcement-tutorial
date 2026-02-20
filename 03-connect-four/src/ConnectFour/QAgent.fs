module ConnectFour.QAgent

open ConnectFour.Domain
open ConnectFour.Rules

type QTable = System.Collections.Generic.Dictionary<string, float[]>

let encodeState (board: Board) : string =
    board
    |> Array.map (fun c ->
        match c with
        | Red    -> 'R'
        | Yellow -> 'Y'
        | Empty  -> '.')
    |> System.String

let createQTable () : QTable = QTable()

let getQ (table: QTable) (state: string) : float[] =
    match table.TryGetValue(state) with
    | true, values -> values
    | false, _ ->
        let values = Array.create cols 0.0
        table.[state] <- values
        values

let chooseAction (rng: System.Random) (table: QTable) (board: Board) (epsilon: float) : int =
    let legal = legalMoves board
    if legal.IsEmpty then failwith "chooseAction: no legal moves (game should be over)"
    if rng.NextDouble() < epsilon then
        legal.[rng.Next(legal.Length)]
    else
        let state = encodeState board
        let qVals = getQ table state
        legal |> List.maxBy (fun c -> qVals.[c])

let updateQ
    (table: QTable) (state: string) (action: int) (reward: float)
    (nextState: string) (nextLegalMoves: int list)
    (alpha: float) (gamma: float) (isTerminal: bool) : unit =
    let qCurr = getQ table state
    let nextMax =
        if isTerminal || nextLegalMoves.IsEmpty then 0.0
        else
            let qNext = getQ table nextState
            nextLegalMoves |> List.map (fun c -> qNext.[c]) |> List.max
    let target = reward + gamma * nextMax
    qCurr.[action] <- qCurr.[action] + alpha * (target - qCurr.[action])

let [<Literal>] RewardWin  =  1.0
let [<Literal>] RewardLoss = -1.0
let [<Literal>] RewardDraw =  0.3
let [<Literal>] RewardStep =  0.0
