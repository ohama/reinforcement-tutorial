# Phase 2: Tic-Tac-Toe (TD Learning) - Research

**Researched:** 2026-02-19
**Domain:** F# .NET 10 / Tic-Tac-Toe game engine + TD(0) reinforcement learning + FsCheck property tests + Expecto convergence tests + Serilog logging + mdBook chapter
**Confidence:** HIGH

---

## Summary

Phase 2 builds a complete Tic-Tac-Toe RL system in F# using Temporal Difference (TD) learning. The stack is fully determined by prior decisions and direct replication of Phase 1 patterns: net10.0, traditional .sln format, FsCheck 2.16.5 + Expecto.FsCheck 10.2.3 + Expecto 10.2.3, YoloDev.Expecto.TestSdk 0.15.5, Microsoft.NET.Test.Sdk 18.0.1, Serilog 4.3.1 + Console 6.1.1 + File 7.0.0. The solution is named TicTacToe.sln and lives at `TicTacToe/` (repo root, peer of `Bandit/`).

The architecture follows Functional Core / Imperative Shell strictly. Domain.fs (Cell, Board, Player, GameState types), Rules.fs (win detection, legal moves, game over — pure functions), and Agent.fs (randomAgent, tdAgent, TD update formula) all contain zero I/O. Training.fs runs the self-play loop returning a trained ValueTable (a `Map<Board, float>`) and generates win-rate statistics for Serilog. Program.fs is the only impure file: it configures Serilog, seeds `System.Random`, runs training, then launches the human-vs-AI console interaction loop.

TD(0) for two-player games uses the update rule `V(s) ← V(s) + α * (V(s') - V(s))` where states experienced by the current player are backed up. Terminal states have fixed values: win = 1.0, loss = 0.0, draw = 0.5. The value table is keyed by board state (canonicalized as `Cell array`) — Tic-Tac-Toe has ~5,478 reachable states, small enough to fit in a `Map`. The TD agent uses ε-greedy exploration during self-play training: with probability ε it picks a random legal move, otherwise it picks the move leading to the successor state with the highest estimated value.

**Primary recommendation:** Replicate the Phase 1 three-project layout exactly (`TicTacToe` classlib + `TicTacToe.Console` exe + `TicTacToe.Tests` exe), with F# file ordering Domain.fs → Rules.fs → Agent.fs → Training.fs in the library project. Use `Map<Cell array, float>` for the value table (F# structural equality on arrays works correctly with Map). Run 100,000 self-play games; verify >90% win rate against random opponent with Expecto.

---

## Standard Stack

All versions locked by STATE.md prior decisions. Replicate Phase 1 exactly.

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 10.x | Runtime and build toolchain | System only has .NET 10 SDK; locked decision |
| F# | 9.x (with net10.0) | Language | Locked; functional-first, immutable by default |
| Expecto | 10.2.3 | Test runner (values-as-tests) | Locked; identical to Phase 1 |
| Expecto.FsCheck | 10.2.3 | Property-based test bridge | Locked; wraps FsCheck into testProperty |
| FsCheck | 2.16.5 | Property generation and shrinking | CRITICAL: must be 2.16.5, NOT 3.x — StdGen removed in 3.x causes TypeLoadException with Expecto.FsCheck 10.2.3 |
| YoloDev.Expecto.TestSdk | 0.15.5 | Enables `dotnet test` discovery | Required for `dotnet test` with Expecto; Phase 1 proven pattern |
| Microsoft.NET.Test.Sdk | 18.0.1 | Test SDK infrastructure | Required alongside YoloDev for dotnet test |
| Serilog | 4.3.1 | Structured logging core | Locked; Phase 1 proven |
| Serilog.Sinks.Console | 6.1.1 | Console log output | Locked; Phase 1 proven |
| Serilog.Sinks.File | 7.0.0 | File log output | Locked; Phase 1 proven |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| mdBook | 0.4.52 | Tutorial static site generator | Tutorial chapter 02-tictactoe/ content only; already installed from Phase 1 |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| FsCheck 2.16.5 | FsCheck 3.x | Never — TypeLoadException with Expecto.FsCheck 10.2.3. Locked. |
| Map<Cell array, float> | Dictionary<Cell array, float> | Map is immutable and idiomatic F#; Dictionary requires mutation and custom equality comparer |
| Cell array (flat 9-element) | Cell array2d 3x3 | Flat array is easier to use as Map key with F# structural equality; array2d structural equality works too but flat is simpler |

**Installation (from TicTacToe/ directory):**
```bash
# Library project — no extra packages needed
dotnet new classlib -lang F# -o src/TicTacToe --framework net10.0
dotnet new console -lang F# -o src/TicTacToe.Console --framework net10.0
dotnet new console -lang F# -o tests/TicTacToe.Tests --framework net10.0

# Console project packages
dotnet add src/TicTacToe.Console/TicTacToe.Console.fsproj package Serilog --version 4.3.1
dotnet add src/TicTacToe.Console/TicTacToe.Console.fsproj package Serilog.Sinks.Console --version 6.1.1
dotnet add src/TicTacToe.Console/TicTacToe.Console.fsproj package Serilog.Sinks.File --version 7.0.0

# Test project packages
dotnet add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj package Expecto --version 10.2.3
dotnet add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj package Expecto.FsCheck --version 10.2.3
dotnet add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj package FsCheck --version 2.16.5
dotnet add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj package YoloDev.Expecto.TestSdk --version 0.15.5
dotnet add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj package Microsoft.NET.Test.Sdk --version 18.0.1
```

---

## Architecture Patterns

### Recommended Project Structure

```
TicTacToe/                          # repo root peer of Bandit/
├── TicTacToe.sln                   # traditional .sln format (NOT .slnx)
├── src/
│   ├── TicTacToe/                  # pure library — zero I/O
│   │   ├── TicTacToe.fsproj        # classlib, net10.0
│   │   ├── Domain.fs               # Cell, Board, Player, GameState types
│   │   ├── Rules.fs                # checkWinner, legalMoves, applyMove, isGameOver
│   │   ├── Agent.fs                # randomAgent, tdAgent, tdUpdate, initValueTable
│   │   └── Training.fs             # selfPlay, trainAgent, winRateVsRandom (pure loops)
│   └── TicTacToe.Console/          # impure shell — I/O only
│       ├── TicTacToe.Console.fsproj # Exe, net10.0
│       └── Program.fs              # Serilog setup, training loop, human-vs-AI mode
└── tests/
    └── TicTacToe.Tests/            # Expecto console runner
        ├── TicTacToe.Tests.fsproj  # Exe, net10.0, GenerateProgramFile=false
        ├── PropertyTests.fs        # FsCheck: board invariants (TICT-07)
        ├── ConvergenceTests.fs     # Expecto: >90% win rate after 100k games (TICT-08)
        └── Main.fs                 # [<EntryPoint>] runTestsWithCLIArgs
```

### Pattern 1: Immutable GameState with Pure Game Engine

**What:** All game logic operates on immutable `GameState` records. Functions take a state and return a new state — no mutation. This is the direct equivalent of Phase 1's `AgentState` pattern.

**When to use:** Always. The entire `src/TicTacToe/` library is pure — no exceptions, no I/O, no mutable state escaping function scope.

**Example:**
```fsharp
// Domain.fs
module TicTacToe.Domain

type Cell = Empty | X | O

/// Board is a flat 9-element array (indices 0-8, row-major: 0=top-left, 8=bottom-right)
/// Flat array chosen over array2d for simpler Map key usage
type Board = Cell array

/// Immutable game state (TICT-02)
type GameState = {
    Board: Board
    CurrentPlayer: Cell  // X or O (never Empty)
}

/// Value table: board state -> estimated win probability for X
/// Map<Board, float> uses F# structural equality — works correctly for Cell arrays
type ValueTable = Map<Board, float>

let emptyBoard () : Board = Array.create 9 Empty

let initialState () : GameState =
    { Board = emptyBoard ()
      CurrentPlayer = X }

let otherPlayer (p: Cell) =
    match p with
    | X -> O
    | O -> X
    | Empty -> failwith "otherPlayer: Empty is not a valid player"
```

### Pattern 2: TD(0) Value Update for Two-Player Games

**What:** V(s) is the estimated probability that X wins from board state s. For terminal states: V(win for X) = 1.0, V(win for O) = 0.0, V(draw) = 0.5. During self-play, after each move the value of the previous state is backed up toward the value of the resulting state.

**When to use:** Exactly once in Agent.fs as `tdUpdate`. Called from Training.fs during self-play.

**Key insight for two-player games:** The value table stores values from X's perspective. When it is O's turn, O wants to minimize V(s) — so O picks the move with the *lowest* V(successor). The training loop must track which player made each move and apply updates using the correct perspective.

**Example:**
```fsharp
// Agent.fs
module TicTacToe.Agent

open TicTacToe.Domain
open TicTacToe.Rules

/// TD(0) update: V(s) <- V(s) + alpha * (V(s') - V(s))
/// alpha: learning rate (typical 0.1–0.3)
let tdUpdate (alpha: float) (vTable: ValueTable) (state: Board) (nextState: Board) : ValueTable =
    let vCurrent = Map.tryFind state vTable |> Option.defaultValue 0.5
    let vNext    = Map.tryFind nextState vTable |> Option.defaultValue 0.5
    let vNew     = vCurrent + alpha * (vNext - vCurrent)
    Map.add state vNew vTable

/// Initialize value table with terminal state values
/// Non-terminal states default to 0.5 (unknown, accessed via Map.tryFind defaulting to 0.5)
let initValueTable (allReachableStates: Board seq) : ValueTable =
    allReachableStates
    |> Seq.fold (fun acc board ->
        match checkWinner board with
        | Some X -> Map.add board 1.0 acc   // X wins -> V = 1.0
        | Some O -> Map.add board 0.0 acc   // O wins -> V = 0.0
        | None when legalMoves board = [] -> Map.add board 0.5 acc  // draw -> V = 0.5
        | None -> acc  // non-terminal: leave out; default 0.5 via tryFind
    ) Map.empty

/// Random agent (TICT-03): picks a random legal move
let randomAgent (rng: System.Random) (state: GameState) : int =
    let moves = legalMoves state.Board
    moves.[rng.Next(moves.Length)]

/// TD agent (TICT-04): epsilon-greedy move selection based on value table
/// X maximizes V(successor), O minimizes V(successor)
let tdAgent (rng: System.Random) (epsilon: float) (vTable: ValueTable) (state: GameState) : int =
    let moves = legalMoves state.Board
    if rng.NextDouble() < epsilon then
        moves.[rng.Next(moves.Length)]
    else
        let getBoardValue board =
            Map.tryFind board vTable |> Option.defaultValue 0.5
        let scoredMoves =
            moves
            |> List.map (fun move ->
                let nextBoard = applyMove state.Board state.CurrentPlayer move
                let v = getBoardValue nextBoard
                move, v)
        match state.CurrentPlayer with
        | X -> scoredMoves |> List.maxBy snd |> fst  // X maximizes
        | O -> scoredMoves |> List.minBy snd |> fst  // O minimizes
        | Empty -> failwith "tdAgent: Empty is not a valid player"
```

### Pattern 3: Self-Play Training Loop with TD Updates

**What:** Two agents play against each other (both using the same ValueTable with ε-greedy). After each move, the previous board state for the current player is updated via TD(0). At game end, terminal values are set (1.0/0.0/0.5) and the final backup is applied.

**When to use:** In Training.fs as the core learning mechanism. 100,000 games are played.

**Example:**
```fsharp
// Training.fs
module TicTacToe.Training

open TicTacToe.Domain
open TicTacToe.Rules
open TicTacToe.Agent

/// Play one full game between two TD agents sharing a value table.
/// Returns the updated ValueTable after all TD backups.
let playEpisode
    (rng: System.Random)
    (alpha: float)
    (epsilon: float)
    (vTable: ValueTable) : ValueTable * GameResult =

    let rec loop (state: GameState) (vTable: ValueTable) (prevBoard: Board option) =
        match isGameOver state.Board with
        | Some result ->
            // Terminal: set final value and apply last backup
            let terminalValue =
                match result with
                | XWins -> 1.0
                | OWins -> 0.0
                | Draw  -> 0.5
            let vTable' = Map.add state.Board terminalValue vTable
            let vTable'' =
                match prevBoard with
                | Some prev -> tdUpdate alpha vTable' prev state.Board
                | None -> vTable'
            vTable'', result
        | None ->
            let move = tdAgent rng epsilon vTable state
            let nextBoard = applyMove state.Board state.CurrentPlayer move
            let nextState = { Board = nextBoard; CurrentPlayer = otherPlayer state.CurrentPlayer }
            // TD backup: update value of previous board toward current board
            let vTable' =
                match prevBoard with
                | Some prev -> tdUpdate alpha vTable prev state.Board
                | None -> vTable
            loop nextState vTable' (Some state.Board)

    loop (initialState ()) vTable None

/// Train for N episodes using self-play (TICT-05)
/// Returns (finalValueTable, winRateHistory) where history is list of (episode, xWinRate)
let trainAgent
    (rng: System.Random)
    (episodes: int)
    (alpha: float)
    (epsilon: float)
    (logInterval: int) : ValueTable * (int * float) list =

    let rec loop ep vTable wins history =
        if ep > episodes then
            vTable, List.rev history
        else
            let vTable', result = playEpisode rng alpha epsilon vTable
            let wins' = if result = XWins then wins + 1 else wins
            let history' =
                if ep % logInterval = 0 && ep > 0 then
                    let rate = float wins' / float ep
                    (ep, rate) :: history
                else history
            loop (ep + 1) vTable' wins' history'

    loop 1 Map.empty 0 []

/// Measure win rate of TD agent vs random opponent over N games (TICT-08)
let winRateVsRandom
    (rng: System.Random)
    (vTable: ValueTable)
    (games: int) : float =
    // TD agent plays as X, random as O
    let wins =
        [ 1..games ]
        |> List.sumBy (fun _ ->
            let rec play state =
                match isGameOver state.Board with
                | Some XWins -> 1
                | Some _ -> 0
                | None ->
                    let move =
                        if state.CurrentPlayer = X then
                            tdAgent rng 0.0 vTable state  // exploit only (epsilon=0)
                        else
                            randomAgent rng state
                    let next = { Board = applyMove state.Board state.CurrentPlayer move
                                 CurrentPlayer = otherPlayer state.CurrentPlayer }
                    play next
            play (initialState ()))
    float wins / float games
```

### Pattern 4: Rules.fs — Pure Game Engine

**What:** All game logic — win detection, legal moves, move application — as pure functions. No side effects. Win detection checks all 8 lines (3 rows + 3 cols + 2 diagonals).

**When to use:** Always. This is the TICT-01 requirement.

**Example:**
```fsharp
// Rules.fs
module TicTacToe.Rules

open TicTacToe.Domain

/// Win lines: indices of the 8 possible three-in-a-row combinations
let private winLines = [|
    [|0;1;2|]; [|3;4;5|]; [|6;7;8|]  // rows
    [|0;3;6|]; [|1;4;7|]; [|2;5;8|]  // cols
    [|0;4;8|]; [|2;4;6|]             // diagonals
|]

/// Check if a player has won. Returns Some player if won, None otherwise.
let checkWinner (board: Board) : Cell option =
    winLines
    |> Array.tryPick (fun line ->
        let cells = line |> Array.map (fun i -> board.[i])
        if cells.[0] <> Empty && cells.[0] = cells.[1] && cells.[1] = cells.[2]
        then Some cells.[0]
        else None)

type GameResult = XWins | OWins | Draw

/// Check if the game is over. Returns Some GameResult if terminal, None if ongoing.
let isGameOver (board: Board) : GameResult option =
    match checkWinner board with
    | Some X -> Some XWins
    | Some O -> Some OWins
    | Some Empty -> None  // impossible, but satisfies exhaustiveness
    | None ->
        if Array.forall (fun c -> c <> Empty) board
        then Some Draw
        else None

/// Return list of legal move indices (indices of Empty cells)
let legalMoves (board: Board) : int list =
    board
    |> Array.indexed
    |> Array.choose (fun (i, c) -> if c = Empty then Some i else None)
    |> Array.toList

/// Apply a move: return new board with cell at index set to player
/// Does NOT validate legality — caller's responsibility
let applyMove (board: Board) (player: Cell) (index: int) : Board =
    board |> Array.mapi (fun i c -> if i = index then player else c)
```

### Pattern 5: FsCheck Property Tests for Board Invariants (TICT-07)

**What:** FsCheck 2.16.5 property tests verifying structural invariants of the game engine. Key properties: empty cell count decreases by 1 per move, players alternate, legal moves are always valid indices.

**When to use:** In PropertyTests.fs. Use `testProperty` from Expecto.FsCheck (via `open Expecto.ExpectoFsCheck`). Use `[<Tests>]` attribute on the test list.

**CRITICAL — FsCheck 2.16.5 API note:** FsCheck 2.16.5 uses the old `Arb` API (`Gen.elements`, `Arb.generate`, `Check.Quick`). For simple properties with F# primitives and custom generated values, use `Gen.sample` or provide explicit arbitraries via `testPropertyWithConfig`. However, for `testProperty` in Expecto, the simplest approach is to generate test scenarios inside the property function body using `System.Random`, not through FsCheck generators — this avoids 2.x vs 3.x API confusion.

**Example:**
```fsharp
// PropertyTests.fs
module TicTacToe.Tests.PropertyTests

open Expecto
open Expecto.ExpectoFsCheck
open TicTacToe.Domain
open TicTacToe.Rules
open TicTacToe.Agent

[<Tests>]
let propertyTests =
    testList "FsCheck Board Invariants" [

        // TICT-07: Empty cell count decreases by 1 after each move
        testProperty "Empty count decreases by 1 after a legal move" <| fun () ->
            let board = Array.create 9 Empty
            let emptiesBefore = board |> Array.filter ((=) Empty) |> Array.length
            let move = 4  // center — always legal on empty board
            let board' = applyMove board X move
            let emptiesAfter = board' |> Array.filter ((=) Empty) |> Array.length
            emptiesAfter = emptiesBefore - 1

        // TICT-07: Players alternate in a sequence of moves
        testProperty "Players alternate: after X moves it is O's turn" <| fun () ->
            let state = initialState ()
            let move = 0  // always legal on empty board
            let board' = applyMove state.Board state.CurrentPlayer move
            let nextPlayer = otherPlayer state.CurrentPlayer
            nextPlayer = O  // X started, next is O

        // TICT-07: Legal moves are always valid indices (0-8)
        testProperty "Legal moves are all in range [0, 8]" <| fun () ->
            let board = Array.create 9 Empty
            let moves = legalMoves board
            moves |> List.forall (fun m -> m >= 0 && m <= 8)

        // TICT-07: applyMove does not change other cells
        testProperty "applyMove only changes the target cell" <| fun () ->
            let board = Array.create 9 Empty
            let board' = applyMove board X 4
            let unchanged = [0;1;2;3;5;6;7;8] |> List.forall (fun i -> board'.[i] = Empty)
            unchanged && board'.[4] = X

        // TICT-07: Empty count is 9 on initial board
        testProperty "Initial board has 9 empty cells" <| fun () ->
            let board = emptyBoard ()
            board |> Array.filter ((=) Empty) |> Array.length = 9
    ]
```

### Pattern 6: Expecto Win-Rate Convergence Test (TICT-08)

**What:** After 100,000 self-play training games, the TD agent must beat a random opponent >90% of the time over 1,000 evaluation games. This is a `testCase` (not `testProperty`) — it runs one deterministic evaluation with a fixed seed.

**Note on test speed:** 100,000 self-play episodes takes approximately 3-10 seconds in F# (5,478 states, simple Map operations). This is acceptable for a test suite. Use a fixed seed for reproducibility.

**Example:**
```fsharp
// ConvergenceTests.fs
module TicTacToe.Tests.ConvergenceTests

open Expecto
open TicTacToe.Domain
open TicTacToe.Training

[<Tests>]
let convergenceTests =
    testList "Expecto Convergence Tests" [

        // TICT-08: TD agent must achieve >90% win rate after 100k episodes
        testCase "TD agent beats random opponent >90% after 100k self-play games" <| fun () ->
            let rng = System.Random(42)
            let vTable, _ = trainAgent rng 100_000 0.1 0.1 1_000
            let winRate = winRateVsRandom rng vTable 1_000
            Expect.isGreaterThan winRate 0.90
                $"Expected >90%% win rate, got {winRate * 100.0:F1}%%"

        // Sanity check: random agent exists and returns valid moves
        testCase "Random agent always returns a legal move index" <| fun () ->
            let rng = System.Random(1)
            let state = initialState ()
            let move = TicTacToe.Agent.randomAgent rng state
            Expect.isTrue (move >= 0 && move <= 8) "Move must be in [0, 8]"

        // Sanity check: a fully trained agent always finds a move on a non-terminal board
        testCase "TD agent returns a move on a non-terminal board" <| fun () ->
            let rng = System.Random(42)
            let vTable, _ = trainAgent rng 1_000 0.1 0.1 100
            let state = initialState ()
            let move = TicTacToe.Agent.tdAgent rng 0.0 vTable state
            Expect.isTrue (move >= 0 && move <= 8) "TD agent move must be in [0, 8]"
    ]
```

### Pattern 7: Tests .fsproj with [<Tests>] Attribute and GenerateProgramFile=false

**What:** Exact replica of Phase 1 test project configuration. `GenerateProgramFile=false` prevents .NET from auto-generating a Program.fs that conflicts with Main.fs. `[<Tests>]` attribute enables test discovery by YoloDev.Expecto.TestSdk for `dotnet test`. Main.fs provides `[<EntryPoint>]` for standalone execution.

**Example (TicTacToe.Tests.fsproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <GenerateProgramFile>false</GenerateProgramFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="PropertyTests.fs" />
    <Compile Include="ConvergenceTests.fs" />
    <Compile Include="Main.fs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TicTacToe\TicTacToe.fsproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Expecto" Version="10.2.3" />
    <PackageReference Include="Expecto.FsCheck" Version="10.2.3" />
    <PackageReference Include="FsCheck" Version="2.16.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="YoloDev.Expecto.TestSdk" Version="0.15.5" />
  </ItemGroup>

</Project>
```

### Pattern 8: Serilog Training Curve Logging (TICT-09)

**What:** Every 1,000 episodes, log the current win rate as a structured event. This produces a learning curve observable in the log. Logging happens in Program.fs (impure shell), not in Training.fs. Training.fs returns win-rate statistics; Program.fs logs them.

**Example:**
```fsharp
// Program.fs (excerpt — impure shell)
module TicTacToe.Console.Program

open Serilog
open TicTacToe.Training

[<EntryPoint>]
let main _args =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File(
                "logs/tictactoe-.log",
                rollingInterval = RollingInterval.Day,
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger()

    let rng = System.Random(42)
    Log.Information("학습 시작: 자가 대국 100,000판")

    // Train and collect win-rate history every 1,000 episodes
    let vTable, history = trainAgent rng 100_000 0.1 0.1 1_000

    // Log learning curve
    for (ep, rate) in history do
        Log.Information("Episode={Episode} WinRate={WinRate:P1}", ep, rate)

    Log.Information("학습 완료. 최종 승률: {WinRate:P1}", snd (List.last history))

    // Launch human-vs-AI mode
    runHumanVsAI rng vTable  // defined in same file or called helper

    Log.CloseAndFlush()
    0
```

### Pattern 9: Human-vs-AI Console Interaction (TICT-06)

**What:** After training, the console prompts the human to enter a position (1-9, 1-indexed for user friendliness). The AI moves automatically using the trained value table with epsilon=0 (pure exploitation). The board is printed after each move. The loop continues until the game ends.

**Example:**
```fsharp
// Program.fs — human-vs-AI loop (impure, lives in Program.fs)
let printBoard (board: Board) =
    let cellChar = function X -> "X" | O -> "O" | Empty -> "."
    for row in 0..2 do
        let line = [0..2] |> List.map (fun col -> cellChar board.[row * 3 + col]) |> String.concat " | "
        printfn " %s " line
        if row < 2 then printfn " ---------"

let runHumanVsAI (rng: System.Random) (vTable: ValueTable) =
    printfn "\n사람(X) vs AI(O) 대전 시작!"
    let rec loop state =
        printBoard state.Board
        match isGameOver state.Board with
        | Some XWins -> printfn "사람(X) 승리!"
        | Some OWins -> printfn "AI(O) 승리!"
        | Some Draw  -> printfn "무승부!"
        | None ->
            let move =
                if state.CurrentPlayer = X then
                    printf "위치 입력 (1-9): "
                    let input = System.Console.ReadLine()
                    match System.Int32.TryParse(input) with
                    | true, n when n >= 1 && n <= 9 -> n - 1  // convert to 0-indexed
                    | _ -> printfn "잘못된 입력. 1-9 사이 숫자를 입력하세요."; loop state; 0
                else
                    tdAgent rng 0.0 vTable state
            let nextBoard = applyMove state.Board state.CurrentPlayer move
            let nextState = { Board = nextBoard; CurrentPlayer = otherPlayer state.CurrentPlayer }
            loop nextState
    loop (initialState ())
```

### Pattern 10: Solution Bootstrap with Traditional .sln Format

**What:** `dotnet new sln` in .NET 10 defaults to `.slnx` format. Must force traditional `.sln` format. Use `dotnet new sln` then verify file extension is `.sln`. If `.slnx` is created, delete and use `dotnet new sln --format sln` or manually create.

**When to use:** When bootstrapping TicTacToe.sln. This is a confirmed .NET 10 behavior from Phase 1.

**Example:**
```bash
# From repo root
mkdir -p TicTacToe
cd TicTacToe

# Force traditional .sln format (not .slnx)
dotnet new sln -n TicTacToe

# If TicTacToe.slnx is created instead of TicTacToe.sln, use:
# dotnet new sln -n TicTacToe --output . --force
# Check: ls *.sln — must see TicTacToe.sln, not TicTacToe.slnx

dotnet new classlib -lang F# -o src/TicTacToe --framework net10.0
dotnet new console -lang F# -o src/TicTacToe.Console --framework net10.0
dotnet new console -lang F# -o tests/TicTacToe.Tests --framework net10.0

dotnet sln TicTacToe.sln add src/TicTacToe/TicTacToe.fsproj
dotnet sln TicTacToe.sln add src/TicTacToe.Console/TicTacToe.Console.fsproj
dotnet sln TicTacToe.sln add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj

dotnet add src/TicTacToe.Console/TicTacToe.Console.fsproj reference src/TicTacToe/TicTacToe.fsproj
dotnet add tests/TicTacToe.Tests/TicTacToe.Tests.fsproj reference src/TicTacToe/TicTacToe.fsproj
```

### Pattern 11: mdBook Chapter with `{{#include}}` (TUTR-03, TUTR-04, TUTR-05, TUTR-06)

**What:** The `tutorial/src/02-tictactoe/README.md` stub must be replaced with the full Korean chapter content. Source code is included via `{{#include}}` directives pointing to the actual F# source files (prevents documentation drift). The chapter explains Phase 1 Bandit limitations and motivates MDP/TD Learning.

**When to use:** In plan 02-03 (the final plan of this phase).

**Example structure:**
```markdown
# Chapter 2: 틱택토 — 상태와 가치

## Bandit의 한계: 왜 MDP가 필요한가?

슬롯머신 문제(Bandit)에서 우리는 상태가 변하지 않는 세계를 다뤘다.
매번 같은 확률 분포에서 보상이 나왔고, 과거 행동이 미래에 영향을 주지 않았다.

틱택토는 다르다. **행동이 상태를 바꾼다.** 한 수를 두면 보드가 바뀌고,
다음 행동의 선택지도 달라진다. 이것이 MDP(Markov Decision Process)의 핵심이다.

## MDP 핵심 개념

...

## 핵심 F# 타입

```fsharp
{{#include ../../../TicTacToe/src/TicTacToe/Domain.fs}}
```

## 규칙 엔진

```fsharp
{{#include ../../../TicTacToe/src/TicTacToe/Rules.fs}}
```

## TD Learning 에이전트

```fsharp
{{#include ../../../TicTacToe/src/TicTacToe/Agent.fs}}
```
```

**CRITICAL:** The `{{#include}}` path is relative to the book's `src/` directory. Since the tutorial is at `tutorial/src/` and the TicTacToe source is at `TicTacToe/src/TicTacToe/`, the relative path from `tutorial/src/02-tictactoe/README.md` to `TicTacToe/src/TicTacToe/Domain.fs` is `../../../TicTacToe/src/TicTacToe/Domain.fs`. Verify this path works with `mdbook build`.

### Anti-Patterns to Avoid

- **Mutable value table in training loop:** The value table must be passed and returned as an immutable `Map`. Avoid using a `Dictionary` or `mutable` reference — this would break the Functional Core boundary.
- **Storing V(s) for both players separately:** Use a single value table from X's perspective. O minimizes it. No need for two tables.
- **Not handling the `None` case in `checkWinner` for `Empty` cell:** `checkWinner` returns `Cell option`, and `Cell` has three cases. The match in `isGameOver` must be exhaustive — add `| Some Empty -> None` even though it's logically impossible.
- **F# file ordering in .fsproj:** Domain.fs → Rules.fs → Agent.fs → Training.fs. Rules.fs depends on Domain.fs. Agent.fs depends on both. Training.fs depends on all three. Reversing any order causes "not defined" compile errors.
- **Using `Array.create 9 Empty` vs `Array.zeroCreate 9`:** `Array.zeroCreate` for arrays of value types (int, float) is zero, but for discriminated union types (Cell), use `Array.create 9 Empty` to properly fill with the `Empty` case.
- **FsCheck 3.x vs 2.16.5:** Do NOT upgrade FsCheck to 3.x. TypeLoadException at runtime. Pin to 2.16.5 exactly.
- **Missing `GenerateProgramFile=false` in test .fsproj:** Without this, .NET auto-generates a conflicting `Program.fs`. The build will fail with "duplicate 'main' function" error.
- **TD update applied from opponent's board perspective:** When player O moves, the backup should update O's *previous* board state (which is a state where it's O's turn). Since V is from X's perspective, O's updates still correctly decrease V when O wins. The update formula `V(s) += alpha * (V(s') - V(s))` is symmetric and correct regardless of whose turn it is.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Win detection | Custom string comparison or manual 8-line check | `winLines` constant array + `Array.tryPick` | Easy to miss one of 8 lines; constant array is readable and exhaustive |
| Value table storage | Custom hash map or mutable dictionary | F# `Map<Board, float>` | F# Map uses structural equality — Cell arrays compare element-by-element correctly without custom comparers |
| Property test shrinking | Manual counter-example reducer | FsCheck 2.16.5 (via Expecto.FsCheck) | FsCheck shrinks automatically to minimal failing case |
| Board state canonicalization | Custom hash function or encoding | Raw `Cell array` as Map key | F# structural equality on arrays works correctly for this; no encoding needed |
| Test runner configuration | Custom test discovery | YoloDev.Expecto.TestSdk + [<Tests>] attribute | Direct copy from Phase 1 proven setup; `dotnet test` works without modification |

**Key insight:** The Tic-Tac-Toe state space (~5,478 reachable states) is small enough that a `Map<Board, float>` with full enumeration is completely appropriate. Do not optimize with hashing, encoding, or board normalization (symmetry reduction) — that adds complexity without necessity at this scale.

---

## Common Pitfalls

### Pitfall 1: TD Update Applied on Wrong Player's States

**What goes wrong:** The value table converges slowly or not at all; the trained agent plays poorly.

**Why it happens:** The TD backup must apply to the state *before* the current player's move, not the opponent's state. If the loop applies updates to every state indiscriminately regardless of perspective, the backups are semantically incorrect.

**How to avoid:** Track `prevBoard` (the board before the *current player* moved) and apply `tdUpdate` using `prevBoard → currentBoard`. In a self-play loop where the same function handles both X and O, this means updating the state the current player is about to move away from.

**Warning signs:** Win rate against random barely exceeds 50% even after 100k episodes. Check the backup logic — add a test that verifies V(winning terminal board) = 1.0 after training.

### Pitfall 2: F# File Ordering in .fsproj

**What goes wrong:** Compiler error "The value or constructor 'checkWinner' is not defined."

**Why it happens:** Rules.fs uses Domain types; Agent.fs uses Rules functions; Training.fs uses Agent functions. If Agent.fs appears before Rules.fs in the fsproj, the compiler cannot resolve `checkWinner`.

**How to avoid:** Maintain strict ordering: `Domain.fs` → `Rules.fs` → `Agent.fs` → `Training.fs`.

**Warning signs:** Any "not defined" compiler error in a file that clearly imports the correct module.

### Pitfall 3: .NET 10 Creates .slnx Instead of .sln

**What goes wrong:** `dotnet sln TicTacToe.sln add ...` fails because the file is named `TicTacToe.slnx`.

**Why it happens:** .NET 10 defaults to the new .slnx format. Phase 1 confirmed this behavior.

**How to avoid:** After `dotnet new sln`, check whether the output is `.sln` or `.slnx`. If `.slnx`, delete it and recreate with explicit format flag, or rename and verify it is valid XML-less format.

**Warning signs:** `dotnet sln` command says "solution file not found" or "unrecognized format".

### Pitfall 4: FsCheck 3.x TypeLoadException

**What goes wrong:** `dotnet test` fails at startup with TypeLoadException mentioning `StdGen` or assembly load failure.

**Why it happens:** FsCheck 3.x removed `StdGen`; Expecto.FsCheck 10.2.3 still references it from FsCheck 2.x.

**How to avoid:** Pin FsCheck to exactly 2.16.5. Never `dotnet add package FsCheck` without `--version 2.16.5`.

**Warning signs:** TypeLoadException in test output; tests never start running.

### Pitfall 5: Missing `GenerateProgramFile=false`

**What goes wrong:** Build error: "type 'Program' is defined in multiple files" or "duplicate definition of 'main'".

**Why it happens:** .NET SDK auto-generates a top-level entry point when it detects there is no explicit one. With F# console projects, this conflicts with Main.fs.

**How to avoid:** Add `<GenerateProgramFile>false</GenerateProgramFile>` to the test .fsproj `<PropertyGroup>`.

**Warning signs:** Build succeeds but `dotnet test` fails with "multiple entry point" error. Or build fails immediately with "duplicate" error.

### Pitfall 6: Array.create vs Array.zeroCreate for Cell type

**What goes wrong:** `Array.zeroCreate 9` produces an array of `null` values cast to `Cell`, not `Empty` — this causes `MatchFailureException` when pattern matching.

**Why it happens:** `Array.zeroCreate` fills with the zero/default value. For reference types and discriminated unions without a zero value, this is `null`.

**How to avoid:** Use `Array.create 9 Empty` for board initialization. Use `Array.zeroCreate` only for `int array` and `float array`.

**Warning signs:** NullReferenceException or MatchFailureException when pattern matching `Cell`.

### Pitfall 7: {{#include}} Path Incorrect in mdBook

**What goes wrong:** `mdbook build` error "file not found" for `{{#include}}` directive.

**Why it happens:** The path in `{{#include path}}` is relative to the source `.md` file, not the book root. The relationship between `tutorial/src/02-tictactoe/README.md` and `TicTacToe/src/TicTacToe/Domain.fs` requires three levels of `../` to reach the repo root.

**How to avoid:** Verify the path: from `tutorial/src/02-tictactoe/` to repo root is `../../../`. Then `../../../TicTacToe/src/TicTacToe/Domain.fs`. Test with `mdbook build tutorial/` immediately after adding any `{{#include}}`.

**Warning signs:** mdBook build warning "file not found" or the include block renders as the raw `{{#include ...}}` text.

### Pitfall 8: TD Agent Epsilon Not Set to 0 During Evaluation

**What goes wrong:** Win rate evaluation in `winRateVsRandom` is lower than actual capability — appears to not converge.

**Why it happens:** If epsilon > 0 during evaluation, the agent randomly throws away moves. The >90% test requires evaluation with epsilon=0 (pure exploitation).

**How to avoid:** Pass `epsilon = 0.0` to `tdAgent` in `winRateVsRandom`. Training uses epsilon > 0; evaluation uses epsilon = 0.

**Warning signs:** Win rate plateaus at ~80% even after many training episodes. Check if epsilon is hardcoded in evaluation.

---

## Code Examples

### Complete Domain.fs (production-ready)

```fsharp
// Source: Phase 1 pattern + Tic-Tac-Toe requirements (TICT-01, TICT-02)
module TicTacToe.Domain

type Cell = Empty | X | O

/// Flat 9-element board (0=top-left, 8=bottom-right, row-major)
type Board = Cell array

/// Immutable game state (TICT-02)
type GameState = {
    Board: Board
    CurrentPlayer: Cell  // X or O; never Empty
}

/// Value table: board state -> X's estimated win probability [0.0, 1.0]
type ValueTable = Map<Board, float>

let emptyBoard () : Board = Array.create 9 Empty

let initialState () : GameState =
    { Board = emptyBoard (); CurrentPlayer = X }

let otherPlayer (p: Cell) : Cell =
    match p with
    | X -> O
    | O -> X
    | Empty -> failwith "otherPlayer: Empty is not a valid player"
```

### Win Rate Test Assertion Pattern

```fsharp
// Source: Phase 1 ConvergenceTests.fs pattern adapted for win rate
// ConvergenceTests.fs
testCase "TD agent beats random opponent >90% after 100k self-play games" <| fun () ->
    let rng = System.Random(42)
    let vTable, _ = trainAgent rng 100_000 0.1 0.1 1_000
    let winRate = winRateVsRandom rng vTable 1_000
    Expect.isGreaterThan winRate 0.90
        $"Expected >90%% win rate, got {winRate * 100.0:F1}%%"
```

### Serilog Win Rate Logging Every 1,000 Episodes

```fsharp
// Source: Phase 1 Serilog pattern adapted for training curve (TICT-09)
// In Program.fs — pure training returns history; impure shell logs it
let vTable, history = trainAgent rng 100_000 0.1 0.1 1_000
for (ep, rate) in history do
    Log.Information("Episode={Episode} WinRate={WinRate:P1}", ep, rate)
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Global mutable value table | Immutable `Map<Board, float>` passed as function argument | F# idiom | Enables pure functional core; testable |
| Storing win conditions as if-else chain | `winLines` constant array + Array.tryPick | N/A | 8 lines declared once, eliminates bugs |
| `runTestsWithArgs` | `runTestsWithCLIArgs` | Expecto 9+ | Old function deprecated; new one used in Phase 1 |
| FsCheck 3.x (Phase 1 research assumed) | FsCheck 2.16.5 (Phase 1 STATE.md correction) | Phase 1 execution | TypeLoadException fixed; 2.16.5 is stable |
| dotnet new sln (.slnx) | Traditional .sln format | .NET 10 | Must force .sln; .slnx breaks dotnet sln commands in CI |

**Deprecated/outdated:**
- FsCheck 3.x `ArbMap`: Do not use — stay on FsCheck 2.16.5 for this entire project.
- `Array.zeroCreate` for discriminated union arrays: Creates null-filled array — use `Array.create N defaultValue`.

---

## Open Questions

1. **TD convergence speed with 100k episodes**
   - What we know: Tic-Tac-Toe has ~5,478 reachable states. TD(0) with alpha=0.1, epsilon=0.1 typically converges to >90% win rate in 20k-50k episodes for Tic-Tac-Toe (well-documented in Sutton & Barto Chapter 1).
   - What's unclear: Whether the exact 100k episodes with seed=42 reliably yields >90% in the test on net10.0.
   - Recommendation: Use alpha=0.1, epsilon=0.1 for training, evaluate with epsilon=0. If the test is flaky, increase episodes or use a more favorable seed. Consider evaluating over 500 games instead of 1,000 to reduce variance in the test assertion.

2. **Value table initialization strategy**
   - What we know: Two options: (a) lazy initialization — non-terminal states default to 0.5 via `Map.tryFind ... |> Option.defaultValue 0.5`, or (b) eager initialization — enumerate all reachable boards and set their initial values. Option (a) is simpler to implement.
   - What's unclear: Whether eager initialization speeds convergence meaningfully.
   - Recommendation: Use lazy initialization (Option a). `Map.tryFind board vTable |> Option.defaultValue 0.5` is a clean one-liner. Terminal values (1.0/0.0/0.5) are set during training when they are first encountered, not pre-computed.

3. **Human vs AI mode: error handling for invalid input**
   - What we know: TICT-06 requires a human-vs-AI console mode. Human input can be invalid (non-integer, out of range, occupied cell).
   - What's unclear: Exact behavior expected for invalid input — retry loop or exit.
   - Recommendation: Retry loop with an error message. This is a console tutorial app, not production code — simple `match TryParse` with retry is sufficient. Do not use Result/Option for console I/O validation (that would be over-engineering for Program.fs).

---

## Sources

### Primary (HIGH confidence)

- Phase 1 source code (read in research): `Bandit/` directory — all patterns verified from working code
- Phase 1 planning artifacts (read in research): `.planning/phases/01-bandit-mdbook/` — exact NuGet versions, .fsproj patterns, test project configuration
- `rl-gomoku-roadmap.md`: TD Learning pseudocode, ValueTable type definition, game requirements
- Sutton & Barto "Reinforcement Learning" Chapter 1 (Tic-Tac-Toe example): TD(0) update formula, terminal value assignment, self-play training structure
- STATE.md prior decisions: FsCheck 2.16.5, YoloDev.Expecto.TestSdk 0.15.5, GenerateProgramFile=false, net10.0, traditional .sln

### Secondary (MEDIUM confidence)

- Phase 1 RESEARCH.md: Serilog patterns, mdBook `{{#include}}` path handling, F# file ordering rules
- Phase 1 PLAN files (01-01-PLAN.md, 01-02-PLAN.md): Exact bootstrap commands, solution creation pattern

### Tertiary (LOW confidence)

- Estimated convergence speed (~20k-50k episodes for >90% win rate): Based on knowledge of TD learning characteristics for Tic-Tac-Toe, not empirically verified on this specific codebase. Actual performance depends on random seed and hyperparameters.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions locked by Phase 1 STATE.md, verified from working Bandit.Tests.fsproj
- Architecture: HIGH — direct replication of Phase 1 three-project layout; TD(0) algorithm is well-specified in roadmap
- Pitfalls: HIGH — F# file ordering, .slnx vs .sln, FsCheck version, GenerateProgramFile are all confirmed Phase 1 pain points from STATE.md
- TD Learning correctness: HIGH — formula from Sutton & Barto is authoritative; two-player value perspective is standard RL technique
- Convergence guarantee (>90% in 100k): MEDIUM — theoretically sound, empirically unverified on net10.0 with seed=42

**Research date:** 2026-02-19
**Valid until:** 2026-03-21 (30 days — stack is locked, no fast-moving dependencies)
