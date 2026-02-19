module Bandit.Environment

open Bandit.Domain

/// Pull an arm and return reward (1.0 or 0.0) — pure, rng passed as parameter
let pullArm (rng: System.Random) (env: BanditEnv) (arm: Arm) : float =
    if rng.NextDouble() < env.RewardProbs.[arm] then 1.0 else 0.0
