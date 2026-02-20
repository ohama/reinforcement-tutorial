# Requirements Archive: v1 F#으로 배우는 강화학습 Tutorial

**Archived:** 2026-02-20
**Status:** SHIPPED

This is the archived requirements specification for v1.
For current requirements, see `.planning/REQUIREMENTS.md` (created for next milestone).

---

# Requirements: F#으로 배우는 강화학습 Tutorial

**Defined:** 2026-02-19
**Core Value:** 각 Phase에서 RL 핵심 개념을 실제 동작하는 F# 코드로 구현하고, property-based test로 검증하며, tutorial 문서로 정리하는 것.

## v1 Requirements

### Tutorial Structure

- [x] **TUTR-01**: mdBook 프로젝트 초기화 (book.toml, src/SUMMARY.md)
- [x] **TUTR-02**: Phase별 chapter 구조 (01-bandit/ ~ 05-gomoku/)
- [x] **TUTR-03**: 각 chapter에 RL 개념 설명 + 핵심 F# 타입 정의 + 알고리즘 구현 포함
- [x] **TUTR-04**: `{{#include}}` 로 실제 소스 코드를 tutorial에 인클루드 (드리프트 방지)
- [x] **TUTR-05**: Phase 간 연결 설명 — 이전 Phase의 한계가 다음 Phase를 동기부여
- [x] **TUTR-06**: 한국어로 작성

### Phase 1: Multi-Armed Bandit

- [x] **BAND-01**: 슬롯머신 환경 구현 (N개 arm, 각각 다른 보상 확률)
- [x] **BAND-02**: ε-greedy 에이전트 구현
- [x] **BAND-03**: UCB1 에이전트 구현
- [x] **BAND-04**: 점진적 평균 업데이트 (incremental mean)
- [x] **BAND-05**: 여러 ε 값 비교 실험 (0.01, 0.1, 0.3)
- [x] **BAND-06**: ε-greedy vs UCB1 성능 비교 출력
- [x] **BAND-07**: FsCheck — 보상 합산 불변 조건 테스트
- [x] **BAND-08**: Expecto — 1000회 시행 후 최적 arm 수렴 검증
- [x] **BAND-09**: Serilog — 에피소드별 보상, 선택 arm 로깅
- [x] **BAND-10**: 독립 F# solution (Bandit.sln)

### Phase 2: Tic-Tac-Toe (TD Learning)

- [x] **TICT-01**: 3x3 보드 및 게임 규칙 구현 (승리 판정, 합법 수 목록)
- [x] **TICT-02**: 불변 GameState 타입 (Board + CurrentPlayer)
- [x] **TICT-03**: 랜덤 에이전트 구현
- [x] **TICT-04**: TD(0) Learning 에이전트 구현 (Value Table: Map<Board, float>)
- [x] **TICT-05**: 자가 대국 학습 루프 (10만 판)
- [x] **TICT-06**: 학습된 AI vs 사람 대전 (콘솔)
- [x] **TICT-07**: FsCheck — 보드 불변 조건 (빈칸 수 감소, 차례 교대 등)
- [x] **TICT-08**: Expecto — 10만 판 학습 후 랜덤 상대 승률 > 90% 검증
- [x] **TICT-09**: Serilog — 학습 곡선 로깅 (매 1000판 승률)
- [x] **TICT-10**: 독립 F# solution (TicTacToe.sln)

### Phase 3: Connect Four (Q-Learning + Minimax)

- [x] **CNCT-01**: 6x7 보드 및 게임 규칙 구현 (중력, 4연속 판정)
- [x] **CNCT-02**: Minimax + Alpha-Beta Pruning AI 구현 (depth 6~8)
- [x] **CNCT-03**: Q-Learning 에이전트 구현 (특징 추출 기반, Dictionary 백엔드)
- [x] **CNCT-04**: Minimax AI vs Q-Learning AI 대결 및 성능 비교
- [x] **CNCT-05**: 사람 vs AI 대전 (콘솔)
- [x] **CNCT-06**: FsCheck — 중력 규칙, 4연속 판정 불변 조건 테스트
- [x] **CNCT-07**: Expecto — Alpha-Beta pruning이 Minimax와 동일 결과 검증
- [x] **CNCT-08**: Serilog — Q값 변화, 대전 결과 로깅
- [x] **CNCT-09**: 독립 F# solution (ConnectFour.sln)

### Phase 4: DQN Connect Four

- [x] **DQN-01**: TorchSharp 설정 및 기본 텐서 연산 검증
- [x] **DQN-02**: 보드 → 3채널 텐서 변환 (내 돌/상대 돌/빈칸)
- [x] **DQN-03**: DQN 모델 정의 (Conv2D + Dense → 7개 Q값)
- [x] **DQN-04**: Experience Replay 버퍼 구현
- [x] **DQN-05**: Target Network 주기적 동기화
- [x] **DQN-06**: 학습 루프 구현 (자가 대국 + 학습)
- [x] **DQN-07**: Phase 3 Minimax AI와 대결 → 승률 추적
- [x] **DQN-08**: FsCheck — 텐서 변환 불변 조건 (채널 합 = 보드 크기)
- [x] **DQN-09**: Expecto — 학습 후 Minimax(depth 4) 대비 승률 > 50% 검증
- [x] **DQN-10**: Serilog — 손실 곡선, 승률 변화 로깅
- [x] **DQN-11**: torch.NewDisposeScope() 필수 사용 (메모리 누수 방지)
- [x] **DQN-12**: 독립 F# solution (DQN.sln)

### Phase 5: Gomoku (MCTS + Policy/Value Network)

- [x] **GMOK-01**: 15x15 보드 및 오목 규칙 구현 (5연속 판정)
- [x] **GMOK-02**: MCTS 기본 구현 (UCB1 selection, 랜덤 시뮬레이션)
- [x] **GMOK-03**: Policy + Value Network 구현 (ResBlock 기반, TorchSharp)
- [x] **GMOK-04**: PUCT 공식으로 MCTS + 신경망 통합
- [x] **GMOK-05**: 자가 대국 학습 파이프라인 (MCTS로 한 판 → 데이터 수집 → 학습)
- [x] **GMOK-06**: 학습된 모델 저장/불러오기
- [x] **GMOK-07**: 사람 vs AI 대전 (콘솔, 난이도 = 시뮬레이션 횟수)
- [x] **GMOK-08**: FsCheck — 5연속 판정, 합법 수 불변 조건 테스트
- [x] **GMOK-09**: Expecto — MCTS backpropagation 관점 전환 정확성 검증
- [x] **GMOK-10**: Expecto — 학습된 AI가 랜덤 상대 승률 > 80% 검증
- [x] **GMOK-11**: Serilog — MCTS 탐색 통계, 학습 진행 로깅
- [x] **GMOK-12**: 독립 F# solution (Gomoku.sln)

### Cross-Cutting

- [x] **XCUT-01**: 모든 Phase에서 Option/Result 패턴 사용 (exception throw 금지)
- [x] **XCUT-02**: 각 Phase 독립 solution — 상호 의존성 없음 (copy-and-evolve)
- [x] **XCUT-03**: Pure game engine (Domain.fs + Rules.fs) / Impure shell (Training.fs + Program.fs) 분리

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TUTR-01 | Phase 1 | Complete |
| TUTR-02 | Phase 1 | Complete |
| TUTR-03 | Phase 1 | Complete |
| TUTR-04 | Phase 1 | Complete |
| TUTR-05 | Phase 2 | Complete |
| TUTR-06 | Phase 1 | Complete |
| BAND-01~10 | Phase 1 | Complete |
| TICT-01~10 | Phase 2 | Complete |
| CNCT-01~09 | Phase 3 | Complete |
| DQN-01~12 | Phase 4 | Complete |
| GMOK-01~12 | Phase 5 | Complete |
| XCUT-01~03 | Phase 1 | Complete |

**Coverage:**
- v1 requirements: 56 total
- Shipped: 56
- Adjusted: 0
- Dropped: 0

---

## Milestone Summary

**Shipped:** 56 of 56 v1 requirements
**Adjusted:** None
**Dropped:** None

All requirements shipped as originally specified. No scope changes during implementation.

---
*Archived: 2026-02-20 as part of v1 milestone completion*
