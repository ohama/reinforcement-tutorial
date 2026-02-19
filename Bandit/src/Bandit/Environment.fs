module Bandit.Environment

open Bandit.Domain

/// Pull an arm and return reward (1.0 = win, 0.0 = loss)
/// rng passed as parameter — never constructed internally (pure function boundary)
let pullArm (rng: System.Random) (env: BanditEnv) (arm: Arm) : float =
    if rng.NextDouble() < env.RewardProbs.[arm] then 1.0 else 0.0
