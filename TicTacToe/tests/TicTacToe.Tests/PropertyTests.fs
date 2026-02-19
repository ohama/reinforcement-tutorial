module TicTacToe.Tests.PropertyTests

open Expecto
open Expecto.ExpectoFsCheck
open TicTacToe.Domain
open TicTacToe.Rules

[<Tests>]
let propertyTests =
    testList "FsCheck board invariants" [

        // TICT-07: Empty cell count decreases by 1 after a move
        testProperty "Empty cell count decreases by 1 after one legal move" <| fun () ->
            let board = Array.create 9 Empty
            let emptiesBefore = board |> Array.filter ((=) Empty) |> Array.length
            let board' = applyMove board X 4  // center -- always legal on empty board
            let emptiesAfter = board' |> Array.filter ((=) Empty) |> Array.length
            emptiesAfter = emptiesBefore - 1

        // TICT-07: X goes first, so next player is O
        testProperty "Players alternate: O follows X" <| fun () ->
            let state = initialState ()
            let nextPlayer = otherPlayer state.CurrentPlayer
            nextPlayer = O  // starts with X, next is O

        // TICT-07: Legal moves are all within [0, 8]
        testProperty "All legal moves are within [0, 8] range" <| fun () ->
            let board = Array.create 9 Empty
            let moves = legalMoves board
            moves |> List.forall (fun m -> m >= 0 && m <= 8)

        // TICT-07: applyMove only changes the target cell
        testProperty "applyMove only changes target cell, leaves rest unchanged" <| fun () ->
            let board = Array.create 9 Empty
            let board' = applyMove board X 4
            let unchanged = [0;1;2;3;5;6;7;8] |> List.forall (fun i -> board'.[i] = Empty)
            unchanged && board'.[4] = X

        // TICT-07: Initial board has 9 empty cells
        testProperty "Initial board has 9 empty cells" <| fun () ->
            let board = emptyBoard ()
            board |> Array.filter ((=) Empty) |> Array.length = 9

    ]
