module Gomoku.Rules

open Gomoku.Domain

let private directions = [| (0,1); (1,0); (1,1); (1,-1) |]  // H, V, diag, anti-diag

let private inBounds r c = r >= 0 && r < BoardSize && c >= 0 && c < BoardSize

let private countDir (board: Board) (r0: int) (c0: int) (dr: int) (dc: int) (player: int) =
    let mutable count = 0
    let mutable r = r0 + dr
    let mutable c = c0 + dc
    while inBounds r c && board.[r * BoardSize + c] = player do
        count <- count + 1
        r <- r + dr
        c <- c + dc
    count

/// Check if the last move at flat index `move` caused a win for the player who placed there.
/// Only checks 4 directions through the placed stone — O(WinLength) not O(225).
let isWinningMove (board: Board) (move: int) =
    let r0 = move / BoardSize
    let c0 = move % BoardSize
    let player = board.[move]
    directions |> Array.exists (fun (dr, dc) ->
        let forward  = countDir board r0 c0 dr dc player
        let backward = countDir board r0 c0 (-dr) (-dc) player
        forward + backward + 1 >= WinLength)

/// All empty cell indices (legal moves).
let legalMoves (board: Board) =
    [| for i in 0 .. BoardSize * BoardSize - 1 do
        if board.[i] = 0 then yield i |]

/// Apply move: copy board, place stone, flip player.
let applyMove (state: GameState) (move: int) : GameState =
    let newBoard = Array.copy state.Board
    newBoard.[move] <- playerValue state.CurrentPlayer
    { Board = newBoard
      CurrentPlayer = opponent state.CurrentPlayer
      LastMove = Some move
      MoveCount = state.MoveCount + 1 }
