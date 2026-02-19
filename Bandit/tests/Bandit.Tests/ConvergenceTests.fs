module Bandit.Tests.ConvergenceTests

open Expecto
open Bandit.Domain
open Bandit.Training
open Bandit.Agent

/// Environment where arm 2 has clearly highest reward probability
let convergenceEnv = { RewardProbs = [| 0.20; 0.35; 0.90 |] }

[<Tests>]
let convergenceTests =
    testList "Expecto Convergence Tests" [

        // BAND-08: UCB1 must identify the best arm (index 2) after 1000 steps
        testCase "UCB1 converges to best arm (highest prob) after 1000 steps" <| fun () ->
            let rng = System.Random(42)
            let state = runEpisodeUcb1 rng convergenceEnv 1000
            let bestArm = state.Values |> Array.indexed |> Array.maxBy snd |> fst
            Expect.equal bestArm 2 "UCB1 should identify arm 2 (p=0.90) as best after 1000 steps"

        // BAND-08: ε-greedy with ε=0.1 should also converge after 1000 steps
        testCase "ε-greedy (ε=0.1) converges to best arm after 1000 steps" <| fun () ->
            let rng = System.Random(42)
            let state = runEpisode rng convergenceEnv (epsilonGreedy rng 0.1) 1000
            let bestArm = state.Values |> Array.indexed |> Array.maxBy snd |> fst
            Expect.equal bestArm 2 "ε-greedy should identify arm 2 (p=0.90) as best after 1000 steps"

        // UCB1 should visit every arm at least once (initialization guarantee)
        testCase "UCB1 visits all arms at least once after 1000 steps" <| fun () ->
            let rng = System.Random(42)
            let state = runEpisodeUcb1 rng convergenceEnv 1000
            let allVisited = Array.forall (fun c -> c > 0) state.Counts
            Expect.isTrue allVisited "UCB1 must visit all arms at least once (initialization phase)"

        // High-epsilon agent explores more — verify it still visits all arms
        testCase "ε-greedy (ε=0.3) visits all arms at least once after 100 steps" <| fun () ->
            let rng = System.Random(42)
            let state = runEpisode rng convergenceEnv (epsilonGreedy rng 0.3) 100
            let allVisited = Array.forall (fun c -> c > 0) state.Counts
            Expect.isTrue allVisited "High-epsilon agent should explore all arms within 100 steps"
    ]
