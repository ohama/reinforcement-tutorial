module TicTacToe.Domain

type Cell = Empty | X | O

/// Flat 9-element board (0=top-left, 8=bottom-right, row-major)
/// 0|1|2
/// 3|4|5
/// 6|7|8
type Board = Cell array

/// Immutable game state (TICT-02)
type GameState = {
    Board: Board
    CurrentPlayer: Cell  // X or O; Empty is never valid
}

/// Value table: board state -> X's estimated win probability [0.0, 1.0]
type ValueTable = Map<Board, float>

/// Create a board initialized with 9 empty cells
/// CRITICAL: Do not use Array.zeroCreate — returns null for DU types
/// Must use Array.create 9 Empty
let emptyBoard () : Board = Array.create 9 Empty

let initialState () : GameState =
    { Board = emptyBoard (); CurrentPlayer = X }

let otherPlayer (p: Cell) : Cell =
    match p with
    | X -> O
    | O -> X
    | Empty -> failwith "otherPlayer: Empty is not a valid player"
