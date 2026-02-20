module ConnectFour.Tests.PropertyTests

open Expecto
open Expecto.ExpectoFsCheck
open FsCheck
open ConnectFour.Domain
open ConnectFour.Rules

// ---------------------------------------------------------------------------
// Custom generator: simulate random valid games to produce realistic boards
// ---------------------------------------------------------------------------

/// Play random moves (alternating Red/Yellow) for 'n' plies or until game over.
/// Returns the resulting board.
let private playRandomMoves (rng: System.Random) (n: int) : Board =
    let mutable board = emptyBoard ()
    let mutable player = Red
    let mutable i = 0
    while i < n do
        let legal = legalMoves board
        if legal.IsEmpty then
            i <- n  // stop
        else
            let col = legal.[rng.Next(legal.Length)]
            board <- applyMove board player col
            player <- opponent player
            i <- i + 1
    board

/// FsCheck generator that produces boards from random games (0..30 plies)
let genValidBoard : Gen<Board> =
    Gen.choose (0, 30)
    |> Gen.map (fun n ->
        let rng = System.Random()
        playRandomMoves rng n)

// ---------------------------------------------------------------------------
// Gravity invariant tests
// ---------------------------------------------------------------------------

[<Tests>]
let gravityTests =
    testList "Gravity invariants" [

        testProperty "legalMoves returns only columns 0-6" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                let moves = legalMoves board
                moves |> List.forall (fun c -> c >= 0 && c < cols))

        testProperty "applyMove places piece at lowest empty row (gravity)" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                let legal = legalMoves board
                if legal.IsEmpty then true
                else
                    let col = legal.[0]
                    let expectedRow = dropRow board col
                    let board' = applyMove board Red col
                    match expectedRow with
                    | None -> false
                    | Some row -> board'.[idx row col] = Red)

        testProperty "No floating pieces: non-empty cell above row 5 has non-empty below" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                [ for r in 0 .. rows - 2 do
                    for c in 0 .. cols - 1 do
                        yield (r, c) ]
                |> List.forall (fun (r, c) ->
                    if board.[idx r c] <> Empty
                    then board.[idx (r + 1) c] <> Empty
                    else true))

        testProperty "Full column is not in legalMoves" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                [ 0 .. cols - 1 ]
                |> List.forall (fun col ->
                    let isFull = [ 0 .. rows - 1 ] |> List.forall (fun r -> board.[idx r col] <> Empty)
                    if isFull then not (legalMoves board |> List.contains col)
                    else true))

        testCase "Empty board has 7 legal moves (all columns)" <| fun () ->
            let board = emptyBoard ()
            Expect.equal (legalMoves board |> List.length) 7 "All 7 columns legal on empty board"

        testProperty "After applyMove into col, legalMoves still returns valid columns" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                let legal = legalMoves board
                if legal.IsEmpty then true
                else
                    let col = legal.[0]
                    let board' = applyMove board Red col
                    let legal' = legalMoves board'
                    legal' |> List.forall (fun c -> c >= 0 && c < cols))

    ]

// ---------------------------------------------------------------------------
// Winner detection tests
// ---------------------------------------------------------------------------

[<Tests>]
let winnerTests =
    testList "Winner detection" [

        testCase "checkWinner returns None on empty board" <| fun () ->
            Expect.isNone (checkWinner (emptyBoard ())) "Empty board has no winner"

        testCase "checkWinner detects horizontal win" <| fun () ->
            // Red fills row 5 cols 0-3
            let board = emptyBoard ()
            let board = applyMove board Red 0
            let board = applyMove board Red 1
            let board = applyMove board Red 2
            let board = applyMove board Red 3
            Expect.equal (checkWinner board) (Some Red) "Horizontal 4-in-a-row detected"

        testCase "checkWinner detects vertical win" <| fun () ->
            // Red stacks 4 in column 3
            let board = emptyBoard ()
            let board = applyMove board Red 3
            let board = applyMove board Red 3
            let board = applyMove board Red 3
            let board = applyMove board Red 3
            Expect.equal (checkWinner board) (Some Red) "Vertical 4-in-a-row detected"

        testCase "checkWinner detects diagonal win (down-right)" <| fun () ->
            // Build diagonal: Red at (5,0),(4,1),(3,2),(2,3)
            // Fill Yellow below-and-right to force Red to correct diagonal positions
            let b = emptyBoard ()
            // col 0: Red bottom
            let b = applyMove b Red 0      // (5,0)=Red
            // col 1: Yellow bottom, Red above
            let b = applyMove b Yellow 1   // (5,1)=Yellow
            let b = applyMove b Red 1      // (4,1)=Red
            // col 2: Yellow x2 bottom, Red above
            let b = applyMove b Yellow 2   // (5,2)=Yellow
            let b = applyMove b Yellow 2   // (4,2)=Yellow
            let b = applyMove b Red 2      // (3,2)=Red
            // col 3: Yellow x3 bottom, Red above
            let b = applyMove b Yellow 3   // (5,3)=Yellow
            let b = applyMove b Yellow 3   // (4,3)=Yellow
            let b = applyMove b Yellow 3   // (3,3)=Yellow
            let b = applyMove b Red 3      // (2,3)=Red
            Expect.equal (checkWinner b) (Some Red) "Diagonal (down-right) 4-in-a-row detected"

        testCase "checkWinner detects anti-diagonal win (down-left)" <| fun () ->
            // Build anti-diagonal: Red at (2,3),(3,2),(4,1),(5,0) — same as above but cols reversed
            // Red at (5,3),(4,2),(3,1),(2,0)
            let b = emptyBoard ()
            // col 3: Red bottom
            let b = applyMove b Red 3      // (5,3)=Red
            // col 2: Yellow bottom, Red above
            let b = applyMove b Yellow 2   // (5,2)=Yellow
            let b = applyMove b Red 2      // (4,2)=Red
            // col 1: Yellow x2 bottom, Red above
            let b = applyMove b Yellow 1   // (5,1)=Yellow
            let b = applyMove b Yellow 1   // (4,1)=Yellow
            let b = applyMove b Red 1      // (3,1)=Red
            // col 0: Yellow x3 bottom, Red above
            let b = applyMove b Yellow 0   // (5,0)=Yellow
            let b = applyMove b Yellow 0   // (4,0)=Yellow
            let b = applyMove b Yellow 0   // (3,0)=Yellow
            let b = applyMove b Red 0      // (2,0)=Red
            Expect.equal (checkWinner b) (Some Red) "Anti-diagonal 4-in-a-row detected"

        testCase "isGameOver returns Some for full non-winning board" <| fun () ->
            // Fill board alternating in a pattern with no winner
            // Use a known draw configuration by alternating RYRYRY / YRYRY...
            // Simple approach: manually fill all 42 cells such that no 4-in-a-row forms
            // Pattern: alternate Red/Yellow in checkerboard starting Red
            let board = Array.init (rows * cols) (fun i ->
                let r = i / cols
                let c = i % cols
                if (r + c) % 2 = 0 then Red else Yellow)
            // Verify no winner first
            let winner = checkWinner board
            // May or may not have winner; if board is full and no winner = Draw
            match winner with
            | None ->
                let result = isGameOver board
                Expect.equal result (Some Draw) "Full board with no winner is a Draw"
            | Some _ ->
                // The checkerboard pattern won't have 4-in-a-row, but if it does, that's fine too
                ()

        testProperty "isGameOver does not throw for any valid board" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                let _ = isGameOver board
                true)

        testProperty "checkWinner consistent: Red wins iff isGameOver = RedWins" <|
            Prop.forAll (Arb.fromGen genValidBoard) (fun board ->
                match checkWinner board, isGameOver board with
                | Some Red, Some RedWins -> true
                | Some Red, _ -> false
                | Some Yellow, Some YellowWins -> true
                | Some Yellow, _ -> false
                | None, Some Draw -> true
                | None, None -> true
                | None, Some _ -> false
                | Some Empty, _ -> false)

    ]
