module ConnectFourDQN.DQNAgent

open TorchSharp
open type TorchSharp.torch
open ConnectFourDQN.Domain

let boardToTensor (board: Board) (myPiece: Cell) (oppPiece: Cell) : Tensor =
    let flat = Array.init (3 * 6 * 7) (fun i ->
        let ch   = i / (6 * 7)
        let rem  = i % (6 * 7)
        let cell = board.[rem / 7 * 7 + rem % 7]
        match ch, cell with
        | 0, c when c = myPiece  -> 1.0f
        | 1, c when c = oppPiece -> 1.0f
        | 2, Empty               -> 1.0f
        | _                      -> 0.0f)
    torch.tensor(flat, dtype=ScalarType.Float32).reshape([| 3L; 6L; 7L |])
