module Gomoku.PolicyValueNet

open TorchSharp
open type TorchSharp.torch
open type TorchSharp.torch.nn
open Gomoku.Domain

// IMPORTANT: `open type TorchSharp.torch` shadows F# int and int64.
// Always use Operators.int and Operators.int64 for numeric conversion throughout this file.

/// Convert a GameState to a [4, 15, 15] float32 tensor.
/// Channel 0: current player's stones (1.0 where current player placed)
/// Channel 1: opponent's stones (1.0 where opponent placed)
/// Channel 2: last move indicator (1.0 at LastMove position, else 0.0)
/// Channel 3: turn indicator (1.0 everywhere if Black to move, 0.0 if White to move)
let boardToTensor (state: GameState) : Tensor =
    let size = BoardSize * BoardSize  // 225
    let data = Array.zeroCreate<float32> (4 * size)
    let myVal  = playerValue state.CurrentPlayer   // 1 or -1
    let oppVal = -myVal
    for i in 0 .. size - 1 do
        if state.Board.[i] = myVal  then data.[i]          <- 1.0f  // ch0
        if state.Board.[i] = oppVal then data.[size + i]   <- 1.0f  // ch1
    match state.LastMove with
    | Some m -> data.[2 * size + m] <- 1.0f                          // ch2
    | None   -> ()
    if state.CurrentPlayer = Black then
        for i in 0 .. size - 1 do data.[3 * size + i] <- 1.0f       // ch3 = 1.0 if Black
    // Create tensor and reshape to [4, 15, 15]
    torch.tensor(data, dtype = ScalarType.Float32)
          .reshape([| 4L; Operators.int64 BoardSize; Operators.int64 BoardSize |])

/// Dual-head Policy/Value Network.
/// Architecture: 3-conv shared backbone + BatchNorm + ReLU → policy head (225 log-probs) + value head (scalar tanh).
type PolicyValueNet(name: string) as this =
    inherit Module<Tensor, Tensor>(name)

    // Shared backbone: 3 conv layers with BatchNorm + ReLU
    let conv1 = Conv2d(4L,   32L, 3L, padding = 1L)
    let bn1   = BatchNorm2d(32L)
    let relu1 = ReLU()
    let conv2 = Conv2d(32L,  64L, 3L, padding = 1L)
    let bn2   = BatchNorm2d(64L)
    let relu2 = ReLU()
    let conv3 = Conv2d(64L, 128L, 3L, padding = 1L)
    let bn3   = BatchNorm2d(128L)
    let relu3 = ReLU()

    // Policy head: 128 → 4 channels (1×1 conv) → flatten → linear → 225
    let pConv  = Conv2d(128L, 4L, 1L)
    let pBn    = BatchNorm2d(4L)
    let pRelu  = ReLU()
    let pFc    = Linear(4L * Operators.int64 BoardSize * Operators.int64 BoardSize,
                        Operators.int64 (BoardSize * BoardSize))

    // Value head: 128 → 2 channels (1×1 conv) → flatten → linear 256 → linear 1 → tanh
    let vConv  = Conv2d(128L, 2L, 1L)
    let vBn    = BatchNorm2d(2L)
    let vRelu  = ReLU()
    let vFc1   = Linear(2L * Operators.int64 BoardSize * Operators.int64 BoardSize, 256L)
    let vRelu2 = ReLU()
    let vFc2   = Linear(256L, 1L)
    let vTanh  = Tanh()

    do this.RegisterComponents()

    /// Shared backbone forward pass.
    member private _.backbone(x: Tensor) : Tensor =
        x
        |> conv1.forward |> bn1.forward |> relu1.forward
        |> conv2.forward |> bn2.forward |> relu2.forward
        |> conv3.forward |> bn3.forward |> relu3.forward

    /// Forward (required by Module base) — returns policy log-probs [B, 225].
    override this.forward(x: Tensor) : Tensor =
        let h = this.backbone(x)
        let pRaw =
            h |> pConv.forward |> pBn.forward |> pRelu.forward
            |> fun t -> t.flatten(1L)
        let logits = pFc.forward(pRaw)
        torch.nn.functional.log_softmax(logits, 1L)

    /// Policy log-probabilities [B, 225] — same as forward().
    member this.policy(x: Tensor) : Tensor =
        this.forward(x)

    /// Value prediction [B, 1], range [-1, 1] via tanh.
    member this.value(x: Tensor) : Tensor =
        let h = this.backbone(x)
        let vRaw =
            h |> vConv.forward |> vBn.forward |> vRelu.forward
            |> fun t -> t.flatten(1L)
        let vHidden = vRelu2.forward(vFc1.forward(vRaw))
        vTanh.forward(vFc2.forward(vHidden))

    /// Combined forward: returns (logProbs [B,225], value [B,1]) — convenience for training.
    member this.forwardBoth(x: Tensor) : Tensor * Tensor =
        let h = this.backbone(x)
        // Policy
        let pRaw =
            h |> pConv.forward |> pBn.forward |> pRelu.forward
            |> fun t -> t.flatten(1L)
        let logProbs = torch.nn.functional.log_softmax(pFc.forward(pRaw), 1L)
        // Value
        let vRaw =
            h |> vConv.forward |> vBn.forward |> vRelu.forward
            |> fun t -> t.flatten(1L)
        let vHidden = vRelu2.forward(vFc1.forward(vRaw))
        let vOut = vTanh.forward(vFc2.forward(vHidden))
        (logProbs, vOut)
