module Bandit.Console.Program

open Serilog
open Bandit.Domain
open Bandit.Training

/// Standard 10-arm bandit environment (Sutton & Barto Chapter 2 benchmark)
let defaultEnv = {
    RewardProbs = [| 0.10; 0.15; 0.20; 0.25; 0.30; 0.40; 0.50; 0.60; 0.70; 0.90 |]
}

let printSeparator () = Log.Information("{Sep}", String.replicate 50 "-")

let printEpsilonResults (results: (float * AgentState) list) =
    Log.Information("=== ε-greedy 비교 (1000 steps, 10-arm bandit) ===")
    for (eps, state) in results do
        let bestArm = state.Values |> Array.indexed |> Array.maxBy snd |> fst
        let totalR = totalReward state
        Log.Information(
            "  ε={Epsilon:F2}  최적 arm={BestArm}  추정 가치={BestValue:F3}  총 보상≈{TotalReward:F1}",
            eps, bestArm, state.Values.[bestArm], totalR)

let printStrategyComparison (epsilonState: AgentState) (ucb1State: AgentState) (epsilon: float) =
    Log.Information("=== ε-greedy (ε={Epsilon:F2}) vs UCB1 ===", epsilon)
    let epsilonReward = totalReward epsilonState
    let ucb1Reward    = totalReward ucb1State
    Log.Information("  ε-greedy 총 보상≈{R:F1}", epsilonReward)
    Log.Information("  UCB1     총 보상≈{R:F1}", ucb1Reward)
    if ucb1Reward > epsilonReward then
        Log.Information("  승자: UCB1 (+{Diff:F1})", ucb1Reward - epsilonReward)
    elif epsilonReward > ucb1Reward then
        Log.Information("  승자: ε-greedy (+{Diff:F1})", epsilonReward - ucb1Reward)
    else
        Log.Information("  결과: 동점")

[<EntryPoint>]
let main _args =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File(
                "logs/bandit-.log",
                rollingInterval = RollingInterval.Day,
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger()

    let rng = System.Random(42)
    let steps = 1000

    let epsilonResults = compareEpsilons rng defaultEnv steps [0.01; 0.1; 0.3]
    printEpsilonResults epsilonResults

    printSeparator ()

    let epsilonState, ucb1State = compareStrategies rng defaultEnv steps 0.1
    printStrategyComparison epsilonState ucb1State 0.1

    Log.CloseAndFlush()
    0
