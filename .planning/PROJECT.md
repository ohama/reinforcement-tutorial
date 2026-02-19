# F#으로 배우는 강화학습 Tutorial

## What This Is

F#으로 강화학습(RL)을 단계별로 구현하면서 배우는 self-learning tutorial 프로젝트.
mdBook 기반 tutorial 사이트와 Phase별 독립 F# console 프로젝트로 구성된다.
Bandit(가장 쉬움)에서 시작해 Gomoku MCTS(가장 어려움)까지 점진적으로 난이도를 높인다.

## Core Value

각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] mdBook tutorial 사이트 구조 (tutorial/ 디렉토리, md 파일만)
- [ ] Phase 1: Multi-Armed Bandit — ε-greedy, UCB1, Exploration vs Exploitation
- [ ] Phase 2: Tic-Tac-Toe — MDP, Value Function, TD Learning, 자가 대국
- [ ] Phase 3: Connect Four — Q-Learning, Minimax + Alpha-Beta Pruning
- [ ] Phase 4: Connect Four + DQN — Neural Network, Experience Replay, Target Network
- [ ] Phase 5: Gomoku (Console) — MCTS, Policy/Value Network, Self-Play
- [ ] 각 Phase별 Expecto 단위 테스트 + FsCheck property-based 테스트
- [ ] 각 Phase별 학습 수렴 검증 테스트
- [ ] Serilog 로깅 (학습 과정 추적)
- [ ] Option/Result 패턴 (exception 대신)

### Out of Scope

- F# 언어 기초 설명 — 저자가 F# 전문가, tutorial 독자도 F# 기본 안다고 가정
- Web frontend (Fable, Feliz, Giraffe, SignalR) — 콘솔 프로젝트만
- GPU 학습 최적화 — 학습용이므로 CPU로 충분
- 렌주 룰 (금수 규칙) — 기본 오목 규칙만으로 충분

## Context

- **로드맵 참고 파일**: `rl-gomoku-roadmap.md` — Phase별 RL 개념, 핵심 코드, 수식 포함
- **프로젝트 구조**: 각 Phase별 독립 .sln + .fsproj, tutorial/에는 mdBook md 파일만
- **테스트 전략**: 게임 규칙은 FsCheck property-based test로 불변 조건 검증, 학습 과정은 Expecto로 수렴 확인
- **Tutorial 내용**: F# 문법 설명 없음, RL 개념 + 핵심 F# 타입 정의 + 알고리즘 구현에 집중
- **언어**: 한국어로 작성

### 디렉토리 구조

```
tutorial/                # mdBook (md 파일만)
  book.toml
  src/
    SUMMARY.md
    01-bandit/
    02-tictactoe/
    03-connect-four/
    04-dqn/
    05-gomoku/

01-bandit/               # 독립 F# solution
  Bandit.sln
  src/Bandit/
  tests/Bandit.Tests/

02-tictactoe/            # 독립 F# solution
  TicTacToe.sln
  src/TicTacToe/
  tests/TicTacToe.Tests/

03-connect-four/         # 독립 F# solution
04-dqn/                  # 독립 F# solution
05-gomoku/               # 독립 F# solution
```

### Phase별 RL 개념 매핑

| Phase | 게임 | RL 핵심 개념 | 난이도 |
|-------|------|-------------|--------|
| 1 | Bandit (슬롯머신) | Exploration vs Exploitation, ε-greedy, UCB1 | ⭐ |
| 2 | Tic-Tac-Toe | MDP, Value Function, TD Learning | ⭐⭐ |
| 3 | Connect Four | Q-Learning, Minimax, Alpha-Beta Pruning | ⭐⭐⭐ |
| 4 | Connect Four + DQN | Neural Network (TorchSharp), Experience Replay, Target Network | ⭐⭐⭐⭐ |
| 5 | Gomoku (Console) | MCTS, Policy/Value Network, Self-Play | ⭐⭐⭐⭐⭐ |

## Constraints

- **Language**: F# (전 과정) — 다른 언어 사용 불가
- **Testing**: Expecto + FsCheck — xUnit 등 다른 프레임워크 사용 안 함
- **Logging**: Serilog — 학습 과정 추적용
- **Error Handling**: Option/Result 패턴 — exception throw 금지
- **Neural Network**: TorchSharp — Phase 4, 5에서 사용
- **Output**: 콘솔 앱만 — Web/GUI 없음

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| mdBook for tutorial site | 정적 사이트 생성, Markdown 기반, Rust 생태계 안정적 | — Pending |
| Phase별 독립 solution | 각 Phase가 자체 완결적, 의존성 격리 | — Pending |
| TorchSharp for neural nets | PyTorch API와 동일, F# 호환, .NET 공식 지원 | — Pending |
| Console only (no web) | RL 학습에 집중, web은 별도 프로젝트로 분리 | — Pending |
| F# 문법 설명 제외 | 저자가 F# 전문가, RL 개념에 집중 | — Pending |

---
*Last updated: 2026-02-19 after initialization*
