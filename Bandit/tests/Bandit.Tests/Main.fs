module Bandit.Tests.Main

open Expecto
open Bandit.Tests.PropertyTests
open Bandit.Tests.ConvergenceTests

[<EntryPoint>]
let main args =
    let allTests =
        testList "Bandit Test Suite" [
            propertyTests
            convergenceTests
        ]
    runTestsWithCLIArgs [] args allTests
