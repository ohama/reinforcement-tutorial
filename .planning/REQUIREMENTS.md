# Requirements: F#으로 배우는 강화학습 Tutorial

**Defined:** 2026-02-19
**Core Value:** 각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.

## v1 Requirements

### Tutorial Structure

- [ ] **TUTR-01**: mdBook 프로젝트 초기화 (book.toml, src/SUMMARY.md)
- [ ] **TUTR-02**: Phase별 chapter 구조 (01-bandit/ ~ 05-gomoku/)
- [ ] **TUTR-03**: 각 chapter에 RL 개념 설명 + 핵심 F# 타입 정의 + 알고리즘 구현 포함
- [ ] **TUTR-04**: `{{#include}}` 로 실제 소스 코드를 tutorial에 인클루드 (드리프트 방지)
- [ ] **TUTR-05**: Phase 간 연결 설명 — 이전 Phase의 한계가 다음 Phase를 동기부여
- [ ] **TUTR-06**: 한국어로 작성

### Phase 1: Multi-Armed Bandit

- [ ] **BAND-01**: 슬롯머신 환경 구현 (N개 arm, 각각 다른 보상 확률)
- [ ] **BAND-02**: ε-greedy 에이전트 구현
- [ ] **BAND-03**: UCB1 에이전트 구현
- [ ] **BAND-04**: 점진적 평균 업데이트 (incremental mean)
- [ ] **BAND-05**: 여러 ε 값 비교 실험 (0.01, 0.1, 0.3)
- [ ] **BAND-06**: ε-greedy vs UCB1 성능 비교 출력
- [ ] **BAND-07**: FsCheck — 보상 합산 불변 조건 테스트
- [ ] **BAND-08**: Expecto — 1000회 시행 후 최적 arm 수렴 검증
- [ ] **BAND-09**: Serilog — 에피소드별 보상, 선택 arm 로깅
- [ ] **BAND-10**: 독립 F# solution (Bandit.sln)

### Phase 2: Tic-Tac-Toe (TD Learning)

- [ ] **TICT-01**: 3×3 보드 및 게임 규칙 구현 (승리 판정, 합법 수 목록)
- [ ] **TICT-02**: 불변 GameState 타입 (Board + CurrentPlayer)
- [ ] **TICT-03**: 랜덤 에이전트 구현
- [ ] **TICT-04**: TD(0) Learning 에이전트 구현 (Value Table: Map<Board, float>)
- [ ] **TICT-05**: 자가 대국 학습 루프 (10만 판)
- [ ] **TICT-06**: 학습된 AI vs 사람 대전 (콘솔)
- [ ] **TICT-07**: FsCheck — 보드 불변 조건 (빈칸 수 감소, 차례 교대 등)
- [ ] **TICT-08**: Expecto — 10만 판 학습 후 랜덤 상대 승률 > 90% 검증
- [ ] **TICT-09**: Serilog — 학습 곡선 로깅 (매 1000판 승률)
- [ ] **TICT-10**: 독립 F# solution (TicTacToe.sln)

### Phase 3: Connect Four (Q-Learning + Minimax)

- [ ] **CNCT-01**: 6×7 보드 및 게임 규칙 구현 (중력, 4연속 판정)
- [ ] **CNCT-02**: Minimax + Alpha-Beta Pruning AI 구현 (depth 6~8)
- [ ] **CNCT-03**: Q-Learning 에이전트 구현 (특징 추출 기반, Dictionary 백엔드)
- [ ] **CNCT-04**: Minimax AI vs Q-Learning AI 대결 및 성능 비교
- [ ] **CNCT-05**: 사람 vs AI 대전 (콘솔)
- [ ] **CNCT-06**: FsCheck — 중력 규칙, 4연속 판정 불변 조건 테스트
- [ ] **CNCT-07**: Expecto — Alpha-Beta pruning이 Minimax와 동일 결과 검증
- [ ] **CNCT-08**: Serilog — Q값 변화, 대전 결과 로깅
- [ ] **CNCT-09**: 독립 F# solution (ConnectFour.sln)

### Phase 4: DQN Connect Four

- [ ] **DQN-01**: TorchSharp 설정 및 기본 텐서 연산 검증
- [ ] **DQN-02**: 보드 → 3채널 텐서 변환 (내 돌/상대 돌/빈칸)
- [ ] **DQN-03**: DQN 모델 정의 (Conv2D + Dense → 7개 Q값)
- [ ] **DQN-04**: Experience Replay 버퍼 구현
- [ ] **DQN-05**: Target Network 주기적 동기화
- [ ] **DQN-06**: 학습 루프 구현 (자가 대국 + 학습)
- [ ] **DQN-07**: Phase 3 Minimax AI와 대결 → 승률 추적
- [ ] **DQN-08**: FsCheck — 텐서 변환 불변 조건 (채널 합 = 보드 크기)
- [ ] **DQN-09**: Expecto — 학습 후 Minimax(depth 4) 대비 승률 > 50% 검증
- [ ] **DQN-10**: Serilog — 손실 곡선, 승률 변화 로깅
- [ ] **DQN-11**: torch.NewDisposeScope() 필수 사용 (메모리 누수 방지)
- [ ] **DQN-12**: 독립 F# solution (DQN.sln)

### Phase 5: Gomoku (MCTS + Policy/Value Network)

- [ ] **GMOK-01**: 15×15 보드 및 오목 규칙 구현 (5연속 판정)
- [ ] **GMOK-02**: MCTS 기본 구현 (UCB1 selection, 랜덤 시뮬레이션)
- [ ] **GMOK-03**: Policy + Value Network 구현 (ResBlock 기반, TorchSharp)
- [ ] **GMOK-04**: PUCT 공식으로 MCTS + 신경망 통합
- [ ] **GMOK-05**: 자가 대국 학습 파이프라인 (MCTS로 한 판 → 데이터 수집 → 학습)
- [ ] **GMOK-06**: 학습된 모델 저장/불러오기
- [ ] **GMOK-07**: 사람 vs AI 대전 (콘솔, 난이도 = 시뮬레이션 횟수)
- [ ] **GMOK-08**: FsCheck — 5연속 판정, 합법 수 불변 조건 테스트
- [ ] **GMOK-09**: Expecto — MCTS backpropagation 관점 전환 정확성 검증
- [ ] **GMOK-10**: Expecto — 학습된 AI가 랜덤 상대 승률 > 80% 검증
- [ ] **GMOK-11**: Serilog — MCTS 탐색 통계, 학습 진행 로깅
- [ ] **GMOK-12**: 독립 F# solution (Gomoku.sln)

### Cross-Cutting

- [ ] **XCUT-01**: 모든 Phase에서 Option/Result 패턴 사용 (exception throw 금지)
- [ ] **XCUT-02**: 각 Phase 독립 solution — 상호 의존성 없음 (copy-and-evolve)
- [ ] **XCUT-03**: Pure game engine (Domain.fs + Rules.fs) / Impure shell (Training.fs + Program.fs) 분리

## v2 Requirements

### Console Visualization

- **VIS-01**: Spectre.Console로 학습 진행 프로그레스 바
- **VIS-02**: ASCII 학습 곡선 차트 (승률 변화 추이)

### Extended Algorithms

- **EXT-01**: Thompson Sampling (Phase 1 추가 전략)
- **EXT-02**: Double DQN (Phase 4 확장)
- **EXT-03**: Prioritized Experience Replay (Phase 4 확장)

### Web Frontend

- **WEB-01**: Fable + Feliz 오목 보드 UI
- **WEB-02**: Giraffe 백엔드 + SignalR 실시간 통신
- **WEB-03**: 사람 vs AI Web 대전

## Out of Scope

| Feature | Reason |
|---------|--------|
| F# 언어 기초 설명 | 저자가 F# 전문가, RL 개념에 집중 |
| GPU 학습 최적화 | 학습용이므로 CPU로 충분 |
| 렌주 룰 (금수 규칙) | 기본 오목 규칙만으로 충분 |
| OpenAI Gym 연동 | Python 의존성 불필요 |
| Mobile/Desktop GUI | 콘솔 앱만 |
| 모델 체크포인트 Git LFS | 학습용 프로젝트, 모델은 로컬 |

## Traceability

Note: TUTR-03, TUTR-04, TUTR-06 are established in Phase 1 and the pattern is carried forward into each subsequent phase as part of that phase's plan. TUTR-05 first applies in Phase 2 (first cross-phase connection). XCUT-01~03 are established in Phase 1 and enforced throughout.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TUTR-01 | Phase 1 | Pending |
| TUTR-02 | Phase 1 | Pending |
| TUTR-03 | Phase 1 | Pending |
| TUTR-04 | Phase 1 | Pending |
| TUTR-05 | Phase 2 | Pending |
| TUTR-06 | Phase 1 | Pending |
| BAND-01 | Phase 1 | Pending |
| BAND-02 | Phase 1 | Pending |
| BAND-03 | Phase 1 | Pending |
| BAND-04 | Phase 1 | Pending |
| BAND-05 | Phase 1 | Pending |
| BAND-06 | Phase 1 | Pending |
| BAND-07 | Phase 1 | Pending |
| BAND-08 | Phase 1 | Pending |
| BAND-09 | Phase 1 | Pending |
| BAND-10 | Phase 1 | Pending |
| TICT-01 | Phase 2 | Pending |
| TICT-02 | Phase 2 | Pending |
| TICT-03 | Phase 2 | Pending |
| TICT-04 | Phase 2 | Pending |
| TICT-05 | Phase 2 | Pending |
| TICT-06 | Phase 2 | Pending |
| TICT-07 | Phase 2 | Pending |
| TICT-08 | Phase 2 | Pending |
| TICT-09 | Phase 2 | Pending |
| TICT-10 | Phase 2 | Pending |
| CNCT-01 | Phase 3 | Pending |
| CNCT-02 | Phase 3 | Pending |
| CNCT-03 | Phase 3 | Pending |
| CNCT-04 | Phase 3 | Pending |
| CNCT-05 | Phase 3 | Pending |
| CNCT-06 | Phase 3 | Pending |
| CNCT-07 | Phase 3 | Pending |
| CNCT-08 | Phase 3 | Pending |
| CNCT-09 | Phase 3 | Pending |
| DQN-01 | Phase 4 | Pending |
| DQN-02 | Phase 4 | Pending |
| DQN-03 | Phase 4 | Pending |
| DQN-04 | Phase 4 | Pending |
| DQN-05 | Phase 4 | Pending |
| DQN-06 | Phase 4 | Pending |
| DQN-07 | Phase 4 | Pending |
| DQN-08 | Phase 4 | Pending |
| DQN-09 | Phase 4 | Pending |
| DQN-10 | Phase 4 | Pending |
| DQN-11 | Phase 4 | Pending |
| DQN-12 | Phase 4 | Pending |
| GMOK-01 | Phase 5 | Pending |
| GMOK-02 | Phase 5 | Pending |
| GMOK-03 | Phase 5 | Pending |
| GMOK-04 | Phase 5 | Pending |
| GMOK-05 | Phase 5 | Pending |
| GMOK-06 | Phase 5 | Pending |
| GMOK-07 | Phase 5 | Pending |
| GMOK-08 | Phase 5 | Pending |
| GMOK-09 | Phase 5 | Pending |
| GMOK-10 | Phase 5 | Pending |
| GMOK-11 | Phase 5 | Pending |
| GMOK-12 | Phase 5 | Pending |
| XCUT-01 | Phase 1 | Pending |
| XCUT-02 | Phase 1 | Pending |
| XCUT-03 | Phase 1 | Pending |

**Coverage:**
- v1 requirements: 56 total
- Mapped to phases: 56
- Unmapped: 0

---
*Requirements defined: 2026-02-19*
*Last updated: 2026-02-19 after roadmap creation — traceability clarified to single-phase per requirement*
