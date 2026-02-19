module TicTacToe.Rules

open TicTacToe.Domain

/// 8 winning lines (3 rows + 3 columns + 2 diagonals)
let private winLines = [|
    [|0;1;2|]; [|3;4;5|]; [|6;7;8|]  // rows
    [|0;3;6|]; [|1;4;7|]; [|2;5;8|]  // columns
    [|0;4;8|]; [|2;4;6|]             // diagonals
|]

/// Check winner: Some player if there's a winner, None otherwise
let checkWinner (board: Board) : Cell option =
    winLines
    |> Array.tryPick (fun line ->
        let cells = line |> Array.map (fun i -> board.[i])
        if cells.[0] <> Empty && cells.[0] = cells.[1] && cells.[1] = cells.[2]
        then Some cells.[0]
        else None)

type GameResult = XWins | OWins | Draw

/// Check if game is over: Some GameResult if over, None if still in progress
let isGameOver (board: Board) : GameResult option =
    match checkWinner board with
    | Some X -> Some XWins
    | Some O -> Some OWins
    | Some Empty -> None  // Logically impossible but satisfies pattern match exhaustiveness
    | None ->
        if Array.forall (fun c -> c <> Empty) board
        then Some Draw
        else None

/// List of legal moves (indices of Empty cells)
let legalMoves (board: Board) : int list =
    board
    |> Array.indexed
    |> Array.choose (fun (i, c) -> if c = Empty then Some i else None)
    |> Array.toList

/// Apply move: returns a new board with the given index set to player
/// Legality check is the caller's responsibility
let applyMove (board: Board) (player: Cell) (index: int) : Board =
    board |> Array.mapi (fun i c -> if i = index then player else c)
