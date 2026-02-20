module ConnectFourDQN.ReplayBuffer

// An experience stores state/nextState as float32[] arrays (flattened [3,6,7] = 126 elements).
// CRITICAL: Do NOT store tensors in Experience or ReplayBuffer.
// Tensors held between training steps (outside a dispose scope) cause memory leaks.
// float32[] arrays are plain .NET heap objects — safe to hold across steps.
type Experience = {
    StateData:     float32[]  // flattened [3*6*7 = 126] board encoding
    Action:        int        // column chosen (0..6)
    Reward:        float32    // +1.0 win, -1.0 loss, +0.3 draw, 0.0 step
    NextStateData: float32[]  // flattened [3*6*7] board encoding after move
    Done:          bool       // true if episode ended (win, loss, or draw)
}

// Fixed-capacity circular replay buffer.
// Push overwrites oldest experience when full.
// Sample returns a random batch of size batchSize (requires size >= batchSize).
type ReplayBuffer(capacity: int) =
    let buffer = Array.zeroCreate<Experience> capacity
    let mutable pos  = 0
    let mutable size = 0

    member _.Capacity = capacity
    member _.Size     = size

    member _.Push(e: Experience) =
        buffer.[pos] <- e
        pos  <- (pos + 1) % capacity
        size <- min (size + 1) capacity

    member _.Sample(batchSize: int) (rng: System.Random) : Experience[] =
        if size < batchSize then
            failwithf "ReplayBuffer.Sample: need %d experiences, have %d" batchSize size
        Array.init batchSize (fun _ -> buffer.[rng.Next(size)])
