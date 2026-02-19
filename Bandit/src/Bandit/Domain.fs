module Bandit.Domain

/// Index of an arm (0-based)
type Arm = int

/// Immutable agent state: visit counts and estimated Q-values per arm
type AgentState = {
    Counts: int array
    Values: float array
}

/// Bandit environment: N arms with fixed reward probabilities
type BanditEnv = {
    RewardProbs: float array
}

/// Validation helpers using Result (XCUT-01: no exceptions)
let validateEpsilon (epsilon: float) : Result<float, string> =
    if epsilon >= 0.0 && epsilon <= 1.0 then Ok epsilon
    else Error $"epsilon must be in [0,1], got {epsilon}"

let validateEnv (env: BanditEnv) : Result<BanditEnv, string> =
    if env.RewardProbs.Length = 0 then Error "BanditEnv must have at least one arm"
    elif Array.exists (fun p -> p < 0.0 || p > 1.0) env.RewardProbs then
        Error "All reward probabilities must be in [0,1]"
    else Ok env
