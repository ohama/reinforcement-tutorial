# F#으로 배우는 강화학습 Tutorial

## What This Is

F#으로 강화학습(RL) 핵심 알고리즘을 단계별로 구현하며 배우는 실전 튜토리얼.
슬롯머신(상태 없음)에서 시작해 오목 AlphaZero(MCTS + 신경망)까지, 5개 독립 F# solution과 한국어 mdBook 5챕터로 구성된다.

## Core Value

각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.

## Requirements

### Validated

- ✓ mdBook tutorial 사이트 구조 (5챕터, {{#include}} 18개) — v1
- ✓ Phase 1: Multi-Armed Bandit — ε-greedy, UCB1, Exploration vs Exploitation — v1
- ✓ Phase 2: Tic-Tac-Toe — MDP, Value Function, TD Learning, 자가 대국 — v1
- ✓ Phase 3: Connect Four — Q-Learning, Minimax + Alpha-Beta Pruning — v1
- ✓ Phase 4: Connect Four + DQN — Neural Network, Experience Replay, Target Network — v1
- ✓ Phase 5: Gomoku (Console) — MCTS, Policy/Value Network, Self-Play — v1
- ✓ 각 Phase별 Expecto 단위 테스트 + FsCheck property-based 테스트 (59 tests) — v1
- ✓ 각 Phase별 학습 수렴 검증 테스트 — v1
- ✓ Serilog 로깅 (학습 과정 추적) — v1
- ✓ Option/Result 패턴 (exception 대신) — v1

### Active

(None — v1 complete. Next milestone requirements TBD.)

### Out of Scope

- F# 언어 기초 설명 — 저자가 F# 전문가, tutorial 독자도 F# 기본 안다고 가정
- Web frontend (Fable, Feliz, Giraffe, SignalR) — 콘솔 프로젝트만
- GPU 학습 최적화 — 학습용이므로 CPU로 충분
- 렌주 룰 (금수 규칙) — 기본 오목 규칙만으로 충분

## Context

Shipped v1 with 3,640 LOC F# across 50 source files.
Tech stack: F# 9 / .NET 10, TorchSharp-cpu 0.106.0 (Phase 4-5), Expecto 10.2.3 + FsCheck 2.16.5, Serilog, mdBook.
All 5 solutions build independently. 59 tests pass. mdBook builds with 18 {{#include}} directives.

### 디렉토리 구조

```
reinforcement-tutorial/
├── Bandit/                    # Phase 1: Multi-Armed Bandit
├── TicTacToe/                 # Phase 2: TD Learning
├── 03-connect-four/           # Phase 3: Q-Learning + Minimax
├── 04-connect-four-dqn/       # Phase 4: DQN (TorchSharp)
├── 05-gomoku-mcts/            # Phase 5: MCTS + AlphaZero
└── tutorial/                  # 한국어 mdBook 튜토리얼 (5챕터)
```

### Phase별 RL 개념 매핑

| Phase | 게임 | RL 핵심 개념 | 테스트 |
|-------|------|-------------|--------|
| 1 | Bandit (슬롯머신) | ε-greedy, UCB1 | 8 |
| 2 | Tic-Tac-Toe | TD(0) Learning, 자가 대국 | 8 |
| 3 | Connect Four | Q-Learning, Minimax + Alpha-Beta | 20 |
| 4 | Connect Four DQN | Conv2D DQN, Experience Replay, Target Network | 9 |
| 5 | Gomoku | MCTS, PUCT, Policy/Value Network, Self-Play | 14 |

## Constraints

- **Language**: F# (전 과정) — 다른 언어 사용 불가
- **Testing**: Expecto + FsCheck — xUnit 등 다른 프레임워크 사용 안 함
- **Logging**: Serilog — 학습 과정 추적용
- **Error Handling**: Option/Result 패턴 — exception throw 금지
- **Neural Network**: TorchSharp-cpu 0.106.0 — Phase 4, 5에서 사용
- **Output**: 콘솔 앱만 — Web/GUI 없음
- **.NET**: net10.0 (not net9.0) — system has .NET 10 SDK only

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| mdBook for tutorial site | 정적 사이트 생성, Markdown 기반, Rust 생태계 안정적 | ✓ Good — 18 {{#include}} 잘 동작 |
| Phase별 독립 solution | 각 Phase가 자체 완결적, 의존성 격리 | ✓ Good — copy-and-evolve 패턴 유효 |
| TorchSharp for neural nets | PyTorch API와 동일, F# 호환, .NET 공식 지원 | ✓ Good — ARM64 macOS NativeLoader로 해결 |
| Console only (no web) | RL 학습에 집중, web은 별도 프로젝트로 분리 | ✓ Good |
| F# 문법 설명 제외 | 저자가 F# 전문가, RL 개념에 집중 | ✓ Good |
| net10.0 (not net9.0) | System has .NET 10 SDK only | ✓ Good — all 15 projects consistent |
| Traditional .sln (not .slnx) | dotnet new sln --format sln required | ✓ Good — 5/5 solutions |
| FsCheck 2.16.5 (not 3.x) | Expecto.FsCheck 10.2.3 requires StdGen (removed in 3.x) | ✓ Good — avoids TypeLoadException |
| Board as int array (Phase 5) | 0=empty, 1=Black, -1=White — fast MCTS simulation | ✓ Good — 100% win rate vs random |
| NativeLoader ARM64 pattern | module-level `do load()` preloads TorchSharp dylibs | ✓ Good — prevents SIGSEGV on macOS |
| MctsNode as mutable class | Parent pointers + Dictionary children for MCTS tree | ✓ Good — Dirichlet noise injection works |

---
*Last updated: 2026-02-20 after v1 milestone*
