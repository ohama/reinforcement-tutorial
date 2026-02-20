# Milestone v1: F#으로 배우는 강화학습 Tutorial

**Status:** SHIPPED 2026-02-20
**Phases:** 1-5
**Total Plans:** 19

## Overview

5개의 독립 F# console 프로젝트를 순서대로 구현하며 RL 핵심 개념을 학습한다. 각 Phase는 완전히 동작하는 F# solution과 대응하는 한국어 mdBook 챕터를 함께 출하한다. Bandit(상태 없음)에서 시작해 TicTacToe(소규모 상태 공간) → Connect Four(대규모 상태 공간 + Minimax) → DQN(신경망 일반화) → Gomoku MCTS(트리 탐색 + 신경망 유도)로 점진적으로 복잡도를 높인다.

## Phases

### Phase 1: Bandit + mdBook 기반

**Goal**: 전체 프로젝트 구조와 개발 규약이 확립되고, Multi-Armed Bandit으로 Exploration vs Exploitation을 직접 관찰할 수 있다
**Depends on**: Nothing (first phase)
**Requirements**: TUTR-01, TUTR-02, TUTR-03, TUTR-06, BAND-01~10, XCUT-01~03
**Plans**: 3 plans

Plans:
- [x] 01-01-PLAN.md — mdBook 스캐폴드 + Bandit.sln 세 프로젝트 구조
- [x] 01-02-PLAN.md — Bandit 게임 엔진 전체 구현 (ε-greedy, UCB1, incremental mean)
- [x] 01-03-PLAN.md — FsCheck 프로퍼티 테스트 + Expecto 수렴 테스트 + 한국어 mdBook 챕터

**Completed:** 2026-02-19

### Phase 2: Tic-Tac-Toe (TD Learning)

**Goal**: MDP와 TD Learning 개념이 동작하는 F# 코드로 검증되고, 학습된 AI가 콘솔에서 사람과 대전할 수 있다
**Depends on**: Phase 1
**Requirements**: TUTR-03~06, TICT-01~10
**Plans**: 3 plans

Plans:
- [x] 02-01-PLAN.md — TicTacToe.sln 부트스트랩 + Domain.fs + Rules.fs + FsCheck 보드 불변 조건
- [x] 02-02-PLAN.md — Agent.fs + Training.fs (TD(0) 자가 대국) + Expecto 수렴 테스트
- [x] 02-03-PLAN.md — Program.fs (Serilog + 사람 vs AI) + 한국어 mdBook 챕터

**Completed:** 2026-02-19

### Phase 3: Connect Four (Q-Learning + Minimax)

**Goal**: Q-Learning과 Minimax Alpha-Beta가 동일 게임에서 비교되고, 대규모 상태 공간에서 Q-table의 한계가 실증된다
**Depends on**: Phase 2
**Requirements**: TUTR-03~06, CNCT-01~09
**Plans**: 4 plans

Plans:
- [x] 03-01-PLAN.md — ConnectFour.sln 부트스트랩 + Domain.fs + Rules.fs + FsCheck
- [x] 03-02-PLAN.md — Minimax.fs (Negamax + Alpha-Beta)
- [x] 03-03-PLAN.md — QAgent.fs + Training.fs (자가 대국 루프)
- [x] 03-04-PLAN.md — Program.fs + 한국어 mdBook 챕터

**Completed:** 2026-02-20

### Phase 4: Connect Four DQN

**Goal**: TorchSharp Conv2D DQN이 학습되고, Phase 3 Minimax(depth 4) 대비 승률 > 50%를 달성하며 메모리 누수 없이 안정 작동한다
**Depends on**: Phase 3
**Requirements**: TUTR-03~06, DQN-01~12
**Plans**: 4 plans

Plans:
- [x] 04-01-PLAN.md — DQN.sln 부트스트랩 + NativeLoader + boardToTensor + FsCheck
- [x] 04-02-PLAN.md — DQNModel + ReplayBuffer + DQNAgent
- [x] 04-03-PLAN.md — 커리큘럼 학습 루프 + 벤치마크 테스트
- [x] 04-04-PLAN.md — Program.fs + Korean mdBook 04-dqn 챕터

**Completed:** 2026-02-20

### Phase 5: Gomoku MCTS

**Goal**: MCTS + Policy/Value Network 자가 대국 AI가 구현되고, 랜덤 상대 승률 > 80%를 달성하며 사람과 콘솔 대전이 가능하다
**Depends on**: Phase 4
**Requirements**: TUTR-03~06, GMOK-01~12
**Plans**: 5 plans

Plans:
- [x] 05-01-PLAN.md — Gomoku.sln 부트스트랩 + Domain.fs + Rules.fs + FsCheck
- [x] 05-02-PLAN.md — MctsNode + Mcts.fs (PUCT + 랜덤 롤아웃) + Expecto 테스트
- [x] 05-03-PLAN.md — NativeLoader + PolicyValueNet + mctsSearchWithNet
- [x] 05-04-PLAN.md — SelfPlay.fs + Training.fs + Program.fs + 사람 vs AI
- [x] 05-05-PLAN.md — Serilog 구조화 로깅 + 한국어 mdBook 챕터

**Completed:** 2026-02-20

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Bandit + mdBook 기반 | 3/3 | Complete | 2026-02-19 |
| 2. Tic-Tac-Toe (TD Learning) | 3/3 | Complete | 2026-02-19 |
| 3. Connect Four (Q-Learning + Minimax) | 4/4 | Complete | 2026-02-20 |
| 4. Connect Four DQN | 4/4 | Complete | 2026-02-20 |
| 5. Gomoku MCTS | 5/5 | Complete | 2026-02-20 |

## Milestone Summary

**Key Decisions:**

- mdBook for tutorial site — 정적 사이트, {{#include}} 소스 임베딩
- Phase별 독립 solution — copy-and-evolve, 의존성 격리
- TorchSharp-cpu 0.106.0 — PyTorch API, ARM64 macOS NativeLoader
- net10.0 + Traditional .sln format — system constraint
- FsCheck 2.16.5 (not 3.x) — Expecto.FsCheck 호환성
- Board as int array for Gomoku — MCTS 시뮬레이션 속도
- MctsNode as mutable class — parent pointers + Dictionary children

**Issues Resolved:**

- .NET 10 defaults to .slnx → `dotnet new sln --format sln` fix
- FsCheck 3.x TypeLoadException → downgrade to 2.16.5
- TorchSharp ARM64 macOS SIGSEGV → NativeLoader preload pattern
- `open type TorchSharp.torch` shadows F# float/sqrt/log → qualified calls
- Dirichlet distribution → pure F# Marsaglia-Tsang Gamma sampler

**Technical Debt:**

- Phase 04 FS3391 warning: DQNAgent.fs line 62 implicit float32→Scalar conversion (cosmetic)
- Phase 04 SC3: 50K DQN training vs Minimax depth 4 requires manual `dotnet run` verification

---
*Archived: 2026-02-20 as part of v1 milestone completion*
