module Bandit.Agent

open Bandit.Domain

/// Update Q-value estimate using incremental mean (BAND-04)
let incrementalMean (state: AgentState) (arm: Arm) (reward: float) : AgentState =
    let n = state.Counts.[arm] + 1
    let newVal = state.Values.[arm] + (1.0 / float n) * (reward - state.Values.[arm])
    { Counts = state.Counts |> Array.mapi (fun i c -> if i = arm then n else c)
      Values = state.Values |> Array.mapi (fun i v -> if i = arm then newVal else v) }

/// ε-greedy arm selection (BAND-02)
let epsilonGreedy (rng: System.Random) (epsilon: float) (state: AgentState) : Arm =
    if rng.NextDouble() < epsilon then
        rng.Next(state.Values.Length)
    else
        state.Values
        |> Array.indexed
        |> Array.maxBy snd
        |> fst

/// UCB1 arm selection (BAND-03)
let ucb1 (totalSteps: int) (state: AgentState) : Arm =
    match Array.tryFindIndex (fun c -> c = 0) state.Counts with
    | Some arm -> arm
    | None ->
        let t = float totalSteps
        state.Values
        |> Array.mapi (fun i q -> q + sqrt (2.0 * log t / float state.Counts.[i]))
        |> Array.indexed
        |> Array.maxBy snd
        |> fst
