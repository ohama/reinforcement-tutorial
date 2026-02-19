module TicTacToe.Tests.Main

open Expecto

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args TicTacToe.Tests.PropertyTests.propertyTests
