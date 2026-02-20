module ConnectFourDQN.DQNModel

open TorchSharp
open type TorchSharp.torch
open type TorchSharp.torch.nn

// DQN for Connect Four (6x7 board)
// Input:  [batch, 3, 6, 7]  — 3-channel board encoding (my/opp/empty)
// Output: [batch, 7]        — Q-value for each of the 7 columns
//
// Architecture (verified shape on ARM64 2026-02-20):
//   Conv2d(3→64, 3×3, pad=1)  → [B,64,6,7]
//   ReLU
//   Conv2d(64→128, 3×3, pad=1) → [B,128,6,7]
//   ReLU
//   Flatten                   → [B,5376]  (128*6*7=5376)
//   Linear(5376→256)
//   ReLU
//   Linear(256→7)
type DQNModel(name: string) as this =
    inherit Module<Tensor, Tensor>(name)

    // Fields must be `let` bindings (NOT properties).
    // RegisterComponents() discovers them via reflection.
    let conv1   = Conv2d(3L,   64L,  3L, padding = 1L)
    let conv2   = Conv2d(64L, 128L,  3L, padding = 1L)
    let relu1   = ReLU()
    let relu2   = ReLU()
    let relu3   = ReLU()
    let flatten = Flatten()
    let fc1     = Linear(128L * 6L * 7L, 256L)  // 5376 → 256
    let fc2     = Linear(256L, 7L)

    // Mandatory: registers conv1, conv2, fc1, fc2 for gradient tracking + serialization
    do this.RegisterComponents()

    override _.forward(x: Tensor) : Tensor =
        x --> conv1 --> relu1 --> conv2 --> relu2 --> flatten --> fc1 --> relu3 --> fc2
