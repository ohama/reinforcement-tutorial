module Bandit.Domain

/// Index of an arm (0-based)
type Arm = int

/// Immutable agent state: visit counts and estimated values per arm
type AgentState = {
    Counts: int array
    Values: float array
}

/// Bandit environment: N arms with fixed reward probabilities
type BanditEnv = {
    RewardProbs: float array
}
