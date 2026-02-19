module TicTacToe.Tests.ConvergenceTests

open Expecto
open TicTacToe.Domain
open TicTacToe.Training

[<Tests>]
let convergenceTests =
    testList "Expecto 수렴 테스트" [

        // TICT-08: 100k 에피소드 학습 후 랜덤 상대 승률 > 90%
        testCase "TD 에이전트가 100k 자가 대국 후 랜덤 상대 승률 > 90%" <| fun () ->
            let rng = System.Random(42)
            let vTable, _ = trainAgent rng 100_000 0.1 0.1 1_000
            let winRate = winRateVsRandom rng vTable 1_000
            Expect.isGreaterThan winRate 0.90
                $"승률 > 90%% 기대, 실제: {winRate * 100.0:F1}%%"

        // 랜덤 에이전트 기본 확인: 유효한 수 반환
        testCase "랜덤 에이전트는 항상 합법 수(0-8)를 반환한다" <| fun () ->
            let rng = System.Random(1)
            let state = initialState ()
            let move = TicTacToe.Agent.randomAgent rng state
            Expect.isTrue (move >= 0 && move <= 8) "수는 [0, 8] 범위 내여야 한다"

        // TD 에이전트 기본 확인: 비종단 보드에서 유효한 수 반환
        testCase "TD 에이전트는 비종단 보드에서 합법 수를 반환한다" <| fun () ->
            let rng = System.Random(42)
            let vTable, _ = trainAgent rng 1_000 0.1 0.1 100
            let state = initialState ()
            let move = TicTacToe.Agent.tdAgent rng 0.0 vTable state
            Expect.isTrue (move >= 0 && move <= 8) "TD 에이전트 수는 [0, 8] 범위 내여야 한다"

    ]
