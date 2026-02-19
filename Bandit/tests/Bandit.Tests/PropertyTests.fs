module Bandit.Tests.PropertyTests

open Expecto
open Expecto.ExpectoFsCheck
open Bandit.Domain
open Bandit.Training

/// Standard 3-arm environment for property tests
let testEnv = { RewardProbs = [| 0.3; 0.5; 0.9 |] }

[<Tests>]
let propertyTests =
    testList "FsCheck Property Tests" [

        // BAND-07: Core invariant — total visits must equal steps taken
        testProperty "Counts sum equals total steps" <| fun () ->
            let rng = System.Random(42)
            let steps = 500
            let state = runEpisode rng testEnv (Bandit.Agent.epsilonGreedy rng 0.1) steps
            Array.sum state.Counts = steps

        // BAND-07: Value estimates must be in [0, 1] range for binary reward arms
        testProperty "All value estimates are in [0, 1] for binary reward environment" <| fun () ->
            let rng = System.Random(99)
            let steps = 300
            let state = runEpisode rng testEnv (Bandit.Agent.epsilonGreedy rng 0.1) steps
            Array.forall (fun v -> v >= 0.0 && v <= 1.0) state.Values

        // BAND-07: Counts must be non-negative
        testProperty "All arm visit counts are non-negative" <| fun () ->
            let rng = System.Random(7)
            let steps = 200
            let state = runEpisode rng testEnv (Bandit.Agent.epsilonGreedy rng 0.3) steps
            Array.forall (fun c -> c >= 0) state.Counts

        // UCB1 variant: same invariant holds for UCB1 episode
        testProperty "UCB1: Counts sum equals total steps" <| fun () ->
            let rng = System.Random(13)
            let steps = 300
            let state = runEpisodeUcb1 rng testEnv steps
            Array.sum state.Counts = steps
    ]
