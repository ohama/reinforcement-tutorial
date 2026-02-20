module ConnectFour.Tests.MinimaxTests

open Expecto
open ConnectFour.Domain
open ConnectFour.Rules
open ConnectFour.Minimax

[<Tests>]
let minimaxTests =
    testList "Minimax Alpha-Beta tests" [

        testCase "chooseMoveAB and chooseMoveNaive both choose center column on empty board (depth 4)" <| fun () ->
            let board = emptyBoard ()
            let abCol, _ = chooseMoveAB board Red 4
            let naiveCol  = chooseMoveNaive board Red 4
            Expect.equal abCol 3 "Alpha-Beta should choose center column (3) on empty board"
            Expect.equal naiveCol 3 "Naive Minimax should choose center column (3) on empty board"

        testCase "chooseMoveAB agrees with chooseMoveNaive on non-trivial position (depth 3)" <| fun () ->
            // Build a non-trivial board: a few moves played
            let board =
                emptyBoard ()
                |> fun b -> applyMove b Red 2
                |> fun b -> applyMove b Yellow 4
                |> fun b -> applyMove b Red 3
                |> fun b -> applyMove b Yellow 3
                |> fun b -> applyMove b Red 2
                |> fun b -> applyMove b Yellow 5
            let abCol, pruneCount = chooseMoveAB board Red 3
            let naiveCol          = chooseMoveNaive board Red 3
            Expect.equal abCol naiveCol "Alpha-Beta must agree with naive Minimax on best move"
            Expect.isTrue (pruneCount > 0) $"Alpha-Beta should prune at least one branch (got {pruneCount})"

        testCase "Minimax immediately selects a winning move when 3-in-a-row exists (depth 1)" <| fun () ->
            // Red has 3 in a row in cols 0,1,2 at row 5; col 3 completes the win
            let board =
                emptyBoard ()
                |> fun b -> applyMove b Red 0
                |> fun b -> applyMove b Red 1
                |> fun b -> applyMove b Red 2
            let abCol, _ = chooseMoveAB board Red 1
            Expect.equal abCol 3 "Minimax should complete 4-in-a-row by choosing col 3"

        testCase "Minimax blocks opponent's winning move (depth 2)" <| fun () ->
            // Yellow has 3 in a row in cols 0,1,2 at row 5.
            // The left extension (col -1) does not exist, so col 3 is the ONLY blocking move.
            // Red must play col 3 to block, otherwise Yellow wins immediately.
            let board =
                emptyBoard ()
                |> fun b -> applyMove b Yellow 0
                |> fun b -> applyMove b Yellow 1
                |> fun b -> applyMove b Yellow 2
            let abCol, _ = chooseMoveAB board Red 2
            Expect.equal abCol 3 $"Red should block Yellow's win at col 3; chose col {abCol}"

        testCase "chooseMoveAB returns a column that is in legalMoves" <| fun () ->
            let board =
                emptyBoard ()
                |> fun b -> applyMove b Red 3
                |> fun b -> applyMove b Yellow 2
                |> fun b -> applyMove b Red 4
            let abCol, _ = chooseMoveAB board Yellow 3
            let legal = legalMoves board
            Expect.contains legal abCol $"chooseMoveAB returned col {abCol} which is not in legalMoves {legal}"

        testCase "pruneCount > 0 on non-trivial board at depth 4" <| fun () ->
            let board =
                emptyBoard ()
                |> fun b -> applyMove b Red 3
                |> fun b -> applyMove b Yellow 3
                |> fun b -> applyMove b Red 2
                |> fun b -> applyMove b Yellow 4
            let _, pruneCount = chooseMoveAB board Red 4
            Expect.isTrue (pruneCount > 0) $"Alpha-Beta must prune at least one branch at depth 4 (got {pruneCount})"

    ]
