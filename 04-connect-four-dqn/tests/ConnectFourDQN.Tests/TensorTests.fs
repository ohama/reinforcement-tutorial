module ConnectFourDQN.Tests.TensorTests

open Expecto
open FsCheck
open ConnectFourDQN.Domain
open ConnectFourDQN.Rules
open ConnectFourDQN.DQNAgent
open TorchSharp

/// Generate a valid (possibly partial) Connect Four board by playing random plies.
let genValidBoard : Gen<Board> =
    gen {
        let rng = System.Random(42)
        let plies = rng.Next(0, 31)
        let mutable board = emptyBoard ()
        let mutable player = Red
        let mutable moves = plies
        while moves > 0 do
            let legal = legalMoves board
            if legal.IsEmpty || isGameOver board |> Option.isSome then
                moves <- 0
            else
                let col = legal.[rng.Next(legal.Length)]
                board <- applyMove board player col
                player <- opponent player
                moves <- moves - 1
        return board
    }

/// Arbitrary for valid boards
let arbBoard = Arb.fromGen genValidBoard

/// Property: sum of all values in a [3,6,7] tensor equals 42.0f (each cell contributes 1.0f to exactly one channel).
[<Tests>]
let tensorSumInvariant =
    testProperty "boardToTensor sum invariant: each cell maps to exactly one channel" <| fun () ->
        Prop.forAll arbBoard (fun board ->
            use _scope = torch.NewDisposeScope()
            let t = boardToTensor board Red Yellow
            let total = t.sum().item<float32>()
            total = 42.0f
        )

/// Property: tensor shape is always [3, 6, 7].
[<Tests>]
let tensorShapeTest =
    testProperty "boardToTensor shape is [3,6,7]" <| fun () ->
        Prop.forAll arbBoard (fun board ->
            use _scope = torch.NewDisposeScope()
            let t = boardToTensor board Red Yellow
            t.shape = [| 3L; 6L; 7L |]
        )

/// Unit test: empty board channel 2 (empty channel) is all ones.
[<Tests>]
let emptyBoardAllEmpty =
    test "boardToTensor on empty board: channel 2 is all 1.0f" {
        use _scope = torch.NewDisposeScope()
        let board = emptyBoard ()
        let t = boardToTensor board Red Yellow
        // channel 2 should be all 1.0f (all empty)
        let ch2 = t.[2L].flatten(0, -1)
        let ch2Sum = ch2.sum().item<float32>()
        Expect.equal ch2Sum 42.0f "Channel 2 sum should be 42.0f for empty board"
        // channels 0 and 1 should be all 0.0f
        let ch0Sum = t.[0L].sum().item<float32>()
        let ch1Sum = t.[1L].sum().item<float32>()
        Expect.equal ch0Sum 0.0f "Channel 0 sum should be 0.0f for empty board"
        Expect.equal ch1Sum 0.0f "Channel 1 sum should be 0.0f for empty board"
    }
