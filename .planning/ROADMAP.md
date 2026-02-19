# Roadmap: F#으로 배우는 강화학습 Tutorial

## Overview

5개의 독립 F# console 프로젝트를 순서대로 구현하며 RL 핵심 개념을 학습한다. 각 Phase는 완전히 동작하는 F# solution과 대응하는 한국어 mdBook 챕터를 함께 출하한다. Bandit(상태 없음)에서 시작해 TicTacToe(소규모 상태 공간) → Connect Four(대규모 상태 공간 + Minimax) → DQN(신경망 일반화) → Gomoku MCTS(트리 탐색 + 신경망 유도)로 점진적으로 복잡도를 높인다.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Bandit + mdBook 기반** - 프로젝트 전체 구조 확립, ε-greedy + UCB1 구현, mdBook 스캐폴드
- [ ] **Phase 2: Tic-Tac-Toe (TD Learning)** - MDP + Value Function + 자가 대국으로 TD(0) 에이전트 구현
- [ ] **Phase 3: Connect Four (Q-Learning + Minimax)** - 특징 기반 Q-Learning과 Alpha-Beta Pruning 비교
- [ ] **Phase 4: Connect Four DQN** - TorchSharp Conv2D 신경망으로 Q-table 한계 극복
- [ ] **Phase 5: Gomoku MCTS** - MCTS + Policy/Value Network + 자가 대국 파이프라인

## Phase Details

### Phase 1: Bandit + mdBook 기반
**Goal**: 전체 프로젝트 구조와 개발 규약이 확립되고, Multi-Armed Bandit으로 Exploration vs Exploitation을 직접 관찰할 수 있다
**Depends on**: Nothing (first phase)
**Requirements**: TUTR-01, TUTR-02, TUTR-03, TUTR-06, BAND-01, BAND-02, BAND-03, BAND-04, BAND-05, BAND-06, BAND-07, BAND-08, BAND-09, BAND-10, XCUT-01, XCUT-02, XCUT-03
**Success Criteria** (what must be TRUE):
  1. `dotnet run`으로 Bandit 앱을 실행하면 ε=0.01/0.1/0.3 비교와 ε-greedy vs UCB1 승자가 콘솔에 출력된다
  2. `dotnet test`를 실행하면 FsCheck 보상 합산 불변 조건 테스트와 Expecto 1000회 최적 arm 수렴 검증이 통과한다
  3. `mdbook build`가 성공하고, 01-bandit/ 챕터에 RL 개념 설명과 핵심 F# 타입 정의가 한국어로 있다
  4. Domain.fs/Rules.fs에 I/O 코드가 없고, 모든 에러 처리가 Option/Result 패턴을 사용한다
**Plans**: 3 plans

Plans:
- [ ] 01-01-PLAN.md — mdBook 스캐폴드 + Bandit.sln 세 프로젝트 구조 (순수 라이브러리 / 콘솔 셸 / 테스트 러너)
- [ ] 01-02-PLAN.md — Bandit 게임 엔진 전체 구현 (ε-greedy, UCB1, incremental mean, compareEpsilons, Serilog 로깅)
- [ ] 01-03-PLAN.md — FsCheck 프로퍼티 테스트 + Expecto 수렴 테스트 + 한국어 mdBook 01-bandit 챕터 완성

### Phase 2: Tic-Tac-Toe (TD Learning)
**Goal**: MDP와 TD Learning 개념이 동작하는 F# 코드로 검증되고, 학습된 AI가 콘솔에서 사람과 대전할 수 있다
**Depends on**: Phase 1
**Requirements**: TUTR-03, TUTR-04, TUTR-05, TUTR-06, TICT-01, TICT-02, TICT-03, TICT-04, TICT-05, TICT-06, TICT-07, TICT-08, TICT-09, TICT-10
**Success Criteria** (what must be TRUE):
  1. `dotnet test`를 실행하면 FsCheck 보드 불변 조건 테스트(빈칸 수 감소, 차례 교대)와 Expecto 승률 > 90% 검증이 통과한다
  2. `dotnet run`으로 사람 vs AI 대전 모드를 선택하면 학습된 에이전트와 콘솔에서 게임을 플레이할 수 있다
  3. Serilog가 매 1000판 승률을 구조화 로그로 출력하고, 학습 곡선이 수렴하는 것을 로그에서 관찰할 수 있다
  4. mdBook 02-tictactoe/ 챕터에 `{{#include}}` 로 실제 소스가 인클루드되어 있고, Phase 1 Bandit의 한계와 MDP 필요성이 설명된다
**Plans**: TBD

Plans:
- [ ] 02-01: TicTacToe 게임 엔진 (Domain.fs, Rules.fs — 순수 함수, FsCheck 프로퍼티)
- [ ] 02-02: TD(0) 에이전트 + 자가 대국 학습 루프 (Agent.fs, Training.fs, Expecto 수렴 테스트)
- [ ] 02-03: 사람 vs AI 콘솔 모드 + Serilog 로깅 + mdBook 챕터

### Phase 3: Connect Four (Q-Learning + Minimax)
**Goal**: Q-Learning과 Minimax Alpha-Beta가 동일 게임에서 비교되고, 대규모 상태 공간에서 Q-table의 한계가 실증된다
**Depends on**: Phase 2
**Requirements**: TUTR-03, TUTR-04, TUTR-05, TUTR-06, CNCT-01, CNCT-02, CNCT-03, CNCT-04, CNCT-05, CNCT-06, CNCT-07, CNCT-08, CNCT-09
**Success Criteria** (what must be TRUE):
  1. `dotnet test`를 실행하면 FsCheck 중력 규칙 + 4연속 판정 불변 조건과 Expecto Alpha-Beta/Minimax 동일 결과 검증이 통과한다
  2. `dotnet run`으로 Minimax AI vs Q-Learning AI 대결을 실행하면 승률과 Alpha-Beta 가지치기 통계가 콘솔에 출력된다
  3. `dotnet run`으로 사람 vs AI 모드를 선택하면 사람이 Minimax 또는 Q-Learning AI와 대전할 수 있다
  4. mdBook 03-connect-four/ 챕터에 "왜 Q-table이 여기서 한계에 부딪히는가" 섹션이 있고 Phase 4 DQN 필요성이 설명된다
**Plans**: TBD

Plans:
- [ ] 03-01: Connect Four 게임 엔진 (Domain.fs, Rules.fs — 중력, 4연속 판정, FsCheck 프로퍼티)
- [ ] 03-02: Minimax + Alpha-Beta 구현 (depth 6~8, 평가 함수)
- [ ] 03-03: Q-Learning 에이전트 + QTable 모듈 (Dictionary 백엔드, 특징 추출)
- [ ] 03-04: 대결 비교 출력 + Serilog + mdBook 챕터

### Phase 4: Connect Four DQN
**Goal**: TorchSharp Conv2D DQN이 학습되고, Phase 3 Minimax(depth 4) 대비 승률 > 50%를 달성하며 메모리 누수 없이 안정 작동한다
**Depends on**: Phase 3
**Requirements**: TUTR-03, TUTR-04, TUTR-05, TUTR-06, DQN-01, DQN-02, DQN-03, DQN-04, DQN-05, DQN-06, DQN-07, DQN-08, DQN-09, DQN-10, DQN-11, DQN-12
**Success Criteria** (what must be TRUE):
  1. `dotnet test`를 실행하면 텐서 변환 형상 검증, Experience Replay 용량 테스트, done-mask 정확성 검증이 모두 통과한다
  2. DQN 학습 루프가 50K 에피소드 동안 메모리 증가 없이 실행되고, 손실 곡선과 승률 변화가 Serilog로 기록된다
  3. 학습된 모델이 .pt 파일로 저장되고, 저장된 파일에서 불러온 에이전트가 Phase 3 Minimax(depth 4)와 대결에서 승률 > 50%를 달성한다
  4. mdBook 04-dqn/ 챕터에 `{{#include}}` 로 boardToTensor, DQN 모델 정의, 학습 루프가 인클루드되어 있다
**Plans**: TBD

Plans:
- [ ] 04-01: TorchSharp 설정 + 텐서 변환 검증 (DQN-01, DQN-02, DQN-08, DQN-11 포함)
- [ ] 04-02: DQN 모델 + Experience Replay + Target Network (DQN-03, DQN-04, DQN-05)
- [ ] 04-03: 학습 루프 + Minimax 대결 벤치마크 + 모델 저장/불러오기 (DQN-06, DQN-07, DQN-09)
- [ ] 04-04: Serilog 학습 추적 + mdBook 챕터 (DQN-10, TUTR-03, TUTR-04, TUTR-05, TUTR-06)

### Phase 5: Gomoku MCTS
**Goal**: MCTS + Policy/Value Network 자가 대국 AI가 구현되고, 랜덤 상대 승률 > 80%를 달성하며 사람과 콘솔 대전이 가능하다
**Depends on**: Phase 4
**Requirements**: TUTR-03, TUTR-04, TUTR-05, TUTR-06, GMOK-01, GMOK-02, GMOK-03, GMOK-04, GMOK-05, GMOK-06, GMOK-07, GMOK-08, GMOK-09, GMOK-10, GMOK-11, GMOK-12
**Success Criteria** (what must be TRUE):
  1. `dotnet test`를 실행하면 5연속 판정 + 합법 수 FsCheck 프로퍼티, MCTS backpropagation 관점 전환 Expecto 테스트, 랜덤 상대 승률 > 80% Expecto 테스트가 모두 통과한다
  2. `dotnet run`으로 자가 대국 학습 파이프라인을 실행하면 Serilog가 MCTS 탐색 통계와 학습 진행 상황을 구조화 로그로 출력한다
  3. 학습된 모델을 저장하고, 저장된 모델을 불러와 사람 vs AI 콘솔 대전(난이도 = 시뮬레이션 횟수)을 플레이할 수 있다
  4. mdBook 05-gomoku/ 챕터에 MCTS 알고리즘, PUCT 수식, Policy/Value Network 설계가 한국어로 설명되고 `{{#include}}` 로 핵심 코드가 인클루드된다
  5. 모든 5개 챕터가 Phase 간 연결 설명("이전 Phase의 한계 → 다음 Phase의 동기부여")을 포함하고 mdBook이 에러 없이 빌드된다
**Plans**: TBD

Plans:
- [ ] 05-01: Gomoku 게임 엔진 (Domain.fs, Rules.fs — 15×15, 5연속, FsCheck 프로퍼티)
- [ ] 05-02: MCTS 기본 구현 (MctsNode, UCB1 selection, expand, simulate, backpropagate, Expecto 단위 테스트)
- [ ] 05-03: Policy/Value Network + PUCT 통합 (ResBlock, TorchSharp, GMOK-03, GMOK-04)
- [ ] 05-04: 자가 대국 파이프라인 + 모델 저장/불러오기 + 사람 vs AI (GMOK-05, GMOK-06, GMOK-07)
- [ ] 05-05: Serilog 로깅 + Expecto 수렴 테스트 + mdBook 챕터 완성

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Bandit + mdBook 기반 | 0/3 | Not started | - |
| 2. Tic-Tac-Toe (TD Learning) | 0/3 | Not started | - |
| 3. Connect Four (Q-Learning + Minimax) | 0/4 | Not started | - |
| 4. Connect Four DQN | 0/4 | Not started | - |
| 5. Gomoku MCTS | 0/5 | Not started | - |
