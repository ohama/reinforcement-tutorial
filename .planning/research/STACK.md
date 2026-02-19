# Stack Research

**Domain:** F# Reinforcement Learning Tutorial (Console + mdBook)
**Researched:** 2026-02-19
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET SDK | 9.0 (use 9, not 10) | Runtime and build toolchain | .NET 9 is the current stable STS release (Nov 2024 – Nov 2026). TorchSharp 0.106 targets net6.0/netstandard2.0 and is forward-compatible; .NET 10 (LTS, Nov 2025) is new and TorchSharp compatibility is not yet fully verified in community practice. Use .NET 9 for the safest TorchSharp experience through this project lifetime. |
| F# | 9.0 (ships with .NET 9 SDK) | Primary language for all phases | Mandatory. F# 9 brings improved discriminated unions, nullability, and performance. All code is F#; no C# files. |
| mdBook | 0.4.52 (stable) | Tutorial documentation site | Rust-based static site generator. Markdown-native, GitHub Pages-ready, no JavaScript build pipeline needed. Simpler than Docusaurus for pure-text RL tutorials. |

### Phase 1–3 Libraries (No Neural Network)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | 10.2.3 | F#-native test runner | All phases. Tests compile as console apps; no external test host needed. Parallel by default. |
| Expecto.FsCheck | 10.2.3 | FsCheck integration for Expecto | All phases. Bridges Expecto's test runner with FsCheck property generators. Must match Expecto version. |
| FsCheck | 3.3.2 | Property-based testing | All phases. Game rule invariants (board validity, legal moves, terminal state detection) are ideal PBT targets. |
| Serilog | 4.3.1 | Structured logging | All phases. Tracks ε-greedy exploration rates, reward curves, training episode counts. |
| Serilog.Sinks.Console | 6.1.1 | Console log output | All phases. Real-time training progress visible during long learning loops. |
| Serilog.Sinks.File | 7.0.0 | File log output | Phase 2+. Persist training logs for post-run analysis without interrupting console display. |

### Phase 4–5 Libraries (Neural Network)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| TorchSharp | 0.106.0 | PyTorch .NET bindings (tensors, autograd, nn modules) | Phase 4 (DQN) and Phase 5 (MCTS + Policy/Value Net). Same API as Python PyTorch. Official F# support. |
| TorchSharp-cpu | 0.105.2 | CPU-only LibTorch native runtime | Phase 4–5 on developer machines. The convenience package that bundles TorchSharp + libtorch-cpu. Use this instead of manually referencing libtorch-cpu. |

> **TorchSharp package split note:** `TorchSharp-cpu` is the all-in-one package (TorchSharp + libtorch-cpu). Reference it directly and you do not need separate `TorchSharp` + `libtorch-cpu` references. This avoids version mismatch errors. Version 0.105.2 is the latest cpu convenience package; TorchSharp core is 0.106.0 — pin both explicitly.

### Optional Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Spectre.Console | 0.54.0 | Rich console tables, progress bars | Phase 1+. Display per-episode reward summaries, training progress bars, and comparison tables in console. Much better than `printf`. Not mandatory but highly recommended for tutorial readability. |
| MathNet.Numerics.FSharp | 5.0.0 | Statistics, probability distributions | Phase 1 (UCB1 confidence bounds), Phase 2 (statistical convergence tests). Use when you need Beta distributions or chi-square tests for convergence verification. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| `dotnet` CLI | Build, test, run all projects | Use `dotnet new console -lang F#` to scaffold each phase's project. |
| Fantomas | F# code formatter | Add as `dotnet tool` in each solution: `dotnet tool install fantomas`. Enforces consistent style across tutorial code. |
| mdBook CLI | Build and serve tutorial site | Install via `cargo install mdbook`. Run `mdbook serve tutorial/` during authoring. |
| Paket (optional) | NuGet dependency manager | Skip for this project. Standard `dotnet add package` and PackageReference in `.fsproj` is sufficient for 5 independent solutions. Paket adds ceremony without benefit here. |

## Project Structure per Phase

Each phase is an independent `.sln` with two `.fsproj` files:

```xml
<!-- src/Bandit/Bandit.fsproj — console app -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <Optimize>true</Optimize>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Domain.fs" />
    <Compile Include="Agent.fs" />
    <Compile Include="Program.fs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Serilog" Version="4.3.1" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
    <PackageReference Include="Spectre.Console" Version="0.54.0" />
  </ItemGroup>
</Project>
```

```xml
<!-- tests/Bandit.Tests/Bandit.Tests.fsproj — Expecto test runner as console app -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="PropertyTests.fs" />
    <Compile Include="ConvergenceTests.fs" />
    <Compile Include="Main.fs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bandit\Bandit.fsproj" />
    <PackageReference Include="Expecto" Version="10.2.3" />
    <PackageReference Include="Expecto.FsCheck" Version="10.2.3" />
    <PackageReference Include="FsCheck" Version="3.3.2" />
  </ItemGroup>
</Project>
```

For Phase 4–5, add to the src project:
```xml
<PackageReference Include="TorchSharp-cpu" Version="0.105.2" />
```

## Installation

```bash
# Install mdBook for tutorial site
cargo install mdbook

# Scaffold a new phase (example: Phase 1)
mkdir 01-bandit && cd 01-bandit
dotnet new sln -n Bandit
dotnet new console -lang F# -o src/Bandit
dotnet new console -lang F# -o tests/Bandit.Tests
dotnet sln add src/Bandit/Bandit.fsproj
dotnet sln add tests/Bandit.Tests/Bandit.Tests.fsproj

# Add libraries to main project
dotnet add src/Bandit package Serilog --version 4.3.1
dotnet add src/Bandit package Serilog.Sinks.Console --version 6.1.1
dotnet add src/Bandit package Serilog.Sinks.File --version 7.0.0
dotnet add src/Bandit package Spectre.Console --version 0.54.0

# Add libraries to test project
dotnet add tests/Bandit.Tests package Expecto --version 10.2.3
dotnet add tests/Bandit.Tests package Expecto.FsCheck --version 10.2.3
dotnet add tests/Bandit.Tests package FsCheck --version 3.3.2

# Phase 4–5 only: add TorchSharp
dotnet add src/DqnConnect4 package TorchSharp-cpu --version 0.105.2

# Install Fantomas per solution
dotnet new tool-manifest
dotnet tool install fantomas
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| .NET 9 | .NET 10 | When TorchSharp 0.107+ explicitly targets net10.0 (not yet as of Feb 2026). Upgrade all 5 phases together at that point. |
| .NET 9 | .NET 8 (LTS) | If .NET 9 support is dropped mid-project. .NET 8 also works with TorchSharp but misses F# 9 language features. |
| TorchSharp-cpu (0.105.2) | TorchSharp + libtorch-cpu separately | Only if you need to mix versions (unusual). Convenience package is simpler. |
| TorchSharp | Diffsharp | DiffSharp is not maintained as of 2024; no community momentum. Do not use. |
| TorchSharp | Direct neural net from scratch | Valid for learning, but Phase 4 goal is "experience DQN" not "build backprop." TorchSharp teaches PyTorch-equivalent patterns transferable to Python. |
| Expecto | xUnit | xUnit lacks F#-native test DSL. Expecto tests read as F# values, not attributes. Mandatory per project constraints. |
| Expecto | NUnit | Same reason as xUnit. Expecto is idiomatic F#. |
| Serilog | Microsoft.Extensions.Logging | MEL adds abstraction overhead; Serilog gives direct structured logging with rich sinks. Simpler for console training loops. |
| Serilog | Logary | Logary is F#-native but has smaller ecosystem and fewer sinks. Serilog has wider adoption and more resources. |
| Spectre.Console | Printf / printfn | printf is fine for trivial output. Spectre.Console adds progress bars and tables that make 10,000-episode training loops readable. Not mandatory. |
| mdBook | Docusaurus | Docusaurus requires Node.js build pipeline. mdBook is a single Rust binary — simpler CI/CD, no npm dependency hell. |
| mdBook | GitBook | GitBook is SaaS-only now. mdBook is open source and self-hosted. |
| MathNet.Numerics.FSharp | Custom statistics | Use MathNet when you need Beta/Gaussian distributions for UCB1 or convergence analysis. Skip if basic statistics suffice. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| DiffSharp | Abandoned; last meaningful release ~2021; incompatible with current .NET | TorchSharp |
| Microsoft.ML (ML.NET) | Designed for pipeline-based supervised ML, not RL agent loops. No Q-learning, MCTS, or self-play support. | TorchSharp for neural nets; pure F# for RL logic |
| Microsoft.ML.TorchSharp | Wrapper layer adding extra abstraction on top of TorchSharp. Hides tensor operations that students need to see. | TorchSharp directly |
| Fable / Feliz / Giraffe | Web stack; explicitly out of scope (Phase 5 is console-only per PROJECT.md) | Not needed |
| FSharp.Data | Data access library for CSV/JSON/HTML type providers. Not relevant for RL game environments. | Not needed |
| Paket | Package manager alternative to NuGet. Adds complexity. 5 independent solutions with few packages do not benefit from Paket's graph solving. | Standard `dotnet add package` |
| XPlot | Older F# charting library; requires browser to display. Console-only project cannot render HTML charts. | Spectre.Console for tables; log files for data export |
| Plotly.NET | Opens browser for charts; unusable in a console-only tutorial. | Spectre.Console (tables/progress) or Serilog (log files) |

## Stack Patterns by Variant

**Phase 1–3 (no neural networks):**
- Use: `Serilog` + `Serilog.Sinks.Console` + `Serilog.Sinks.File` + `Spectre.Console`
- Testing: `Expecto` + `Expecto.FsCheck` + `FsCheck`
- No TorchSharp dependency at all

**Phase 4–5 (neural networks):**
- Add: `TorchSharp-cpu` (convenience package)
- Keep all Phase 1–3 libraries
- TorchSharp adds ~500MB of native libtorch binaries; build/restore is slow on first run; warn readers

**If running on Apple Silicon (M1/M2/M3/M4 Mac):**
- `TorchSharp-cpu` works via Rosetta 2 emulation for x86_64
- Native ARM64 support: use `TorchSharp` + `libtorch-cpu` with `osx-arm64` runtime identifier
- As of TorchSharp 0.106, the -cpu convenience package may not include ARM64 natives; verify before Phase 4

**If reader wants GPU acceleration (not required by tutorial):**
- Replace `TorchSharp-cpu` with `TorchSharp-cuda-linux` or `TorchSharp-cuda-windows`
- Document this as optional footnote only

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| TorchSharp-cpu 0.105.2 | libtorch-cpu 2.5.x | Convenience package pins libtorch internally; do not add separate libtorch-cpu reference |
| TorchSharp 0.106.0 | libtorch-cpu 2.10.0.0 | If using core package separately, libtorch-cpu must be version 2.10.0.0 exactly |
| Expecto 10.2.3 | Expecto.FsCheck 10.2.3 | Must use matching major.minor; mixing versions causes test discovery failures |
| Expecto.FsCheck 10.2.3 | FsCheck 3.3.2 | Expecto.FsCheck 10.x targets FsCheck 3.x; do not mix with FsCheck 2.x |
| Serilog 4.3.1 | Serilog.Sinks.Console 6.1.1 | Sinks 6.x requires Serilog 4.x |
| Serilog 4.3.1 | Serilog.Sinks.File 7.0.0 | File sink 7.x requires Serilog 4.x |
| .NET 9.0 | TorchSharp-cpu 0.105.2 | TorchSharp targets netstandard2.0; works on net9.0 forward |

## Sources

- [NuGet Gallery | TorchSharp 0.106.0](https://www.nuget.org/packages/TorchSharp/) — version confirmed, .NET targets
- [NuGet Gallery | TorchSharp-cpu 0.105.2](https://www.nuget.org/packages/TorchSharp-cpu/) — CPU convenience package
- [GitHub | dotnet/TorchSharp](https://github.com/dotnet/TorchSharp) — F# API support, examples
- [NuGet Gallery | Expecto 10.2.3](https://www.nuget.org/packages/Expecto/) — current stable
- [NuGet Gallery | Expecto.FsCheck 10.2.3](https://www.nuget.org/packages/Expecto.FsCheck/) — version and date verified
- [NuGet Gallery | FsCheck 3.3.2](https://www.nuget.org/packages/FsCheck) — current stable
- [NuGet Gallery | Serilog 4.3.1](https://www.nuget.org/packages/serilog/) — current stable
- [NuGet Gallery | Serilog.Sinks.Console 6.1.1](https://www.nuget.org/packages/serilog.sinks.console/) — updated Nov 2025
- [NuGet Gallery | Serilog.Sinks.File 7.0.0](https://www.nuget.org/packages/serilog.sinks.file/) — updated Apr 2025
- [NuGet Gallery | Spectre.Console 0.54.0](https://www.nuget.org/packages/spectre.console) — current stable
- [NuGet Gallery | MathNet.Numerics.FSharp 5.0.0](https://www.nuget.org/packages/MathNet.Numerics.FSharp) — stable (6.0.0-beta2 is prerelease)
- [crates.io | mdbook](https://crates.io/crates/mdbook) — 0.4.52 latest stable (0.5.2 also available)
- [mdBook Documentation](https://rust-lang.github.io/mdBook/guide/installation.html) — installation guide
- [GitHub | galassie/fs-spectre](https://github.com/galassie/fs-spectre) — F# Spectre.Console wrapper (alternative to direct Spectre.Console use)

---
*Stack research for: F# Reinforcement Learning Tutorial (console, mdBook)*
*Researched: 2026-02-19*
