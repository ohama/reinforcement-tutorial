module Bandit.Training

open Bandit.Domain
open Bandit.Environment
open Bandit.Agent

/// Run one episode of N steps, returns final AgentState — pure
let runEpisode
    (rng: System.Random)
    (env: BanditEnv)
    (selectArm: System.Random -> AgentState -> int)
    (steps: int) : AgentState =

    let initial = { Counts = Array.zeroCreate env.RewardProbs.Length
                    Values = Array.zeroCreate env.RewardProbs.Length }
    (initial, List.init steps id)
    ||> List.fold (fun state _ ->
        let arm = selectArm rng state
        let reward = pullArm rng env arm
        incrementalMean state arm reward)
