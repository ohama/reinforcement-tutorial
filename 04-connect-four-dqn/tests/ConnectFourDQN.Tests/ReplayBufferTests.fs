module ConnectFourDQN.Tests.ReplayBufferTests

open Expecto
open FsCheck
open ConnectFourDQN.ReplayBuffer

let makeExp (i: int) : Experience =
    { StateData     = Array.create 126 (float32 i)
      Action        = i % 7
      Reward        = 0.0f
      NextStateData = Array.create 126 0.0f
      Done          = false }

[<Tests>]
let bufferCapacity =
    testCase "ReplayBuffer: circular overwrite — size never exceeds capacity" (fun () ->
        let buf = ReplayBuffer(10)
        for i in 0 .. 19 do
            buf.Push(makeExp i)
        Expect.equal buf.Size 10 "Size should be capped at capacity"
        Expect.equal buf.Capacity 10 "Capacity unchanged")

[<Tests>]
let bufferSampleSize =
    testCase "ReplayBuffer: Sample returns exactly batchSize experiences" (fun () ->
        let rng = System.Random(0)
        let buf = ReplayBuffer(100)
        for i in 0 .. 99 do
            buf.Push(makeExp i)
        let batch = buf.Sample 32 rng
        Expect.equal batch.Length 32 "Sample must return exactly batchSize elements")

[<Tests>]
let bufferSampleFailsWhenEmpty =
    testCase "ReplayBuffer: Sample raises when not enough experiences" (fun () ->
        let rng = System.Random(0)
        let buf = ReplayBuffer(100)
        buf.Push(makeExp 0)
        Expect.throws (fun () -> buf.Sample 32 rng |> ignore) "Should fail when size < batchSize")

[<Tests>]
let doneMaskTest =
    testCase "ReplayBuffer: done=true experiences are stored and retrievable" (fun () ->
        let rng = System.Random(0)
        let buf = ReplayBuffer(10)
        let doneExp = { makeExp 0 with Done = true }
        for _ in 0 .. 8 do
            buf.Push(makeExp 1)
        buf.Push(doneExp)
        // Sample many times — at least one done=true should appear
        let found = Array.init 200 (fun _ -> (buf.Sample 5 rng) |> Array.exists (fun e -> e.Done))
                    |> Array.exists id
        Expect.isTrue found "done=true experience must be sampleable")
