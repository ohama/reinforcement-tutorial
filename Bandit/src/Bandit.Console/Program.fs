module Bandit.Console.Program

open Serilog

[<EntryPoint>]
let main _args =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger()
    Log.Information("Bandit — Phase 1 placeholder")
    Log.CloseAndFlush()
    0
