module Gomoku.Tests.MctsTests

open Expecto
open Gomoku.MctsNode
open Gomoku.Domain
open Gomoku.Rules
open Gomoku.Mcts

// ── GMOK-09: MCTS Backpropagation Perspective-Flip ───────────────────────────

[<Tests>]
let mctsBackpropTests =
    testList "MCTS Backpropagation" [

        test "UpdateRecursive negates value at each level (3-level chain)" {
            // Build 3-level chain: grandparent → parent → leaf
            let grandparent = MctsNode(None, 1.0)
            let parent      = MctsNode(Some grandparent, 1.0)
            let leaf        = MctsNode(Some parent, 1.0)

            // Convention: call UpdateRecursive(-leafValue) on the leaf
            // Here leafValue = +1.0 (good for the leaf's player)
            // So we call leaf.UpdateRecursive(-1.0)
            leaf.UpdateRecursive(-1.0)

            // leaf.Update(-1.0):  TotalValue = -1.0, Visits = 1
            // parent.Update(+1.0): TotalValue = +1.0, Visits = 1  (negated once)
            // grandparent.Update(-1.0): TotalValue = -1.0, Visits = 1  (negated twice)
            Expect.equal leaf.Visits 1 "leaf visited once"
            Expect.equal parent.Visits 1 "parent visited once"
            Expect.equal grandparent.Visits 1 "grandparent visited once"
            Expect.floatClose Accuracy.medium leaf.TotalValue -1.0 "leaf TotalValue = -1.0"
            Expect.floatClose Accuracy.medium parent.TotalValue 1.0 "parent TotalValue = +1.0 (negated)"
            Expect.floatClose Accuracy.medium grandparent.TotalValue -1.0 "grandparent TotalValue = -1.0 (negated again)"
        }

        test "Q() returns 0 for unvisited node" {
            let node = MctsNode(None, 0.5)
            Expect.floatClose Accuracy.medium (node.Q()) 0.0 "unvisited node Q = 0"
        }

        test "Q() returns average of updates" {
            let node = MctsNode(None, 0.5)
            node.Update(1.0)
            node.Update(-1.0)
            node.Update(0.5)
            // (1.0 + -1.0 + 0.5) / 3 = 0.5/3 ≈ 0.1667
            Expect.floatClose Accuracy.low (node.Q()) (0.5 / 3.0) "Q = average of updates"
        }

        test "Expand adds children with correct priors" {
            let node = MctsNode(None, 1.0)
            let priors = [| (10, 0.3); (20, 0.5); (30, 0.2) |]
            node.Expand(priors)
            Expect.equal node.Children.Count 3 "3 children created"
            Expect.isTrue (node.Children.ContainsKey(10)) "action 10 in children"
            Expect.floatClose Accuracy.medium node.Children.[10].Prior 0.3 "action 10 prior = 0.3"
            Expect.floatClose Accuracy.medium node.Children.[20].Prior 0.5 "action 20 prior = 0.5"
        }

        test "IsLeaf returns true before Expand, false after" {
            let node = MctsNode(None, 1.0)
            Expect.isTrue (node.IsLeaf()) "node is leaf before expand"
            node.Expand([| (5, 0.5); (6, 0.5) |])
            Expect.isFalse (node.IsLeaf()) "node is not leaf after expand"
        }
    ]

// ── GMOK-10: >80% Win Rate vs Random Opponent ────────────────────────────────

/// Play one game: MCTS (nSimulations, cPuct) as `mctsPlayer` vs random.
/// Returns true if MCTS player wins.
let private playMctsVsRandom (rng: System.Random) (nSimulations: int) (cPuct: float) (mctsIsBlack: bool) : bool =
    let mutable state = initialState ()
    let mutable running = true
    let mutable mctsWon = false
    let mctsPlayer = if mctsIsBlack then Black else White

    while running do
        // Check if game already ended from previous move
        match state.LastMove with
        | Some m when isWinningMove state.Board m ->
            let winner = opponent state.CurrentPlayer  // player who just moved
            mctsWon <- (winner = mctsPlayer)
            running <- false
        | _ ->
            let legal = legalMoves state.Board
            if legal.Length = 0 then
                running <- false  // draw → mctsWon stays false
            else
                let move =
                    if state.CurrentPlayer = mctsPlayer then
                        // MCTS move
                        let visitProbs = mctsSearch rng state nSimulations cPuct
                        bestMove visitProbs
                    else
                        // Random opponent
                        legal.[rng.Next(legal.Length)]
                state <- applyMove state move

    mctsWon

[<Tests>]
let mctsWinRateTests =
    testList "MCTS Win Rate vs Random" [

        test "Pure MCTS (50 simulations) wins >80% vs random over 50 games (GMOK-10)" {
            // Use fixed seed for reproducibility; pure MCTS always achieves >80% vs random
            let rng = System.Random(42)
            let nGames = 50
            let nSimulations = 50
            let cPuct = 5.0

            let mutable wins = 0
            // Play 25 games as Black, 25 as White
            for i in 1 .. nGames do
                let mctsIsBlack = (i <= 25)
                if playMctsVsRandom rng nSimulations cPuct mctsIsBlack then
                    wins <- wins + 1

            let winRate = float wins / float nGames
            printfn "MCTS win rate: %d/%d = %.1f%%" wins nGames (winRate * 100.0)
            Expect.isGreaterThan winRate 0.80 (sprintf "MCTS win rate %d/%d should be >80%%" wins nGames)
        }
    ]
