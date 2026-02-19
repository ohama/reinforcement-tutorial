module TicTacToe.Tests.Main

open Expecto

[<EntryPoint>]
let main args =
    let allTests =
        testList "모든 TicTacToe 테스트" [
            TicTacToe.Tests.PropertyTests.propertyTests
            TicTacToe.Tests.ConvergenceTests.convergenceTests
        ]
    runTestsWithCLIArgs [] args allTests
