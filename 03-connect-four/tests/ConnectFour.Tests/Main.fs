module ConnectFour.Tests.Main

open Expecto

[<EntryPoint>]
let main argv =
    let allTests =
        testList "All ConnectFour Tests" [
            ConnectFour.Tests.PropertyTests.gravityTests
            ConnectFour.Tests.PropertyTests.winnerTests
        ]
    runTestsWithCLIArgs [] argv allTests
