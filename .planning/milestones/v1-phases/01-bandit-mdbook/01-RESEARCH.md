# Phase 1: Bandit + mdBook 기반 - Research

**Researched:** 2026-02-19
**Domain:** F# .NET 9 console app + Expecto/FsCheck property testing + Serilog logging + mdBook static site
**Confidence:** HIGH

---

## Summary

Phase 1 establishes the entire project foundation: an independent F# solution implementing Multi-Armed Bandit RL algorithms (ε-greedy, UCB1) with property-based and convergence tests, plus an mdBook documentation site. The stack is fully decided — .NET 9 / F# 9, Expecto 10.2.3 + FsCheck 3.3.2 for testing, Serilog 4.3.1 for logging, and mdBook 0.4.52 for the tutorial site.

The core architectural pattern is the **Functional Core / Imperative Shell** split: Domain.fs and Rules.fs are purely functional (no side effects, no exceptions — use Result/Option types), while Training.fs and Program.fs handle all I/O, randomness, and logging. F# requires strict source file ordering in .fsproj — files must appear in dependency order (Domain.fs first, Program.fs last). This is the most common F# pitfall for .NET developers coming from C#.

mdBook 0.4.52 is a Rust-based static site generator used by the Rust project itself. It compiles Markdown into a searchable HTML book. The SUMMARY.md file is the authoritative table of contents — its indented link structure defines chapter hierarchy. For this phase, the book needs a single chapter under `src/01-bandit/` introducing the Bandit problem.

**Primary recommendation:** Structure the solution as `Bandit.sln` → `src/Bandit/` (main library) + `src/Bandit.Console/` (entrypoint) + `tests/Bandit.Tests/` (Expecto console runner). All pure logic lives in the Bandit library; Serilog and console I/O live only in Bandit.Console.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 9.x | Runtime + build toolchain | Locked decision; LTS, latest stable |
| F# | 9.x | Language | Locked decision; functional-first, .NET native |
| Expecto | 10.2.3 | Test runner (values-as-tests) | Locked decision; idiomatic F# testing, parallel by default |
| Expecto.FsCheck | 10.2.3 | Property-based testing bridge | Same version as Expecto; wraps FsCheck into testProperty |
| FsCheck | 3.3.2 | Property generation + shrinking | Locked decision; standard F# PBT library |
| Serilog | 4.3.1 | Structured logging core | Locked decision; industry standard for .NET |
| Serilog.Sinks.Console | 6.1.1 | Console log output | Locked decision |
| Serilog.Sinks.File | 7.0.0 | File log output | Locked decision |
| mdBook | 0.4.52 | Tutorial static site generator | Locked decision; Rust-based, no JS build chain |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto.BenchmarkDotNet | 10.x | Perf benchmarks | Only if performance comparison is needed (not in Phase 1) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Expecto | xUnit + FsUnit | Expecto is more idiomatic F# — tests are values, not attributes |
| FsCheck 3.x | Hedgehog | FsCheck has better Expecto integration via Expecto.FsCheck package |
| Serilog | Microsoft.Extensions.Logging | Serilog has structured events and better F# interop |
| mdBook | Docusaurus | mdBook requires no Node.js; simpler Markdown-first workflow |

**Installation (dotnet):**
```bash
dotnet add package Expecto --version 10.2.3
dotnet add package Expecto.FsCheck --version 10.2.3
dotnet add package FsCheck --version 3.3.2
dotnet add package Serilog --version 4.3.1
dotnet add package Serilog.Sinks.Console --version 6.1.1
dotnet add package Serilog.Sinks.File --version 7.0.0
```

**Installation (mdBook):**
```bash
cargo install mdbook --version 0.4.52
# or via pre-built binary from https://github.com/rust-lang/mdBook/releases
```

---

## Architecture Patterns

### Recommended Project Structure

```
Bandit.sln
├── src/
│   ├── Bandit/                    # Pure library — no side effects
│   │   ├── Bandit.fsproj
│   │   ├── Domain.fs              # Types: Arm, AgentState, BanditEnv
│   │   ├── Environment.fs         # SlotMachine: pull arm → reward (pure, seeded)
│   │   ├── Agent.fs               # epsilonGreedy, ucb1, incrementalMean
│   │   └── Training.fs            # runEpisode, compareStrategies (pure loops)
│   └── Bandit.Console/            # Impure shell — I/O only
│       ├── Bandit.Console.fsproj
│       └── Program.fs             # Serilog setup, print results, [<EntryPoint>]
├── tests/
│   └── Bandit.Tests/              # Expecto console runner
│       ├── Bandit.Tests.fsproj
│       ├── PropertyTests.fs       # FsCheck: reward sum invariants
│       └── ConvergenceTests.fs    # Expecto: 1000-step optimal arm convergence
└── docs/
    └── book/                      # mdBook source
        ├── book.toml
        └── src/
            ├── SUMMARY.md
            └── 01-bandit/
                └── README.md      # Chapter 1 content (Korean)
```

### Pattern 1: Functional Core / Imperative Shell

**What:** All RL logic is pure F# functions with no side effects. The "shell" (Program.fs) handles randomness seeding, Serilog logging, and console output. Pure functions receive `System.Random` as a parameter rather than calling it internally.

**When to use:** Always. XCUT-03 mandates this split.

**Example:**
```fsharp
// Source: architecture decision + F# functional core pattern
// Domain.fs — pure types
type Arm = int
type AgentState = { Counts: int array; Values: float array }
type BanditEnv = { RewardProbs: float array }

// Environment.fs — pure (Random passed in)
let pullArm (rng: System.Random) (env: BanditEnv) (arm: Arm) : float =
    if rng.NextDouble() < env.RewardProbs.[arm] then 1.0 else 0.0

// Agent.fs — pure strategy functions
let incrementalMean (state: AgentState) (arm: Arm) (reward: float) : AgentState =
    let n = state.Counts.[arm] + 1
    let oldVal = state.Values.[arm]
    let newVal = oldVal + (1.0 / float n) * (reward - oldVal)
    { state with
        Counts = state.Counts |> Array.mapi (fun i c -> if i = arm then n else c)
        Values = state.Values |> Array.mapi (fun i v -> if i = arm then newVal else v) }

// Program.fs — impure shell
let main args =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("bandit.log")
            .CreateLogger()
    let rng = System.Random(42)
    // ... call pure functions, log results
```

### Pattern 2: Result/Option for Error Handling (XCUT-01)

**What:** Never throw exceptions. Use `Result<'T, 'Err>` for operations that can fail, `Option<'T>` for optional values.

**When to use:** Everywhere in pure functions. Exceptions are allowed only in Program.fs for unrecoverable startup errors.

**Example:**
```fsharp
// Source: F# Result type (standard library)
let validateEpsilon (epsilon: float) : Result<float, string> =
    if epsilon >= 0.0 && epsilon <= 1.0 then Ok epsilon
    else Error $"epsilon must be in [0,1], got {epsilon}"

// Chaining with Result.bind
let createAgent epsilon numArms =
    validateEpsilon epsilon
    |> Result.map (fun e -> { Counts = Array.zeroCreate numArms
                               Values = Array.zeroCreate numArms
                               Epsilon = e })
```

### Pattern 3: ε-greedy with Incremental Mean

**What:** Standard bandit algorithm. Chooses random arm with probability ε, best-known arm with probability (1-ε). Updates value estimates with incremental mean (O(1) memory, numerically stable).

**Example:**
```fsharp
// Source: Sutton & Barto Chapter 2 + roadmap code
let epsilonGreedy (rng: System.Random) (epsilon: float) (state: AgentState) : Arm =
    if rng.NextDouble() < epsilon then
        rng.Next(state.Values.Length)      // random exploration
    else
        state.Values
        |> Array.indexed
        |> Array.maxBy snd
        |> fst                              // greedy exploitation

// BAND-04: incremental mean — no full history needed
// NewEstimate ← OldEstimate + (1/n) * (Reward - OldEstimate)
let incrementalMean (oldVal: float) (n: int) (reward: float) : float =
    oldVal + (1.0 / float n) * (reward - oldVal)
```

### Pattern 4: UCB1 Algorithm

**What:** Deterministic upper confidence bound strategy. Selects arm with highest Q(a) + sqrt(2 * ln(t) / N(a)). Guarantees O(log T) regret.

**Example:**
```fsharp
// Source: UCB1 formula (Auer et al., 2002)
let ucb1 (totalSteps: int) (state: AgentState) : Arm =
    let t = float totalSteps
    state.Values
    |> Array.mapi (fun i q ->
        let n = float (max state.Counts.[i] 1)
        q + sqrt (2.0 * log t / n))
    |> Array.indexed
    |> Array.maxBy snd
    |> fst
```

**Important:** UCB1 must pull each arm once before applying the formula. Initialize by selecting each arm in order for the first `numArms` steps.

### Pattern 5: Expecto Test Structure

**What:** Tests are F# values (not attributes). `testList` groups tests. `testCase` for unit tests. `testProperty` for FsCheck properties. Runner is a console app with `[<EntryPoint>]`.

**Example:**
```fsharp
// Source: Expecto README (https://github.com/haf/expecto)
open Expecto
open Expecto.ExpectoFsCheck

let banditTests =
    testList "Bandit" [
        // BAND-07: FsCheck property — reward sum invariant
        testProperty "Total reward equals sum of step rewards" <| fun (steps: int) ->
            let steps = abs steps % 1000 + 1
            let env = { RewardProbs = [| 0.3; 0.5; 0.7 |] }
            let rng = System.Random(42)
            let rewards = [1..steps] |> List.map (fun _ -> pullArm rng env 0)
            List.sum rewards >= 0.0  // rewards are non-negative

        // BAND-08: Expecto convergence test
        testCase "UCB1 converges to best arm after 1000 steps" <| fun () ->
            let env = { RewardProbs = [| 0.2; 0.5; 0.9 |] }
            let rng = System.Random(42)
            let finalState = runEpisode rng env ucb1 1000
            let bestArm = Array.indexed finalState.Values |> Array.maxBy snd |> fst
            Expect.equal bestArm 2 "Should converge to arm 2 (highest prob)"
    ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args banditTests
```

### Pattern 6: Serilog Configuration in F#

**What:** Method-chaining pattern from C# works identically in F# via `.` access. Use `Log.Logger <-` assignment to set global logger. Call `Log.CloseAndFlush()` before exit.

**Example:**
```fsharp
// Source: Serilog docs + codesuji.com F# Serilog post
open Serilog

let configureLogging () =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/bandit.log",
                rollingInterval = RollingInterval.Day)
            .CreateLogger()

// Usage
Log.Information("Episode {Episode}: arm={Arm}, reward={Reward}", ep, arm, reward)
Log.CloseAndFlush()   // IMPORTANT: flush before process exit
```

### Pattern 7: mdBook Structure

**What:** mdBook reads `src/SUMMARY.md` to determine chapter structure. Linked `.md` files become HTML pages. `book.toml` configures title, language, src dir.

**Example:**
```toml
# book.toml
[book]
title = "F#으로 배우는 강화학습"
authors = ["Your Name"]
description = "F# 강화학습 튜토리얼"
language = "ko"
src = "src"

[output.html]
```

```markdown
<!-- src/SUMMARY.md -->
# 목차

- [소개](README.md)
- [Chapter 1: 슬롯머신과 탐색-활용 딜레마](01-bandit/README.md)
  - [ε-greedy 알고리즘](01-bandit/epsilon-greedy.md)
  - [UCB1 알고리즘](01-bandit/ucb1.md)
  - [실험 결과](01-bandit/results.md)
- [Chapter 2: 틱택토 — 상태와 가치](02-tictactoe/README.md)
```

### Anti-Patterns to Avoid

- **Forward file references in fsproj:** F# compiler reads files top-to-bottom. `Agent.fs` cannot reference types from `Training.fs`. Always declare before use.
- **Mutable agent state as global:** Each training run must be stateless — pass state as function argument, return updated state. Never use `mutable` globals.
- **Random inside pure functions:** `System.Random()` is an I/O operation (seeds from clock). Pass `rng` as a parameter to all environment/agent functions.
- **Throwing exceptions in domain logic:** XCUT-01 forbids this. Use `Result` for invalid inputs.
- **Missing `Log.CloseAndFlush()`:** Serilog buffers output. Without flush, log lines are lost on process exit.
- **UCB1 without initialization:** If any arm has `Counts = 0`, log(t)/0 = infinity. Must pull each arm once first.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Property test shrinking | Custom counter-example reducer | FsCheck (via Expecto.FsCheck) | FsCheck shrinks automatically; hand-rolling misses edge cases |
| Log formatting/rotation | printfn with timestamps | Serilog + .Sinks.File | File rotation, structured events, thread-safe |
| Test runner infrastructure | Custom test discovery | Expecto with `runTestsWithCLIArgs` | Parallel execution, CI exit codes, CLI filtering |
| HTML doc generation | Hand-written HTML | mdBook | Search, navigation, themes for free |
| Statistical properties | Ad-hoc assertion on single run | FsCheck with high `maxTest` count | FsCheck generates hundreds of random inputs automatically |

**Key insight:** The entire testing pyramid for this phase (property tests + convergence tests) is covered by Expecto + Expecto.FsCheck. There is no need for a separate test runner or manual assertion library.

---

## Common Pitfalls

### Pitfall 1: F# File Ordering in .fsproj

**What goes wrong:** Compiler error "The value or constructor 'X' is not defined" when trying to use a type from a later-listed file.

**Why it happens:** Unlike C#, F# resolves identifiers strictly in source-file order. There are no forward declarations.

**How to avoid:** Always list files in `.fsproj` in dependency order:
```xml
<Compile Include="Domain.fs" />
<Compile Include="Environment.fs" />
<Compile Include="Agent.fs" />
<Compile Include="Training.fs" />
<Compile Include="Program.fs" />
```

**Warning signs:** Compiler says a type exists but can't be found; circular reference errors.

### Pitfall 2: FsCheck 3.x API vs 2.x Docs

**What goes wrong:** Using `Arb.register<MyArb>()` or `Arb.from<MyType>` raises a compilation error.

**Why it happens:** FsCheck 3.x removed `register`, `from`, `generate`, `shrink` from `Arb.FSharp`. Default types are now looked up via `ArbMap`.

**How to avoid:** For simple property tests with primitive types (int, float, list), FsCheck 3.x works identically to 2.x. Only custom `Arbitrary` registration changed. In Phase 1, all generated inputs are standard types — no custom generators needed.

**Warning signs:** Build errors mentioning `ArbMap`, removed functions.

### Pitfall 3: Expecto.FsCheck NuGet Version Mismatch

**What goes wrong:** Runtime exception — FsCheck assembly version not found.

**Why it happens:** `Expecto.FsCheck` 10.2.3 requires exactly `FsCheck` 3.3.2. Installing a different FsCheck version causes binding redirect failures.

**How to avoid:** Pin both: `Expecto 10.2.3`, `Expecto.FsCheck 10.2.3`, `FsCheck 3.3.2`.

**Warning signs:** Assembly load exception at test startup.

### Pitfall 4: Serilog Log.CloseAndFlush() Missing

**What goes wrong:** Last N log lines not written to file; file appears truncated.

**Why it happens:** Serilog.Sinks.File uses a background writer. Process exit skips the flush.

**How to avoid:** Always call `Log.CloseAndFlush()` or use `use logger = LoggerConfiguration()...CreateLogger()` with `use` for automatic disposal.

**Warning signs:** Log file is smaller than expected; last episode missing.

### Pitfall 5: UCB1 Division by Zero on Unvisited Arms

**What goes wrong:** `sqrt(2.0 * log(t) / 0.0)` returns `infinity` or `NaN`; agent always picks arm 0.

**Why it happens:** UCB1 formula divides by visit count. If any arm has never been pulled, count = 0.

**How to avoid:** Implement initialization phase — pull each arm once before applying UCB1:
```fsharp
let ucb1WithInit (totalSteps: int) (state: AgentState) : Arm =
    // Pull unvisited arms first
    match Array.tryFindIndex (fun c -> c = 0) state.Counts with
    | Some arm -> arm
    | None ->
        let t = float totalSteps
        state.Values
        |> Array.mapi (fun i q -> q + sqrt(2.0 * log t / float state.Counts.[i]))
        |> Array.indexed
        |> Array.maxBy snd
        |> fst
```

**Warning signs:** One arm has all visits; UCB1 performs identically to greedy.

### Pitfall 6: mdBook SUMMARY.md Chapter Links

**What goes wrong:** `mdbook build` error "file not found" or broken navigation.

**Why it happens:** SUMMARY.md links are relative to `src/`. If the `.md` file doesn't exist at the referenced path, mdBook errors out.

**How to avoid:** Either create all linked files before running `mdbook build`, or run `mdbook init` which auto-creates missing files. Use `mdbook serve` during development for live reload.

**Warning signs:** Build warning "file not found for chapter".

---

## Code Examples

### Complete ε-greedy Episode Runner

```fsharp
// Source: Architecture pattern (functional core)
// Training.fs
let runEpisode
    (rng: System.Random)
    (env: BanditEnv)
    (selectArm: System.Random -> AgentState -> int)
    (steps: int) : AgentState =

    let initial = {
        Counts = Array.zeroCreate env.RewardProbs.Length
        Values = Array.zeroCreate env.RewardProbs.Length
    }

    (initial, [1..steps])
    ||> List.fold (fun state _ ->
        let arm = selectArm rng state
        let reward = pullArm rng env arm
        incrementalMean state arm reward)
```

### Multi-epsilon Comparison (BAND-05)

```fsharp
// Source: BAND-05 requirement
let compareEpsilons (rng: System.Random) (env: BanditEnv) (steps: int) =
    [0.01; 0.1; 0.3]
    |> List.map (fun eps ->
        let agent = epsilonGreedy rng eps
        let finalState = runEpisode rng env agent steps
        let totalReward = Array.sum finalState.Values  // approximation
        eps, totalReward)
```

### Expecto + FsCheck Property (BAND-07)

```fsharp
// Source: Expecto.FsCheck pattern (https://github.com/haf/expecto)
open Expecto
open Expecto.ExpectoFsCheck

let rewardProperties =
    testList "Reward invariants" [
        testProperty "Reward sum is non-negative for non-negative arm probabilities" <|
            fun (probs: float list) ->
                let probs = probs |> List.map (fun p -> abs p % 1.0) |> List.truncate 5
                let probs = if List.isEmpty probs then [0.5] else probs
                let env = { RewardProbs = Array.ofList probs }
                let rng = System.Random(1)
                let state = runEpisode rng env (epsilonGreedy rng 0.1) 100
                Array.forall (fun v -> v >= 0.0) state.Values

        testPropertyWithConfig
            { FsCheckConfig.defaultConfig with maxTest = 1000 }
            "Counts sum equals total steps" <|
            fun () ->
                let env = { RewardProbs = [| 0.3; 0.5; 0.7 |] }
                let rng = System.Random(42)
                let steps = 500
                let state = runEpisode rng env (epsilonGreedy rng 0.1) steps
                Array.sum state.Counts = steps
    ]
```

### Serilog Episode Logging (BAND-09)

```fsharp
// Source: Serilog docs + codesuji.com F# example
open Serilog

let logEpisode (episode: int) (arm: int) (reward: float) (cumulativeReward: float) =
    Log.Information(
        "Episode={Episode} Arm={Arm} Reward={Reward:F2} Cumulative={Cumulative:F2}",
        episode, arm, reward, cumulativeReward)

// Setup in Program.fs
Log.Logger <-
    LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(
            outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
        .WriteTo.File("logs/bandit-.log", rollingInterval = RollingInterval.Day)
        .CreateLogger()
```

### mdBook book.toml (TUTR-01)

```toml
[book]
title = "F#으로 배우는 강화학습"
authors = [""]
description = "슬롯머신부터 오목 AI까지 — F#으로 구현하는 강화학습"
language = "ko"
src = "src"

[output.html]
git-repository-url = ""
edit-url-template = ""
```

### mdBook SUMMARY.md (TUTR-02)

```markdown
# 목차

[소개](README.md)

---

# 기초

- [Chapter 1: 슬롯머신 — 탐색과 활용](01-bandit/README.md)
  - [Multi-Armed Bandit 문제](01-bandit/problem.md)
  - [ε-greedy 알고리즘](01-bandit/epsilon-greedy.md)
  - [UCB1 알고리즘](01-bandit/ucb1.md)
  - [F# 구현 핵심 타입](01-bandit/types.md)
  - [실험 결과 비교](01-bandit/results.md)

# (다음 단계)

- [Chapter 2: 틱택토](02-tictactoe/README.md)
```

### Bandit.Tests .fsproj (BAND-10, BAND-07, BAND-08)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="PropertyTests.fs" />
    <Compile Include="ConvergenceTests.fs" />
    <Compile Include="Main.fs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Expecto" Version="10.2.3" />
    <PackageReference Include="Expecto.FsCheck" Version="10.2.3" />
    <PackageReference Include="FsCheck" Version="3.3.2" />
    <ProjectReference Include="..\..\src\Bandit\Bandit.fsproj" />
  </ItemGroup>
</Project>
```

### Solution Bootstrap Commands (BAND-10)

```bash
# 1. Create solution
dotnet new sln -o Bandit && cd Bandit

# 2. Create projects
dotnet new classlib -lang F# -o src/Bandit
dotnet new console -lang F# -o src/Bandit.Console
dotnet new console -lang F# -o tests/Bandit.Tests

# 3. Add to solution
dotnet sln add src/Bandit/Bandit.fsproj
dotnet sln add src/Bandit.Console/Bandit.Console.fsproj
dotnet sln add tests/Bandit.Tests/Bandit.Tests.fsproj

# 4. Add project references
dotnet add src/Bandit.Console/Bandit.Console.fsproj reference src/Bandit/Bandit.fsproj
dotnet add tests/Bandit.Tests/Bandit.Tests.fsproj reference src/Bandit/Bandit.fsproj

# 5. Add NuGet to console project
dotnet add src/Bandit.Console/Bandit.Console.fsproj package Serilog --version 4.3.1
dotnet add src/Bandit.Console/Bandit.Console.fsproj package Serilog.Sinks.Console --version 6.1.1
dotnet add src/Bandit.Console/Bandit.Console.fsproj package Serilog.Sinks.File --version 7.0.0

# 6. Add NuGet to test project
dotnet add tests/Bandit.Tests/Bandit.Tests.fsproj package Expecto --version 10.2.3
dotnet add tests/Bandit.Tests/Bandit.Tests.fsproj package Expecto.FsCheck --version 10.2.3
dotnet add tests/Bandit.Tests/Bandit.Tests.fsproj package FsCheck --version 3.3.2

# 7. mdBook
mdbook init docs/book --title "F#으로 배우는 강화학습"
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| NUnit/MSTest for F# | Expecto (values-as-tests) | ~2016 | Tests composable as F# values; no reflection |
| FsCheck 2.x Arb API | FsCheck 3.x ArbMap | 2023 | Custom Arbitrary registration changed; standard types unchanged |
| Serilog.Sinks.Console 4.x | Serilog.Sinks.Console 6.x | 2024 | ANSI theme improvements; API compatible |
| `runTestsWithArgs` | `runTestsWithCLIArgs` | Expecto 9+ | Old function still works but deprecated |
| mdBook 0.4.x Rust-only install | mdBook 0.4.52 binary releases | 2024 | Pre-built binaries available for all platforms |

**Deprecated/outdated:**
- `Arb.register<T>()` in FsCheck: Removed in 3.x. Use `ArbMap` if custom generators needed (not needed in Phase 1).
- `runTestsWithArgs`: Replaced by `runTestsWithCLIArgs`. Both work but prefer the latter.
- `LiterateConsole` Serilog sink: Removed. Use `Serilog.Sinks.Console` 6.x instead.
- `WriteTo.RollingFile()`: Removed. Use `WriteTo.File(..., rollingInterval = ...)` instead.

---

## Open Questions

1. **Bandit.Console vs single project**
   - What we know: Separating pure library (Bandit.fsproj) from the console entrypoint enforces XCUT-03. Some simpler projects combine them.
   - What's unclear: Whether a two-project split (Bandit.fsproj + Bandit.Tests.fsproj) is sufficient, with Program.fs inside Bandit.fsproj.
   - Recommendation: Use the three-project layout (library + console + tests) to strictly enforce XCUT-03 at the project boundary. This also makes the library reusable in later phases.

2. **mdBook location — in the Bandit solution or root?**
   - What we know: mdBook is independent of .NET projects. TUTR-02 says "Phase별 chapter 구조".
   - What's unclear: Whether the `docs/` folder lives inside `Bandit/` or at the repo root.
   - Recommendation: Put mdBook at the repo root (`docs/book/`) since it covers all 5 phases. The Bandit chapter (`src/01-bandit/`) is created in Phase 1 but the book infrastructure exists for all phases.

3. **Serilog in test project**
   - What we know: BAND-09 says Serilog logs episode data. Tests should not log to files.
   - What's unclear: Whether Serilog should be initialized in the test runner.
   - Recommendation: Do NOT add Serilog to `Bandit.Tests`. Logging belongs only in `Bandit.Console`. Tests call pure functions and use Expecto's own output.

---

## Sources

### Primary (HIGH confidence)
- [GitHub: haf/expecto](https://github.com/haf/expecto) — testCase/testList/testProperty API, runTestsWithCLIArgs
- [GitHub: haf/expecto FsCheck.fs](https://github.com/haf/expecto/blob/main/Expecto.FsCheck/FsCheck.fs) — testProperty/testPropertyWithConfig implementation
- [FsCheck QuickStart](https://fscheck.github.io/FsCheck/QuickStart.html) — property syntax, Check.Quick
- [FsCheck RunningTests](https://fscheck.github.io/FsCheck/RunningTests.html) — Config.Quick.WithMaxTest, QuickThrowOnFailure
- [mdBook Guide: Creating](https://rust-lang.github.io/mdBook/guide/creating.html) — mdbook init, build, serve
- [mdBook Configuration](https://rust-lang.github.io/mdBook/format/configuration/general.html) — book.toml fields
- [Microsoft Learn: F# CLI](https://learn.microsoft.com/en-us/dotnet/fsharp/get-started/get-started-command-line) — dotnet new sln/console, project references
- [GitHub: serilog-sinks-console](https://github.com/serilog/serilog-sinks-console) — v6.1.1, outputTemplate
- [GitHub: serilog-sinks-file](https://github.com/serilog/serilog-sinks-file) — v7.0.0, rollingInterval

### Secondary (MEDIUM confidence)
- [codesuji.com: F# and Serilog](https://www.codesuji.com/2017/08/20/F-and-Serilog/) — F# LoggerConfiguration syntax (2017, API stable)
- [NuGet: Expecto.FsCheck 10.2.3](https://www.nuget.org/packages/Expecto.FsCheck/) — version confirmed
- [FsCheck Release Notes](https://github.com/fscheck/FsCheck/blob/master/FsCheck%20Release%20Notes.md) — 3.x API changes

### Tertiary (LOW confidence)
- WebSearch for UCB1 F# functional implementation — no direct F# source found; UCB1 formula well-established from Auer et al. (2002)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions locked by prior decisions, verified on NuGet
- Architecture: HIGH — F# file ordering is well-documented; Functional Core/Imperative Shell is established pattern
- Pitfalls: HIGH — F# file ordering and UCB1 init are classic gotchas; FsCheck 3.x API change is documented
- mdBook: HIGH — official docs clear; no Rust knowledge required for usage

**Research date:** 2026-02-19
**Valid until:** 2026-03-21 (30 days — stable ecosystem, no fast-moving dependencies in Phase 1)
