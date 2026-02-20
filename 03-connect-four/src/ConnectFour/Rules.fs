module ConnectFour.Rules

open ConnectFour.Domain

let dropRow (board: Board) (col: int) : int option =
    [ rows - 1 .. -1 .. 0 ]
    |> List.tryFind (fun row -> board.[idx row col] = Empty)

let legalMoves (board: Board) : int list =
    [ 0 .. cols - 1 ]
    |> List.filter (fun col -> dropRow board col |> Option.isSome)

let applyMove (board: Board) (player: Cell) (col: int) : Board =
    match dropRow board col with
    | None -> failwith $"applyMove: column {col} is full"
    | Some row ->
        board |> Array.mapi (fun i c -> if i = idx row col then player else c)

let private checkDirection (board: Board) (r: int) (c: int) (dr: int) (dc: int) (player: Cell) : bool =
    [ 0 .. 3 ]
    |> List.forall (fun k ->
        let nr = r + k * dr
        let nc = c + k * dc
        nr >= 0 && nr < rows && nc >= 0 && nc < cols && board.[idx nr nc] = player)

let checkWinner (board: Board) : Cell option =
    let directions = [ (0,1); (1,0); (1,1); (1,-1) ]
    [ for r in 0 .. rows-1 do
      for c in 0 .. cols-1 do
      for (dr, dc) in directions do
        yield (r, c, dr, dc) ]
    |> List.tryPick (fun (r, c, dr, dc) ->
        let cell = board.[idx r c]
        if cell <> Empty && checkDirection board r c dr dc cell
        then Some cell
        else None)

type GameResult = RedWins | YellowWins | Draw

let isGameOver (board: Board) : GameResult option =
    match checkWinner board with
    | Some Red    -> Some RedWins
    | Some Yellow -> Some YellowWins
    | Some Empty  -> None
    | None ->
        if Array.forall (fun c -> c <> Empty) board then Some Draw
        else None
