module Bandit.Training

open Bandit.Domain
open Bandit.Environment
open Bandit.Agent

/// Run one episode of N steps, return final AgentState — pure, no I/O (XCUT-03)
/// selectArm: AgentState -> Arm (rng already captured in the closure by caller)
let runEpisode
    (rng: System.Random)
    (env: BanditEnv)
    (selectArm: AgentState -> Arm)
    (steps: int) : AgentState =

    let initial = { Counts = Array.zeroCreate env.RewardProbs.Length
                    Values = Array.zeroCreate env.RewardProbs.Length }
    (initial, List.init steps id)
    ||> List.fold (fun state _ ->
        let arm = selectArm state
        let reward = pullArm rng env arm
        incrementalMean state arm reward)

/// UCB1 adapter: uses a mutable counter per episode to track total pulls
let runEpisodeUcb1
    (rng: System.Random)
    (env: BanditEnv)
    (steps: int) : AgentState =

    let initial = { Counts = Array.zeroCreate env.RewardProbs.Length
                    Values = Array.zeroCreate env.RewardProbs.Length }
    let mutable totalPulls = 0
    (initial, List.init steps id)
    ||> List.fold (fun state _ ->
        totalPulls <- totalPulls + 1
        let arm = ucb1 totalPulls state
        let reward = pullArm rng env arm
        incrementalMean state arm reward)

/// Compare multiple epsilon values — returns list of (epsilon, finalState) (BAND-05)
let compareEpsilons
    (rng: System.Random)
    (env: BanditEnv)
    (steps: int)
    (epsilons: float list) : (float * AgentState) list =
    epsilons
    |> List.map (fun eps ->
        let rng2 = System.Random(rng.Next())
        let state = runEpisode rng2 env (epsilonGreedy rng2 eps) steps
        eps, state)

/// Compare ε-greedy (best epsilon) vs UCB1 — returns (epsilonState, ucb1State) (BAND-06)
let compareStrategies
    (rng: System.Random)
    (env: BanditEnv)
    (steps: int)
    (epsilon: float) : AgentState * AgentState =
    let rng1 = System.Random(rng.Next())
    let rng2 = System.Random(rng.Next())
    let epsilonState = runEpisode rng1 env (epsilonGreedy rng1 epsilon) steps
    let ucb1State = runEpisodeUcb1 rng2 env steps
    epsilonState, ucb1State

/// Sum of all per-step rewards approximated from final state
let totalReward (state: AgentState) : float =
    Array.map2 (fun c v -> float c * v) state.Counts state.Values |> Array.sum
