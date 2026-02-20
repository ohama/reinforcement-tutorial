module Gomoku.Domain

[<Literal>]
let BoardSize = 15
[<Literal>]
let WinLength = 5

// 0 = Empty, 1 = Black, -1 = White
// int array — NOT discriminated union. Reason: trivial float32 tensor conversion (float32 cell).
type Board = int array  // length = BoardSize * BoardSize = 225, row-major

type Player = Black | White

let playerValue = function Black -> 1 | White -> -1
let opponent = function Black -> White | White -> Black

type GameState = {
    Board: Board
    CurrentPlayer: Player
    LastMove: int option   // flat index 0..224, None before first move
    MoveCount: int
}

let emptyBoard () : Board = Array.zeroCreate (BoardSize * BoardSize)
// NOTE: Array.zeroCreate is safe here — int array, not DU array (DU would give nulls)

let initialState () = {
    Board = emptyBoard ()
    CurrentPlayer = Black
    LastMove = None
    MoveCount = 0
}
