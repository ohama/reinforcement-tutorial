# Phase 3: Connect Four (Q-Learning + Minimax) - Research

**Researched:** 2026-02-20
**Domain:** Game AI — Minimax Alpha-Beta Pruning + Q-Learning, F# functional game engine
**Confidence:** HIGH

---

## Summary

This phase implements Connect Four on a 6×7 board, comparing Q-Learning (tabular) and Minimax with Alpha-Beta Pruning. The primary educational goal is to demonstrate _why_ tabular Q-Learning breaks at this scale, while Minimax remains tractable.

**What was researched:**
Connect Four game engine design (gravity, 4-in-a-row detection), Minimax Alpha-Beta Pruning in F# functional style, tabular Q-Learning with feature extraction for large state spaces, FsCheck property patterns for board invariants, Expecto test patterns for algorithm equivalence, the Score4 F# reference implementation, and state space size analysis.

**Standard approach:**
The game engine uses a flat `Cell array` (42 elements, row-major) with gravity implemented as "find lowest empty row in column." Minimax uses negamax variant with alpha-beta pruning, depth 6–8, and a window-scoring evaluation function. Q-Learning uses a `Dictionary<string, float[]>` (board hash → Q-values per column) with feature-based state encoding to bound the table size; the full raw state space (≈4.5 trillion positions) is used as the teachable demonstration of why DQN is needed in Phase 4.

**Key recommendations:**
- Mirror the Phase 2 pure/impure separation: `Domain.fs` + `Rules.fs` (pure) vs `Minimax.fs` + `QAgent.fs` + `Training.fs` (impure-tolerant) vs `Program.fs` (I/O shell)
- Use flat `Cell array` (not 2D), indexed as `board.[row * 7 + col]`, consistent with Phase 2 pattern
- Encode Q-table keys as `sprintf` board strings (e.g. `".XO.XO..."`) — string hashing in Dictionary is fast and avoids custom equality
- Implement Minimax as `minimaxAB : Board -> Player -> int -> int -> int -> int` where return value is score from current player's POV (negamax style simplifies alternation)
- Track `mutable pruneCount` as a `ref` cell passed through recursion for Alpha-Beta statistics output

**Primary recommendation:** Build Domain + Rules first (with FsCheck tests passing), then Minimax, then Q-Learning, then the comparison harness — in that order. Do not attempt to build all four simultaneously.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsCheck | 2.16.5 | Property-based tests for gravity + 4-in-a-row invariants | Locked decision from Phase 2; FsCheck 3.x incompatible |
| Expecto | 10.2.3 | Unit/integration tests — Alpha-Beta vs Minimax equivalence | Locked from Phase 2 |
| YoloDev.Expecto.TestSdk | 0.15.5 | `dotnet test` test discovery for Expecto | Locked from Phase 2 |
| Serilog | 4.3.1 | Structured logging of Q-values, match results | Locked from Phase 2 |
| Serilog.Sinks.Console | 6.1.1 | Console output | Locked |
| Serilog.Sinks.File | 7.0.0 | File sink for training logs | Locked |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.NET.Test.Sdk | 18.0.1 | Required by `dotnet test` runner | Always, in test project |
| Expecto.FsCheck | 10.2.3 | Expecto integration for FsCheck (testProperty combinator) | When using `testProperty` inside Expecto test lists |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `Dictionary<string, float[]>` Q-table | `Map<Board, float[]>` | Map is immutable and thread-safe but ~10× slower for training; Dictionary is imperative but essential for large episode counts |
| Flat array board `Cell[]` | 2D array `Cell[,]` | 2D arrays are idiomatic for grids but `Array2D` in F# lacks `Array.map`/`Array.indexed`; flat array with row-major indexing matches Phase 2 pattern exactly |
| String hash key for Q-table | Custom `Board` equality | String keys are zero-effort, debuggable, and fast enough; custom equality requires implementing `IEqualityComparer` |
| Negamax (single-function minimax) | Separate max/min functions | Negamax halves the code; separate functions are easier to teach but harder to maintain |

**Installation:**
```bash
# In ConnectFour.sln directory:
dotnet new sln --format sln -n ConnectFour
dotnet new classlib -lang F# -n ConnectFour -o src/ConnectFour
dotnet new classlib -lang F# -n ConnectFour.Console -o src/ConnectFour.Console
dotnet new classlib -lang F# -n ConnectFour.Tests -o tests/ConnectFour.Tests

# Packages for src/ConnectFour (pure engine — no packages needed)
# Packages for src/ConnectFour.Console:
dotnet add src/ConnectFour.Console/ConnectFour.Console.fsproj package Serilog --version 4.3.1
dotnet add src/ConnectFour.Console/ConnectFour.Console.fsproj package Serilog.Sinks.Console --version 6.1.1
dotnet add src/ConnectFour.Console/ConnectFour.Console.fsproj package Serilog.Sinks.File --version 7.0.0

# Packages for tests/ConnectFour.Tests:
dotnet add tests/ConnectFour.Tests/ConnectFour.Tests.fsproj package FsCheck --version 2.16.5
dotnet add tests/ConnectFour.Tests/ConnectFour.Tests.fsproj package Expecto --version 10.2.3
dotnet add tests/ConnectFour.Tests/ConnectFour.Tests.fsproj package Expecto.FsCheck --version 10.2.3
dotnet add tests/ConnectFour.Tests/ConnectFour.Tests.fsproj package Microsoft.NET.Test.Sdk --version 18.0.1
dotnet add tests/ConnectFour.Tests/ConnectFour.Tests.fsproj package YoloDev.Expecto.TestSdk --version 0.15.5
```

---

## Architecture Patterns

### Recommended Project Structure
```
ConnectFour/
├── ConnectFour.sln                    # Traditional .sln format
├── src/
│   ├── ConnectFour/
│   │   ├── ConnectFour.fsproj         # Pure library — no I/O, no Serilog
│   │   ├── Domain.fs                  # Types: Cell, Board, GameState, Player
│   │   ├── Rules.fs                   # Gravity, dropPiece, checkWinner, legalMoves
│   │   ├── Minimax.fs                 # minimaxAB, scoreBoard, evaluateWindow
│   │   └── QAgent.fs                  # QTable module, encodeState, qLearn, chooseAction
│   └── ConnectFour.Console/
│       ├── ConnectFour.Console.fsproj  # Exe, references ConnectFour.fsproj
│       ├── Training.fs                # playEpisode, trainQLearning, runMatchup
│       └── Program.fs                 # Serilog setup, menu, main
└── tests/
    └── ConnectFour.Tests/
        ├── ConnectFour.Tests.fsproj   # GenerateProgramFile=false required
        ├── PropertyTests.fs           # FsCheck: gravity + 4-in-a-row invariants
        ├── MinimaxTests.fs            # Expecto: Alpha-Beta == Minimax for small boards
        └── Main.fs                    # [<EntryPoint>] runTestsWithCLIArgs
```

### Pattern 1: Flat Array Board with Gravity (Domain + Rules)

**What:** 6×7 board stored as 42-element `Cell array`, row 0 = top. Gravity implemented by scanning from row 5 (bottom) upward to find lowest empty cell in a column.

**When to use:** Mirroring Phase 2 flat-array approach; avoids `Array2D` API inconsistencies in F#.

```fsharp
// Domain.fs
module ConnectFour.Domain

type Cell = Empty | Red | Yellow

type Board = Cell array  // length 42, row-major: index = row * 7 + col

type GameState = {
    Board: Board
    CurrentPlayer: Cell  // Red or Yellow; Empty never valid
}

let rows = 6
let cols = 7

let emptyBoard () : Board = Array.create (rows * cols) Empty

let initialState () : GameState =
    { Board = emptyBoard (); CurrentPlayer = Red }

let inline idx row col = row * cols + col
```

```fsharp
// Rules.fs
module ConnectFour.Rules

open ConnectFour.Domain

/// Gravity: returns Some row (bottom-most empty row in col), or None if full
let dropRow (board: Board) (col: int) : int option =
    [ rows - 1 .. -1 .. 0 ]
    |> List.tryFind (fun row -> board.[idx row col] = Empty)

/// Legal columns: columns where at least one empty cell exists
let legalMoves (board: Board) : int list =
    [ 0 .. cols - 1 ]
    |> List.filter (fun col -> dropRow board col |> Option.isSome)

/// Apply column move for player; returns new board (immutable)
let applyMove (board: Board) (player: Cell) (col: int) : Board =
    match dropRow board col with
    | None -> failwith $"applyMove: column {col} is full"
    | Some row ->
        board |> Array.mapi (fun i c -> if i = idx row col then player else c)

/// Check if 4 in a row for given cell at (row, col) with direction (dr, dc)
let private checkDirection (board: Board) (r: int) (c: int) (dr: int) (dc: int) (player: Cell) : bool =
    [ 0..3 ]
    |> List.forall (fun k ->
        let nr = r + k * dr
        let nc = c + k * dc
        nr >= 0 && nr < rows && nc >= 0 && nc < cols && board.[idx nr nc] = player)

/// Check winner: returns Some Cell (Red/Yellow) or None
let checkWinner (board: Board) : Cell option =
    let directions = [(0,1); (1,0); (1,1); (1,-1)]  // horiz, vert, diag/, diag\
    [ for r in 0..rows-1 do
      for c in 0..cols-1 do
      for (dr,dc) in directions do
        yield (r, c, dr, dc) ]
    |> List.tryPick (fun (r, c, dr, dc) ->
        let cell = board.[idx r c]
        if cell <> Empty && checkDirection board r c dr dc cell
        then Some cell
        else None)

type GameResult = RedWins | YellowWins | Draw

let isGameOver (board: Board) : GameResult option =
    match checkWinner board with
    | Some Red    -> Some RedWins
    | Some Yellow -> Some YellowWins
    | Some Empty  -> None
    | None ->
        if Array.forall (fun c -> c <> Empty) board then Some Draw
        else None
```

### Pattern 2: Minimax with Alpha-Beta (Negamax style)

**What:** Negamax formulation — a single function that always returns score from the **current player's perspective**. Score is negated when recursing. Alpha-Beta prunes branches using `α` (best current player can guarantee) and `β` (best opponent can guarantee = worst for current player).

**When to use:** Negamax is standard for two-player zero-sum games; it halves code vs separate max/min functions and is less error-prone.

**Evaluation function design** (the key concern flagged in STATE.md):
- Score windows of 4 cells in all directions
- Per window: count current player's pieces (p) and opponent's pieces (o) in the window
- A window is "alive" (can still form 4) only if `o = 0` (not blocked by opponent)
- Scoring: alive window with p=3 → +50, p=2 → +3, p=1 → +1; blocked → 0
- Terminal: win → +10000 (adjusted by depth for faster wins), draw → 0
- Center column preference: pieces in column 3 get +3 bonus

```fsharp
// Minimax.fs
module ConnectFour.Minimax

open ConnectFour.Domain
open ConnectFour.Rules

/// Score a window of 4 cells from current player's perspective
let private scoreWindow (window: Cell list) (player: Cell) : int =
    let opp = if player = Red then Yellow else Red
    let p = window |> List.filter ((=) player) |> List.length
    let o = window |> List.filter ((=) opp) |> List.length
    if o > 0 then 0  // blocked window
    else
        match p with
        | 4 -> 10000
        | 3 -> 50
        | 2 -> 3
        | 1 -> 1
        | _ -> 0

/// Extract all windows of size 4 in all directions
let private allWindows (board: Board) : (Cell list * Cell list) list =
    // Returns pairs: (window cells for Red, window cells for Yellow) -- actually returns raw windows
    // Caller scores relative to current player
    [ // Horizontal
      for r in 0..rows-1 do
        for c in 0..cols-4 do
          yield [ board.[idx r c]; board.[idx r (c+1)]; board.[idx r (c+2)]; board.[idx r (c+3)] ]
      // Vertical
      for c in 0..cols-1 do
        for r in 0..rows-4 do
          yield [ board.[idx r c]; board.[idx (r+1) c]; board.[idx (r+2) c]; board.[idx (r+3) c] ]
      // Diagonal \
      for r in 0..rows-4 do
        for c in 0..cols-4 do
          yield [ board.[idx r c]; board.[idx (r+1) (c+1)]; board.[idx (r+2) (c+2)]; board.[idx (r+3) (c+3)] ]
      // Diagonal /
      for r in 3..rows-1 do
        for c in 0..cols-4 do
          yield [ board.[idx r c]; board.[idx (r-1) (c+1)]; board.[idx (r-2) (c+2)]; board.[idx (r-3) (c+3)] ]
    ]

/// Heuristic board evaluation from current player's perspective
let evaluateBoard (board: Board) (player: Cell) : int =
    let opp = if player = Red then Yellow else Red
    let windows = allWindows board
    let score =
        windows |> List.sumBy (fun w ->
            scoreWindow w player - scoreWindow w opp)
    // Center column preference
    let centerBonus =
        [ 0..rows-1 ]
        |> List.filter (fun r -> board.[idx r 3] = player)
        |> List.length
        |> (*) 3
    score + centerBonus

/// Negamax with Alpha-Beta pruning
/// Returns score from current player's perspective
/// pruneCount: mutable counter for statistics
let rec minimaxAB
    (board: Board)
    (player: Cell)
    (depth: int)
    (alpha: int)
    (beta: int)
    (pruneCount: int ref) : int =

    match isGameOver board with
    | Some result ->
        match result with
        | RedWins    -> if player = Red    then 10000 + depth else -(10000 + depth)
        | YellowWins -> if player = Yellow then 10000 + depth else -(10000 + depth)
        | Draw       -> 0
    | None when depth = 0 ->
        evaluateBoard board player
    | None ->
        let moves = legalMoves board
        let opp = if player = Red then Yellow else Red
        // Move ordering: center columns first
        let orderedMoves = moves |> List.sortBy (fun c -> abs (c - 3))
        let rec loop moves alpha bestScore =
            match moves with
            | [] -> bestScore
            | col :: rest ->
                let nextBoard = applyMove board player col
                let childScore = -(minimaxAB nextBoard opp (depth-1) (-beta) (-alpha) pruneCount)
                let newBest = max bestScore childScore
                let newAlpha = max alpha newBest
                if newAlpha >= beta then
                    pruneCount.Value <- pruneCount.Value + 1
                    newBest  // Beta cutoff (pruned)
                else
                    loop rest newAlpha newBest
        loop orderedMoves alpha System.Int32.MinValue

/// Choose best column for player using Minimax+Alpha-Beta
let chooseMoveAB (board: Board) (player: Cell) (depth: int) : int * int =
    let pruneCount = ref 0
    let moves = legalMoves board
    let opp = if player = Red then Yellow else Red
    let orderedMoves = moves |> List.sortBy (fun c -> abs (c - 3))
    let scored =
        orderedMoves
        |> List.map (fun col ->
            let nextBoard = applyMove board player col
            let score = -(minimaxAB nextBoard opp (depth-1) System.Int32.MinValue System.Int32.MaxValue pruneCount)
            col, score)
    let bestMove = scored |> List.maxBy snd |> fst
    bestMove, pruneCount.Value
```

### Pattern 3: Q-Learning with Feature-Based State Encoding

**What:** Use a string hash of the raw board as the dictionary key. The Q-table is `Dictionary<string, float[]>` mapping board state → array of 7 Q-values (one per column). This is NOT function approximation — it IS tabular Q-Learning, but using raw board state means the table grows large rapidly (demonstrating the limitation).

**Design rationale for the "limitation demonstration":**
- The full Connect Four state space has ~4.5 trillion positions. Storing all visited states as strings in a Dictionary is feasible during training (only visited states are stored) but the table grows large and the agent cannot generalize to unvisited states.
- After training, explicitly report: `Dictionary.Count` entries visited vs `4,531,985,219,092` total possible states
- This directly motivates Phase 4 DQN: neural networks generalize across unvisited states

**Q-update:** Standard Q-learning (not TD(0) value table from Phase 2):
```
Q(s, a) ← Q(s, a) + α × [r + γ × max_a'(Q(s', a')) − Q(s, a)]
```

```fsharp
// QAgent.fs
module ConnectFour.QAgent

open ConnectFour.Domain
open ConnectFour.Rules

/// Encode board state as string key for Dictionary
let encodeState (board: Board) : string =
    board
    |> Array.map (fun c -> match c with Red -> 'R' | Yellow -> 'Y' | Empty -> '.')
    |> System.String

/// Q-table: state key → float array of length 7 (one Q-value per column)
type QTable = System.Collections.Generic.Dictionary<string, float[]>

let private defaultQValues () = Array.create cols 0.0

/// Get Q-values for state (creates entry if missing)
let getQ (table: QTable) (state: string) : float[] =
    match table.TryGetValue(state) with
    | true, values -> values
    | false, _ ->
        let values = defaultQValues ()
        table.[state] <- values
        values

/// Choose action epsilon-greedy; invalid columns get Q = -infinity
let chooseAction (rng: System.Random) (table: QTable) (board: Board) (epsilon: float) : int =
    let valid = legalMoves board |> Set.ofList
    if rng.NextDouble() < epsilon then
        // Random legal move
        let moves = Set.toArray valid
        moves.[rng.Next(moves.Length)]
    else
        let state = encodeState board
        let qVals = getQ table state
        // Among legal columns only, pick max Q
        [ 0..cols-1 ]
        |> List.filter (fun c -> Set.contains c valid)
        |> List.maxBy (fun c -> qVals.[c])

/// Q-learning update: Q(s,a) ← Q(s,a) + α×(r + γ×maxQ(s') - Q(s,a))
let updateQ (table: QTable) (state: string) (action: int) (reward: float) (nextState: string) (alpha: float) (gamma: float) (isTerminal: bool) =
    let qCurr = getQ table state
    let nextMax =
        if isTerminal then 0.0
        else
            let qNext = getQ table nextState
            Array.max qNext
    let target = reward + gamma * nextMax
    qCurr.[action] <- qCurr.[action] + alpha * (target - qCurr.[action])
    // No need to re-insert: Dictionary value is a mutable array (reference type)
```

**Reward structure:**
- Win: +1.0
- Loss: -1.0
- Draw: +0.3
- Intermediate move: 0.0 (sparse rewards only)

**Training parameters:**
- Episodes: 50,000–100,000 (less than Phase 2 TicTacToe since Q-table grows large)
- α (learning rate): 0.1
- γ (discount): 0.95 (longer-horizon game than TicTacToe)
- ε (epsilon): 0.15, decaying toward 0.05 over training
- Opponent during training: random agent (simpler to implement and train against)

### Pattern 4: FsCheck Property Tests for Connect Four

**Gravity invariants to test:**
1. After `applyMove board player col`, the piece at the returned row is exactly `player`
2. After `applyMove`, no piece "floats" (every non-empty cell has a non-empty cell below it or is in row 5)
3. `legalMoves` returns only columns 0–6
4. A full column is not in `legalMoves`
5. `legalMoves` count decreases by 0 or 1 after each move (column may still have space)

**4-in-a-row invariants to test:**
1. On a freshly emptied board, `checkWinner` returns None
2. After placing 4 in a row manually (horizontal/vertical/diagonal), `checkWinner` returns Some player
3. `isGameOver` on a full board with no winner returns Some Draw

**FsCheck custom generator for valid boards:**
```fsharp
// Generate a valid board by simulating random games of k moves
let genValidBoard (maxMoves: int) =
    gen {
        let! k = Gen.choose (0, maxMoves)
        let rng = System.Random()
        let board = ref (ConnectFour.Domain.emptyBoard ())
        let player = ref ConnectFour.Domain.Red
        for _ in 1..k do
            let moves = ConnectFour.Rules.legalMoves !board
            if not moves.IsEmpty then
                let col = moves.[rng.Next(moves.Length)]
                board := ConnectFour.Rules.applyMove !board !player col
                player := if !player = Red then Yellow else Red
        return !board
    }
```

### Pattern 5: Expecto Tests for Alpha-Beta Equivalence

**What:** Verify that `minimaxAB` (with α=-∞, β=+∞) produces the same best-move choice as a naive minimax (without pruning) on small boards or early-game positions.

**Strategy:** Run both on the same position at depth 4–5, assert the chosen column matches. This is the CNCT-07 requirement.

```fsharp
[<Tests>]
let minimaxTests =
    testList "Expecto Minimax/Alpha-Beta equivalence" [
        testCase "Alpha-Beta agrees with full Minimax on empty board depth 4" <| fun () ->
            let board = emptyBoard ()
            let pruneCount = ref 0
            let abMove, pruned = chooseMoveAB board Red 4
            // Naive minimax (pruneCount always 0, full search)
            // Both should choose center column (col 3) on empty board
            Expect.equal abMove 3 "Both should choose center column"
            Expect.isGreaterThan pruned 0 "Alpha-Beta should prune at least one branch"

        testCase "Alpha-Beta agrees with full Minimax when immediate win available" <| fun () ->
            // Set up a position where Red has 3 in a row horizontally and can win with col 3
            let board = emptyBoard ()
            let board' = applyMove board Red 0
            let board'' = applyMove board' Yellow 6
            let board3 = applyMove board'' Red 1
            let board4 = applyMove board3 Yellow 6
            let board5 = applyMove board4 Red 2
            // Red can win by playing col 3
            let abMove, _ = chooseMoveAB board5 Red 5
            Expect.equal abMove 3 "Alpha-Beta should find the winning move"
    ]
```

### Anti-Patterns to Avoid

- **Using `Array2D` for the board:** `Array2D` in F# lacks `Array.map`, `Array.indexed`, `Array.forall`. Use flat `Cell[]` with `idx row col = row * 7 + col`.
- **Mutable board state in Minimax:** Minimax must use immutable board copies. Using a single mutable board with undo-move is fast but error-prone in F#. Use `Array.mapi` for immutable copy-on-move.
- **Storing Q-values in `Map` (immutable):** For 50k+ episodes, `Map` updates are too slow. Use `Dictionary<string, float[]>` with mutable float arrays.
- **Blocking column that is full:** `applyMove` must check `dropRow` returns `Some`; FsCheck tests must use `legalMoves` to filter columns before generating moves.
- **Minimax depth too deep at root:** Depth 8 explores up to 7^8 ≈ 5.7M nodes before pruning. On empty board, this takes >1 second. Use depth 6 for AI vs AI matches; depth 7–8 only for human vs AI (user is waiting anyway).
- **Not negating score in negamax:** The critical negamax invariant: `childScore = -(minimaxAB nextBoard opp ...)`. Forgetting the negation produces an agent that tries to lose.
- **Using `System.Int32.MinValue` as initial alpha:** Negating `Int32.MinValue` overflows to itself in F#. Use `-1_000_000` as effective negative infinity.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Diagonal win detection | Custom direction iterator | Standard `checkDirection board r c dr dc` with 4 direction tuples | Off-by-one errors are common; the (1,-1) diagonal is frequently wrong |
| Q-table persistence | Custom serialization | `System.Text.Json` or simply report stats and discard (training is fast enough to re-run) | Not required by CNCT-08; Serilog logs are sufficient |
| Move ordering for Alpha-Beta | Complex history heuristic | Simple `List.sortBy (fun c -> abs(c - 3))` (center-first) | Center-first ordering reduces nodes explored by 60–80% for Connect Four; complex history heuristics add code complexity for marginal gain at depth 6 |
| Custom FsCheck shrinker for boards | Minimal counter-example shrinking | Default `Arb.fromGen` with no shrinker | Board shrinkers are complex to write; for educational tests, finding _any_ counter-example is sufficient |
| Transposition table | `Dictionary<Board, (int*int)>` for caching | Skip for depth ≤ 8 | Transposition tables significantly complicate code; at depth 6–8 alpha-beta is fast enough without them (~100ms per move) |

**Key insight:** Connect Four's apparent complexity (4.5 trillion states) is manageable for Minimax via alpha-beta (depth-limited tree search) but completely intractable for tabular Q-Learning (cannot enumerate the table). This asymmetry IS the educational point — do not try to "fix" Q-Learning with DQN in this phase (that is Phase 4's job).

---

## Common Pitfalls

### Pitfall 1: Integer Overflow in Alpha-Beta
**What goes wrong:** Using `System.Int32.MinValue` as initial `bestScore` and then negating it (for negamax) produces `System.Int32.MinValue` again (overflow).
**Why it happens:** `Int32.MinValue = -2147483648`; `-Int32.MinValue` overflows to `Int32.MinValue` in unchecked arithmetic.
**How to avoid:** Use `-1_000_000` as effective negative infinity and `1_000_000` as positive infinity. Win scores of ±10000 fit comfortably.
**Warning signs:** AI consistently plays into losing positions or throws `OverflowException` in debug builds.

### Pitfall 2: Q-Learning Opponent Perspective in Two-Player Game
**What goes wrong:** Q-values from Red's perspective are used to update Yellow's Q-values (or vice versa), causing the agent to learn to lose.
**Why it happens:** In two-player games, the state after Red moves is evaluated from Yellow's perspective on the next turn. The reward signal must flip sign.
**How to avoid:** Use separate Q-tables for Red and Yellow, OR always encode the board from the "current player's perspective" (flip Red/Yellow labels so current player is always "self"). The simpler approach for this educational phase: two separate Q-tables, one per player, both updated via self-play.
**Warning signs:** Q-Learning agent wins rate stays near 0% throughout training.

### Pitfall 3: Gravity Bug — Pieces "Float"
**What goes wrong:** A piece is placed at the wrong row, leaving an empty cell below it.
**Why it happens:** `dropRow` scans `[rows-1 .. -1 .. 0]` (bottom to top); if the range is accidentally `[0..rows-1]` (top to bottom), pieces land at the top instead of bottom.
**How to avoid:** FsCheck gravity invariant test: after any `applyMove`, for every non-empty cell at row `r` where `r < rows-1`, the cell at `r+1` is also non-empty.
**Warning signs:** FsCheck "no piece floats" property fails; visual board display shows gaps below pieces.

### Pitfall 4: 4-in-a-Row Includes Wrong Diagonals
**What goes wrong:** One of the two diagonal directions is missed or implemented with wrong signs, so diagonal wins are not detected.
**Why it happens:** The "anti-diagonal" (bottom-left to top-right) uses `(dr=1, dc=-1)` or equivalently `(dr=-1, dc=1)`. The start position bounds must be `r in 3..rows-1, c in 0..cols-4` for one direction and `r in 0..rows-4, c in 0..cols-4` for the other.
**How to avoid:** Test all four win directions explicitly in Expecto unit tests with manually constructed boards.
**Warning signs:** Agent seems oblivious to diagonal threats; games that should end don't.

### Pitfall 5: FsCheck `testProperty` Inside Expecto Requires `Expecto.FsCheck` Package
**What goes wrong:** `testProperty` function not found; compilation error.
**Why it happens:** `testProperty` is in `Expecto.ExpectoFsCheck` namespace from the `Expecto.FsCheck` NuGet package, not from `Expecto` itself.
**How to avoid:** Add `PackageReference Include="Expecto.FsCheck" Version="10.2.3"` and `open Expecto.ExpectoFsCheck` in test files.
**Warning signs:** Compilation error: `The value or constructor 'testProperty' is not defined`.

### Pitfall 6: `GenerateProgramFile=false` Missing in Test Project
**What goes wrong:** `dotnet test` fails with "multiple entry points" error.
**Why it happens:** When test project uses `[<EntryPoint>]` in `Main.fs`, .NET SDK also auto-generates a `Program.fs`; the collision causes build failure.
**How to avoid:** Always add `<GenerateProgramFile>false</GenerateProgramFile>` to the test project's `<PropertyGroup>`.
**Warning signs:** Build error mentioning `duplicate 'main'` or `Program.fs` conflict.

### Pitfall 7: Q-Table Key Collisions from Symmetric Boards
**What goes wrong:** Boards that are horizontal mirrors produce different string keys, so the agent cannot generalize between them; training takes 2× as many episodes.
**Why it happens:** Raw board encoding includes left-right position; column 0 and column 6 are treated as completely different.
**How to avoid:** For this educational phase, accept the limitation — it is part of the Q-table scalability story. Document it explicitly in the mdBook chapter as evidence that the agent requires exponentially more training data than Minimax.
**Warning signs:** This is expected behavior, not a bug. Do not try to add symmetry normalization (adds complexity without supporting the educational narrative).

---

## Code Examples

Verified patterns from prior phases and research:

### Solution File Creation (Prior Decision: Traditional .sln Format)
```bash
# In ConnectFour/ directory:
dotnet new sln --format sln -n ConnectFour
# Note: --format sln creates traditional .sln (not .slnx); required for net10.0 compatibility
```

### Test Project fsproj (Critical Settings)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <GenerateProgramFile>false</GenerateProgramFile>  <!-- CRITICAL: prevent duplicate main -->
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="PropertyTests.fs" />
    <Compile Include="MinimaxTests.fs" />
    <Compile Include="Main.fs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ConnectFour\ConnectFour.fsproj" />
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

### Negamax Safe Bounds (avoids Int32.MinValue overflow)
```fsharp
// Use these constants instead of System.Int32.Min/MaxValue:
let [<Literal>] NegInf = -1_000_000
let [<Literal>] PosInf =  1_000_000
// Win/loss scores of ±10000 fit well within these bounds
```

### Q-Update with Reward Perspective Flip (Two-Player Self-Play)
```fsharp
// Training.fs: After Red plays col, get reward, then Yellow plays
// The NEXT state's Q-values are from Yellow's perspective.
// Use separate tables: redTable, yellowTable
let updatePlayer (table: QTable) (state: string) (col: int) (reward: float) (nextState: string) (isTerminal: bool) =
    updateQ table state col reward nextState 0.1 0.95 isTerminal
// Red reward = +1 for Red win; Yellow reward = +1 for Yellow win (opposite of Red's reward)
```

### Serilog Structured Logging Pattern (Consistent with Phase 2)
```fsharp
// Program.fs
Log.Logger <-
    LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
        .WriteTo.File("logs/connectfour-.log",
            rollingInterval = RollingInterval.Day,
            outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
        .CreateLogger()

// Log match result with statistics
Log.Information("Match {GameNum}: Winner={Winner} Moves={Moves} PruneCount={PruneCount}",
    gameNum, winner, moveCount, totalPrunes)

// Log Q-table growth
Log.Information("Episode={Episode} QTableSize={Size} Epsilon={Epsilon:F3}",
    ep, table.Count, currentEpsilon)
```

### Alpha-Beta Statistics Output Pattern
```fsharp
// Track pruning across a full AI vs AI match
let mutable totalPrunes = 0
let totalPrunes = ref 0
// In chooseMoveAB: return (bestCol, pruneCount.Value)
// In match loop: totalPrunes := !totalPrunes + pruned

printfn "\n=== AI vs AI Match Results ==="
printfn "Minimax wins: %d / %d" minimaxWins totalGames
printfn "Q-Learning wins: %d / %d" qWins totalGames
printfn "Total Alpha-Beta prunes: %d" !totalPrunes
printfn "Q-Table states visited: %d / 4,531,985,219,092 possible" table.Count
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Separate max/min functions in minimax | Negamax (single recursive function, negate score) | Well-established by 2000s | Halves code, eliminates sign bugs |
| Full minimax without pruning | Alpha-Beta with move ordering | Well-established | 10–100× node reduction; center-first ordering adds another 2–5× |
| Raw 3^42 Q-table | Feature-based approximation or DQN | DQN: 2013 (Atari paper) | Q-table for full Connect Four is physically impossible; DQN generalizes |
| Transposition tables | Still relevant, but complex | N/A | Skip for depth ≤ 8; add only if depth > 8 needed |
| Manual board state encoding | String-based hash key | N/A (educational simplification) | Debuggable; performance acceptable for 50–100k episodes |

**Deprecated/outdated:**
- Full minimax (no alpha-beta): Only useful for verifying alpha-beta correctness in tests (which is exactly CNCT-07's purpose)
- Linear function approximation for Q-Learning: Neither needed nor beneficial here; the educational narrative requires demonstrating tabular Q-Learning's raw failure mode

---

## Open Questions

1. **Q-Learning training depth: when does it plateau?**
   - What we know: With 50k episodes of self-play vs random, Q-Learning on a 5×4 board achieves ~60–70% win rate vs random; full 6×7 board with 100k episodes likely achieves 40–60%
   - What's unclear: Whether Q-Learning will achieve a win rate that is "demonstrably worse" than Minimax in a fixed number of episodes; if both achieve 70%+ vs random, the contrast is weak
   - Recommendation: Use Minimax as the Q-Learning opponent during evaluation (not random). A trained Q-agent vs depth-4 Minimax should show a clear win-rate gap (~20–40% for Q-agent) that motivates DQN

2. **Minimax depth for human vs AI mode**
   - What we know: Depth 6 takes <100ms per move after alpha-beta pruning; depth 8 takes 200–500ms
   - What's unclear: Whether depth 6 is "hard enough" to be interesting for a human player
   - Recommendation: Use depth 6 for AI vs AI (speed), depth 7 for human vs AI (quality); make depth a configurable parameter

3. **Q-table size after training**
   - What we know: Connect Four has ~4.5 trillion total positions; after 100k episodes, only a fraction are visited; Dictionary will likely hold 100k–1M entries
   - What's unclear: Exact count without running the experiment
   - Recommendation: Log `table.Count` every 10k episodes in Serilog; report final count prominently in console output AND in mdBook chapter as concrete evidence

4. **FsCheck valid board generator complexity**
   - What we know: Generating gravity-valid boards by simulating random games works correctly
   - What's unclear: Whether FsCheck's default 100 test iterations are sufficient to find edge cases (e.g., full column, terminal states)
   - Recommendation: Run FsCheck with `config = { Config.Quick with MaxTest = 500 }` for the gravity invariants; the generator is fast enough

---

## Sources

### Primary (HIGH confidence)
- Prior phase implementations (Phase 1 Bandit, Phase 2 TicTacToe) — patterns directly reused
- Cornell CS312 lecture notes on minimax/alpha-beta — pseudocode verified
- FsCheck official documentation (fscheck.github.io/FsCheck/TestData.html) — Arbitrary/generator patterns
- Score4 F# implementation (thanassis.space/score4.html) — F# board type, minimax structure
- Connect Four Robot documentation (roboticsproject.readthedocs.io) — evaluation function window scoring

### Secondary (MEDIUM confidence)
- WebSearch: Connect Four state space ~4.5 trillion positions — confirmed by multiple academic sources
- WebSearch: Q-table size for Connect Four (3^42 × 7) — confirmed by multiple sources
- deepexploration.org/blog — evaluation function scoring weights (9/50/3/1 for 3/3/2/1 pieces)
- gamesolver.org blog — move ordering center-first optimization, pruning statistics

### Tertiary (LOW confidence)
- Specific win-rate numbers for Q-Learning vs Minimax — no definitive benchmark found for tabular Q-Learning on full 6×7 board; all practical implementations use DQN
- Optimal epsilon decay schedule for Connect Four Q-Learning — standard RL guidance only

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages locked from Phase 2 decisions
- Architecture: HIGH — mirrors Phase 2 structure with clear additions
- Game engine (Domain + Rules): HIGH — straightforward extension of TicTacToe patterns
- Minimax/Alpha-Beta: HIGH — well-documented algorithm with F# functional style clearly derivable
- Q-Learning feature design: MEDIUM — tabular Q-Learning on full Connect Four is unusual (most literature skips to DQN); string key approach is pragmatic but not literature-verified
- FsCheck gravity tests: HIGH — standard FsCheck patterns apply directly
- Win rate comparisons: LOW — specific numbers not verifiable without running experiments

**Research date:** 2026-02-20
**Valid until:** 2026-03-22 (packages are stable; algorithm knowledge doesn't expire)
