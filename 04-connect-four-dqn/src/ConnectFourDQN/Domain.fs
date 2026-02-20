module ConnectFourDQN.Domain

type Cell = Empty | Red | Yellow

/// 6x7 board as flat 42-element array, row-major: index = row * 7 + col
/// Row 0 = top, Row 5 = bottom
type Board = Cell array

type GameState = {
    Board: Board
    CurrentPlayer: Cell
}

let rows = 6
let cols = 7

let inline idx row col = row * cols + col

let emptyBoard () : Board = Array.create (rows * cols) Empty

let initialState () : GameState =
    { Board = emptyBoard (); CurrentPlayer = Red }

let opponent (player: Cell) : Cell =
    match player with
    | Red    -> Yellow
    | Yellow -> Red
    | Empty  -> failwith "opponent: Empty is not a player"
