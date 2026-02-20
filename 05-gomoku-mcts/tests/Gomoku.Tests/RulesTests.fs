module Gomoku.Tests.RulesTests

open Expecto
open FsCheck
open Gomoku.Domain
open Gomoku.Rules

/// Play a random game up to maxMoves plies, stopping on win or draw.
/// Returns the final GameState.
let private playRandomGame (rng: System.Random) (maxMoves: int) : GameState =
    let mutable state = initialState ()
    let mutable running = true
    let mutable moves = 0
    while running && moves < maxMoves do
        let legal = legalMoves state.Board
        if legal.Length = 0 then
            running <- false
        else
            let move = legal.[rng.Next(legal.Length)]
            state <- applyMove state move
            match state.LastMove with
            | Some m when isWinningMove state.Board m -> running <- false
            | _ -> moves <- moves + 1
    state

[<Tests>]
let rulesTests =
    testList "Gomoku Rules Properties" [

        // GMOK-08 invariant 1: legalMoves count + occupied count = 225 (always)
        testProperty "legalMoves + occupied = 225 at any game state" (fun (seed: int) ->
            let rng = System.Random(abs seed)
            let maxMoves = rng.Next(1, 100)
            let state = playRandomGame rng maxMoves
            let occupied = state.Board |> Array.filter (fun c -> c <> 0) |> Array.length
            let legal = legalMoves state.Board
            legal.Length + occupied = BoardSize * BoardSize)

        // GMOK-08 invariant 2: after applyMove, legalMoves decreases by 1
        testProperty "legalMoves decreases by 1 after applyMove" (fun (seed: int) ->
            let rng = System.Random(abs seed)
            let state = playRandomGame rng (rng.Next(0, 50))
            let legal = legalMoves state.Board
            if legal.Length = 0 then true  // terminal — skip
            else
                let move = legal.[rng.Next(legal.Length)]
                // Check game not already won
                match state.LastMove with
                | Some m when isWinningMove state.Board m -> true  // skip — terminal
                | _ ->
                    let next = applyMove state move
                    let nextLegal = legalMoves next.Board
                    nextLegal.Length = legal.Length - 1)

        // GMOK-08 invariant 3: isWinningMove requires exactly WinLength consecutive stones
        // Verify: empty board has no winning move (base case)
        test "isWinningMove returns false on empty board cell" {
            let board = emptyBoard ()
            board.[7 * BoardSize + 7] <- 1  // place one stone at center
            let won = isWinningMove board (7 * BoardSize + 7)
            Expect.isFalse won "single stone is not a winning move"
        }

        // Verify: 5 consecutive horizontal stones IS a win
        test "isWinningMove returns true for 5 consecutive horizontal stones" {
            let board = emptyBoard ()
            for col in 3 .. 7 do
                board.[5 * BoardSize + col] <- 1  // row 5, cols 3-7
            let won = isWinningMove board (5 * BoardSize + 5)  // middle stone
            Expect.isTrue won "5 consecutive horizontal stones should win"
        }

        // Verify: 4 consecutive stones is NOT a win
        test "isWinningMove returns false for 4 consecutive stones" {
            let board = emptyBoard ()
            for col in 3 .. 6 do
                board.[5 * BoardSize + col] <- 1  // row 5, cols 3-6
            let won = isWinningMove board (5 * BoardSize + 4)
            Expect.isFalse won "4 consecutive stones should not win"
        }

        // Verify: 5 consecutive diagonal stones IS a win
        test "isWinningMove returns true for 5 consecutive diagonal stones" {
            let board = emptyBoard ()
            for d in 0 .. 4 do
                board.[(3 + d) * BoardSize + (3 + d)] <- -1  // White diagonal
            let won = isWinningMove board (5 * BoardSize + 5)  // center of diagonal
            Expect.isTrue won "5 consecutive diagonal stones should win"
        }

        // Verify: MoveCount increments correctly
        testProperty "MoveCount increments by 1 per applyMove" (fun (seed: int) ->
            let rng = System.Random(abs seed)
            let state = playRandomGame rng (rng.Next(0, 30))
            let legal = legalMoves state.Board
            if legal.Length = 0 then true
            else
                let move = legal.[0]
                let next = applyMove state move
                next.MoveCount = state.MoveCount + 1)

        // Verify: applyMove flips CurrentPlayer
        testProperty "applyMove alternates CurrentPlayer" (fun (seed: int) ->
            let rng = System.Random(abs seed)
            let state = playRandomGame rng (rng.Next(0, 30))
            let legal = legalMoves state.Board
            if legal.Length = 0 then true
            else
                let move = legal.[0]
                let next = applyMove state move
                next.CurrentPlayer = opponent state.CurrentPlayer)
    ]
